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
/// 
/// </summary>
/// <param name="baseType">NULL表示服务自身</param>
/// <param name="serviceLifetime">服务生命周期</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectAttribute(Type baseType = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : Attribute
{
    public ServiceLifetime ServiceLifetime { get; set; } = serviceLifetime;

    public Type BaseType { get; set; } = baseType;
}


#if NET7_0_OR_GREATER

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectAttribute<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : AutoInjectAttribute(typeof(T), serviceLifetime)
{
}

#endif

#if NET8_0_OR_GREATER

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AutoInjectKeyedAttribute<T>(string key, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) : AutoInjectAttribute(typeof(T), serviceLifetime)
{
    public string Key { get; set; } = key ?? throw new ArgumentNullException(nameof(key));
}

#endif
