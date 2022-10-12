using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    [Flags]
    public enum ModLoadingIssues
    {
        None                           = 0,
        Other                          = 1 << 0, // can be used by custom IModDependencyGraph implementations that may have other issues
        Incompatible                   = 1 << 1, // mod has incompatibilities
        CyclicDependencies             = 1 << 2, // there is at least one cyclic dependency route in the mod's dependency graph
        MissingDependencies            = 1 << 3, // there is at least one direct dependency that is missing
        UnsupportedVersionDependencies = 1 << 4, // there is at least one direct dependency with a version unsupported by the mod
        DependenciesWithLoadingIssues  = 1 << 5, // there is at least one direct/transient dependency that has loading issues
    }
    
    /// <summary>
    /// Contains all the loading information for a mod. The loading information includes the mod's dependency graph,
    /// any missing/unsupported dependencies and all existing issues for the mod's loading.
    /// </summary>
    public interface IModLoadingInfo
    {
        /// <summary>
        /// The mod that this loading information is made for.
        /// </summary>
        IMod Mod { get; }
        
        /// <summary>
        /// True if there are no loading issues for the mod.
        /// </summary>
        bool CanBeLoaded => LoadingIssues == ModLoadingIssues.None;
        
        /// <summary>
        /// Provides information about all existing issues for loading the mod.
        /// </summary>
        ModLoadingIssues LoadingIssues { get; }
        
        /// <summary>
        /// Loading information for the mod's direct dependencies. It does not include missing dependencies as we don't have
        /// the mod instances for them.
        /// </summary>
        IReadOnlyCollection<IModLoadingInfo> Dependencies { get; }
        
        /// <summary>
        /// Mod's missing direct dependencies IDs.
        /// </summary>
        IReadOnlyCollection<string> MissingDependencies { get; }

        /// <summary>
        /// Populates the results collection with the loading information of the dependencies causing the specified issues. Issues can include
        /// multiple flags. Not all flags are related to direct or transient dependencies (i.e.: Incompatible).
        /// </summary>
        void GetDependenciesThatCauseTheIssues(ModLoadingIssues issues, ICollection<IModLoadingInfo> results);
    }
}