using System.IO;

namespace USerialization.Unity
{
    public class SerializationPools
    {
        public const int PoolSize = 2048 * 4;

        private static readonly ObjectPool<SerializerOutput> OutputPool =
            new ObjectPool<SerializerOutput>(() => new SerializerOutput(PoolSize), (output) =>
            {
                if (output.Stream == null)
                    return;
                output.Flush();
                output.SetStream(null);
            });

        private static readonly ObjectPool<SerializerInput> InputPool =
            new ObjectPool<SerializerInput>(() => new SerializerInput(PoolSize), (input) =>
            {
                input.FinishRead();
                input.SetStream(null);
            });

        public static PooledObjectHandle<SerializerOutput> GetOutput(Stream stream, out SerializerOutput item)
        {
            var objectHandle = OutputPool.Get(out item);
            item.SetStream(stream);
            return objectHandle;
        }

        public static PooledObjectHandle<SerializerInput> GetInput(Stream stream, out SerializerInput item)
        {
            var objectHandle = InputPool.Get(out item);
            item.SetStream(stream);
            return objectHandle;
        }
    }
}