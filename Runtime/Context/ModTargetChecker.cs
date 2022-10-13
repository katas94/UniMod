namespace Katas.UniMod
{
    /// <summary>
    /// Default target checker that you can extend for overriding the app versioning rules.
    /// </summary>
    public class ModTargetChecker : IModTargetChecker
    {
        public string AppId { get; }
        public string AppVersion { get; }
        
        public bool SupportStandaloneMods = true;
        public bool SupportModsCreatedForOtherApps = false;
        
        public ModTargetChecker(string appId, string appVersion)
        {
            AppId = appId;
            AppVersion = appVersion;
        }

        public ModIssues CheckForIssues(ModTargetInfo target)
        {
            var issues = ModIssues.None;
            
            bool isAppSupported;
            bool isAppVersionSupported;

            // if the mod was created for our app, then check if the version is supported
            if (target.AppId == AppId)
            {
                isAppSupported = true;
                isAppVersionSupported = !IsAppVersionSupported(target.AppVersion);
            }
            // if the mod is standalone (does not target a specific app), then set it supported depending on the config
            else if (string.IsNullOrEmpty(target.AppId))
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
            if (!UniModUtility.IsSemanticVersionSupportedByCurrent(target.UniModVersion, UniMod.Version))
                issues |= ModIssues.UnsupportedUniModVersion;
            if (!isAppSupported)
                issues |= ModIssues.UnsupportedApp;
            if (!isAppVersionSupported)
                issues |= ModIssues.UnsupportedAppVersion;
            if (!UniModUtility.IsPlatformCompatible(target.Platform))
                issues |= ModIssues.UnsupportedPlatform;
            
            return issues;
        }

        public bool CheckForIssues(ModTargetInfo target, out ModIssues issues)
        {
            issues = CheckForIssues(target);
            return issues != ModIssues.None;
        }

        /// <summary>
        /// Whether or not the given app version is supported by the current app version. Override this if you want to implement
        /// your own versioning rules for your app. Uses semantic versioning rules by default.
        /// </summary>
        protected virtual bool IsAppVersionSupported(string version)
        {
            return UniModUtility.IsSemanticVersionSupportedByCurrent(version, AppVersion);
        }
    }
}