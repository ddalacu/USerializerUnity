using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace USerialization.Unity
{
    public class CallbackClassSerializationProvider : ISerializationProvider
    {
        public bool TryGet(USerializer serializer, Type type, out DataSerializer serializationMethods)
        {
            serializationMethods = default;

            if (type.IsArray)
                return false;

            if (type.IsValueType)
                return false;

            if (type.IsPrimitive)
                return false;

            if (typeof(ISerializationCallbackReceiver).IsAssignableFrom(type) == false)
            {
                serializationMethods = default;
                return false;
            }

            if (serializer.DataTypesDatabase.TryGet(out ObjectDataTypeLogic objectDataTypeLogic) == false)
                return false;

            if (serializer.SerializationPolicy.ShouldSerialize(type) == false)
                return false;

            serializationMethods = new ClassDataSerializer(type, objectDataTypeLogic.Value);

            return true;
        }

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public sealed unsafe class ClassDataSerializer : DataSerializer
        {
            private TypeInstantiator _instantiator;

            private FieldsSerializer _fieldsSerializer;

            private readonly DataType _dataType;

            public override DataType GetDataType() => _dataType;

            protected override void Initialize(USerializer serializer)
            {
                var (metas, serializationDatas) = FieldSerializationData.GetFields(_instantiator.Type, serializer);

                _fieldsSerializer = new FieldsSerializer(metas, serializationDatas, serializer.DataTypesDatabase);
            }

            public ClassDataSerializer(Type type, DataType objectDataType)
            {
                if (type == null)
                    throw new ArgumentNullException(nameof(type));

                if (type.IsValueType)
                    throw new ArgumentException(nameof(type));

                _instantiator = new TypeInstantiator(type);
                _dataType = objectDataType;
            }

            private int _stack;

            private const int MaxStack = 32;

            public override void Write(void* fieldAddress, SerializerOutput output, object context)
            {
                var obj = Unsafe.Read<object>(fieldAddress);

                if (obj == null)
                {
                    output.WriteNull();
                    return;
                }

                if (_stack >= MaxStack)
                    throw new CircularReferenceException("Circular references are not suported!");

                _stack++;

                Unsafe.As<ISerializationCallbackReceiver>(obj).OnBeforeSerialize();

                var track = output.BeginSizeTrack();

                var pinnable = Unsafe.As<object, PinnableObject>(ref obj);

                fixed (byte* objectAddress = &pinnable.Pinnable)
                {
                    _fieldsSerializer.Write(objectAddress, output, context);
                }

                output.WriteSizeTrack(track);

                _stack--;
            }

            public override void Read(void* fieldAddress, SerializerInput input, object context)
            {
                ref var instance = ref Unsafe.AsRef<object>(fieldAddress);

                if (input.BeginReadSize(out var end))
                {
                    if (instance == null)
                        instance = _instantiator.CreateInstance();

                    var pinnable = Unsafe.As<object, PinnableObject>(ref instance);
                    fixed (byte* objectAddress = &pinnable.Pinnable)
                    {
                        _fieldsSerializer.Read(objectAddress, input, context);
                    }

                    input.EndObject(end);

                    Unsafe.As<ISerializationCallbackReceiver>(instance).OnAfterDeserialize();
                }
                else
                {
                    instance = null;
                }
            }
        }
    }
}