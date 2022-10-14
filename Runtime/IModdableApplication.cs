namespace Katas.UniMod
{
    public interface IModdableApplication
    {
        string Id { get; }
        string Version { get; }
        
        ModIssues GetModIssues(IMod mod);
        bool IsModSupported(IMod mod, out ModIssues issues);
    }
}