using System;

namespace Katas.UniMod
{
    /// <summary>
    /// Issues that can prevent a mod from loading correctly.
    /// </summary>
    [Flags]
    public enum ModIssues
    {
        /// <summary>
        /// Mod has no issues.
        /// </summary>
        None                           = 0,
        /// <summary>
        /// Mod has unknown issues. This field can be used by custom implementations.
        /// </summary>
        Unknown                        = 1 << 0,
        
// issues regarding host support
        /// <summary>
        /// Mod was built using a different Unity version.
        /// </summary>
        UnsupportedUnityVersion        = 1 << 1,
        /// <summary>
        /// Mod was built using an unsupported UniMod version.
        /// </summary>
        UnsupportedUniModVersion       = 1 << 2,
        /// <summary>
        /// Mod was built for a different platform.
        /// </summary>
        UnsupportedPlatform            = 1 << 3,
        /// <summary>
        /// Mod was built for an unsupported host.
        /// </summary>
        UnsupportedHost                 = 1 << 4,
        /// <summary>
        /// Mod was built for an unsupported host version.
        /// </summary>
        UnsupportedHostVersion          = 1 << 5,
        /// <summary>
        /// Mod has content that it is either unsupported or explicitly disabled by the host (i.e.: scripting assemblies).
        /// </summary>
        UnsupportedContent             = 1 << 6,
        
// issues regarding mod's dependencies
        /// <summary>
        /// There is at least one cyclic route in the mod's dependency graph.
        /// </summary>
        CyclicDependencies             = 1 << 7,
        /// <summary>
        /// There is at least one direct dependency that is missing.
        /// </summary>
        MissingDependencies            = 1 << 8,
        /// <summary>
        /// There is at least one direct dependency with a version that is not supported by the mod.
        /// </summary>
        UnsupportedDependenciesVersion = 1 << 9,
        /// <summary>
        /// There is at least one direct or transient dependency that has issues.
        /// </summary>
        DependenciesWithIssues         = 1 << 10,
    }
}