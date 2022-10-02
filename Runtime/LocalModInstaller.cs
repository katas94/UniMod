using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Katas.UniMod
{
    public class LocalModInstaller : ILocalModInstaller
    {
        public readonly string InstallationFolder;

        public LocalModInstaller(string installationFolder)
        {
            InstallationFolder = installationFolder;
            
            if (!Directory.Exists(InstallationFolder))
                Directory.CreateDirectory(InstallationFolder);
        }

        public async UniTask DownloadAndInstallModsAsync(IEnumerable<string> modUrls, CancellationToken cancellationToken = default)
        {
            // prepare all the install tasks for concurrent execution
            List<UniTask> tasks = GlobalPool<List<UniTask>>.Pick();
            tasks.Clear();
            IEnumerator<string> enumerator = modUrls.GetEnumerator();
            while(enumerator.MoveNext())
                tasks.Add(DownloadAndInstallModAsync(enumerator.Current, cancellationToken));
            enumerator.Dispose();

            try
            {
                await UniTaskUtility.WhenAll(tasks);
            }
            finally
            {
                tasks.Clear();
                GlobalPool<List<UniTask>>.Release(tasks);
            }
        }

        public async UniTask DownloadAndInstallModAsync(string modUrl, CancellationToken cancellationToken = default, IProgress<float> progress = null)
        {
            if (string.IsNullOrEmpty(modUrl))
                throw new Exception("Null or empty mod URL");
            
            UnityWebRequest request = await UnityWebRequest.Get(modUrl)
                .SendWebRequest().ToUniTask(cancellationToken: cancellationToken, progress: progress);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!string.IsNullOrEmpty(request.error))
                throw new Exception($"Failed to download mod from URL: {modUrl}\n{nameof(UnityWebRequest)} error: {request.error}");
            
            try
            {
                await InstallModAsync(request.downloadHandler.data);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to install mod from URL: {modUrl}", exception);
            }
        }

        public UniTask InstallModsAsync(string folderPath, bool deleteModFilesAfter = false)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new Exception("Null or empty folder path");
            
            string[] modFilePaths = Directory.GetFiles(folderPath);
            return InstallModsAsync(modFilePaths, deleteModFilesAfter);
        }

        public async UniTask InstallModsAsync(IEnumerable<string> modFilePaths, bool deleteModFilesAfter = false)
        {
            // prepare all the install tasks for concurrent execution
            List<UniTask> tasks = GlobalPool<List<UniTask>>.Pick();
            tasks.Clear();
            IEnumerator<string> enumerator = modFilePaths.GetEnumerator();
            while(enumerator.MoveNext())
                tasks.Add(InstallModAsync(enumerator.Current, deleteModFilesAfter));
            enumerator.Dispose();

            try
            {
                await UniTaskUtility.WhenAll(tasks);
            }
            finally
            {
                tasks.Clear();
                GlobalPool<List<UniTask>>.Release(tasks);
            }
        }

        public async UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter = false)
        {
            if (string.IsNullOrEmpty(modFilePath))
                throw new Exception("Null or empty mod file path");

            try
            {
                // load the mod file bytes and install the mod from its buffer.
                // we don't need to check if the file's extension is the one from the UniMod specification,
                // as long as the mod is a compressed archive file we will be able to install it
                byte[] modBuffer = await File.ReadAllBytesAsync(modFilePath);
                await InstallModAsync(modBuffer);
            }
            catch (Exception exception)
            {
                // wrap any exceptions to provide more information
                throw new Exception($"Failed to install mod from file: {modFilePath}", exception);
            }
            
            if (!deleteModFileAfter)
                return;
            
            // try to delete the mod file after but don't throw if it fails
            try { File.Delete(modFilePath); }
            catch (Exception exception) { Debug.LogWarning($"Could not delete mod file after installation: {modFilePath}\n{exception}"); }
        }

        public async UniTask InstallModAsync(byte[] modBuffer)
        {
            if (modBuffer is null)
                throw new Exception("Null mod buffer");
            
            // install the mod on a separated thread
            await UniTask.SwitchToThreadPool();

            try
            {
                // open the mod archive and fetch the mod id by reading the name of the root folder
                using var memoryStream = new MemoryStream(modBuffer);
                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
                string firstEntryFullName = archive.Entries.FirstOrDefault()?.FullName;
                string modId = firstEntryFullName?.Split('/')[0];

                if (string.IsNullOrEmpty(modId))
                    throw new Exception("Couldn't fetch the mod ID from the mod archive");

                // get the mod's installation path to check if there is a previous installation. In that case delete it first
                string installationPath = Path.Combine(InstallationFolder, modId);
                
                if (Directory.Exists(installationPath))
                    IOUtils.DeleteDirectory(installationPath);

                // install the mod by extracting it into the installation folder
                archive.ExtractToDirectory(InstallationFolder, true);
            }
            finally
            {
                // make sure that the caller is back on the main thread
                await UniTask.SwitchToMainThread();
            }
        }
    }
}