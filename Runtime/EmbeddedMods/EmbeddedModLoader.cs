using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    public sealed class EmbeddedModLoader : IModLoader
    {
        private static readonly IReadOnlyList<Assembly> EmptyAssemblies = new List<Assembly>(0).AsReadOnly();
        
        public readonly EmbeddedModConfig Config;
        
        public ModInfo Info { get; }
        public string Source { get; }
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public bool IsLoaded { get; private set; }
        public IResourceLocator ResourceLocator { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; private set; }

        private readonly ModStartup _startup;
        private readonly IReadOnlyList<Assembly> _loadedAssemblies;
        
        private UniTaskCompletionSource _loadOperation;

        public EmbeddedModLoader(EmbeddedModConfig config, string source = EmbeddedModSource.SourceLabel)
        {
            Config = config;
            _startup = config.startup;
            _loadedAssemblies = GetLoadedAssemblies(config).AsReadOnly();
            
            Info = UniModUtility.CreateModInfoFromEmbeddedConfig(config);
            Source = source;
            ContainsAssets = config.ContainsAssets;
            ContainsAssemblies = _loadedAssemblies.Count > 0;
            // to simulate how local mods are loaded, lets not assign these properties yet even though the assets/assemblies are already loaded
            ResourceLocator = EmptyLocator.Instance;
            LoadedAssemblies = EmptyAssemblies;
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
            throw new System.NotImplementedException();
        }
        
        private async UniTask InternalLoadAsync(IUniModContext context, IMod mod)
        {
            if (IsLoaded)
                return;
            
            // to simulate how local mods are loaded, we will assign now the properties to the already loaded assets/assemblies
            if (ContainsAssets)
                ResourceLocator = await EmbeddedModAssetsLocator.CreateAsync(Config.modId, Config.assets);
            if (ContainsAssemblies)
                LoadedAssemblies = _loadedAssemblies;
            
            // run startup script and methods
            if (_startup)
                await _startup.StartAsync(context, mod);
            
            await UniModUtility.RunStartupMethodsFromAssembliesAsync(LoadedAssemblies, context, mod);
            
            IsLoaded = true;
        }

        private static List<Assembly> GetLoadedAssemblies(EmbeddedModConfig config)
        {
            // find the assemblies config for the current platform
            foreach (EmbeddedModAssemblies configAssemblies in config.assemblies)
            {
                RuntimePlatform currentPlatform = Application.platform;
                
#if UNITY_EDITOR
                currentPlatform = currentPlatform switch
                {
                    RuntimePlatform.WindowsEditor => RuntimePlatform.WindowsPlayer,
                    RuntimePlatform.OSXEditor => RuntimePlatform.OSXPlayer,
                    RuntimePlatform.LinuxEditor => RuntimePlatform.LinuxPlayer,
                    _ => currentPlatform
                };
#endif
                
                if (configAssemblies.platform == currentPlatform)
                {
                    var results = new List<Assembly>(configAssemblies.names.Count);
                    
                    // get all the domain loaded assemblies and register them by name
                    using var _ = DictionaryPool<string, Assembly>.Get(out var assembliesByName);
                    foreach (Assembly assembly in DomainAssemblies.Assemblies)
                        assembliesByName[assembly.GetName().Name] = assembly;
                    
                    // find the assemblies in the domain corresponding to the names in the config
                    foreach (string assemblyName in configAssemblies.names)
                        if (assembliesByName.TryGetValue(assemblyName, out Assembly assembly))
                            results.Add(assembly);
                    
                    return results;
                }
            }
            
            return new List<Assembly>(0);
        }
    }
}