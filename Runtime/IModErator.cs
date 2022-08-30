using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.Modman
{
    public interface IModErator
    {
        IEnumerable<string> InstalledModIds { get; }
        IEnumerable<IMod> InstalledMods { get; }
        string InstallationFolder { get; set; } // implementer should have a default path so callers are not obliged to set one
        
        UniTask InstallModsAsync();
        UniTask InstallModsAsync(string folderPath, bool deleteModFilesAfter);
        UniTask InstallModsAsync(IEnumerable<string> modFilePaths, bool deleteModFilesAfter);
        UniTask InstallModsAsync(bool deleteModFilesAfter, params string[] modFilePaths);
        UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter);
        UniTask UninstallModAsync(string id);
        UniTask RefreshInstalledModsAsync();
        UniTask<bool> TryLoadAllModsContentAsync(List<string> failedModIds);
        UniTask<bool> TryLoadAllModsAssembliesAsync(List<string> failedModIds);
        UniTask<bool> TryLoadAllModsAsync(List<string> failedModIds);
        IMod GetInstalledMod(string id);
    }
}