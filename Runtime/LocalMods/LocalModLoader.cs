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
    /// Mod loader implementation for mods installed locally.
    /// </summary>
    public sealed class LocalModLoader : IModLoader
    {
        public readonly string ModFolder;

        public ModInfo Info { get; }
        public string Source { get; }
        public bool IsLoaded { get; private set; }
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public IResourceLocator ResourceLocator { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        private readonly string _assembliesFolder;
        private readonly string _catalogPath;
        private readonly List<Assembly> _loadedAssemblies;
        
        private UniTaskCompletionSource _loadOperation;

        public LocalModLoader(string modFolder, ModInfo info, string source = LocalModSource.SourceLabel)
        {
            ModFolder = modFolder;
            _assembliesFolder = Path.Combine(modFolder, UniMod.AssembliesFolder);
            _catalogPath = Path.Combine(modFolder, UniMod.AssetsFolder, UniMod.AddressablesCatalogFileName);
            _loadedAssemblies = new List<Assembly>();
            
            Info = info;
            Source = source;
            ContainsAssets = File.Exists(_catalogPath);
            ContainsAssemblies = Directory.Exists(_assembliesFolder);
            ResourceLocator = EmptyLocator.Instance;
            LoadedAssemblies = _loadedAssemblies.AsReadOnly();
        }

        public async UniTask LoadAsync(IUniModContext context, IMod mod)
        {
            if (_loadOperation != null)
            {
                await _loadOperation.Task;
                return;
            }
            
            _loadOperation = new UniTaskCompletionSource();

            try
            {
                await InternalLoadAsync(context, mod);
                _loadOperation.TrySetResult();
            }
            catch (Exception exception)
            {
                _loadOperation.TrySetException(exception);
                throw;
            }
        }

        public UniTask<Texture2D> LoadThumbnailAsync()
        {
            throw new NotImplementedException();
        }

        private async UniTask InternalLoadAsync(IUniModContext context, IMod mod)
        {
            if (IsLoaded)
                return;
            
            if (ContainsAssemblies)
                await UniModUtility.LoadAssembliesAsync(_assembliesFolder, _loadedAssemblies);
            
            if (ContainsAssets)
                ResourceLocator = await Addressables.LoadContentCatalogAsync(_catalogPath, true);

            // run startup script and methods
            await UniModUtility.RunModStartupFromAssetsAsync(context, mod);
            await UniModUtility.RunStartupMethodsFromAssembliesAsync(LoadedAssemblies, context, mod);
            
            IsLoaded = true;
        }
    }
}