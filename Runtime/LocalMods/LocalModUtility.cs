using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Katas.UniMod
{
    /// <summary>
    /// Some utility methods used by the LocalMod implementation. They are exposed statically in this class so they can be reused by other mod implementations.
    /// </summary>
    public static class LocalModUtility
    {
        private static readonly bool IsDebugBuild = Debug.isDebugBuild;
        
        public static bool IsPlatformSupported(string platform)
        {
            if (platform == UniModSpecification.AnyPlatform)
                return true;
            
            // try to get the RuntimePlatform value from the info
            if (!Enum.TryParse(platform, false, out RuntimePlatform runtimePlatform))
                return false;
            
#if UNITY_EDITOR
            // special case for unity editor (mod builds are never set to any of the Editor platforms)
            return Application.platform switch
            {
                RuntimePlatform.WindowsEditor => runtimePlatform == RuntimePlatform.WindowsPlayer,
                RuntimePlatform.OSXEditor => runtimePlatform == RuntimePlatform.OSXPlayer,
                RuntimePlatform.LinuxEditor => runtimePlatform == RuntimePlatform.LinuxPlayer,
                _ => false
            };
#else
            return Application.platform == runtimePlatform;
#endif
        }
        
        /// <summary>
        /// Loads into the application domain all the assemblies found in the given folder. Symbol store files will also be loaded if present and running
        /// a debug build. If a results collection is given, it will be populated with the loaded assemblies.
        /// </summary>
        public static async UniTask LoadAssembliesAsync(string folder, ICollection<Assembly> results = null)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return;
            
            // fetch all the assembly file paths from the folder and load them in parallel
            string[] paths = Directory.GetFiles(folder, "*.dll");
            (Assembly[] assemblies, Exception exception) = await UniTaskUtility.WhenAllNoThrow(paths.Select(LoadAssemblyAsync));
            
            // add all loaded assemblies into the given assemblies collection
            if (results is not null)
                foreach (Assembly assembly in assemblies)
                    if (assembly is not null)
                        results.Add(assembly);
            
            if (exception is not null)
                throw exception;
        }
        
        /// <summary>
        /// Loads the assembly at the given file path into the application domain.
        /// </summary>
        public static async UniTask<Assembly> LoadAssemblyAsync(string filePath)
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                // load the raw assembly and try to load it into the app domain
                RawAssembly rawAssembly = await LoadRawAssemblyOnSameThreadAsync(filePath);
                return DomainAssemblies.Load(rawAssembly.Assembly, rawAssembly.SymbolStore);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }
        
        /// <summary>
        /// Loads the assembly raw bytes at the given file path. Symbol store raw bytes will also be loaded if
        /// a .pdb file exists at the same path with the same name and we are on a debug build.
        /// </summary>
        public static async UniTask<RawAssembly> LoadRawAssemblyAsync(string filePath)
        {
            await UniTask.SwitchToThreadPool();

            try
            {
                return await LoadRawAssemblyOnSameThreadAsync(filePath);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        /// <summary>
        /// Given a mod instance it will try to fetch the ModStartup object from its content and invoke it.
        /// </summary>
        public static async UniTask RunStartupObjectFromContentAsync(IMod mod)
        {
            if (mod?.ResourceLocator is null)
                return;
            
            // check if the mod contains a startup script
            if (!mod.ResourceLocator.Locate(UniModSpecification.StartupAddress, typeof(object), out IList<IResourceLocation> locations))
                return;
            
            // load and execute the startup script
            IResourceLocation location = locations.FirstOrDefault();
            if (location is null)
                return;
            
            var startup = await Addressables.LoadAssetAsync<ModStartup>(location);
            if (startup)
                await startup.StartAsync(mod);
        }
        
        /// <summary>
        /// Given a mod instance it will run all the methods marked as ModStartup from its assemblies. ModStartup methods returning
        /// a UniTask object will be executed concurrently.
        /// </summary>
        public static UniTask RunStartupMethodsFromAssembliesAsync(IMod mod)
        {
            if (mod?.LoadedAssemblies is null)
                return UniTask.CompletedTask;
            
            // use Linq to invoke all ModStartup methods from the mod assemblies
            IEnumerable<UniTask> tasks = mod.LoadedAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .SelectMany(type => type.GetMethods())
                .Select(methodInfo => InvokeModStartupMethodAsync(methodInfo, mod));
            
            return UniTaskUtility.WhenAll(tasks);
        }

        /// <summary>
        /// If the provided method info instance is from a ModStartup method (has the ModStartupAttribute), it will execute
        /// it with the given mod instance. The method can either return void or a UniTask object and it can receive no arguments or
        /// receive an IMod instance as the only argument.
        /// </summary>
        public static UniTask InvokeModStartupMethodAsync(MethodInfo methodInfo, IMod mod)
        {
            if (methodInfo is null || mod is null || !methodInfo.IsStatic || methodInfo.GetCustomAttributes(typeof(ModStartupAttribute), false).Length == 0)
                return UniTask.CompletedTask;

            ParameterInfo[] parameters = methodInfo.GetParameters();
            
            // accept methods with no parameters or methods with one IMod parameter
            object result = parameters.Length switch
            {
                0 => methodInfo.Invoke(null, null),
                1 when parameters[0].ParameterType == typeof(IMod) => methodInfo.Invoke(null, new object[] { mod }),
                _ => null
            };

            if (result is UniTask task)
                return task;
            
            return UniTask.CompletedTask;
        }
        
        private static async UniTask<RawAssembly> LoadRawAssemblyOnSameThreadAsync(string filePath)
        {
            var result = new RawAssembly
            {
                Assembly = await File.ReadAllBytesAsync(filePath)
            };

            if (!IsDebugBuild)
                return result;
            
            // look for the assembly's pdb file and load it if exists
            string pdbFilePath = null;
            
            try
            {
                string folderPath = Path.GetDirectoryName(filePath) ?? string.Empty;
                pdbFilePath = Path.GetFileNameWithoutExtension(filePath);
                pdbFilePath = Path.Combine(folderPath, $"{pdbFilePath}.pdb");
                
                if (File.Exists(pdbFilePath))
                    result.SymbolStore = await File.ReadAllBytesAsync(pdbFilePath);
            }
            catch (Exception exception)
            {
                // don't throw if the pdb file couldn't be loaded, we can still load the assembly
                Debug.LogWarning($"Failed to read the symbol store file from {pdbFilePath}\n{exception}");
            }
            
            return result;
        }

        public struct RawAssembly
        {
            public byte[] Assembly;
            public byte[] SymbolStore;
        }
    }
}