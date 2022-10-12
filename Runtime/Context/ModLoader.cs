using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.UniMod
{
    /// <summary>
    /// Default implementation of the mod loader. It uses the default ModLoadingInfo implementation and allows for refreshing the loading info when desired.
    /// </summary>
    public sealed class ModLoader : IModLoader
    {
        public IReadOnlyCollection<IModLoadingInfo> AllLoadingInfo { get; }
        
        private readonly Dictionary<string, ModLoadingInfo> _loadingInfo;
        private readonly List<IModLoadingInfo> _allLoadingInfo;
        private readonly Dictionary<IMod, UniTaskCompletionSource<bool>> _loadingOperations;

        public ModLoader()
        {
            _loadingInfo = new Dictionary<string, ModLoadingInfo>();
            _allLoadingInfo = new List<IModLoadingInfo>();
            _loadingOperations = new Dictionary<IMod, UniTaskCompletionSource<bool>>();
            AllLoadingInfo = _allLoadingInfo.AsReadOnly();
        }
        
        public void SetMods(IEnumerable<IMod> mods)
        {
            _loadingInfo.Clear();
            _allLoadingInfo.Clear();
            ModLoadingInfo.ResolveModLoadingInformation(mods, _loadingInfo);
            
            foreach (ModLoadingInfo loadingInfo in _loadingInfo.Values)
                _allLoadingInfo.Add(loadingInfo);
        }

        public IModLoadingInfo GetLoadingInfo(IMod mod)
        {
            return GetLoadingInfo(mod?.Info.Id);
        }

        public IModLoadingInfo GetLoadingInfo(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                return null;
            
            return _loadingInfo.TryGetValue(modId, out ModLoadingInfo loadingInfo) ? loadingInfo : null;
        }

        public UniTask<bool> TryLoadAllModsAsync()
        {
            return WhenAll(_allLoadingInfo.Select(TryLoadModAndDependenciesAsync));
        }

        public UniTask<bool> TryLoadModsAndDependenciesAsync(params IMod[] mods)
        {
            if (mods is null)
                return UniTask.FromResult(false);
            
            return WhenAll(mods.Select(TryLoadModAndDependenciesAsync));
        }

        public UniTask<bool> TryLoadModsAndDependenciesAsync(IEnumerable<IMod> mods)
        {
            if (mods is null)
                return UniTask.FromResult(false);
            
            return WhenAll(mods.Select(TryLoadModAndDependenciesAsync));
        }

        public UniTask<bool> TryLoadModsAndDependenciesAsync(params string[] modIds)
        {
            if (modIds is null)
                return UniTask.FromResult(false);
            
            return WhenAll(modIds.Select(TryLoadModAndDependenciesAsync));
        }

        public UniTask<bool> TryLoadModsAndDependenciesAsync(IEnumerable<string> modIds)
        {
            if (modIds is null)
                return UniTask.FromResult(false);
            
            return WhenAll(modIds.Select(TryLoadModAndDependenciesAsync));
        }

        public UniTask<bool> TryLoadModAndDependenciesAsync(IMod mod)
        {
            return TryLoadModAndDependenciesAsync(mod?.Info.Id);
        }

        public UniTask<bool> TryLoadModAndDependenciesAsync(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                return UniTask.FromResult(false);
            if (_loadingInfo.TryGetValue(modId, out ModLoadingInfo loadingInfo))
                return TryLoadModAndDependenciesAsync(loadingInfo);
            
            return UniTask.FromResult(false);
        }
        
        private async UniTask<bool> TryLoadModAndDependenciesAsync(IModLoadingInfo loadingInfo)
        {
            // try to load all mod dependencies first
            if (loadingInfo.CanBeLoaded)
                return await WhenAll(loadingInfo.Dependencies.Select(TryLoadModAndDependenciesAsync));
            
            // try to load the mod
            IMod mod = loadingInfo.Mod;
            if (mod.IsLoaded)
                return true;
            
            // ideally all IMod implementations should properly handle multiple concurrent calls to LoadAsync, but just in case lets handle that here too
            if (_loadingOperations.TryGetValue(mod, out UniTaskCompletionSource<bool> operation))
                return await operation.Task;
            
            operation = new UniTaskCompletionSource<bool>();

            try
            {
                await mod.LoadAsync();
                operation.TrySetResult(true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ModLoader] Failed to load mod {mod.Info.Id}\n{exception}");
                operation.TrySetResult(false);
                return false;
            }
        }

        private async UniTask<bool> WhenAll(IEnumerable<UniTask<bool>> tasks)
        {
            bool[] results = await UniTask.WhenAll(tasks);
            return results.All(result => result);
        }
    }
}