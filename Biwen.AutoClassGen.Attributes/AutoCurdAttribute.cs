
namespace Biwen.AutoClassGen.Attributes;


#if NET7_0_OR_GREATER


/// <summary>
/// 自动实现CURD
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoCurdAttribute<T> : Attribute where T : class
{
    public AutoCurdAttribute(string @namespace)
    {
        Namespace = @namespace;
    }

    /// <summary>
    /// 生成的类所在的命名空间
    /// </summary>
    public string Namespace { get; private set; }

    /// <summary>
    /// The DbContext type to be used for CRUD operations.
    /// </summary>
    public T DbContext { get; private set; } = default(T);


}

#endif