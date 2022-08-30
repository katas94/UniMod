using System;

namespace Katas.Modman
{
    public class ModLoadException : Exception
    {
        public ModLoadException(string modId, string message)
            : base($"Could not load mod {modId}: {message}") { }
        public ModLoadException(string modId, Exception innerException)
            : base($"Could not load mod {modId}", innerException) { }
    }
}