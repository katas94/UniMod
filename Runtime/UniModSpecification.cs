using System.IO;
using UnityEngine;

namespace Katas.UniMod
{
    public static class UniModSpecification
    {
        public const string InfoFile = "info.json";
        public const string ModFileExtensionNoDot = "umod";
        public const string ModFileExtension = "." + ModFileExtensionNoDot;
        public const string CatalogName = "mod_content";
        public const string CatalogFileName = "catalog_" + CatalogName + ".json";
        public const string StartupAddress = "__mod_startup";
        public const string AssembliesFolder = "Assemblies";
        public const string AnyPlatform = "any";
        
        private const string ModsFolderName = "UniMod-Mods";
        private const string AddressablesLoadPath = "{{UnityEngine.Application.persistentDataPath}}/" + ModsFolderName +"/";
        public static readonly string LocalInstallationFolder = Path.Combine(Application.persistentDataPath, ModsFolderName);

        public static string GetAddressablesModLoadPath(string modId)
        {
            return AddressablesLoadPath + modId;
        }
    }
}