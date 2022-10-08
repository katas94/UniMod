using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Official IModContext implementation.
    /// </summary>
    public class UniModContext : IModContext
    {
        public IReadOnlyList<IMod> Mods { get; }
        public IReadOnlyList<IModSource> Sources { get; }
        public string InstallationFolder { get; }

        private readonly List<IMod> _mods;
        private readonly Dictionary<string, IMod> _modsMap;
        private readonly ILocalModInstaller _installer;
        private readonly ModSourceGroup _sources;

        /// <summary>
        /// Creates a UniMod instance with a default configuration. You will probably want to use this in most cases.
        /// </summary>
        public static UniModContext CreateDefaultContext()
        {
            string installationFolder = UniModSpecification.LocalInstallationFolder;
            var installer = new LocalModInstaller(installationFolder);
            var context = new UniModContext(installer);
            var localModSource = new LocalModSource(context, installationFolder);
            context.AddSource(localModSource);
            
            return context;
        }

        public UniModContext(ILocalModInstaller installer)
            : this(installer, Array.Empty<IModSource>()) { }

        public UniModContext(ILocalModInstaller installer, params IModSource[] sources)
            : this(installer, sources as IEnumerable<IModSource>) { }
        
        public UniModContext(ILocalModInstaller installer, IEnumerable<IModSource> sources)
        {
            _mods = new List<IMod>();
            _modsMap = new Dictionary<string, IMod>();
            _installer = installer;
            _sources = new ModSourceGroup(sources);
            InstallationFolder = _installer.InstallationFolder;
            
            Mods = _mods.AsReadOnly();
            Sources = _sources.Sources;
        }
        
        public IMod GetMod(string modId)
        {
            return _modsMap.TryGetValue(modId, out IMod mod) ? mod : null;
        }

        public async UniTask RefreshAsync()
        {
            // install and delete any mod files added in the installation folder
            await _installer.InstallModsAsync(InstallationFolder, true);
            // fetch from all sources
            await _sources.FetchAsync();
            
            // get all mods from the sources after the fetch and reconstruct the map
            _mods.Clear();
            _modsMap.Clear();
            await _sources.GetAllModsAsync(_mods);
            
            foreach (IMod mod in _mods)
                _modsMap[mod.Info.ModId] = mod;
        }

        #region WRAPPERS
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
        #endregion
    }
}