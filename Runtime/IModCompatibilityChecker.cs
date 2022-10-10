namespace Katas.UniMod
{
    public interface IModCompatibilityChecker
    {
        ModIncompatibilities GetIncompatibilities(ModTargetInfo target);
        bool IsCompatible(ModTargetInfo target, out ModIncompatibilities incompatibilities);
        bool IsCompatible(ModTargetInfo target);
    }
}