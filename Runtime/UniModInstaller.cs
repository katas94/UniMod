using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.UniMod
{
    public class UniModInstaller : IModInstaller
    {
        public readonly string InstallationFolder;

        public UniModInstaller(string installationFolder)
        {
            InstallationFolder = installationFolder;
            
            if (!Directory.Exists(InstallationFolder))
                Directory.CreateDirectory(InstallationFolder);
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
            var exceptions = new List<Exception>();
            
            async UniTask InstallWithoutThrowingAsync(string modFilePath)
            {
                try
                {
                    await InstallModAsync(modFilePath, deleteModFilesAfter);
                }
                catch (Exception exception)
                {
                    exceptions.Add(new Exception($"Failed to install mod from file: {modFilePath}", exception));
                }
            }
            
            // if any installation fails, it will not stop the rest of installations from trying
            await UniTask.WhenAll(modFilePaths.Select(InstallWithoutThrowingAsync));
            
            // if there has been any non-successful installation then we throw an aggregate exception
            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }

        public async UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter = false)
        {
            if (string.IsNullOrEmpty(modFilePath))
                throw new Exception("Null or empty mod file path");
            
            // load the mod file bytes and install the mod from its buffer.
            // we won't check if the file's extension is the one from the UniMod specification,
            // as long as the mod is a compressed archive file we will be able to install it
            byte[] modBuffer = await File.ReadAllBytesAsync(modFilePath);
            await InstallModAsync(modBuffer);
            
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