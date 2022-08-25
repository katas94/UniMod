using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Modman
{
    public abstract class ModInitialiser : MonoBehaviour
    {
        public abstract UniTask Initialise ();
    }
}
