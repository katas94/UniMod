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
        public ModIncompatibilities Incompatibilities { get; }
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
            Incompatibilities = Context.GetIncompatibilities(Info.Target);
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
            
            // load assemblies
            if (Info.Type is ModType.ContentAndAssemblies or ModType.Assemblies)
            {
                string assembliesFolder = Path.Combine(ModFolder, UniMod.AssembliesFolder);
                await UniModUtility.LoadAssembliesAsync(assembliesFolder, _loadedAssemblies);
            }
            
            // load content
            if (Info.Type is ModType.ContentAndAssemblies or ModType.Content)
            {
                // load mod content catalog
                string catalogPath = Path.Combine(ModFolder, UniMod.AddressablesCatalogFileName);
                if (!File.Exists(catalogPath))
                    throw CreateLoadFailedException($"Couldn't find mod's Addressables catalogue at {catalogPath}");
                
                ResourceLocator = await Addressables.LoadContentCatalogAsync(catalogPath, true);
            }

            // run startup script and methods
            try
            {
                await UniModUtility.RunStartupObjectFromContentAsync(this);
                await UniModUtility.RunStartupMethodsFromAssembliesAsync(this);
            }
            catch (Exception exception)
            {
                throw CreateLoadFailedException($"Something went wrong while running mod startup.\n{exception}");
            }
            
            IsLoaded = true;
        }

        private Exception CreateLoadFailedException(string message)
        {
            return new Exception($"Failed to load {Info.Id}: {message}");
        }
    }
}