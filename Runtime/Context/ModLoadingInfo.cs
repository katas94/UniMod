using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    /// <summary>
    /// Default implementation of the mod loading information. You can only use it through a static method that will resolve
    /// a collection of mods into the mod loading information for each one and add them to a given collection.
    /// </summary>
    public sealed class ModLoadingInfo : IModLoadingInfo
    {
        public IMod Mod { get; }
        public ModLoadingIssues LoadingIssues { get; private set; }
        public IReadOnlyCollection<IModLoadingInfo> Dependencies => _dependencies;
        public IReadOnlyCollection<string> MissingDependencies => _missingDependencies;

        private readonly List<IModLoadingInfo> _dependencies;
        private readonly HashSet<string> _missingDependencies;
        private readonly Dictionary<ModLoadingIssues, List<IModLoadingInfo>> _issueCauses;

        private bool _resolved;

        private ModLoadingInfo(IMod mod)
        {
            Mod = mod ?? throw new NullReferenceException("ModDependencyGraph cannot be instantiated with a null mod instance");
            
            _dependencies = new List<IModLoadingInfo>(Mod.Info.Dependencies?.Count ?? 0);
            _missingDependencies = new HashSet<string>();
            _issueCauses = new Dictionary<ModLoadingIssues, List<IModLoadingInfo>>();
        }
        
        public void GetDependenciesThatCauseTheIssues(ModLoadingIssues issues, ICollection<IModLoadingInfo> results)
        {
            var modLoadingIssues = Enum.GetValues(typeof(ModLoadingIssues)) as ModLoadingIssues[];
            if (modLoadingIssues is null)
                return;

            foreach (ModLoadingIssues issue in modLoadingIssues)
                if ((issues & issue) == issue && _issueCauses.TryGetValue(issue, out List<IModLoadingInfo> causes))
                    foreach (IModLoadingInfo cause in causes)
                        results.Add(cause);
        }

        private void Resolve(IDictionary<string, ModLoadingInfo> loadingInfos)
        {
            if (_resolved)
                return;
            
            _resolved = true;
            
            // check for mod incompatibility issues
            LoadingIssues = Mod.Incompatibilities is ModIncompatibilities.None ?
                ModLoadingIssues.None : ModLoadingIssues.Incompatible;
            
            if (Mod.Info.Dependencies is null)
                return;
            
            // resolve all direct dependencies, this will solve the entire graph recursively
            foreach ((string id, _) in Mod.Info.Dependencies)
            {
                if (loadingInfos.TryGetValue(id, out ModLoadingInfo dependency))
                {
                    _dependencies.Add(dependency);
                    dependency.Resolve(loadingInfos);
                    
                    // check version support (ignore non compliant versions)
                    if (!UniModUtility.IsTargetSemanticVersionCompatibleWith(Mod.Info.Id, dependency.Mod.Info.Id, true))
                    {
                        LoadingIssues |= ModLoadingIssues.UnsupportedVersionDependencies;
                        AddIssueRootCause(ModLoadingIssues.UnsupportedVersionDependencies, dependency);
                    }
                    
                    // check for loading issues
                    if (dependency.LoadingIssues != ModLoadingIssues.None)
                    {
                        LoadingIssues |= ModLoadingIssues.DependenciesWithLoadingIssues;
                        AddIssueRootCause(ModLoadingIssues.DependenciesWithLoadingIssues, dependency);
                    }
                }
                else
                {
                    // mark as missing dependency
                    LoadingIssues |= ModLoadingIssues.MissingDependencies;
                    _missingDependencies.Add(id);
                }
            }

            // check for cyclic dependencies
            if (HasCyclicDependencies())
                LoadingIssues |= ModLoadingIssues.CyclicDependencies;
        }

        private bool HasCyclicDependencies()
        {
            foreach (IModLoadingInfo dependency in _dependencies)
                if (HasCyclicDependencies(dependency))
                    return true;
            
            return false;
        }
        
        private bool HasCyclicDependencies(IModLoadingInfo dependency)
        {
            // if the dependency that we are checking is this one or has cyclic dependencies then return true
            if (dependency == this || (dependency.LoadingIssues & ModLoadingIssues.CyclicDependencies) == ModLoadingIssues.CyclicDependencies)
                return true;
            
            // recursively check the transient dependencies for cyclic dependencies
            foreach (IModLoadingInfo transientDependency in dependency.Dependencies)
                if (HasCyclicDependencies(transientDependency))
                    return true;
            
            return false;
        }
        
        private void AddIssueRootCause(ModLoadingIssues issue, IModLoadingInfo loadingInfo)
        {
            if (!_issueCauses.TryGetValue(issue, out List<IModLoadingInfo> causes))
                _issueCauses[issue] = causes = new List<IModLoadingInfo>();
            
            causes.Add(loadingInfo);
        }
        
        public static void ResolveModLoadingInformation(IEnumerable<IMod> mods, ICollection<ModLoadingInfo> loadingInfos)
        {
            if (mods is null || loadingInfos is null)
                return;
            
            var dictionary = DictionaryPool<string, ModLoadingInfo>.Pick();
            ResolveModLoadingInformation(mods, dictionary);
            
            foreach (ModLoadingInfo loadingInfo in dictionary.Values)
                loadingInfos.Add(loadingInfo);
            
            DictionaryPool<string, ModLoadingInfo>.Release(dictionary);
        }
        
        public static void ResolveModLoadingInformation(IEnumerable<IMod> mods, IDictionary<string, ModLoadingInfo> loadingInfos)
        {
            foreach (IMod mod in mods)
                loadingInfos[mod.Info.Id] = new ModLoadingInfo(mod);
            foreach (ModLoadingInfo loadingInfo in loadingInfos.Values)
                loadingInfo.Resolve(loadingInfos);
        }
    }
}