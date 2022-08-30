using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Katas.Modman
{
    public abstract class ModStartup : ScriptableObject
    {
        public abstract UniTask StartAsync ();
    }
}
