using System;
using System.IO;
using UnityEngine;

namespace Katas.UniMod
{
    public static class UniModRuntime
    {
        public static IUniModContext Context
        {
            get
            {
                if (_context is null)
                    throw new Exception("UniMod context has not been initialized");
                
                return _context;
            }
        }
        
        public static bool IsContextInitialized => _context is not null;
        
        private static IUniModContext _context;
        
        // UniMod package version. Needs to be manually updated
        public const string Version = "0.0.1";
        
        public const string InfoFile = "info.json";
        public const string ModFileExtensionNoDot = "umod";
        public const string ModFileExtension = "." + ModFileExtensionNoDot;
        public const string AddressablesCatalogFileName = "catalog.json";
        public const string StartupAddress = "__mod_startup";
        public const string AssetsFolder = "Assets";
        public const string AssembliesFolder = "Assemblies";
        public const string AnyPlatform = null;
        
        public static readonly bool IsDebugBuild = Debug.isDebugBuild;
        
        private const string LocalInstallationFolderName = "UniMods";
        private const string AddressablesLoadPath = "{{UnityEngine.Application.persistentDataPath}}/" + LocalInstallationFolderName +"/";
        public static readonly string LocalInstallationFolder = Path.Combine(Application.persistentDataPath, LocalInstallationFolderName);
        
        /// <summary>
        /// Initializes the default UniMod context with the specified host ID and version. The UniMod context can be only initialized once.
        /// </summary>
        public static void InitializeContext(string hostId, string hostVersion)
        {
            if (_context is not null)
                throw new Exception("UniMod context has already been initialized");
            
            InitializeContext(new ModHost(hostId, hostVersion));
        }
        
        /// <summary>
        /// Initializes the default UniMod context with the specified host. The UniMod context can be only initialized once.
        /// </summary>
        public static void InitializeContext(IModHost host)
        {
            if (_context is not null)
                throw new Exception("UniMod context has already been initialized");
            
            string installationFolder = LocalInstallationFolder;
            var installer = new LocalModInstaller(installationFolder);
            var localModSource = new LocalModSource(installationFolder);
            var context = new UniModContext(host, installer, localModSource);
            
            InitializeContext(context);
        }

        /// <summary>
        /// Initializes the UniMod context with the given context implementation. The UniMod context can be only initialized once.
        /// </summary>
        public static void InitializeContext(IUniModContext context)
        {
            if (_context is not null)
                throw new Exception("UniMod context has already been initialized");
            
            _context = context;
        }
        
        public static string GetAddressablesLoadPathForMod(string modId)
        {
            return Path.Combine(AddressablesLoadPath, modId, AssetsFolder);
        }
    }
}