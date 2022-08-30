using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.Modman
{
    public class PlayerMod : IMod
    {
        public string Path { get; }
        public ModInfo Info { get; }
        public ModStatus Status { get; }
        public bool IsContentLoaded { get; private set; }
        public bool AreAssembliesLoaded { get; private set; }

        public PlayerMod(string path, ModInfo info)
        {
            Path = path;
            Info = info;
        }
        
        public UniTask LoadContentAsync()
        {
            throw new System.NotImplementedException();
        }

        public UniTask LoadAssembliesAsync()
        {
            throw new System.NotImplementedException();
        }

        public UniTask StartAsync()
        {
            throw new System.NotImplementedException();
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