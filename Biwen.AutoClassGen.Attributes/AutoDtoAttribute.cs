namespace Biwen.AutoClassGen.Attributes;

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

/// <summary>
/// 提供复杂对象的DTO,支持嵌套生成
/// 支持自定义层级默认2层
/// 比如Person下面的Address和Hobby,将会生成AddressDTO和HobbyDTO
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoDtoComplexAttribute(int maxNestingLevel = 2) : Attribute
{
    /// <summary>
    /// 嵌套层级
    /// </summary>
    public int MaxNestingLevel { get; set; } = maxNestingLevel;
}

/// <summary>
/// 用于标记目标类属性,在生成DTO时忽略
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class AutoDtoIgronedAttribute : Attribute
{
}
