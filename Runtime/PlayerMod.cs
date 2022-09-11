using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.Modman
{
    public class PlayerMod : IMod
    {
        public readonly string Path;
        
        public ModInfo Info { get; }
        public ModStatus Status { get; }
        public bool IsLoaded { get; private set; }
        public bool AreAssembliesLoaded { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        private readonly List<Assembly> _loadedAssemblies = new();

        public PlayerMod(string path, ModInfo info)
        {
            Path = path;
            Info = info;
        }

        public UniTask LoadAsync(bool loadAssemblies)
        {
            return default;
        }

        public UniTask UninstallAsync()
        {
            IOUtils.DeleteDirectory(Path);
            return UniTask.CompletedTask;
        }

        public UniTask<Sprite> LoadThumbnailAsync()
        {
            throw new System.NotImplementedException();
        }

        public void UnloadThumbnail()
        {
            throw new System.NotImplementedException();
        }
    }
}