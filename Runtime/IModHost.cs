namespace Katas.UniMod
{
    /// <summary>
    /// A mod host have an ID and a version so mods can target it. It also defines the logic to check mod compatibility.
    /// </summary>
    public interface IModHost
    {
        string Id { get; }
        string Version { get; }
        
        ModIssues GetModIssues(IMod mod);
        bool IsModSupported(IMod mod, out ModIssues issues);
    }
}