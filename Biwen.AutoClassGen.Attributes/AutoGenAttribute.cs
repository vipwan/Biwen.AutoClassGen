namespace Biwen.AutoClassGen.Attributes;

using System;

/// <summary>
///  Indicates that the class should be auto generated.
/// </summary>
/// <remarks>
/// AutoGen
/// </remarks>
/// <param name="className">类名</param>
/// <param name="interfaceName">命名空间全名,如:Biwen.QuickApi</param>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class AutoGenAttribute(string className, string interfaceName) : Attribute
{
    /// <summary>
    /// The name of the class to be generated.
    /// </summary>
    public string ClassName { get; private set; } = className;
    /// <summary>
    /// full interface name
    /// </summary>
    public string InterfaceName { get; private set; } = interfaceName;
}