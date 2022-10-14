using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.UniMod
{
    /// <summary>
    /// Default implementation of the mod loading context. It uses the default ModStatus implementation and provides a public method to rebuild the context.
    /// </summary>
    public sealed class ModLoadingContext : IModLoadingContext
    {
        public IReadOnlyCollection<IModStatus> Statuses { get; }

        private readonly IModContext _context;
        private readonly Dictionary<string, ModStatus> _statuses;
        private readonly List<IModStatus> _statusesList;
        private readonly Dictionary<IMod, UniTaskCompletionSource<bool>> _loadingOperations;

        public ModLoadingContext(IModContext context)
        {
            _context = context;
            _statuses = new Dictionary<string, ModStatus>();
            _statusesList = new List<IModStatus>();
            _loadingOperations = new Dictionary<IMod, UniTaskCompletionSource<bool>>();
            Statuses = _statusesList.AsReadOnly();
        }
        
        public void RebuildContext(IEnumerable<IMod> mods)
        {
            _statuses.Clear();
            _statusesList.Clear();
            ModStatus.ResolveStatuses(mods, _context.Application, _statuses);
            _statusesList.AddRange(_statuses.Values);
        }

        public IModStatus GetStatus(IMod mod)
        {
            return GetStatus(mod?.Info.Id);
        }

        public IModStatus GetStatus(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                return null;
            
            return _statuses.TryGetValue(modId, out ModStatus status) ? status : null;
        }

        public UniTask<bool> TryLoadAllModsAsync()
        {
            return WhenAll(_statusesList.Select(TryLoadModAsync));
        }

        public UniTask<bool> TryLoadModsAsync(params IMod[] mods)
        {
            return mods is null ? UniTask.FromResult(false) : WhenAll(mods.Select(TryLoadModAsync));
        }

        public UniTask<bool> TryLoadModsAsync(IEnumerable<IMod> mods)
        {
            return mods is null ? UniTask.FromResult(false) : WhenAll(mods.Select(TryLoadModAsync));
        }

        public UniTask<bool> TryLoadModsAsync(params string[] modIds)
        {
            return modIds is null ? UniTask.FromResult(false) : WhenAll(modIds.Select(TryLoadModAsync));
        }

        public UniTask<bool> TryLoadModsAsync(IEnumerable<string> modIds)
        {
            return modIds is null ? UniTask.FromResult(false) : WhenAll(modIds.Select(TryLoadModAsync));
        }

        public UniTask<bool> TryLoadModAsync(IMod mod)
        {
            return TryLoadModAsync(mod?.Info.Id);
        }

        public UniTask<bool> TryLoadModAsync(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                return UniTask.FromResult(false);
            if (_statuses.TryGetValue(modId, out ModStatus status))
                return TryLoadModAsync(status);
            
            return UniTask.FromResult(false);
        }
        
        private async UniTask<bool> TryLoadModAsync(IModStatus status)
        {
            if (!status.CanBeLoaded)
                return false;
            
            IMod mod = status.Mod;
            if (mod.IsLoaded)
                return true;
            
            // ideally all IMod implementations should properly handle multiple concurrent calls to LoadAsync, but just in case lets handle that here too
            if (_loadingOperations.TryGetValue(mod, out UniTaskCompletionSource<bool> operation))
                return await operation.Task;
            
            _loadingOperations[mod] = operation = new UniTaskCompletionSource<bool>();
            
            // try to load all mod dependencies first
            if (status.Dependencies.Count > 0)
            {
                bool didDependenciesLoad = await WhenAll(status.Dependencies.Select(TryLoadModAsync));

                if (!didDependenciesLoad)
                {
                    Debug.LogError($"[ModLoader] Failed to load mod {mod.Info.Id}: at least one dependency failed to load.");
                    operation.TrySetResult(false);
                    return false;
                }
            }
            
            // try to load the mod
            try
            {
                await mod.LoadAsync(_context);
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

        private static async UniTask<bool> WhenAll(IEnumerable<UniTask<bool>> tasks)
        {
            bool[] results = await UniTask.WhenAll(tasks);
            return results.All(result => result);
        }
    }
}