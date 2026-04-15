using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

            if (serializer.SerializationPolicy.ShouldSerialize(type) == false)
                return false;

            var activator = ObjectActivator.GetActivator(type);

            serializationMethods = new CallbackClassDataSerializer(type,
                activator,
                (fieldInfo) => serializer.SerializationPolicy.ShouldSerialize(fieldInfo));

            return true;
        }

        public sealed class CallbackClassDataSerializer : ClassDataSerializer
        {
            public CallbackClassDataSerializer(Type type, Func<object> activator, Func<FieldInfo, bool> shouldSerialize)
                : base(type, activator, shouldSerialize)
            {
            }

            public override void Read(Span<byte> span, ref SerializerInput input)
            {
                base.Read(span, ref input);
                ref var instance =
                    ref Unsafe.As<byte, ISerializationCallbackReceiver>(ref MemoryMarshal.GetReference(span));
                instance?.OnAfterDeserialize();
            }

            public override void Write(ReadOnlySpan<byte> span, ref SerializerOutput output)
            {
                ref var instance =
                    ref Unsafe.As<byte, ISerializationCallbackReceiver>(ref MemoryMarshal.GetReference(span));
                instance?.OnBeforeSerialize();

                base.Write(span, ref output);
            }
        }
    }
}