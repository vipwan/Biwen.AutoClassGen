namespace Biwen.AutoClassGen.Attributes
{
    using System;

    /// <summary>
    ///  Indicates that the class should be auto generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class AutoGenAttribute : Attribute
    {
        /// <summary>
        /// The name of the class to be generated.
        /// </summary>
        public string ClassName { get; private set; }
        /// <summary>
        /// full interface name
        /// </summary>
        public string InterfaceName { get; private set; }

        /// <summary>
        /// AutoGen
        /// </summary>
        /// <param name="className">类名</param>
        /// <param name="interfaceName">命名空间全名,如:Biwen.QuickApi</param>
        public AutoGenAttribute(string className, string interfaceName)
        {
            ClassName = className;
            InterfaceName = interfaceName;
        }
    }
}