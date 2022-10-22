using System.Collections.Generic;

// This file contains static pool classes for every generic collection in the System.Collections.Generic namespace.

namespace Katas.UniMod
{
    /// <summary>
    /// Base class for static collection pools.
    /// </summary>
    internal class CollectionPool<TCollection, TItem> : StaticPool<TCollection>
        where TCollection : class, ICollection<TItem>, new()
    {
        public new static Pool<TCollection> Instance => _instance ??= CreatePool();
        
        public new static Pool<TCollection> CreatePool()
            => new(CreateNew, onRelease: collection => collection.Clear());
    }
    
    internal class ListPool<T>                        : CollectionPool<List<T>, T> { }
    internal class LinkedListPool<T>                  : CollectionPool<LinkedList<T>, T> { }
    internal class SortedListPool<TKey, TValue>       : CollectionPool<SortedList<TKey, TValue>, KeyValuePair<TKey, TValue>> { }
    internal class HashSetPool<T>                     : CollectionPool<HashSet<T>, T> { }
    internal class SortedSetPool<T>                   : CollectionPool<SortedSet<T>, T> { }
    internal class DictionaryPool<TKey, TValue>       : CollectionPool<Dictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> { }
    internal class SortedDictionaryPool<TKey, TValue> : CollectionPool<SortedDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>> { }
    
    // Stack and Queue collections don't inherit from any interface containing the Clear method. Microsoft should seriously consider in adding
    // the clear method to the ICollection non-generic interface.
    internal class StackPool<T> : StaticPool<Stack<T>>
    {
        public new static Pool<Stack<T>> Instance => _instance ??= CreatePool();
        
        public new static Pool<Stack<T>> CreatePool()
            => new(CreateNew, onRelease: stack => stack.Clear());
    }
    
    internal class QueuePool<T> : StaticPool<Queue<T>>
    {
        public new static Pool<Queue<T>> Instance => _instance ??= CreatePool();
        
        public new static Pool<Queue<T>> CreatePool()
            => new(CreateNew, onRelease: stack => stack.Clear());
    }
}