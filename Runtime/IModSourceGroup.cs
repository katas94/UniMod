using System.Collections.Generic;

namespace Katas.UniMod
{
    /// <summary>
    /// Handles a group of mod sources.
    /// </summary>
    public interface IModSourceGroup
    {
        IReadOnlyList<IModSource> Sources { get; }

        bool AddSource(IModSource source);
        bool AddSource(IModSource source, int insertAtIndex);
        void AddSources(IEnumerable<IModSource> sources);
        void AddSources(IEnumerable<IModSource> sources, int insertAtIndex);
        bool RemoveSource(IModSource source);
        void RemoveSourceAt(int index);
        void RemoveSources(IEnumerable<IModSource> sources);
        void ClearSources();
    }
}