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
}