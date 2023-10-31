# Biwen.AutoClassGen
	- ��������C#��Ĺ��ߣ��Զ������������,�������Ե�Attributeȫ������Interface

## �÷�

- 1.��Interface�ж�������

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

- 2.��ע��Ҫ���ɵ���

```c#
    //֧�ֶ�α�ע���������ɶ����
    [AutoGen("MyClassClone", "Biwen.AutoClassGen.TestConsole.Classes")]
    [AutoGen("MyClass", "Biwen.AutoClassGen.TestConsole.Classes")]
    public interface IMyClass : ITestInterface, ITest2Interface
    {
    }

    //����ӿ����з�������Ҫ����һ��partial�࣬ʵ�ֽӿ��еķ���
    //����ӿ���û�з��������Բ�����partial��
    //��Ȼpartial�����Ҫ,һ�㺬��ҵ���߼� ������Ҫ���о���
    public partial class MyClass
    {
        public string TestMethod(string arg1, int arg2)
        {
            return $"{arg1} {arg2}";
        }
    }

```
- 3.Gen�Զ�������

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