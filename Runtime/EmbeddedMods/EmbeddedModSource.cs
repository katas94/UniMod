using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.UniMod
{
    public sealed class EmbeddedModSource : MonoBehaviour, IModSource
    {
        [SerializeField] private List<EmbeddedModConfig> configs;
        
        private readonly Dictionary<string, EmbeddedModLoader> _loaders = new();
        private readonly Dictionary<string, EmbeddedModConfig> _configsById = new();

        public UniTask FetchAsync()
        {
            _configsById.Clear();
            
            foreach (EmbeddedModConfig config in configs)
                if (config)
                    _configsById[config.modId] = config;
            
            return UniTask.CompletedTask;
        }

        public UniTask GetAllIdsAsync(ICollection<string> results)
        {
            foreach (EmbeddedModConfig config in configs)
                if (config)
                    results.Add(config.modId);
            
            return UniTask.CompletedTask;
        }

        public UniTask<IModLoader> GetLoaderAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new Exception("Null or empty mod ID");
            if (!_configsById.TryGetValue(id, out EmbeddedModConfig config))
                throw new Exception($"Couldn't find loader for ID {id}");
            
            if (!_loaders.TryGetValue(id, out EmbeddedModLoader loader))
                _loaders[id] = loader = new EmbeddedModLoader(config);
            
            return UniTask.FromResult<IModLoader>(loader);
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
            return GetLoadersAsync(_configsById.Keys, results);
        }
    }
}