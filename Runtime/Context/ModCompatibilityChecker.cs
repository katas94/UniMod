using Semver;

namespace Katas.UniMod
{
    public class ModCompatibilityChecker : IModCompatibilityChecker
    {
        public readonly string TargetId;
        public readonly string TargetVersion;
        
        /// <summary>
        /// If true, mods created without a target will be treated as compatible.
        /// </summary>
        public bool TreatModsWithoutTargetAsCompatible = true;
        
        /// <summary>
        /// If true, mods created for other targets will be treated as compatible (target version will be ignored for obvious reasons).
        /// </summary>
        public bool TreatModsForOtherTargetsAsCompatible = false;
        
        /// <summary>
        /// If true, non-compliant versions (not following the Semantic Versioning 2.0.0 standard) will be treated as compatible versions.
        /// </summary>
        public bool TreatNonCompliantVersionsAsCompatible = false;
        
        public ModCompatibilityChecker(string targetId, string targetVersion)
        {
            TargetId = targetId;
            TargetVersion = targetVersion;
        }
        
        public bool IsTargetCompatible(ModTargetInfo target)
        {
            if (!SemVersion.TryParse("", SemVersionStyles.Strict, out SemVersion version))
                return false;
            
            return true;
        }

        public ModIncompatibilities GetIncompatibilities(ModTargetInfo target)
        {
            ModIncompatibilities incompatibilities = ModIncompatibilities.None;
            
            bool isTargetCompatible;
            bool isTargetVersionCompatible;

            // if the mod was created for another target, then set it compatible depending on the config
            if (target.TargetId != TargetId)
            {
                isTargetCompatible = TreatModsForOtherTargetsAsCompatible;
                isTargetVersionCompatible = true;
            }
            // if the mod was created with no target, then set it compatible depending on the config
            else if (string.IsNullOrEmpty(target.TargetId))
            {
                isTargetCompatible = TreatModsWithoutTargetAsCompatible;
                isTargetVersionCompatible = true;
            }
            // if the mod was created for our target, then check the target version compatibility
            else
            {
                isTargetCompatible = true;
                isTargetVersionCompatible = !IsTargetVersionCompatible(target.TargetVersion);
            }
            
            // check the other incompatibilities and populate the flags accordingly
            if (!UniModUtility.IsTargetSemanticVersionCompatibleWith(target.UniModVersion, UniMod.Version))
                incompatibilities |= ModIncompatibilities.UniModVersion;
            if (!isTargetCompatible)
                incompatibilities |= ModIncompatibilities.Target;
            if (!isTargetVersionCompatible)
                incompatibilities |= ModIncompatibilities.TargetVersion;
            if (!UniModUtility.IsPlatformCompatible(target.Platform))
                incompatibilities |= ModIncompatibilities.Platform;
            
            return incompatibilities;
        }

        public bool IsCompatible(ModTargetInfo target, out ModIncompatibilities incompatibilities)
        {
            incompatibilities = GetIncompatibilities(target);
            return incompatibilities == ModIncompatibilities.None;
        }

        public bool IsCompatible(ModTargetInfo target)
        {
            ModIncompatibilities incompatibilities = GetIncompatibilities(target);
            return incompatibilities == ModIncompatibilities.None;
        }

        /// <summary>
        /// Whether or not the given mod's target version is compatible with the current target version. Override this if you want to implement
        /// your own versioning rules for your project.
        /// </summary>
        protected virtual bool IsTargetVersionCompatible(string targetVersion)
        {
            return UniModUtility.IsTargetSemanticVersionCompatibleWith(targetVersion, TargetVersion);
        }
    }
}