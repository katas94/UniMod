using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Katas.UniMod
{
    public class UniModRegistry : IModRegistry
    {
        public int ModCount => _mods.Count;
        public IEnumerable<string> ModIds => _mods.Keys;
        public IEnumerable<IMod> Mods => _mods.Values;
        
        public readonly string InstallationFolder;

        private readonly Dictionary<string, IMod> _mods = new();

        public UniModRegistry(string installationFolder)
        {
            InstallationFolder = installationFolder;
        }

        public IMod GetMod(string modId)
        {
            return _mods.TryGetValue(modId, out IMod mod) ? mod : null;
        }

        public bool TryRegisterMod(IMod mod, out string error)
        {
            if (mod is null)
            {
                error = "The mod is null";
                return false;
            }
            
            string modId = mod.Info.ModId;

            if (string.IsNullOrEmpty(modId))
            {
                error = "The mod ID is null or empty";
                return false;
            }

            if (_mods.ContainsKey(modId))
            {
                error = $"A mod with ID {modId} is already registered";
                return false;
            }
            
            error = null;
            _mods[modId] = mod;
            return true;
        }

        public bool TryUnregisterMod(string modId, out string error)
        {
            if (string.IsNullOrEmpty(modId))
            {
                error = "The mod ID is null or empty";
                return false;
            }

            if (!_mods.TryGetValue(modId, out IMod mod))
            {
                error = $"No mod is registered with the ID {modId}";
                return false;
            }

            if (mod.IsLoaded)
            {
                error = $"The mod with ID {modId} is currently loaded";
                return false;
            }
            
            error = null;
            _mods.Remove(modId);
            return true;
        }

        public async UniTask RefreshAsync()
        {
            if (!Directory.Exists(InstallationFolder))
                return;
            
            // get all mod folders from the installation folder and create the mod instances from them
            string[] modFolders = Directory.GetDirectories(InstallationFolder);
            var exceptions = new List<Exception>(modFolders.Length);
            IMod[] mods = await UniTask.WhenAll
            (
                modFolders.Select
                (
                    modFolder => CreateModNoThrowAsync(modFolder, exceptions)
                )
            );
            
            // register the newly created mods
            foreach (IMod mod in mods)
                if (mod is not null)
                    _mods[mod.Info.ModId] = mod;
            
            // TODO: resolve dependency tree
            
            // make sure that callers stay on the main thread after awaiting this
            await UniTask.SwitchToMainThread();
            
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        private async UniTask<IMod> CreateModNoThrowAsync(string modFolder, ICollection<Exception> exceptions)
        {
            try
            {
                return await CreateModAsync(modFolder);
            }
            catch (Exception exception)
            {
                exceptions.Add(new Exception($"Failed to create mod from: {modFolder}", exception));
                return null;
            }
        }

        /// <summary>
        /// Given a valid mod folder that matches the UniMod specification (it must be named with the mod's ID and contain the mod's information file),
        /// it will create and return an IMod instance. This method runs and returns on a separated thread.
        /// </summary>
        private async UniTask<IMod> CreateModAsync(string modFolder)
        {
            await UniTask.SwitchToThreadPool();
            
            string modId = Path.GetFileName(modFolder);
            if (string.IsNullOrEmpty(modId))
                throw new Exception($"Could not parse the mod ID from: {modFolder}");
            
            // do nothing if the instance already exists
            if (_mods.ContainsKey(modId))
                return null;
            
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