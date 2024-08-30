namespace Biwen.AutoClassGen.Attributes;

/// <summary>
/// 自动注入的装饰器特性,特性请标注于被装饰的接口或类上
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoDecorAttribute(Type implement) : Attribute
{
    public Type ImplementType { get; private set; } = implement;
}
#if NET7_0_OR_GREATER

/// <summary>
/// 自动注入的装饰器特性,特性请标注于被装饰的接口或类上
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoDecorAttribute<T> : AutoDecorAttribute where T : class
{
    public AutoDecorAttribute() : base(typeof(T))
    {
    }
}

#endif


/// <summary>
/// 自动注入的装饰器特性,标注于装饰器上
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoDecorForAttribute(Type forDecorate) : Attribute
{
    public Type ForDecorateType { get; private set; } = forDecorate;
}

#if NET7_0_OR_GREATER

/// <summary>
/// 自动注入的装饰器特性,标注于装饰器上
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoDecorForAttribute<T> : AutoDecorForAttribute where T : class
{
    public AutoDecorForAttribute() : base(typeof(T))
    {
    }
}

#endif






