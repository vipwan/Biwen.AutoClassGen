namespace Biwen.AutoClassGen.Attributes;

/// <summary>
/// 自动注入的装饰器特性,<paramref name="implement"/>为装饰器,特性请标注于被装饰的接口或类上
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoDecorAttribute(Type implement) : Attribute
{
    public Type ImplementType { get; private set; } = implement;
}
#if NET7_0_OR_GREATER

/// <summary>
/// 自动注入的装饰器特性,<typeparamref name="T"/> 为被标注服务or接口的装饰器,特性请标注于被装饰的接口或类上
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoDecorAttribute<T> : AutoDecorAttribute where T : class
{
    public AutoDecorAttribute() : base(typeof(T))
    {
    }
}

#endif
