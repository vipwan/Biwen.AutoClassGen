using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Biwen.AutoClassGen.TestConsole.Interfaces
{

    /// <summary>
    /// 测试接口1
    /// </summary>
    public interface ITestInterface
    {
        [DefaultValue("hello world"),Required]
        [Description("hello world")]
        string? TestProperty { get; set; }

        string TestMethod(string arg1, int arg2);
    }

    /// <summary>
    /// 测试接口2
    /// </summary>
    public interface ITest2Interface
    {
        [DefaultValue("hello"), Required]
        string? Hello { get; set; }

        [DefaultValue("world")]
        [StringLength(100, MinimumLength = 2)]
        string? World { get; set; }
    }
}