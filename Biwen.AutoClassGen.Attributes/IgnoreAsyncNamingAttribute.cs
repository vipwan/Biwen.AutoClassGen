namespace Biwen.AutoClassGen.Attributes;

/// <summary>
/// 标记此特性的方法将不检查异步方法命名约定
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IgnoreAsyncNamingAttribute : Attribute
{
}
