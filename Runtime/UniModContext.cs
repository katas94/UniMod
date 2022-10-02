using System;

namespace Katas.UniMod
{
    public static class UniModContext
    {
        public static ILocalModInstaller Installer { get; private set; }
        public static IModRegistry Registry { get; private set; }
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized)
                return;
            
            string installationFolder = UniModSpecification.ModsFolderPath; 
            var installer = new LocalModInstaller(installationFolder);
            var registry = new UniModRegistry(installationFolder);
            Initialize(installer, registry);
        }

        public static void Initialize(ILocalModInstaller installer, IModRegistry registry)
        {
            if (IsInitialized)
                throw new Exception($"The {nameof(UniModContext)} has already been initialized");
            
            Installer = installer;
            Registry = registry;
            IsInitialized = true;
        }
    }
}