using System.Collections.Generic;

namespace Katas.UniMod
{
    /// <summary>
    /// Contains contextual information about a mod: if it has any issues, its dependencies, etc.
    /// </summary>
    public interface IModStatus
    {
        /// <summary>
        /// The mod instance that this status refers to.
        /// </summary>
        IMod Mod { get; }
        
        /// <summary>
        /// Issues that can cause the mod to not load properly.
        /// </summary>
        ModIssues Issues { get; }
        
        /// <summary>
        /// Statuses for the mod's direct dependencies. It does not include missing dependencies as we don't have
        /// the mod instances for them.
        /// </summary>
        IReadOnlyCollection<IModStatus> Dependencies { get; }
        
        /// <summary>
        /// Mod's missing direct dependencies IDs.
        /// </summary>
        IReadOnlyCollection<string> MissingDependencies { get; }

        /// <summary>
        /// Populates the results collection with the statuses of the dependencies causing the specified issues. Issues can include
        /// multiple flags. Not all issues are related with dependencies, like the IncompatiblePlatform issue, those will never produce results.
        /// </summary>
        void GetDependenciesRelatedToIssues(ModIssues issues, ICollection<IModStatus> results);
    }
}