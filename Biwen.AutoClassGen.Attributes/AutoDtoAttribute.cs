namespace Biwen.AutoClassGen.Attributes;

using System;

/// <summary>
/// 自动创建Dto
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoDtoAttribute(Type fromType, params string[] ignoredProperties) : Attribute
{
    /// <summary>
    /// 从指定类型创建
    /// </summary>
    public Type FromType { get; private set; } = fromType;

    public string[] IgnoredProperties { get; private set; } = ignoredProperties;
}

#if NET7_0_OR_GREATER

/// <summary>
/// 自动创建Dto
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoDtoAttribute<T>(params string[] ignoredProperties) : AutoDtoAttribute(typeof(T), ignoredProperties) where T : class
{
}

#endif
