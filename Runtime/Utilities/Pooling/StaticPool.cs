using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    /// <summary>
    /// Offers static access to thread-local pools for objects that have parameterless constructors.
    /// </summary>
    internal static class StaticPool<T>
        where T : class, new()
    {
        [ThreadStatic] private static Pool<T> _instance;
        
        public static Pool<T> Instance => _instance ??= CreatePool();

        public static int TotalCount => Instance.TotalCount;
        public static int PooledCount => Instance.PooledCount;
        public static int ActiveCount => Instance.ActiveCount;
        
        public static Pool<T> CreatePool()
            => new(CreateNew);
        
        public static T Get()
            => Instance.Get();
        
        public static void Release(T obj)
            => Instance.Release(obj);

        public static Pool<T>.Handle Get(out T obj)
            => Instance.Get(out obj);

        public static IEnumerable<T> Get(int count)
            => Instance.Get(count);

        public static void Get(int count, ICollection<T> collection)
            => Instance.Get(count, collection);

        public static void Release(IEnumerable<T> objects)
            => Instance.Release(objects);

        public static void Clear()
            => Instance.Clear();
        
        private static T CreateNew()
            => new();
    }
}