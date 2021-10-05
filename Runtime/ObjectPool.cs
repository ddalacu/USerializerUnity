using System;
using System.Collections.Concurrent;

namespace USerialization.Unity
{
    public class ObjectPool<T>
    {
        private ConcurrentBag<T> _objects;
        private Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null)
                throw new ArgumentNullException("objectGenerator");
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            T item;
            if (_objects.TryTake(out item))
                return item;
            return _objectGenerator();
        }

        public void PutObject(T item)
        {
            _objects.Add(item);
        }

        public PooledObjectHandle<T> Get(out T item)
        {
            item = GetObject();
            return new PooledObjectHandle<T>(this, item);
        }
    }

    public struct PooledObjectHandle<T> : IDisposable
    {
        private ObjectPool<T> _pool;
        private T _item;

        public PooledObjectHandle(ObjectPool<T> pool, T item)
        {
            _pool = pool;
            _item = item;
        }

        public void Dispose()
        {
            if (_pool == null)
                return;
            _pool.PutObject(_item);
            _pool = null;
            _item = default;
        }
    }
}