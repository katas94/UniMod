using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public abstract class ModStartup : ScriptableObject
    {
        public abstract UniTask StartAsync (IMod mod);
    }
}
