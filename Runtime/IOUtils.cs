using System;
using System.IO;

namespace Katas.UniMod
{
    public static class IOUtils
    {
        /// <summary>
        /// Returns all file paths of files with the given extension (without dot) under the given root folder. If the recurse parameter
        /// is set to true, it will include all subfolders.
        /// </summary>
        public static string[] FindAllFilesWithExtension (string rootFolder, string extension, bool recurse = false)
            => Directory.GetFiles(rootFolder, $"*.{extension}", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Recursive method to delete the specified directory path and all its contents.
        /// </summary>
        public static void DeleteDirectory (string path, bool deleteFolderMetaFile = false)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            if (deleteFolderMetaFile)
            {
                string metaFile = path + ".meta";

                if (File.Exists(metaFile))
                    File.Delete(metaFile);
            }

            string[] files = Directory.GetFiles(path);
            string[] directories = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string directory in directories)
                DeleteDirectory(directory);

            Directory.Delete(path, false);
        }

        /// <summary>
        /// Recursive method to copy the specified directory path and all its contents.
        /// </summary>
        public static void CopyDirectory (string src, string dest, bool ignoreMetaFiles = false)
        {
            if (!Directory.Exists(src))
                throw new DirectoryNotFoundException($"Could not find the specified directory: \"{src}\"");

            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            string[] files = Directory.GetFiles(src);
            string[] directories = Directory.GetDirectories(src);

            foreach (string file in files)
                if (!ignoreMetaFiles || Path.GetExtension(file) != ".meta")
                    File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));

            foreach (string directory in directories)
                CopyDirectory(directory, Path.Combine(dest, Path.GetFileName(directory)), ignoreMetaFiles);
        }

        /// <summary>
        /// Gets a unique folder name. If a path (must be a folder) is given, it will make sure that the unique name given is not in conflict with any subfolder.
        /// </summary>
        public static string GetUniqueFolderName (string path = null)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return Guid.NewGuid().ToString();
            
            string guid;

            do
            {
                guid = Guid.NewGuid().ToString();
            }
            while (Directory.Exists(Path.Combine(path, guid)));

            return guid;
        }

        /// <summary>
        /// Gets a unique folder path inside the given root.
        /// </summary>
        public static string GetUniqueFolderPath (string root)
            => Path.Combine(root, GetUniqueFolderName(root));

        /// <summary>
        /// Creates a temporary folder in a local temp path. It returns the path.
        /// </summary>
        public static string CreateTmpFolder ()
        {
#if UNITY_EDITOR
            string path = GetUniqueFolderPath("Temp");
#else
            string path = GetUniqueFolderPath(UnityEngine.Application.temporaryCachePath);
#endif

            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Given a file path and an extension (without dot character), it will return the path making sure that it is of the given file extension.
        /// <br/>Examples:<br/>
        /// EnsureFileExtension("C:/Test/file", "exe") // returns "C:/Test/file.exe"<br/>
        /// EnsureFileExtension("C:/Test/file.", "exe") // returns "C:/Test/file.exe"
        /// EnsureFileExtension("C:/Test/file.dll", "exe") // returns "C:/Test/file.dll.exe"
        /// EnsureFileExtension("C:/Test/file.dll", "exe", true) // returns "C:/Test/file.exe"
        /// EnsureFileExtension("C:/Test/file.dll.bytes", "exe", true) // returns "C:/Test/file.dll.exe"
        /// </summary>
        public static string EnsureFileExtension (string path, string extension, bool changeExtension = false)
        {
            string dotExtension = "." + extension;

            if (path.EndsWith(dotExtension))
                return path;
            else if (path.EndsWith("."))
                return path + extension;
            else if (changeExtension)
                return Path.ChangeExtension(path, dotExtension);
            else
                return path + dotExtension;
        }
    }
}