using Biwen.AutoClassGen.TestConsole.Interfaces;

namespace Biwen.AutoClassGen.TestConsole.Classes
{


    [AutoGen("MyClassClone", "Biwen.AutoClassGen.TestConsole.Classes")]
    [AutoGen("MyClass", "Biwen.AutoClassGen.TestConsole.Classes")]
    public interface IMyClass : ITestInterface, ITest2Interface
    {

    }

    public partial class MyClass
    {
        public string TestMethod(string arg1, int arg2)
        {
            return $"{arg1} {arg2}";
        }
    }

    public partial class MyClassClone
    {
        public string TestMethod(string arg1, int arg2)
        {
            return $"{arg1} {arg2}";
        }
    }


    [AutoGen("My2Class", "Biwen.AutoClassGen.TestConsole.Classes")]
    public interface IMy2Class : ITest2Interface
    {

    }
}