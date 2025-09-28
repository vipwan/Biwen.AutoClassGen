namespace Biwen.AutoClassGen.Attributes;

#nullable enable

#if NET7_0_OR_GREATER

/// <summary>
/// 提供静态映射方法接口
/// </summary>
/// <typeparam name="TFrom"></typeparam>
/// <typeparam name="TTo"></typeparam>
public interface IStaticAutoDtoMapper<TFrom, TTo>
{
    /// <summary>
    /// 转换
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    static abstract void Map(TFrom from, TTo to);
}

/// <summary>
/// 提供静态映射方法接口的自动创建Dto
/// </summary>
/// <typeparam name="TFrom"></typeparam>
/// <param name="mapper"></param>
/// <param name="ignoredProperties"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoDtoWithMapperAttribute<TFrom>(Type? mapper = null, params string[] ignoredProperties) : AutoDtoAttribute(typeof(TFrom), ignoredProperties)
    where TFrom : class
{
    public Type? Mapper { get; private set; } = mapper;
}

#endif

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
