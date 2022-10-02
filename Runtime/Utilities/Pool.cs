using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    internal sealed class Pool<T>
        where T : new()
    {
        private readonly Stack<T> _pool = new();
    
        public T Pick()
        {
            return _pool.Count > 0 ? _pool.Pop() : new T();
        }

        public void Release(T instance)
        {
            if (instance is not null)
                _pool.Push(instance);
        }

        public void Release(IEnumerable<T> instances)
        {
            foreach (T instance in instances)
                Release(instance);
        }

        public void Clear()
        {
            foreach (T instance in _pool)
                if (instance is IDisposable disposable)
                    disposable.Dispose();
        
            _pool.Clear();
        }
    }
}