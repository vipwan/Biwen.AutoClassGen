namespace Biwen.AutoClassGen.Attributes;

/// <summary>
/// 服务生命周期
/// </summary>
public enum ServiceLifetime
{
    Singleton = 1,
    Transient = 2,
    Scoped = 4,
}

/// <summary>
/// AutoInject
/// </summary>
/// <param name="baseType">NULL表示服务自身</param>
/// <param name="serviceLifetime">服务生命周期</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectAttribute(Type baseType = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime ServiceLifetime { get; set; } = serviceLifetime;

    public Type BaseType { get; set; } = baseType;
}


/// <summary>
/// AutoInjectKeyed
/// </summary>
/// <param name="key">服务Key，不能为空</param>
/// <param name="baseType">NULL表示服务自身</param>
/// <param name="serviceLifetime">服务生命周期</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectKeyedAttribute(string key, Type baseType = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime ServiceLifetime { get; set; } = serviceLifetime;

    public Type BaseType { get; set; } = baseType;

    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));

}



#if NET7_0_OR_GREATER

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectAttribute<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : AutoInjectAttribute(typeof(T), serviceLifetime)
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectKeyedAttribute<T>(string key, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : AutoInjectAttribute(typeof(T), serviceLifetime)
{
    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));
}

#endif
