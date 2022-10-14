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
        public IModdableApplication Application { get; }
        public IReadOnlyList<IMod> Mods { get; }
        public IReadOnlyCollection<IModStatus> Statuses { get; }
        public IReadOnlyList<IModSource> Sources { get; }
        public string InstallationFolder => _installer.InstallationFolder;

        private readonly ILocalModInstaller _installer;
        private readonly ModLoadingContext _loadingContext;
        private readonly ModSourceGroup _sources;
        private readonly List<IMod> _mods;
        private readonly Dictionary<string, IMod> _modsMap;

        /// <summary>
        /// Creates a default UniMod context for the specified application parameters.
        /// </summary>
        public static UniModContext CreateDefaultContext(string appId, string appVersion)
        {
            return CreateDefaultContext(new ModdableApplication(appId, appVersion));
        }
        
        /// <summary>
        /// Creates a default UniMod context for the specified application.
        /// </summary>
        public static UniModContext CreateDefaultContext(IModdableApplication application)
        {
            string installationFolder = UniMod.LocalInstallationFolder;
            var installer = new LocalModInstaller(installationFolder);
            var localModSource = new LocalModSource(installationFolder);
            var context = new UniModContext(application, installer, localModSource);
            
            return context;
        }

        public UniModContext(IModdableApplication application, ILocalModInstaller installer)
            : this(application, installer, Array.Empty<IModSource>()) { }

        public UniModContext(IModdableApplication application, ILocalModInstaller installer, params IModSource[] sources)
            : this(application, installer, sources as IEnumerable<IModSource>) { }
        
        public UniModContext(IModdableApplication application, ILocalModInstaller installer, IEnumerable<IModSource> sources)
        {
            _installer = installer;
            _loadingContext = new ModLoadingContext(this);
            _sources = new ModSourceGroup(sources);
            _mods = new List<IMod>();
            _modsMap = new Dictionary<string, IMod>();
            
            Application = application;
            Mods = _mods.AsReadOnly();
            Statuses = _loadingContext.Statuses;
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
                
                // rebuild the mod loading context
                _loadingContext.RebuildContext(_mods);
            }
        }

        #region WRAPPERS
        // mod loading context
        public IModStatus GetStatus(IMod mod)
            => _loadingContext.GetStatus(mod);
        public IModStatus GetStatus(string modId)
            => _loadingContext.GetStatus(modId);
        public UniTask<bool> TryLoadAllModsAsync()
            => _loadingContext.TryLoadAllModsAsync();
        public UniTask<bool> TryLoadModsAsync(params IMod[] mods)
            => _loadingContext.TryLoadModsAsync(mods);
        public UniTask<bool> TryLoadModsAsync(IEnumerable<IMod> mods)
            => _loadingContext.TryLoadModsAsync(mods);
        public UniTask<bool> TryLoadModsAsync(params string[] modIds)
            => _loadingContext.TryLoadModsAsync(modIds);
        public UniTask<bool> TryLoadModsAsync(IEnumerable<string> modIds)
            => _loadingContext.TryLoadModsAsync(modIds);
        public UniTask<bool> TryLoadModAsync(IMod mod)
            => _loadingContext.TryLoadModAsync(mod);
        public UniTask<bool> TryLoadModAsync(string modId)
            => _loadingContext.TryLoadModAsync(modId);

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