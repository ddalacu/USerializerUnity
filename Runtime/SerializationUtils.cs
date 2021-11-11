using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace USerialization.Unity
{
    public static class SerializationUtils
    {
        private static USerializer _serializer;

        public static IntPtr GetMethodPointer(MethodInfo info)
        {
            if (Application.isEditor)
                return info.MethodHandle.GetFunctionPointer();

#if ENABLE_IL2CPP
            return info.MethodHandle.Value;
#else
            return info.MethodHandle.GetFunctionPointer();
#endif
        }

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
                        logger)
                    {
                        GetFunctionPointer = GetMethodPointer
                    };
                }

                return _serializer;
            }
        }
        
        public static readonly ObjectPool<SerializerOutput> OutputPool =
            new ObjectPool<SerializerOutput>(() => new SerializerOutput(2048 * 4), (output) =>
            {
                if (output.Stream == null) 
                    return;
                output.Flush();
                output.SetStream(null);
            });

        public static readonly ObjectPool<SerializerInput> InputPool =
            new ObjectPool<SerializerInput>(() => new SerializerInput(2048 * 4), (input) =>
            {
                input.FinishRead();
                input.SetStream(null);
            });

        public static void Serialize(this USerializer serializer, Stream stream, object obj)
        {
            using (OutputPool.Get(out var output))
            {
                output.SetStream(stream);
                
                var success = serializer.Serialize(output, obj);
                if (success == false)
                    throw new Exception($"Failed to serialize {obj}, returning default!");
            }
        }

        public static void PopulateObject<T>(this USerializer serializer, Stream stream, ref T data)
        {
            using (InputPool.Get(out var input))
            {
                input.SetStream(stream);
                
                var success = serializer.TryDeserialize(input, ref data);
                if (success == false)
                    throw new Exception($"Failed to serialize {typeof(T)}, returning default!");
            }
        }

        public static void PopulateObject<T>(this USerializer serializer, Stream stream, T data) where T : class
        {
            if (data == null)
                throw new Exception("LoadFromStream called with null object!");

            using (InputPool.Get(out var input))
            {
                input.SetStream(stream);
                
                var success = serializer.TryDeserialize(input, ref data);

                if (success == false)
                    throw new Exception($"Failed to serialize {typeof(T)}, returning default!");
            }
        }

        public static T Deserialize<T>(this USerializer serializer, Stream stream)
        {
            using (InputPool.Get(out var input))
            {
                input.SetStream(stream);
                T data = default;
                var success = serializer.TryDeserialize(input, ref data);

                if (success == false)
                    throw new Exception($"Failed to serialize {typeof(T)}, returning default!");
                return data;
            }
        }
    }
}