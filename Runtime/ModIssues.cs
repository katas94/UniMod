using System;

namespace Katas.UniMod
{
    /// <summary>
    /// Issues that can prevent a mod for loading correctly.
    /// </summary>
    [Flags]
    public enum ModIssues
    {
        None                           = 0,
        Unknown                        = 1 << 0, // can be used by custom implementations that may have other issues
        
        // regarding mod's target
        UnsupportedUniModVersion       = 1 << 1, // mod was built with an unsupported UniMod version
        UnsupportedApp                 = 1 << 2, // mod was built for an unsupported app
        UnsupportedAppVersion          = 1 << 3, // mod was built for an unsupported app version
        UnsupportedPlatform            = 1 << 4, // mod was built for an unsupported platform
        
        // regarding mod's dependencies
        CyclicDependencies             = 1 << 5, // there is at least one cyclic route in the mod's dependency graph
        MissingDependencies            = 1 << 6, // there is at least one direct dependency that is missing
        UnsupportedDependenciesVersion = 1 << 7, // there is at least one direct dependency with a version that is not supported by the mod
        DependenciesWithIssues         = 1 << 8, // there is at least one direct/transient dependency that has issues
    }
}