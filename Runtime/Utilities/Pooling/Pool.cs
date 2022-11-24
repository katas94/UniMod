using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    /// <summary>
    /// Generic object pool.
    /// </summary>
    internal sealed class Pool<T>
        where T : class
    {
        private static readonly Action<T> NoOp = delegate { };
        
        public int TotalCount { get; private set; }
        public int PooledCount => _stack.Count;
        public int ActiveCount => TotalCount - _stack.Count;
        
        private readonly Stack<T> _stack;
        private readonly Func<T> _createNew;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        
        public Pool(Func<T> createNew)
            : this(createNew, null, null) { }
        
        public Pool(Func<T> createNew, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _stack = new Stack<T>();
            _createNew = createNew ?? throw new NullReferenceException("Pool cannot be instantiated with a null createNew delegate");
            _onGet = onGet ?? NoOp;
            _onRelease = onRelease ?? NoOp;
        }

        public T Get()
        {
            T obj;
        
            if (_stack.Count == 0)
            {
                obj = _createNew();
                ++TotalCount;
            }
            else
            {
                obj = _stack.Pop();
            }
            
            _onGet(obj);
            return obj;
        }
        
        public void Release(T obj)
        {
            if (obj is null)
                return;
            
            _onRelease(obj);
            _stack.Push(obj);
        }

        public Handle Get(out T obj)
        {
            obj = Get();
            return new Handle(obj, this);
        }

        public IEnumerable<T> Get(int count)
        {
            for (int i = 0; i < count; ++i)
                yield return Get();
        }

        public void Get(int count, ICollection<T> collection)
        {
            if (collection is null)
                return;

            for (int i = 0; i < count; ++i)
                collection.Add(Get());
        }

        public void Release(IEnumerable<T> objects)
        {
            if (objects is null)
                return;

            IEnumerator<T> enumerator = objects.GetEnumerator();
            
            while (enumerator.MoveNext())
                Release(enumerator.Current);
            
            enumerator.Dispose();
        }

        public void Clear()
        {
            TotalCount -= _stack.Count;
            _stack.Clear();
        }
        
        public struct Handle : IDisposable
        {
            private readonly T _object;
            private readonly Pool<T> _pool;
        
            public Handle(T obj, Pool<T> pool)
            {
                _object = obj;
                _pool = pool;
            }
        
            public void Dispose()
            {
                _pool.Release(_object);
            }
        }
    }
}