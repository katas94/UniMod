using System;
using System.Collections.Generic;
using System.Reflection;

namespace Katas.UniMod
{
    /// <summary>
    /// Helper static class to load new assemblies and track loaded assemblies on the current AppDomain
    /// </summary>
    internal static class DomainAssemblies
    {
        /// <summary>
        /// All currently loaded assembly full names.
        /// </summary>
        public static IEnumerable<string> FullNames => LoadedAssemblies.Keys;
        
        /// <summary>
        /// All currently loaded assemblies.
        /// </summary>
        public static IEnumerable<Assembly> Assemblies => LoadedAssemblies.Values;
        
        private static readonly Dictionary<string, Assembly> LoadedAssemblies = new();

        static DomainAssemblies()
        {
            // get all currently loaded assemblies on this domain and map them by their full name
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (Assembly assembly in assemblies)
                LoadedAssemblies[assembly.FullName] = assembly;
        }
        
        /// <summary>
        /// Tries to load the given raw assembly bytes into the current AppDomain. Returns the loaded assembly instance if successful.
        /// </summary>
        public static Assembly Load(byte[] rawAssembly)
        {
            return Load(rawAssembly, out string _);
        }
        
        /// <summary>
        /// Tries to load the given raw assembly bytes into the current AppDomain. Returns the loaded assembly instance if successful.
        /// </summary>
        public static Assembly Load(byte[] rawAssembly, out string message)
        {
            return Load(rawAssembly, null, out message);
        }
        
        /// <summary>
        /// Tries to load the given raw assembly bytes into the current AppDomain. Returns the loaded assembly instance if successful.
        /// The rawSymbolStore parameter is optional (can be set to null).
        /// </summary>
        public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore)
        {
            return Load(rawAssembly, rawSymbolStore, out string _);
        }

        /// <summary>
        /// Tries to load the given raw assembly bytes into the current AppDomain. Returns the loaded assembly instance if successful.
        /// The rawSymbolStore parameter is optional (can be set to null).
        /// </summary>
        public static Assembly Load(byte[] rawAssembly, byte[] rawSymbolStore, out string message)
        {
            if (rawAssembly is null)
            {
                message = "The given raw assembly is null";
                return null;
            }
            
            Assembly assembly;

            try
            {
                assembly = rawSymbolStore is null ? Assembly.Load(rawAssembly) : Assembly.Load(rawAssembly, rawSymbolStore);
            }
            catch (Exception exception)
            {
                message = exception.ToString();
                return null;
            }

            lock (LoadedAssemblies)
            {
                if (LoadedAssemblies.TryGetValue(assembly.FullName, out Assembly loadedAssembly))
                {
                    message = $"The assembly was already loaded in the AppDomain: {assembly.FullName}";
                    return loadedAssembly;
                }
                
                LoadedAssemblies[assembly.FullName] = assembly;
            }
            
            message = null;
            return assembly;
        }
        
        /// <summary>
        /// Gets the Assembly instance for the given full name (only if it is loaded on the domain).
        /// </summary>
        public static Assembly Get(string fullName)
        {
            return LoadedAssemblies.TryGetValue(fullName, out Assembly assembly) ? assembly : null;
        }
        
        /// <summary>
        /// Whether or not the given assembly is loaded on the current AppDomain.
        /// </summary>
        public static bool Contains(Assembly assembly)
        {
            return LoadedAssemblies.ContainsKey(assembly.FullName);
        }
        
        /// <summary>
        /// Whether or not the given assembly full name is loaded on the current AppDomain.
        /// </summary>
        public static bool Contains(string assemblyFullName)
        {
            return LoadedAssemblies.ContainsKey(assemblyFullName);
        }
    }
}