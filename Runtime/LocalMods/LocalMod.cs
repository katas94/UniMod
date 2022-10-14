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

        public ModInfo Info { get; }
        public bool IsLoaded { get; private set; }
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public IResourceLocator ResourceLocator { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies;
        
        private readonly string _assembliesFolder;
        private readonly string _catalogPath;
        private readonly List<Assembly> _loadedAssemblies = new();
        private UniTaskCompletionSource _loadOperation;

        public LocalMod(string modFolder, ModInfo info)
        {
            ModFolder = modFolder;
            Info = info;
            
            _assembliesFolder = Path.Combine(ModFolder, UniMod.AssembliesFolder);
            _catalogPath = Path.Combine(ModFolder, UniMod.AddressablesCatalogFileName);
            ContainsAssets = File.Exists(_catalogPath);
            ContainsAssemblies = Directory.Exists(_assembliesFolder);
        }

        public async UniTask LoadAsync(IModContext context)
        {
            if (_loadOperation != null)
            {
                await _loadOperation.Task;
                return;
            }
            
            _loadOperation = new UniTaskCompletionSource();

            try
            {
                await InternalLoadAsync(context);
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

        private async UniTask InternalLoadAsync(IModContext context)
        {
            if (IsLoaded)
                return;
            
            if (ContainsAssemblies)
                await UniModUtility.LoadAssembliesAsync(_assembliesFolder, _loadedAssemblies);
            
            if (ContainsAssets)
                ResourceLocator = await Addressables.LoadContentCatalogAsync(_catalogPath, true);

            // run startup script and methods
            await UniModUtility.RunStartupObjectFromContentAsync(context, this);
            await UniModUtility.RunStartupMethodsFromAssembliesAsync(LoadedAssemblies, context);
            
            IsLoaded = true;
        }

        private Exception CreateLoadFailedException(string message)
        {
            return new Exception($"Failed to load {Info.Id}: {message}");
        }
    }
}