using System;

namespace Katas.UniMod
{
    /// <summary>
    /// Use this attribute to define mod startup methods from your scripts. Methods marked with this attribute
    /// must be static and will be executed when the mod is loaded, after the startup scriptable object has
    /// been executed. If you declare multiple ModStartup methods, the order of execution will not be guaranteed.
    ///<br/>
    ///<br/>
    /// Valid method signatures are:
    /// <code>
    /// static void Method();
    /// static UniTask MethodAsync();
    /// static void Method(IModContext context, IMod mod);
    /// static UniTask MethodAsync(IModContext context, IMod mod);
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ModStartupAttribute : Attribute
    { }
}