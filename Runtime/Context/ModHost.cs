using UnityEngine;

namespace Katas.UniMod
{
    /// <summary>
    /// Default mod host implementation which can be extended for overriding the host versioning rules. It also provides some
    /// extra configuration parameters like the ability to enable disable support for standalone mods or mods created for other hosts and
    /// the ability to disable support for mods containing assemblies.
    /// </summary>
    public class ModHost : IModHost
    {
        public string Id { get; }
        public string Version { get; }
        
        public bool SupportStandaloneMods = true;
        public bool SupportModsContainingAssemblies = true;
        public bool SupportModsCreatedForOtherHosts = false;
        
        public ModHost(string hostId, string hostVersion)
        {
            Id = hostId;
            Version = hostVersion;
        }
        
        public virtual ModIssues GetModIssues(IMod mod)
        {
            var issues = ModIssues.None;
            
            bool isHostSupported;
            bool isHostVersionSupported;

            // if the mod was created for this host, then check if the version is supported
            if (mod.Target.HostId == Id)
            {
                isHostSupported = true;
                isHostVersionSupported = !IsHostVersionSupported(mod.Target.HostVersion);
            }
            // if the mod is standalone (does not target a specific host), then set it supported depending on the config
            else if (string.IsNullOrEmpty(mod.Target.HostId))
            {
                isHostSupported = SupportStandaloneMods;
                isHostVersionSupported = true; // skip host version checking
            }
            // if the mod was created for another host, then set it supported depending on the config
            else
            {
                isHostSupported = SupportModsCreatedForOtherHosts;
                isHostVersionSupported = true; // skip host version checking
            }
            
            // register the issues
            if (Application.unityVersion != mod.Target.UnityVersion)
                issues |= ModIssues.UnsupportedUnityVersion;
            if (!UniModUtility.IsSemanticVersionSupportedByCurrent(mod.Target.UniModVersion, UniModRuntime.Version))
                issues |= ModIssues.UnsupportedUniModVersion;
            if (!UniModUtility.IsPlatformCompatible(mod.Target.Platform))
                issues |= ModIssues.UnsupportedPlatform;
            if (!isHostSupported)
                issues |= ModIssues.UnsupportedHost;
            if (!isHostVersionSupported)
                issues |= ModIssues.UnsupportedHostVersion;
            if (!SupportModsContainingAssemblies && mod.ContainsAssemblies)
                issues |= ModIssues.UnsupportedContent;
            
            return issues;
        }
        
        public virtual bool IsModSupported(IMod mod, out ModIssues issues)
        {
            issues = GetModIssues(mod);
            return issues == ModIssues.None;
        }
        
        /// <summary>
        /// Whether or not the given host version is supported by the this host version. Override this if you want to implement
        /// your own versioning rules for your project. Uses semantic versioning rules by default.
        /// </summary>
        protected virtual bool IsHostVersionSupported(string version)
        {
            return UniModUtility.IsSemanticVersionSupportedByCurrent(version, Version);
        }
    }
}