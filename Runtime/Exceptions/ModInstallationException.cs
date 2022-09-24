using System;

namespace Katas.UniMod
{
    public sealed class ModInstallationException : Exception
    {
        public ModInstallationException(string modId, string message)
            : base($"Failed to install mod {modId}: {message}") { }
        public ModInstallationException(string modId, Exception innerException)
            : base($"Failed to install mod {modId}", innerException) { }
    }
}