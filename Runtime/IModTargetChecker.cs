namespace Katas.UniMod
{
    public interface IModTargetChecker
    {
        string AppId { get; }
        string AppVersion { get; }
        
        ModIssues CheckForIssues(ModTargetInfo target);
        bool CheckForIssues(ModTargetInfo target, out ModIssues issues);
    }
}