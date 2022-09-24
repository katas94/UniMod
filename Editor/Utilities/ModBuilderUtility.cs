using System;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Some static utility methods for building a mod.
    /// </summary>
    public static class ModBuilderUtility
    {
        /// <summary>
        /// Tries to copy the given managed assembly path into the given output folder. If specified, it will also
        /// try to copy the pdb file (if any).
        /// </summary>
        public static async UniTask CopyManagedAssemblyToOutputFolder (string dllSrcPath, string outputFolder, bool tryCopyPdbToo)
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
    }
}