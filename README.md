# Biwen.AutoClassGen
	- 用于生成C#类的工具，自动生成类的属性,并且属性的Attribute全部来自Interface

## 用法

- 1.在Interface中定义属性

```c#

public interface ITestInterface
{
    [DefaultValue("hello world"),Required]
    [Description("hello world")]
    string? TestProperty { get; set; }

    string TestMethod(string arg1, int arg2);
}
public interface ITest2Interface
{
    [DefaultValue("hello"), Required]
    string? Hello { get; set; }

    [DefaultValue("world")]
    [StringLength(100, MinimumLength = 2)]
    string? World { get; set; }
}

```

- 2.标注需要生成的类

```c#
    //支持多次标注，可以生成多个类
    [AutoGen("MyClassClone", "Biwen.AutoClassGen.TestConsole.Classes")]
    [AutoGen("MyClass", "Biwen.AutoClassGen.TestConsole.Classes")]
    public interface IMyClass : ITestInterface, ITest2Interface
    {
    }

    //如果接口中有方法，需要定义一个partial类，实现接口中的方法
    //如果接口中没有方法，可以不定义partial类
    //当然partial类很重要,一般含有业务逻辑 根据需要自行决定
    public partial class MyClass
    {
        public string TestMethod(string arg1, int arg2)
        {
            return $"{arg1} {arg2}";
        }
    }

```
- 3.Gen自动生成类

```c#

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable

using Biwen.AutoClassGen.TestConsole.Interfaces;

namespace Biwen.AutoClassGen.TestConsole.Classes
{
    public partial class MyClass : IMyClass {

    [System.ComponentModel.DefaultValueAttribute("hello world")]
    [System.ComponentModel.DataAnnotations.RequiredAttribute]
    [System.ComponentModel.DescriptionAttribute("hello world")]
    public string? TestProperty {get;set;}

    [System.ComponentModel.DefaultValueAttribute("hello")]
    [System.ComponentModel.DataAnnotations.RequiredAttribute]
    public string? Hello {get;set;}

    [System.ComponentModel.DefaultValueAttribute("world")]
    [System.ComponentModel.DataAnnotations.StringLengthAttribute(100, MinimumLength = 2)]
    public string? World {get;set;}

    }
}
#pragma warning restore

```