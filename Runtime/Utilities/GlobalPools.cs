using System.Collections.Generic;
using System.Threading;

namespace Katas.UniMod
{
    /// <summary>
    /// Thread-safe global pool of objects that can be instantiated with empty constructors.
    /// </summary>
    internal static class GlobalPool<T>
        where T : new()
    {
        private static readonly ThreadLocal<Pool<T>> Pool = new ThreadLocal<Pool<T>>(() => new Pool<T>());

        public static T Pick()
        {
            return Pool.Value.Pick();
        }

        public static void Release(T instance)
        {
            Pool.Value.Release(instance);
        }

        public static void Release(IEnumerable<T> instances)
        {
            Pool.Value.Release(instances);
        }

        public static void Clear()
        {
            Pool.Value.Clear();
        }
    }
    
    internal static class ListPool<T>
    {
        private static readonly ThreadLocal<Pool<List<T>>> Pool = new ThreadLocal<Pool<List<T>>>(() => new Pool<List<T>>());

        public static List<T> Pick()
        {
            List<T> collection = Pool.Value.Pick();
            return collection;
        }

        public static void Release(List<T> collection)
        {
            collection.Clear();
            Pool.Value.Release(collection);
        }

        public static void Release(IEnumerable<List<T>> collections)
        {
            foreach (List<T> collection in collections)
                Release(collection);
        }

        public static void Clear()
        {
            Pool.Value.Clear();
        }
    }
    
    internal static class HashSetPool<T>
    {
        private static readonly ThreadLocal<Pool<HashSet<T>>> Pool = new ThreadLocal<Pool<HashSet<T>>>(() => new Pool<HashSet<T>>());

        public static HashSet<T> Pick()
        {
            HashSet<T> collection = Pool.Value.Pick();
            return collection;
        }

        public static void Release(HashSet<T> collection)
        {
            collection.Clear();
            Pool.Value.Release(collection);
        }

        public static void Release(IEnumerable<HashSet<T>> collections)
        {
            foreach (HashSet<T> collection in collections)
                Release(collection);
        }

        public static void Clear()
        {
            Pool.Value.Clear();
        }
    }
    
    internal static class StackPool<T>
    {
        private static readonly ThreadLocal<Pool<Stack<T>>> Pool = new ThreadLocal<Pool<Stack<T>>>(() => new Pool<Stack<T>>());

        public static Stack<T> Pick()
        {
            Stack<T> collection = Pool.Value.Pick();
            return collection;
        }

        public static void Release(Stack<T> collection)
        {
            collection.Clear();
            Pool.Value.Release(collection);
        }

        public static void Release(IEnumerable<Stack<T>> collections)
        {
            foreach (Stack<T> collection in collections)
                Release(collection);
        }

        public static void Clear()
        {
            Pool.Value.Clear();
        }
    }
    
    internal static class QueuePool<T>
    {
        private static readonly ThreadLocal<Pool<Queue<T>>> Pool = new ThreadLocal<Pool<Queue<T>>>(() => new Pool<Queue<T>>());

        public static Queue<T> Pick()
        {
            Queue<T> collection = Pool.Value.Pick();
            return collection;
        }

        public static void Release(Queue<T> collection)
        {
            collection.Clear();
            Pool.Value.Release(collection);
        }

        public static void Release(IEnumerable<Queue<T>> collections)
        {
            foreach (Queue<T> collection in collections)
                Release(collection);
        }

        public static void Clear()
        {
            Pool.Value.Clear();
        }
    }
    
    internal static class DictionaryPool<TKey, TValue>
    {
        private static readonly ThreadLocal<Pool<Dictionary<TKey, TValue>>> Pool
            = new ThreadLocal<Pool<Dictionary<TKey, TValue>>>(() => new Pool<Dictionary<TKey, TValue>>());

        public static Dictionary<TKey, TValue> Pick()
        {
            Dictionary<TKey, TValue> collection = Pool.Value.Pick();
            return collection;
        }

        public static void Release(Dictionary<TKey, TValue> collection)
        {
            collection.Clear();
            Pool.Value.Release(collection);
        }

        public static void Release(IEnumerable<Dictionary<TKey, TValue>> collections)
        {
            foreach (Dictionary<TKey, TValue> collection in collections)
                Release(collection);
        }

        public static void Clear()
        {
            Pool.Value.Clear();
        }
    }
}