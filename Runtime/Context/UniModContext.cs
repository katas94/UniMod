using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Default UniMod context implementation that can be instantiated with any implementation of a mod host and local mod installer.
    /// </summary>
    public sealed class UniModContext : IUniModContext
    {
        private readonly IModHost _host;
        private readonly ILocalModInstaller _installer;
        private readonly ModClosure _closure;
        private readonly ModSourceGroup _sources;
        private readonly List<IModLoader> _loaders;
        
        private UniTaskCompletionSource _refreshingOperation;

        public UniModContext(IModHost host, ILocalModInstaller installer)
            : this(host, installer, Array.Empty<IModSource>()) { }

        public UniModContext(IModHost host, ILocalModInstaller installer, params IModSource[] sources)
            : this(host, installer, sources as IEnumerable<IModSource>) { }
        
        public UniModContext(IModHost host, ILocalModInstaller installer, IEnumerable<IModSource> sources)
        {
            _host = host;
            _installer = installer;
            
            // we use this specific implementation of mod closure because it allows us to rebuild the closure when refreshing the context
            _closure = new ModClosure(_host);
            // we use this specific implementation of mod source because it allows us to group multiple sources and treat them as one
            _sources = new ModSourceGroup(sources);
            
            _loaders = new List<IModLoader>();
        }

        public async UniTask RefreshAsync()
        {
            // ensure that multiple close calls to refresh results in one refresh
            if (_refreshingOperation is not null)
            {
                await _refreshingOperation.Task;
                return;
            }
            
            UniTaskCompletionSource operation = _refreshingOperation = new UniTaskCompletionSource();

            try
            {
                await RefreshLocalInstallations(); // installs any mod files added in the installation folder (and deletes the file if succeeded)
                await FetchFromAllSources();

                // get all loaders from the sources
                try
                {
                    _loaders.Clear();
                    await _sources.GetAllLoadersAsync(_loaders);
                }
                catch (Exception exception)
                {
                    throw new Exception("[UniModContext] something went wrong while trying to get the mod loaders from the sources", exception);
                }
                finally
                {
                    // rebuild mod closure with the loaders that were instantiated successfully
                    _closure.RebuildClosure(_loaders);
                }
            }
            catch (Exception exception)
            {
                _refreshingOperation = null;
                operation.TrySetException(exception);
                throw;
            }
            
            _refreshingOperation = null;
            operation.TrySetResult();
        }
        
        private async UniTask RefreshLocalInstallations()
        {
            try
            {
                await _installer.InstallModsAsync(InstallationFolder, true);
            }
            catch (Exception exception)
            {
                throw new Exception("[UniModContext] something went wrong while trying to refresh the local installation folder", exception);
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
                throw new Exception("[UniModContext] something went wrong while trying to fetch from mod sources", exception);
            }
        }

#region WRAPPERS
        // mod host
        public string Id
            => _host.Id;
        public string Version
            => _host.Version;
        public ModIssues GetModIssues(IMod mod)
            => _host.GetModIssues(mod);
        public bool IsModSupported(IMod mod, out ModIssues issues)
            => _host.IsModSupported(mod, out issues);
        
        // mod closure
        public IReadOnlyCollection<IMod> Mods
            => _closure.Mods;
        public IMod GetMod(string id)
            => _closure.GetMod(id);
        public UniTask<bool> TryLoadAllModsAsync()
            => _closure.TryLoadAllModsAsync();
        public UniTask<bool> TryLoadModsAsync(params string[] ids)
            => _closure.TryLoadModsAsync(ids);
        public UniTask<bool> TryLoadModsAsync(IEnumerable<string> ids)
            => _closure.TryLoadModsAsync(ids);
        public UniTask<bool> TryLoadModAsync(string id)
            => _closure.TryLoadModAsync(id);

        // local mod installer
        public string InstallationFolder
            => _installer.InstallationFolder;
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
        public IReadOnlyList<IModSource> Sources
            => _sources.Sources;
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
    }
}