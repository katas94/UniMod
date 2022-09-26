using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModInstaller
    {
        /// <summary>
        /// Installs all the mods found under a folder. Any previous installations will be overwritten.
        /// </summary>
        /// <param name="folderPath">The root folder where multiple mod files are located</param>
        /// <param name="deleteModFilesAfter">If true, all found mod files will be deleted after installation</param>
        UniTask InstallModsAsync(string folderPath, bool deleteModFilesAfter = false);
        
        /// <summary>
        /// Installs all the mods. Any previous installations will be overwritten.
        /// </summary>
        /// <param name="modFilePaths">The paths to multiple mod files to install</param>
        /// <param name="deleteModFilesAfter">If true, all mod files will be deleted after installation</param>
        UniTask InstallModsAsync(IEnumerable<string> modFilePaths, bool deleteModFilesAfter = false);
        
        /// <summary>
        /// Installs the mod. Any previous installation of the same mod will be overwritten.
        /// </summary>
        /// <param name="modFilePath">The path to the mod file</param>
        /// <param name="deleteModFileAfter">If true, the mod file will be deleted after installation</param>
        UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter = false);
        
        /// <summary>
        /// Installs the loaded mod buffer. Any previous installation of the same mod will be overwritten.
        /// </summary>
        /// <param name="modBuffer">The mod archive file loaded as an array of bytes</param>
        UniTask InstallModAsync(byte[] modBuffer);
    }
}