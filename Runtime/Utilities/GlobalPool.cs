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
}