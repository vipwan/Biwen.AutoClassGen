namespace Biwen.AutoClassGen.Attributes
{
    using System;

    /// <summary>
    /// Auto Decoration Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
#pragma warning disable CS9113 // 参数未读。
    public class AutoDecorAttribute(Type implement) : Attribute
#pragma warning restore CS9113 // 参数未读。
    {
    }
#if NET7_0_OR_GREATER

    /// <summary>
    /// Auto Decoration Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AutoDecorAttribute<T> : AutoDecorAttribute where T : class
    {
        public AutoDecorAttribute() : base(typeof(T))
        {
        }
    }

#endif

}