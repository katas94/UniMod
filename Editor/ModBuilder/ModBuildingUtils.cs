using System;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Katas.Mango.Editor
{
    public static class ModBuildingUtils
    {
        /// <summary>
        /// Tries to copy the given assembly into the given output folder. If the build mode is Debug, then it will also
        /// try to copy the pdb file (if any).
        /// </summary>
        public static async UniTask CopyAssemblyToOutputFolder (string path, string outputFolder, CodeOptimization buildMode)
        {
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
                throw new Exception("The given output folder is null/empty or it does not exist");
            if (string.IsNullOrEmpty(path))
                throw new Exception("The given assembly path is null or empty");
            if (!File.Exists(path))
                throw new FileNotFoundException($"Could not find the assembly file at \"{path}\"");
            
            await UniTask.SwitchToThreadPool();

            try
            {
                // get the src/dst paths for both the assembly and its pdb (debugging) file
                string fileName = Path.GetFileName(path);
                string dllSrcPath = path;
                string dllDestPath = Path.Combine(outputFolder, fileName);
                string pdbSrcPath = Path.ChangeExtension(dllSrcPath, ".pdb");
                string pdbDestPath = Path.ChangeExtension(dllDestPath, ".pdb");

                // check if the assembly is a valid net managed assembly
                if (!IsManagedAssembly(dllSrcPath))
                    throw new Exception($"\"{dllSrcPath}\" is not a managed assembly");

                File.Copy(dllSrcPath, dllDestPath, true);

                // copy pdb file only if in debug mode and the file exists. Some assemblies may not come with a pdb file so we are not requiring it
                if (buildMode == CodeOptimization.Debug && File.Exists(pdbSrcPath))
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