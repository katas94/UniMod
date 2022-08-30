using System;

namespace Katas.Modman
{
    public class ModInstallationException : Exception
    {
        public ModInstallationException(string modId, Exception innerException)
            : base($"Failed to install mod {modId}", innerException) { }
    }
}