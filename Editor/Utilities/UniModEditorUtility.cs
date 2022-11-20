using System;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Some mixed utility methods for the UniMod editor classes.
    /// </summary>
    public static partial class UniModEditorUtility
    {
        /// <summary>
        /// Tries to copy the given managed assembly path into the given output folder. If specified, it will also
        /// try to copy the pdb file (if any).
        /// </summary>
        public static async UniTask CopyManagedAssemblyAsync (string dllSrcPath, string outputFolder, bool tryCopyPdbToo)
        {
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
                throw new Exception("The given output folder is null/empty or it does not exist");
            if (string.IsNullOrEmpty(dllSrcPath))
                throw new Exception("The given assembly path is null or empty");
            
            await UniTask.SwitchToThreadPool();

            try
            {
                // check that the assembly exists and it is a valid net managed assembly
                if (!File.Exists(dllSrcPath))
                    throw new FileNotFoundException($"Could not find the assembly file at \"{dllSrcPath}\"");
                if (!IsManagedAssembly(dllSrcPath))
                    throw new Exception($"\"{dllSrcPath}\" is not a managed assembly");
                
                // get the src/dst paths for both the assembly and its pdb (debugging) file
                string fileName = Path.GetFileName(dllSrcPath);
                string dllDestPath = Path.Combine(outputFolder, fileName);
                string pdbSrcPath = Path.ChangeExtension(dllSrcPath, ".pdb");
                string pdbDestPath = Path.ChangeExtension(dllDestPath, ".pdb");


                File.Copy(dllSrcPath, dllDestPath, true);

                // copy pdb file if requested and the file exists. Some assemblies may not come with a pdb file so we won't throw if not found
                if (tryCopyPdbToo && File.Exists(pdbSrcPath))
                    File.Copy(pdbSrcPath, pdbDestPath, true);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }
        
        /// <summary>
        /// Checks if the given filePath corresponds to a valid managed assembly.
        /// </summary>
        public static bool IsManagedAssembly (string filePath)
        {
            try
            {
                _ = AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to get the equivalent RuntimePlatform value from the given BuildTarget. Returns true if succeeded.
        /// </summary>
        public static bool TryGetRuntimePlatformFromBuildTarget(BuildTarget buildTarget, out RuntimePlatform runtimePlatform)
        {
            RuntimePlatform? platform = buildTarget switch
            {
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.PS4 => RuntimePlatform.PS4,
                BuildTarget.PS5 => RuntimePlatform.PS5,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.CloudRendering => RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneWindows => RuntimePlatform.WindowsPlayer,
                BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                BuildTarget.Switch => RuntimePlatform.Switch,
                BuildTarget.WSAPlayer => RuntimePlatform.WSAPlayerARM,
                BuildTarget.XboxOne => RuntimePlatform.XboxOne,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.tvOS => RuntimePlatform.tvOS,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.Lumin => RuntimePlatform.Lumin,
                BuildTarget.GameCoreXboxSeries => RuntimePlatform.GameCoreXboxSeries,
                BuildTarget.GameCoreXboxOne => RuntimePlatform.GameCoreXboxOne,
                BuildTarget.Stadia => RuntimePlatform.Stadia,
                BuildTarget.EmbeddedLinux => RuntimePlatform.EmbeddedLinuxArm64,
                _ => null
            };

            if (platform.HasValue)
            {
                runtimePlatform = platform.Value;
                return true;
            }
            
            runtimePlatform = default;
            return false;
        }
        
        /// <summary>
        /// Gets a unique key for the given asset creating a composition of the given key with the asset GUID.
        /// </summary>
        public static string GetUniqueKeyForAsset(Object asset, string key)
        {
            if (asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                return $"{guid}-{key}";
            
            return null;
        }
        
        public static ModTargetInfo CreateModTargetInfo(ModConfig config, BuildTarget buildTarget)
        {
            return new ModTargetInfo()
            {
                UnityVersion = Application.unityVersion,
                UniModVersion = UniModRuntime.Version,
                Platform = GetModTargetPlatform(buildTarget),
                HostId = string.IsNullOrEmpty(config.appId) ? null : config.appId,
                HostVersion = string.IsNullOrEmpty(config.appVersion) ? null : config.appVersion,
            };
        }

        public static string GetModTargetPlatform(BuildTarget buildTarget)
        {
            // try to get the build target's equivalent runtime platform value
            if (!TryGetRuntimePlatformFromBuildTarget(buildTarget, out RuntimePlatform runtimePlatform))
                throw new Exception($"Couldn't get the equivalent runtime platform value for the current active build target: {buildTarget}");
            
            return runtimePlatform.ToString();
        }
        
        /// <summary>
        /// Refreshes and saves all <see cref="EmbeddedModConfig"/> assets in the project that are currently linked to a <see cref="ModConfig"/> asset.
        /// <br/><br/>
        /// This method is run automatically when entering play mode and before doing a player build.
        /// </summary>
        [InitializeOnEnterPlayMode] // make sure all embedded configs are refreshed when entering play mode
        public static void RefreshAndSaveAllEmbeddedModConfigs()
        {
            const string modConfigFilter = "t:" + nameof(ModConfig);
            string[] configGuids = AssetDatabase.FindAssets(modConfigFilter);

            foreach (string guid in configGuids)
            {
                if (string.IsNullOrEmpty(guid))
                    continue;
                
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                
                var config = AssetDatabase.LoadAssetAtPath<ModConfig>(path);
                if (!config || !config.linkedEmbeddedConfig)
                    continue;
                
                config.SyncEmbeddedConfig(config.linkedEmbeddedConfig);
                AssetDatabase.SaveAssetIfDirty(config.linkedEmbeddedConfig);
            }
        }
    }
}