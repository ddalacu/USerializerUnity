using System;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace USerialization.Unity
{
    public static class SerializationUtils
    {
        private static USerializer _serializer;

        public static USerializer Instance
        {
            get
            {
                if (_serializer == null)
                {
                    var logger = new UnityDebugLogger();

                    ISerializationProvider[] providers =
                    {
                        new CustomSerializerProvider(logger),
                        new EnumSerializer(),
                        new CallbackClassSerializationProvider(),
                        new ClassSerializationProvider(),
                        new StructSerializationProvider(),
                        new ArraySerializer(),
                        new ListSerializer()
                    };

                    _serializer = new USerializer(new UnitySerializationPolicy(), providers, new DataTypesDatabase(),
                        logger);
                }

                return _serializer;
            }
        }


        public static void Serialize(this USerializer serializer, Stream stream, object obj)
        {
            using (SerializationPools.GetOutput(stream, out var output))
            {
                Serialize(serializer, obj, output);
            }
        }

        public static void Serialize(this USerializer serializer, [NotNull] object obj, SerializerOutput output)
        {
            if (serializer.TryGetClassHelper(out var helper, obj.GetType()) == false)
                throw new Exception($"Failed to get serializer for {obj}, returning default!");

            helper.SerializeObject(obj, output, null);
        }

        public static void Populate<T>(this USerializer serializer, Stream stream, ref T data) where T : class
        {
            using (SerializationPools.GetInput(stream, out var input))
            {
                Populate(serializer, ref data, input);
            }
        }

        public static void Populate<T>(this USerializer serializer, ref T data, SerializerInput input)
            where T : class
        {
            ClassSerializationHelper helper;

            if (data != null)
            {
                if (serializer.TryGetClassHelper(out helper, data.GetType()) == false)
                    throw new Exception($"Failed to get serializer for {data}, returning default!");

                helper.PopulateObject(data, input, null);
            }
            else
            {
                if (serializer.TryGetClassHelper(out helper, typeof(T)) == false)
                    throw new Exception($"Failed to get serializer for {data}, returning default!");

                data = (T) helper.DeserializeObject(input, null);
            }
        }

        public static T Deserialize<T>(this USerializer serializer, Stream stream) where T : class
        {
            using (SerializationPools.GetInput(stream, out var input))
            {
                return Deserialize<T>(serializer, input);
            }
        }

        public static T Deserialize<T>(this USerializer serializer, SerializerInput input) where T : class
        {
            if (serializer.TryGetClassHelper(out var helper, typeof(T)) == false)
                throw new Exception($"Failed to get serializer for {typeof(T)}, returning default!");

            return (T) helper.DeserializeObject(input, null);
        }
    }
}