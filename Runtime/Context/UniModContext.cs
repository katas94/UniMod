using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Default UniMod mod context implementation.
    /// </summary>
    public sealed class UniModContext : IModContext
    {
        public IReadOnlyList<IMod> Mods { get; }
        public IReadOnlyCollection<IModLoadingInfo> AllLoadingInfo { get; }
        public IReadOnlyList<IModSource> Sources { get; }
        public string InstallationFolder { get; }

        private readonly List<IMod> _mods;
        private readonly Dictionary<string, IMod> _modsMap;
        private readonly ModLoader _loader;
        private readonly ILocalModInstaller _installer;
        private readonly ModSourceGroup _sources;
        private readonly IModCompatibilityChecker _compatibilityChecker;

        /// <summary>
        /// Creates a UniMod instance with a default configuration. You will probably want to use this in most cases.
        /// </summary>
        public static UniModContext CreateDefaultContext(string targetId, string targetVersion)
        {
            string installationFolder = UniMod.LocalInstallationFolder;
            var installer = new LocalModInstaller(installationFolder);
            var compatibilityChecker = new ModCompatibilityChecker(targetId, targetVersion);
            var context = new UniModContext(installer, compatibilityChecker);
            var localModSource = new LocalModSource(context, installationFolder);
            context.AddSource(localModSource);
            
            return context;
        }
        
        /// <summary>
        /// Creates a UniMod instance with a default configuration and defining your own compatibility checker. You will probably want to use this in most cases.
        /// </summary>
        public static UniModContext CreateDefaultContext(IModCompatibilityChecker compatibilityChecker)
        {
            string installationFolder = UniMod.LocalInstallationFolder;
            var installer = new LocalModInstaller(installationFolder);
            var context = new UniModContext(installer, compatibilityChecker);
            var localModSource = new LocalModSource(context, installationFolder);
            context.AddSource(localModSource);
            
            return context;
        }

        public UniModContext(ILocalModInstaller installer, IModCompatibilityChecker compatibilityChecker)
            : this(installer, compatibilityChecker, Array.Empty<IModSource>()) { }

        public UniModContext(ILocalModInstaller installer, IModCompatibilityChecker compatibilityChecker, params IModSource[] sources)
            : this(installer, compatibilityChecker, sources as IEnumerable<IModSource>) { }
        
        public UniModContext(ILocalModInstaller installer, IModCompatibilityChecker compatibilityChecker, IEnumerable<IModSource> sources)
        {
            _mods = new List<IMod>();
            _modsMap = new Dictionary<string, IMod>();
            _loader = new ModLoader();
            _installer = installer;
            _sources = new ModSourceGroup(sources);
            InstallationFolder = _installer.InstallationFolder;
            
            Mods = _mods.AsReadOnly();
            AllLoadingInfo = _loader.AllLoadingInfo;
            _compatibilityChecker = compatibilityChecker;
            Sources = _sources.Sources;
        }
        
        public IMod GetMod(string modId)
        {
            return _modsMap.TryGetValue(modId, out IMod mod) ? mod : null;
        }

        public async UniTask RefreshAsync()
        {
            await RefreshLocalInstallations(); // installs and deletes any mod files added in the installation folder
            await FetchFromAllSources();
            
            // get all mods from the sources after the fetch and reconstruct the map
            _mods.Clear();
            _modsMap.Clear();

            try
            {
                await _sources.GetAllModsAsync(_mods);
            }
            catch (Exception exception)
            {
                throw new Exception("[UniModContext] there were some errors while trying to get mods from the sources", exception);
            }
            finally
            {
                foreach (IMod mod in _mods)
                    if (mod is not null)
                        _modsMap[mod.Info.Id] = mod;
                
                // set the mods to the loader so it properly updates the loading information (dependency graphs, etc)
                _loader.SetMods(_mods);
            }
        }

        #region WRAPPERS
        // mod loader
        public IModLoadingInfo GetLoadingInfo(IMod mod)
            => _loader.GetLoadingInfo(mod);
        public IModLoadingInfo GetLoadingInfo(string modId)
            => _loader.GetLoadingInfo(modId);
        public UniTask<bool> TryLoadAllModsAsync()
            => _loader.TryLoadAllModsAsync();
        public UniTask<bool> TryLoadModsAndDependenciesAsync(params IMod[] mods)
            => _loader.TryLoadModsAndDependenciesAsync(mods);
        public UniTask<bool> TryLoadModsAndDependenciesAsync(IEnumerable<IMod> mods)
            => _loader.TryLoadModsAndDependenciesAsync(mods);
        public UniTask<bool> TryLoadModsAndDependenciesAsync(params string[] modIds)
            => _loader.TryLoadModsAndDependenciesAsync(modIds);
        public UniTask<bool> TryLoadModsAndDependenciesAsync(IEnumerable<string> modIds)
            => _loader.TryLoadModsAndDependenciesAsync(modIds);
        public UniTask<bool> TryLoadModAndDependenciesAsync(IMod mod)
            => _loader.TryLoadModAndDependenciesAsync(mod);
        public UniTask<bool> TryLoadModAndDependenciesAsync(string modId)
            => _loader.TryLoadModAndDependenciesAsync(modId);

        // local mod installer
        public UniTask DownloadAndInstallModsAsync(IEnumerable<string> modUrls, CancellationToken cancellationToken = default)
            => _installer.DownloadAndInstallModsAsync(modUrls, cancellationToken);
        public UniTask DownloadAndInstallModAsync(string modUrl, CancellationToken cancellationToken = default, IProgress<float> progress = null)
            => _installer.DownloadAndInstallModAsync(modUrl, cancellationToken, progress);
        public UniTask InstallModsAsync(string folderPath, bool deleteModFilesAfter = false)
            => _installer.InstallModsAsync(folderPath, deleteModFilesAfter);
        public UniTask InstallModsAsync(IEnumerable<string> modFilePaths, bool deleteModFilesAfter = false)
            => _installer.InstallModsAsync(modFilePaths, deleteModFilesAfter);
        public UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter = false)
            => _installer.InstallModAsync(modFilePath, deleteModFileAfter);
        public UniTask InstallModAsync(byte[] modBuffer)
            => _installer.InstallModAsync(modBuffer);
        
        // mod source group
        public bool AddSource(IModSource source)
            => _sources.AddSource(source);
        public bool AddSource(IModSource source, int insertAtIndex)
            => _sources.AddSource(source, insertAtIndex);
        public void AddSources(IEnumerable<IModSource> sources)
            => _sources.AddSources(sources);
        public void AddSources(IEnumerable<IModSource> sources, int insertAtIndex)
            => _sources.AddSources(sources, insertAtIndex);
        public bool RemoveSource(IModSource source)
            => _sources.RemoveSource(source);
        public void RemoveSourceAt(int index)
            => _sources.RemoveSourceAt(index);
        public void RemoveSources(IEnumerable<IModSource> sources)
            => _sources.RemoveSources(sources);
        public void ClearSources()
            => _sources.ClearSources();
        
        // compatibility checker
        public ModIncompatibilities GetIncompatibilities(ModTargetInfo target)
            => _compatibilityChecker.GetIncompatibilities(target);
        public bool IsCompatible(ModTargetInfo target, out ModIncompatibilities incompatibilities)
            => _compatibilityChecker.IsCompatible(target, out incompatibilities);
        public bool IsCompatible(ModTargetInfo target)
            => _compatibilityChecker.IsCompatible(target);
        #endregion

        private async UniTask RefreshLocalInstallations()
        {
            try
            {
                await _installer.InstallModsAsync(InstallationFolder, true);
            }
            catch (Exception exception)
            {
                throw new Exception("[UniModContext] there were some errors while trying to refresh the local installation folder", exception);
            }
        }
        
        private async UniTask FetchFromAllSources()
        {
            try
            {
                await _sources.FetchAsync();
            }
            catch (Exception exception)
            {
                throw new Exception("[UniModContext] there were some errors while trying to fetch from mod sources", exception);
            }
        }
    }
}