using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Modman
{
    public abstract class ModStartup : ScriptableObject
    {
        public abstract UniTask StartAsync ();
    }
}
