using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace ModmanEditor
{
    public partial class ModConfig
    {
        private static readonly List<string> TmpList = new();
        
        public List<string> GetAllIncludes(BuildTarget buildTarget)
        {
            var assemblyNames = new List<string>();
            GetAllIncludes(buildTarget, assemblyNames);
            return assemblyNames;
        }
        
        /// <summary>
        /// Populates the given list with all the included assembly names (without the extension) for this mod
        /// config for the given build target. Results groups all managed plugins and user defined assemblies and
        /// are guaranteed to be all compatible with the given build target.
        /// </summary>
        public void GetAllIncludes(BuildTarget buildTarget, List<string> includes)
        {
            if (includes is null)
                return;
            
            // get assembly names from the included assembly definitions
            TmpList.Clear();
            GetIncludedAssemblies(buildTarget, TmpList);
            includes.AddRange(TmpList);

            // get the managed plugin paths
            TmpList.Clear();
            GetIncludedManagedPlugins(buildTarget, TmpList);
            
            // parse paths into the assembly name without extension
            foreach (string path in TmpList)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                
                if (!string.IsNullOrEmpty(name))
                    includes.Add(name);
            }
            
            TmpList.Clear();
        }
    }
}
