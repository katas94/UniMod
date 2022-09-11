using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Katas.Modman
{
    /// <summary>
    /// Helper static class to load new assemblies and track loaded assemblies on the current AppDomain.s
    /// </summary>
    public static class DomainAssemblies
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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (Assembly assembly in assemblies)
                LoadedAssemblies[assembly.FullName] = assembly;
        }

        /// <summary>
        /// Tries to load the given raw assembly bytes into the current AppDomain. Returns true if successful. When returning false, the
        /// out error parameter will be initialized with an error message.
        /// </summary>
        public static bool Load(byte[] rawAssembly, out string error)
        {
            return Load(rawAssembly, null, out error);
        }
        
        /// <summary>
        /// Tries to load the given raw assembly bytes into the current AppDomain. Returns true if successful. When returning false, the
        /// out error parameter will be initialized with an error message. The rawSymbolStore parameter is optional (can be set to null).
        /// </summary>
        public static bool Load(byte[] rawAssembly, byte[] rawSymbolStore, out string error)
        {
            if (rawAssembly is null)
            {
                error = "The given raw assembly bytes is null";
                return false;
            }
            
            Assembly assembly = null;

            try
            {
                assembly = rawSymbolStore is null ? Assembly.Load(rawAssembly) : Assembly.Load(rawAssembly, rawSymbolStore);
            }
            catch (Exception exception)
            {
                error = exception.ToString();
                return false;
            }

            lock (LoadedAssemblies)
            {
                if (LoadedAssemblies.ContainsKey(assembly.FullName))
                {
                    error = $"The assembly is already loaded in the AppDomain: {assembly.FullName}";
                    return false;
                }
                
                LoadedAssemblies[assembly.FullName] = assembly;
            }
            
            error = null;
            Debug.Log($"Loaded assembly: {assembly.FullName}");
            return true;
        }
        
        /// <summary>
        /// Gets the Assembly instance for the given full name (only if it is loaded on the domain).
        /// </summary>
        public static Assembly Get(string fullName)
        {
            return LoadedAssemblies.TryGetValue(fullName, out var assembly) ? assembly : null;
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