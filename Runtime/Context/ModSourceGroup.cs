using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Mod source group implementation that also implements a mod source so it can be handled as a single mod source.
    /// </summary>
    public sealed class ModSourceGroup : IModSource, IModSourceGroup
    {
        public IReadOnlyList<IModSource> Sources { get; }
        
        private readonly List<IModSource> _sources;
        private readonly List<ModSourceEntry> _entries;
        private readonly AsyncMethodController _fetchController;

        public ModSourceGroup()
        {
            _sources = new List<IModSource>();
            _entries = new List<ModSourceEntry>();
            Sources = _sources.AsReadOnly();
            _fetchController = new AsyncMethodController();
        }

        public ModSourceGroup(params IModSource[] sources)
            : this(sources as IEnumerable<IModSource>) { }

        public ModSourceGroup(IEnumerable<IModSource> sources)
        {
            IModSource[] validSources = sources.Where(source => source is not null).ToArray();
            _sources = new List<IModSource>(validSources.Length);
            _entries = new List<ModSourceEntry>(validSources.Length);
            Sources = _sources.AsReadOnly();
            _fetchController = new AsyncMethodController();

            for (int i = 0; i < validSources.Length; ++i)
            {
                _sources.Add(validSources[i]);
                _entries.Add(new ModSourceEntry(validSources[i]));
            }
        }

        public bool AddSource(IModSource source)
        {
            if (source is null || _sources.Contains(source))
                return false;
            
            _sources.Add(source);
            _entries.Add(new ModSourceEntry(source));
            return true;
        }

        public bool AddSource(IModSource source, int insertAtIndex)
        {
            if (insertAtIndex >= _sources.Count)
                return AddSource(source);
            if (source is null || _sources.Contains(source))
                return false;
            
            if (insertAtIndex < 0)
                insertAtIndex = 0;
            
            _sources.Insert(insertAtIndex, source);
            _entries.Insert(insertAtIndex, new ModSourceEntry(source));
            return true;
        }

        public void AddSources(IEnumerable<IModSource> sources)
        {
            foreach (IModSource source in sources)
                AddSource(source);
        }
        
        public void AddSources(IEnumerable<IModSource> sources, int insertAtIndex)
        {
            if (insertAtIndex < 0)
                insertAtIndex = 0;
            
            foreach (IModSource source in sources)
                if (AddSource(source, insertAtIndex))
                    ++insertAtIndex;
        }

        public bool RemoveSource(IModSource source)
        {
             int index = _sources.IndexOf(source);
             if (index < 0 || index >= _sources.Count)
                 return false;
             
             RemoveSourceAt(index);
             return true;
        }
        
        public void RemoveSourceAt(int index)
        {
            if (index < 0 || index >= _sources.Count)
                return;
             
            _sources.RemoveAt(index);
            _entries.RemoveAt(index);
        }

        public void RemoveSources(IEnumerable<IModSource> sources)
        {
            foreach (IModSource source in sources)
                RemoveSource(source);
        }

        public void ClearSources()
        {
            _fetchController.Cancel();
            _sources.Clear();
            _entries.Clear();
        }

        public async UniTask FetchAsync()
        {
            CancellationToken cancellationToken = _fetchController.Invoke();
            var allIds = HashSetPool<string>.Get();

            try
            {
                await UniTaskUtility.WhenAll(_entries.Select(async entry =>
                {
                    entry.Ids.Clear();
                    
                    await entry.Source.FetchAsync();
                    cancellationToken.ThrowIfCancellationRequested();
                    await entry.Source.GetAllIdsAsync(entry.Ids);
                    cancellationToken.ThrowIfCancellationRequested();
                }));
                
                cancellationToken.ThrowIfCancellationRequested();

                foreach (ModSourceEntry entry in _entries)
                {
                    // make it so there are no duplicate ids between sources (first sources will have priority)
                    entry.Ids.ExceptWith(allIds);
                    allIds.UnionWith(entry.Ids);
                }
            }
            finally
            {
                HashSetPool<string>.Release(allIds);
                _fetchController.Finish();
            }
        }

        public UniTask GetAllIdsAsync(ICollection<string> results)
        {
            foreach (ModSourceEntry entry in _entries)
                foreach (string id in entry.Ids)
                    results.Add(id);
            
            return UniTask.CompletedTask;
        }

        public UniTask<IModLoader> GetLoaderAsync(string id)
        {
            foreach (ModSourceEntry entry in _entries)
                if (entry.Ids.Contains(id))
                    return entry.Source.GetLoaderAsync(id);
            
            throw new Exception($"Couldn't find loader for ID {id}");
        }

        public async UniTask GetLoadersAsync(IEnumerable<string> ids, ICollection<IModLoader> results)
        {
            (IModLoader[] loaders, Exception exception) = await UniTaskUtility.WhenAllNoThrow(ids.Select(GetLoaderAsync));
            
            foreach (IModLoader loader in loaders)
                if (loader is not null)
                    results.Add(loader);
            
            if (exception is not null)
                throw exception;
        }

        public UniTask GetAllLoadersAsync(ICollection<IModLoader> results)
        {
            return UniTaskUtility.WhenAll(_entries.Select(
                entry => entry.Source.GetLoadersAsync(entry.Ids, results)
            ));
        }

        private struct ModSourceEntry
        {
            public IModSource Source;
            public HashSet<string> Ids;

            public ModSourceEntry(IModSource source)
            {
                Source = source;
                Ids = new HashSet<string>();
            }
        }
    }
}