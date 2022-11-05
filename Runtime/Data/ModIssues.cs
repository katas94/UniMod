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
        
        // issues regarding host support
        UnsupportedUnityVersion        = 1 << 1, // mod was built with a different Unity version
        UnsupportedUniModVersion       = 1 << 2, // mod was built with an unsupported UniMod version
        UnsupportedPlatform            = 1 << 3, // mod was built for a different platform
        UnsupportedHost                 = 1 << 4, // mod was built for an unsupported host
        UnsupportedHostVersion          = 1 << 5, // mod was built for an unsupported host version
        UnsupportedContent             = 1 << 6, // mod has content that it is either unsupported or explicitly disabled by the host (i.e.: scripting assemblies)
        
        // issues regarding mod's dependencies
        CyclicDependencies             = 1 << 7, // there is at least one cyclic route in the mod's dependency graph
        MissingDependencies            = 1 << 8, // there is at least one direct dependency that is missing
        UnsupportedDependenciesVersion = 1 << 9, // there is at least one direct dependency with a version that is not supported by the mod
        DependenciesWithIssues         = 1 << 10, // there is at least one direct/transient dependency that has issues
    }
}