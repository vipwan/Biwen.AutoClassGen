namespace Biwen.AutoClassGen.Attributes
{
    using System;

    /// <summary>
    /// Auto Decoration Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AutoDecorAttribute : Attribute
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:删除未使用的参数", Justification = "<挂起>")]
        public AutoDecorAttribute(Type implement) { }
    }

#if NET7_0_OR_GREATER

    /// <summary>
    /// Auto Decoration Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
#pragma warning disable SA1402 // File may only contain a single type
    public class AutoDecorAttribute<T> : AutoDecorAttribute where T : class
#pragma warning restore SA1402 // File may only contain a single type
    {
        public AutoDecorAttribute() : base(typeof(T))
        {
        }
    }

#endif

}