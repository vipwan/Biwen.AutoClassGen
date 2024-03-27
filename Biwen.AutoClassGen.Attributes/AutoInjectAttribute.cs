namespace Biwen.AutoClassGen.Attributes
{
    using System;

    /// <summary>
    /// 服务生命周期
    /// </summary>
    public enum ServiceLifetime
    {
        Singleton = 1,
        Transient = 2,
        Scoped = 4,
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AutoInjectAttribute : Attribute
    {
        public ServiceLifetime ServiceLifetime { get; set; }

        public Type BaseType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType">NULL表示服务自身</param>
        /// <param name="serviceLifetime">服务生命周期</param>
        public AutoInjectAttribute(Type baseType = null, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            ServiceLifetime = serviceLifetime;
            BaseType = baseType;
        }
    }


#if NET7_0_OR_GREATER

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class AutoInjectAttribute<T> : Attribute
    {
        public ServiceLifetime ServiceLifetime { get; set; }

        public AutoInjectAttribute(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            ServiceLifetime = serviceLifetime;
        }
    }

#endif

}