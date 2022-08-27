using UnityEngine;

namespace Modman
{
    public abstract class ModInstanceProvider : MonoBehaviour
    {
        public abstract IModInstance GetModInstance();
    }
}