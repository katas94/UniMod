using UnityEngine;

namespace Katas.UniMod
{
    /// <summary>
    /// Default moddable application implementation which can be extended for overriding the app versioning rules. It also provides some
    /// extra configuration parameters like the ability to enable disable support for standalone mods or mods created for other apps and
    /// the ability to disable support for mods containing assemblies.
    /// </summary>
    public class ModdableApplication : IModdableApplication
    {
        public string Id { get; }
        public string Version { get; }
        
        public bool SupportStandaloneMods = true;
        public bool SupportModsCreatedForOtherApps = false;
        public bool SupportModsContainingAssemblies = true;
        
        public ModdableApplication(string appId, string appVersion)
        {
            Id = appId;
            Version = appVersion;
        }
        
        public virtual ModIssues GetModIssues(ModInfo info)
        {
            var issues = ModIssues.None;
            
            bool isAppSupported;
            bool isAppVersionSupported;

            // if the mod was created for this app, then check if the version is supported
            if (info.Target.AppId == Id)
            {
                isAppSupported = true;
                isAppVersionSupported = !IsAppVersionSupported(info.Target.AppVersion);
            }
            // if the mod is standalone (does not target a specific app), then set it supported depending on the config
            else if (string.IsNullOrEmpty(info.Target.AppId))
            {
                isAppSupported = SupportStandaloneMods;
                isAppVersionSupported = true; // skip app version checking
            }
            // if the mod was created for another app, then set it supported depending on the config
            else
            {
                isAppSupported = SupportModsCreatedForOtherApps;
                isAppVersionSupported = true; // skip app version checking
            }
            
            // register the issues
            if (Application.unityVersion != info.Target.UnityVersion)
                issues |= ModIssues.UnsupportedUnityVersion;
            if (!UniModUtility.IsSemanticVersionSupportedByCurrent(info.Target.UniModVersion, UniMod.Version))
                issues |= ModIssues.UnsupportedUniModVersion;
            if (!UniModUtility.IsPlatformCompatible(info.Target.Platform))
                issues |= ModIssues.UnsupportedPlatform;
            if (!isAppSupported)
                issues |= ModIssues.UnsupportedApp;
            if (!isAppVersionSupported)
                issues |= ModIssues.UnsupportedAppVersion;
            if (!SupportModsContainingAssemblies && info.Type is ModType.ContentAndAssemblies or ModType.Assemblies)
                issues |= ModIssues.UnsupportedContent;
            
            return issues;
        }
        
        public virtual bool IsModSupported(ModInfo info, out ModIssues issues)
        {
            issues = GetModIssues(info);
            return issues == ModIssues.None;
        }
        
        /// <summary>
        /// Whether or not the given app version is supported by the this app version. Override this if you want to implement
        /// your own versioning rules for your app. Uses semantic versioning rules by default.
        /// </summary>
        protected virtual bool IsAppVersionSupported(string version)
        {
            return UniModUtility.IsSemanticVersionSupportedByCurrent(version, Version);
        }
    }
}