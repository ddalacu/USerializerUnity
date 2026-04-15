using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
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
                        new PrimitivesSerializerProvider(),
                        new CustomSerializerProvider(logger),
                        new EnumSerializer(),
                        new ArraySerializer(),
                        new ListSerializer(),
                        
                        new TupleSerializationProvider(),
                        new KeyValuePairSerializationProvider(),
                        new DictionarySerializerProvider(),
                        
                        new CallbackClassSerializationProvider(),
                        new ClassSerializationProvider(),
                        new StructSerializationProvider()
                    };

                    _serializer = new USerializer(new UnitySerializationPolicy(), providers,
                        new DataTypesDatabase(),
                        logger, new UnityRuntimeUtils());
                }

                return _serializer;
            }
        }

        public static void Serialize<T>(this USerializer serializer, Stream stream, ref T obj, object context = null)
        {
            var output = new SerializerOutput(2048, ArrayPool<byte>.Shared);
            output.Context = context;
            Serialize(serializer, ref obj, ref output);
            output.Flush(stream);
            output.Dispose();
        }

        public static void Serialize<T>(this USerializer serializer, ref T obj, ref SerializerOutput output)
        {
            if (serializer.TryGetDataSerializer(typeof(T), out var dataSerializer) == false)
                throw new Exception($"Failed to get serializer for {typeof(T)}, returning default!");

            dataSerializer.Serialize(ref obj, ref output);
        }

        public static void Deserialize<T>(this USerializer serializer, Stream stream, ref T output,
            object context = null)
            where T : class
        {
            var input = new SerializerInput(2048, stream, ArrayPool<byte>.Shared);
            input.Context = context;
            Deserialize(serializer, input, ref output);
            input.FinishRead();
            input.Dispose();
        }

        public static void Deserialize<T>(this USerializer serializer, SerializerInput input, ref T output)
            where T : class
        {
            if (serializer.TryGetDataSerializer(typeof(T), out var dataSerializer) == false)
                throw new Exception($"Failed to get serializer for {typeof(T)}, returning default!");

            dataSerializer.Deserialize(ref output, ref input);
        }
    }
}