using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    internal static class UniTaskUtility
    {
        /// <summary>
        /// Enhanced version of UniTask.WhenAll which will always wait for all tasks and throw all exceptions at the end inside an
        /// AggregateException or as the single thrown exception if only one task threw.
        /// </summary>
        public static async UniTask WhenAll(IEnumerable<UniTask> tasks)
        {
            using var _ = StaticPool<WhenAllAwaiter>.Get(out var awaiter);
            await awaiter.WaitAndThrowAll(tasks);
        }

        /// <summary>
        /// Same as UniTask.WhenAll but it will catch all exceptions and throw them as an AggregateException. If only one exception
        /// is thrown then it will be thrown again without wrapping it inside an AggregateException. Exceptions will be thrown after
        /// all tasks have finished.
        /// </summary>
        /// <typeparam name="T">UniTask return type</typeparam>
        public static async UniTask<T[]> WhenAll<T>(IEnumerable<UniTask<T>> tasks)
        {
            using var _ = StaticPool<WhenAllAwaiter<T>>.Get(out var awaiter);
            return await awaiter.WhenAll(tasks);
        }

        /// <summary>
        /// Enhanced version of UniTask.WhenAll which will always return the succeeded results (with default values for the threw task slots).
        /// Any thrown exceptions will be returned inside an AggregateException or as the thrown exception itself is only one task threw.
        /// </summary>
        /// <typeparam name="T">UniTask return type</typeparam>
        /// <returns>The results of the tasks and any thrown exceptions</returns>
        public static async UniTask<(T[] result, Exception exception)> WhenAllNoThrow<T>(IEnumerable<UniTask<T>> tasks)
        {
            using var _ = StaticPool<WhenAllAwaiter<T>>.Get(out var awaiter);
            return await awaiter.WhenAllWithResult(tasks);
        }
    }
}