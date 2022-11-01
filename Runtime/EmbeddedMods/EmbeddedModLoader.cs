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
        public readonly EmbeddedModConfig Config;
        
        public ModInfo Info { get; }
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public bool IsLoaded { get; private set; }
        public IResourceLocator ResourceLocator { get; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        private readonly ModStartup _startup;
        
        private UniTaskCompletionSource _loadOperation;

        public EmbeddedModLoader(EmbeddedModConfig config)
        {
            Config = config;
            _startup = config.startup;
            
            Info = UniModUtility.CreateModInfoFromEmbeddedConfig(config);
            // ContainsAssets = config.containsAssets;
            ResourceLocator = new EmptyResourceLocator();
            LoadedAssemblies = GetLoadedAssemblies(config).AsReadOnly();
            ContainsAssemblies = LoadedAssemblies.Count > 0;
        }
        
        public async UniTask LoadAsync(IModContext context, IMod mod)
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
        
        private async UniTask InternalLoadAsync(IModContext context, IMod mod)
        {
            if (IsLoaded)
                return;
            
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