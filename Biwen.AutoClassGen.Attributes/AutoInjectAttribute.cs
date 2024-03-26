using System;

namespace Biwen.AutoClassGen.Attributes
{
#if NET7_0_OR_GREATER

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