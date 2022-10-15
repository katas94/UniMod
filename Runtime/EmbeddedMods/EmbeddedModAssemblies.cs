using System;
using System.Collections.Generic;
using UnityEngine;

namespace Katas.UniMod
{
    [Serializable]
    public struct EmbeddedModAssemblies
    {
        public RuntimePlatform platform;
        public List<string> names;
    }
}