using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Can group multiple mod sources in one.
    /// </summary>
    public class ModSourceGroup : IModSource, IModSourceGroup
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
            HashSet<string> allIds = HashSetPool<string>.Pick();

            try
            {
                await UniTaskUtility.WhenAll(_entries.Select(async entry =>
                {
                    entry.Ids.Clear();
                    
                    await entry.Source.FetchAsync();
                    cancellationToken.ThrowIfCancellationRequested();
                    await entry.Source.GetAllIdsAsync(entry.Ids);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // make it so there are no duplicate ids between sources (first sources will have priority)
                    entry.Ids.ExceptWith(allIds);
                    allIds.UnionWith(entry.Ids);
                }));
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

        public UniTask<IMod> GetModAsync(string modId)
        {
            foreach (ModSourceEntry entry in _entries)
                if (entry.Ids.Contains(modId))
                    return entry.Source.GetModAsync(modId);
            
            throw new Exception($"Couldn't find mod ID {modId}");
        }

        public async UniTask GetModsAsync(IEnumerable<string> modIds, ICollection<IMod> results)
        {
            (IMod[] mods, Exception exception) = await UniTaskUtility.WhenAllNoThrow(modIds.Select(GetModAsync));
            
            foreach (IMod mod in mods)
                if (mod is not null)
                    results.Add(mod);
            
            if (exception is not null)
                throw exception;
        }

        public UniTask GetAllModsAsync(ICollection<IMod> results)
        {
            return UniTaskUtility.WhenAll(_entries.Select(
                instance => instance.Source.GetModsAsync(instance.Ids, results)
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