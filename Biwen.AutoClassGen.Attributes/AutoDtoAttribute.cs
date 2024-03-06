namespace Biwen.AutoClassGen.Attributes
{
    using System;

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

        public string[] IgnoredProperties { get; private set; }

        public AutoDtoAttribute(Type fromType, params string[] ignoredProperties)
        {
            FromType = fromType;
            IgnoredProperties = ignoredProperties;
        }
    }

#if NET7_0_OR_GREATER

    /// <summary>
    /// 自动创建Dto
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AutoDtoAttribute<T> : AutoDtoAttribute where T : class
    {
        public AutoDtoAttribute(params string[] ignoredProperties) : base(typeof(T), ignoredProperties)
        {
        }
    }

#endif

}