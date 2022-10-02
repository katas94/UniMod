using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Katas.UniMod
{
    /// <summary>
    /// Mod source implementation for mods installed under a local folder.
    /// </summary>
    public class LocalModSource : IModSource
    {
        public readonly string InstallationFolder;

        public LocalModSource(string installationFolder)
        {
            InstallationFolder = installationFolder;
        }
        
        public UniTask FetchIdsAsync(ICollection<string> modIds)
        {
            if (!Directory.Exists(InstallationFolder))
                return UniTask.CompletedTask;
            
            string[] modFolders = Directory.GetDirectories(InstallationFolder);

            foreach (string modFolder in modFolders)
            {
                string modId = Path.GetFileName(modFolder);
                if (!string.IsNullOrEmpty(modId))
                    modIds.Add(modId);
            }
            
            return UniTask.CompletedTask;
        }

        public async UniTask<IMod> FetchModAsync(string modId)
        {
            if (!Directory.Exists(InstallationFolder))
                throw new Exception($"No installation directory found: {InstallationFolder}");
            if (string.IsNullOrEmpty(modId))
                throw new Exception("Null or empty mod ID");

            try
            {
                return await CreateLocalModAsync(modId);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to fetch mod with ID: {modId}", exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        public async UniTask FetchModsAsync(IEnumerable<string> modIds, ICollection<IMod> results)
        {
            if (!Directory.Exists(InstallationFolder))
                throw new Exception($"No installation directory found: {InstallationFolder}");
            
            (IMod[] mods, Exception exception) = await UniTaskUtility.WhenAllNoThrow(modIds.Select(FetchModAsync));
            
            foreach (IMod mod in mods)
                if (mod is not null)
                    results.Add(mod);
            
            if (exception is not null)
                throw exception;
        }

        public async UniTask FetchAllModsAsync(ICollection<IMod> results)
        {
            if (!Directory.Exists(InstallationFolder))
                return;
            
            List<string> modIds = GlobalPool<List<string>>.Pick();

            try
            {
                await FetchIdsAsync(modIds);
                await FetchModsAsync(modIds, results);
            }
            finally
            {
                modIds.Clear();
                GlobalPool<List<string>>.Release(modIds);
            }
        }

        // Tries to create and return a LocalMod instance from the mod on the installation folder matching the given ID.
        // This method runs, returns and throws on a background thread.
        private async UniTask<LocalMod> CreateLocalModAsync(string modId)
        {
            string modFolder = Path.Combine(InstallationFolder, modId);
            if (!Directory.Exists(modFolder))
                throw new Exception($"Mod is not installed, directory not found: {modFolder}");
            
            await UniTask.SwitchToThreadPool();
            
            // check if the info file exists
            string infoPath = Path.Combine(modFolder, UniModSpecification.InfoFile);
            if (!File.Exists(infoPath))
                throw new Exception($"Could not find \"{infoPath}\"");

            try
            {
                // try to read and parse the info file
                string json = await File.ReadAllTextAsync(infoPath);
                var info = JsonConvert.DeserializeObject<ModInfo>(json);
                
                // instantiate and register the mod instance
                return new LocalMod(modFolder, info);
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to parse mod information", exception);
            }
        }
    }
}