using System;
using System.Collections.Generic;

namespace Katas.UniMod
{
    [Serializable]
    public struct EmbeddedModAsset
    {
        public string guid;
        public List<string> labels;
    }
}