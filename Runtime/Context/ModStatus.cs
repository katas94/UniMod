using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    /// <summary>
    /// Default implementation of the mod status. You can only use it through a static method that will resolve
    /// a collection of mods into the statuses for each one and add them to a given collection.
    /// </summary>
    public sealed class ModStatus : IModStatus
    {
        public IMod Mod { get; }
        public ModIssues Issues { get; private set; }
        public IReadOnlyCollection<IModStatus> Dependencies => _dependencies;
        public IReadOnlyCollection<string> MissingDependencies => _missingDependencies;

        private readonly List<IModStatus> _dependencies;
        private readonly HashSet<string> _missingDependencies;
        private readonly Dictionary<ModIssues, List<IModStatus>> _issueCauses;

        private bool _resolved;

        private ModStatus(IMod mod)
        {
            Mod = mod ?? throw new NullReferenceException("ModDependencyGraph cannot be instantiated with a null mod instance");
            
            _dependencies = new List<IModStatus>(Mod.Info.Dependencies?.Count ?? 0);
            _missingDependencies = new HashSet<string>();
            _issueCauses = new Dictionary<ModIssues, List<IModStatus>>();
        }
        
        public void GetDependenciesRelatedToIssues(ModIssues issues, ICollection<IModStatus> results)
        {
            var modLoadingIssues = Enum.GetValues(typeof(ModIssues)) as ModIssues[];
            if (modLoadingIssues is null)
                return;

            foreach (ModIssues issue in modLoadingIssues)
                if ((issues & issue) == issue && _issueCauses.TryGetValue(issue, out List<IModStatus> causes))
                    foreach (IModStatus cause in causes)
                        results.Add(cause);
        }

        private void Resolve(IDictionary<string, ModStatus> statuses, IModdableApplication application)
        {
            if (_resolved)
                return;
            
            _resolved = true;
            
            // get any possible app support issues with the mod
            Issues = application.GetModIssues(Mod.Info);
            
            if (Mod.Info.Dependencies is null)
                return;
            
            // resolve all direct dependencies, this will solve the entire graph recursively
            foreach ((string id, string version) in Mod.Info.Dependencies)
            {
                if (!statuses.TryGetValue(id, out ModStatus dependency))
                {
                    // mark as missing dependency
                    Issues |= ModIssues.MissingDependencies;
                    _missingDependencies.Add(id);
                    continue;
                }

                _dependencies.Add(dependency);
                dependency.Resolve(statuses, application);
                
                // check version support
                if (!UniModUtility.IsSemanticVersionSupportedByCurrent(version, dependency.Mod.Info.Version))
                {
                    Issues |= ModIssues.UnsupportedDependenciesVersion;
                    AddIssueRootCause(ModIssues.UnsupportedDependenciesVersion, dependency);
                }
                
                // check any other issues
                if (dependency.Issues != ModIssues.None)
                {
                    Issues |= ModIssues.DependenciesWithIssues;
                    AddIssueRootCause(ModIssues.DependenciesWithIssues, dependency);
                }
            }

            // check for cyclic dependencies
            if (HasCyclicDependencies())
                Issues |= ModIssues.CyclicDependencies;
        }

        private bool HasCyclicDependencies()
        {
            foreach (IModStatus dependency in _dependencies)
                if (HasCyclicDependencies(dependency))
                    return true;
            
            return false;
        }
        
        private bool HasCyclicDependencies(IModStatus dependency)
        {
            // if the dependency that we are checking is this one or has cyclic dependencies then return true
            if (dependency == this || (dependency.Issues & ModIssues.CyclicDependencies) == ModIssues.CyclicDependencies)
                return true;
            
            // recursively check the transient dependencies for cyclic dependencies
            foreach (IModStatus transientDependency in dependency.Dependencies)
                if (HasCyclicDependencies(transientDependency))
                    return true;
            
            return false;
        }
        
        private void AddIssueRootCause(ModIssues issue, IModStatus status)
        {
            if (!_issueCauses.TryGetValue(issue, out List<IModStatus> causes))
                _issueCauses[issue] = causes = new List<IModStatus>();
            
            causes.Add(status);
        }
        
        public static void ResolveStatuses(IEnumerable<IMod> mods, IModdableApplication application, ICollection<ModStatus> statuses)
        {
            if (mods is null || statuses is null)
                return;
            
            var dictionary = DictionaryPool<string, ModStatus>.Pick();
            ResolveStatuses(mods, application, dictionary);
            
            foreach (ModStatus status in dictionary.Values)
                statuses.Add(status);
            
            DictionaryPool<string, ModStatus>.Release(dictionary);
        }
        
        public static void ResolveStatuses(IEnumerable<IMod> mods, IModdableApplication application, IDictionary<string, ModStatus> statuses)
        {
            foreach (IMod mod in mods)
                statuses[mod.Info.Id] = new ModStatus(mod);
            foreach (ModStatus status in statuses.Values)
                status.Resolve(statuses, application);
        }
    }
}