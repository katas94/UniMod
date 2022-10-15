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
    public sealed class LocalModSource : IModSource
    {
        public readonly string InstallationFolder;
        
        private readonly HashSet<string> _modIds;
        private readonly Dictionary<string, LocalModLoader> _loaders;

        public LocalModSource(string installationFolder)
        {
            InstallationFolder = installationFolder;
            _modIds = new HashSet<string>();
            _loaders = new Dictionary<string, LocalModLoader>();
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

        public async UniTask<IModLoader> GetLoaderAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new Exception("Null or empty mod ID");
            if (!_modIds.Contains(id))
                throw new Exception($"Couldn't find mod ID {id}");
            
            if (_loaders.TryGetValue(id, out LocalModLoader mod))
                return mod;

            try
            {
                mod = await CreateLocalModAsync(id);
                _loaders[id] = mod;
                return mod;
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to get mod with ID: {id}", exception);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        public async UniTask GetLoadersAsync(IEnumerable<string> ids, ICollection<IModLoader> results)
        {
            (IModLoader[] mods, Exception exception) = await UniTaskUtility.WhenAllNoThrow(ids.Select(GetLoaderAsync));
            
            foreach (IModLoader mod in mods)
                if (mod is not null)
                    results.Add(mod);
            
            if (exception is not null)
                throw exception;
        }

        public UniTask GetAllLoadersAsync(ICollection<IModLoader> results)
        {
            return GetLoadersAsync(_modIds, results);
        }
        
        // Tries to create and return a LocalMod instance from the mod on the installation folder matching the given ID.
        // This method runs, returns and throws on a background thread.
        private async UniTask<LocalModLoader> CreateLocalModAsync(string modId)
        {
            string modFolder = Path.Combine(InstallationFolder, modId);
            if (!Directory.Exists(modFolder))
                throw new Exception($"Mod is not installed, directory not found: {modFolder}");
            
            await UniTask.SwitchToThreadPool();
            
            // check if the info file exists
            string infoPath = Path.Combine(modFolder, UniMod.InfoFile);
            if (!File.Exists(infoPath))
                throw new Exception($"Could not find \"{infoPath}\"");

            try
            {
                // try to read and parse the info file
                string json = await File.ReadAllTextAsync(infoPath);
                var info = JsonConvert.DeserializeObject<ModInfo>(json);
                
                // instantiate and register the mod instance
                return new LocalModLoader(modFolder, info);
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to parse mod information", exception);
            }
        }
    }
}