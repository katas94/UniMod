using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Can group multiple mod sources in one.
    /// </summary>
    public class ModSourceGroup : IModSource
    {
        private readonly (IModSource source, HashSet<string> ids)[] _sources;

        public ModSourceGroup(IEnumerable<IModSource> sources)
        {
            IModSource[] validSources = sources.Where(source => source is not null).ToArray();
            _sources = new (IModSource, HashSet<string>)[validSources.Length];
            
            for (int i = 0; i < _sources.Length; ++i)
                _sources[i] = (validSources[i], new HashSet<string>());
        }

        public async UniTask FetchAsync()
        {
            HashSet<string> allIds = HashSetPool<string>.Pick();

            try
            {
                await UniTaskUtility.WhenAll(_sources.Select(async instance =>
                {
                    instance.ids.Clear();
                    
                    await instance.source.FetchAsync();
                    await instance.source.GetAllIdsAsync(instance.ids);
                    
                    // make it so there are no duplicate ids between sources (first sources will have priority)
                    instance.ids.ExceptWith(allIds);
                    allIds.UnionWith(instance.ids);
                }));
            }
            finally
            {
                HashSetPool<string>.Release(allIds);
            }
        }

        public UniTask GetAllIdsAsync(ICollection<string> results)
        {
            foreach ((_, HashSet<string> ids) in _sources)
                foreach (string id in ids)
                    results.Add(id);
            
            return UniTask.CompletedTask;
        }

        public UniTask<IMod> GetModAsync(string modId)
        {
            foreach ((IModSource source, HashSet<string> ids) in _sources)
                if (ids.Contains(modId))
                    return source.GetModAsync(modId);
            
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
            return UniTaskUtility.WhenAll(_sources.Select(
                instance => instance.source.GetModsAsync(instance.ids, results)
            ));
        }
    }
}