using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Katas.Mango
{
    public abstract class ModStartup : ScriptableObject
    {
        public abstract UniTask StartAsync ();
    }
}
