using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    /// <summary>
    /// Mod implementation for mods installed locally.
    /// </summary>
    public sealed class LocalMod : IMod
    {
        public readonly string ModFolder;

        public IModContext Context { get; }
        public ModInfo Info { get; }
        public ModStatus Status { get; }
        public bool IsLoaded { get; private set; }
        public IResourceLocator ResourceLocator { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies;
        
        private readonly List<Assembly> _loadedAssemblies = new();
        private UniTaskCompletionSource _loadOperation;

        public LocalMod(IModContext context, string modFolder, ModInfo info)
        {
            Context = context;
            ModFolder = modFolder;
            Info = info;
        }

        public async UniTask LoadAsync()
        {
            if (_loadOperation != null)
            {
                await _loadOperation.Task;
                return;
            }
            
            _loadOperation = new UniTaskCompletionSource();

            try
            {
                await InternalLoadAsync();
                _loadOperation.TrySetResult();
            }
            catch (Exception exception)
            {
                _loadOperation.TrySetException(exception);
                throw;
            }
        }

        public UniTask<Sprite> LoadThumbnailAsync()
        {
            throw new NotImplementedException();
        }

        private async UniTask InternalLoadAsync()
        {
            if (IsLoaded)
                return;
            
            // check mod's platform
            if (!LocalModUtility.IsPlatformSupported(Info.Platform))
                throw new Exception($"[{Info.ModId}] this mod was built for {Info.Platform} platform");
            
            // check if the mod was built for this version of the app
            if (string.IsNullOrEmpty(Info.AppVersion))
                Debug.LogWarning($"[{Info.ModId}] could not get the app version that this mod was built for. The mod is not guaranteed to work and the application could crash or be unstable");
            if (Info.AppVersion != Application.version)
                Debug.LogWarning($"[{Info.ModId}] this mod was built for app version {Info.AppVersion}, so it is not guaranteed to work and the application could crash or be unstable");
            
            // load assemblies
            if (Info.Type is ModType.ContentAndAssemblies or ModType.Assemblies)
            {
                string assembliesFolder = Path.Combine(ModFolder, UniModSpecification.AssembliesFolder);
                await LocalModUtility.LoadAssembliesAsync(assembliesFolder, _loadedAssemblies);
            }
            
            // load content
            if (Info.Type is ModType.ContentAndAssemblies or ModType.Content)
            {
                // load mod content catalog
                string catalogPath = Path.Combine(ModFolder, UniModSpecification.CatalogFileName);
                if (!File.Exists(catalogPath))
                    throw new Exception($"Mod's catalogue doesn't exist: {catalogPath}");
                
                ResourceLocator = await Addressables.LoadContentCatalogAsync(catalogPath, true);
            }

            // run startup script and methods
            await LocalModUtility.RunStartupObjectFromContentAsync(this);
            await LocalModUtility.RunStartupMethodsFromAssembliesAsync(this);
            
            IsLoaded = true;
            Debug.Log($"[UniMod] {Info.ModId} loaded!");
        }
    }
}