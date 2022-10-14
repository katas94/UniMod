namespace Katas.UniMod
{
    public interface IModdableApp
    {
        string Id { get; }
        string Version { get; }
        
        ModIssues GetModIssues(ModInfo info);
        bool IsModSupported(ModInfo info, out ModIssues issues);
    }
}