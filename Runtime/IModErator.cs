using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.Mango
{
    public interface IModErator
    {
        IEnumerable<string> InstalledModIds { get; }
        IEnumerable<IMod> InstalledMods { get; }
        string InstallationFolder { get; set; } // implementer should have a default path so callers are not obliged to set one
        
        UniTask RefreshInstallationFolderAsync();
        
        UniTask InstallModsAsync(string folderPath, bool deleteModFilesAfter);
        UniTask InstallModsAsync(IEnumerable<string> modFilePaths, bool deleteModFilesAfter);
        UniTask InstallModsAsync(bool deleteModFilesAfter, params string[] modFilePaths);
        UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter);
        UniTask UninstallModAsync(string id);
        
        UniTask<bool> TryLoadAllModsAsync(bool loadAssemblies, List<string> failedModIds);
        IMod GetMod(string id);
    }
}