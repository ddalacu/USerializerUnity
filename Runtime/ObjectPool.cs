namespace USerialization.Unity
{
    using System;
    using System.Collections.Generic;

    public class ObjectPool<T>
    {
        private readonly Stack<T> _objects;
        private readonly Func<T> _objectGenerator;
        private readonly Action<T> _cleanObject;

        public ObjectPool(Func<T> objectGenerator, Action<T> cleanObject = null)
        {
            if (objectGenerator == null)
                throw new ArgumentNullException(nameof(objectGenerator));

            _objects = new Stack<T>(32);
            _objectGenerator = objectGenerator;
            _cleanObject = cleanObject;
        }

        public T GetObject()
        {
            lock (_objects)
            {
                if (_objects.Count > 0)
                    return _objects.Pop();
            }

            return _objectGenerator();
        }

        public void PutObject(T item)
        {
            _cleanObject?.Invoke(item);

            lock (_objects)
            {
                _objects.Push(item);
            }
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