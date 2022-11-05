using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Implement this to create your own serialized mod startup script to include in your mod config.
    /// </summary>
    public abstract class ModStartup : ScriptableObject
    {
        public abstract UniTask StartAsync (IMod mod);
    }
}
