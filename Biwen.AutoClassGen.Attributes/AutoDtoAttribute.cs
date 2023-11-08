using System;

namespace Biwen.AutoClassGen.Attributes
{
    /// <summary>
    /// 自动创建Dto
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AutoDtoAttribute : Attribute
    {
        /// <summary>
        /// 从指定类型创建
        /// </summary>
        public Type FromType { get; private set; }

        public string[] ExcludeProps { get; private set; }

        public AutoDtoAttribute(Type fromType, params string[] excludeProps)
        {
            FromType = fromType;
            ExcludeProps = excludeProps;
        }
    }

#if NET7_0_OR_GREATER

    /// <summary>
    /// 自动创建Dto
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
#pragma warning disable SA1402 // File may only contain a single type
    public class AutoDtoAttribute<T> : AutoDtoAttribute where T : class
#pragma warning restore SA1402 // File may only contain a single type
    {
        public AutoDtoAttribute(params string[] excludeProps) : base(typeof(T), excludeProps)
        {
        }
    }

#endif

}