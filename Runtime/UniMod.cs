using System.IO;
using UnityEngine;

namespace Katas.UniMod
{
    public static class UniMod
    {
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

        public static string GetAddressablesLoadPathForMod(string modId)
        {
            return Path.Combine(AddressablesLoadPath, modId, AssetsFolder);
        }
    }
}