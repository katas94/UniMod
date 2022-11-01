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
        public string ApplicationId => _application.Id;
        public string ApplicationVersion => _application.Version;
        public IReadOnlyCollection<IMod> Mods => _closure.Mods;
        public IReadOnlyList<IModSource> Sources => _sources.Sources;
        public string InstallationFolder => _installer.InstallationFolder;

        private readonly IModdableApplication _application;
        private readonly ILocalModInstaller _installer;
        private readonly ModClosure _closure;
        private readonly ModSourceGroup _sources;
        private readonly List<IModLoader> _loaders;
        
        private UniTaskCompletionSource _refreshingOperation;

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
            _application = application;
            _installer = installer;
            _closure = new ModClosure(this, _application);
            _sources = new ModSourceGroup(sources);
            _loaders = new List<IModLoader>();
        }

        public async UniTask RefreshAsync()
        {
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
                    throw new Exception(
                        "[UniModContext] there were some errors while trying to get the mod loaders from the sources",
                        exception);
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

#region WRAPPERS
        // mod loading context
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
    }
}