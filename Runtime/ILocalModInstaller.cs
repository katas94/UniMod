using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Provides different methods to install mods locally.
    /// </summary>
    public interface ILocalModInstaller
    {
        /// <summary>
        /// The folder were all the mods are installed to.
        /// </summary>
        string InstallationFolder { get; }
        
        /// <summary>
        /// Downloads and installs all the mods provided by the given URLs.
        /// </summary>
        UniTask DownloadAndInstallModsAsync(IEnumerable<string> modUrls, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Downloads and installs the mod provided by the given URL.
        /// </summary>
        UniTask DownloadAndInstallModAsync(string modUrl, CancellationToken cancellationToken = default, IProgress<float> progress = null);

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