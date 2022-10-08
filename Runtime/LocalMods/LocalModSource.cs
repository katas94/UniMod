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
        public readonly IModContext Context;
        public readonly string InstallationFolder;
        
        private readonly HashSet<string> _modIds;
        private readonly Dictionary<string, LocalMod> _mods;

        public LocalModSource(IModContext context, string installationFolder)
        {
            Context = context;
            InstallationFolder = installationFolder;
            _modIds = new HashSet<string>();
            _mods = new Dictionary<string, LocalMod>();
        }
        
        public UniTask FetchAsync()
        {
            if (!Directory.Exists(InstallationFolder))
                return UniTask.CompletedTask;
            
            _modIds.Clear();
            string[] modFolders = Directory.GetDirectories(InstallationFolder);

            foreach (string modFolder in modFolders)
            {
                string modId = Path.GetFileName(modFolder);
                if (!string.IsNullOrEmpty(modId))
                    _modIds.Add(modId);
            }

            return UniTask.CompletedTask;
        }

        public UniTask GetAllIdsAsync(ICollection<string> results)
        {
            foreach (string modId in _modIds)
                results.Add(modId);
            
            return UniTask.CompletedTask;
        }

        public async UniTask<IMod> GetModAsync(string modId)
        {
            if (string.IsNullOrEmpty(modId))
                throw new Exception("Null or empty mod ID");
            if (!_modIds.Contains(modId))
                throw new Exception($"Couldn't find mod ID {modId}");
            
            if (_mods.TryGetValue(modId, out LocalMod mod))
                return mod;

            try
            {
                mod = await CreateLocalModAsync(modId);
                _mods[modId] = mod;
                return mod;
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to get mod with ID: {modId}", exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        public async UniTask GetModsAsync(IEnumerable<string> modIds, ICollection<IMod> results)
        {
            (IMod[] mods, Exception exception) = await UniTaskUtility.WhenAllNoThrow(modIds.Select(GetModAsync));
            
            foreach (IMod mod in mods)
                if (mod is not null)
                    results.Add(mod);
            
            if (exception is not null)
                throw exception;
        }

        public UniTask GetAllModsAsync(ICollection<IMod> results)
        {
            return GetModsAsync(_modIds, results);
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
                return new LocalMod(Context, modFolder, info);
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to parse mod information", exception);
            }
        }
    }
}