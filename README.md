## Biwen.AutoClassGen
使用场景:很多时候我们的请求对象会特别多比如GetIdRequest,GetUserRequest etc...,这些Request可能大量存在相同的字段,
比如多租户Id,分页数,这些属性字段可能又存在验证规则,绑定规则,以及Swagger描述等信息,
如果这些代码都需要人肉敲那会增加很多工作量,所以Biwen.AutoClassGen应运而生,解决这个痛点...
- 用于生成C#类的工具，自动生成类的属性,并且属性的Attribute全部来自Interface

### 用法

#### 1.在Interface中定义属性

```c#
    /// <summary>
    /// 分页
    /// </summary>
    public interface IPager
    {
        /// <summary>
        /// 页码
        /// </summary>
        [DefaultValue(0), Description("页码,从0开始")]
        [Range(0, int.MaxValue)]
        int? CurrentPage { get; set; }
        /// <summary>
        /// 分页项数
        /// </summary>
        [DefaultValue(10), Description("每页项数,10-30之间")]
        [Range(10, 30)]
        int? PageLen { get; set; }
    }
    /// <summary>
    /// 查询
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// 关键字
        /// </summary>
        [StringLength(100), Description("查询关键字")]
        string? KeyWord { get; set; }
    }
    /// <summary>
    /// 多租户请求
    /// </summary>
    public interface ITenantRequest
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        [Required, Description("租户ID"), DefaultValue("default")]
        [FromHeader(Name = "tenant-id")]
        string? TenantId { get; set; }
    }
```
#### 2.标注需要生成的类

```c#

    //支持多次标注，可以生成多个类
    [AutoGen("QueryRequest", "Biwen.AutoClassGen.Models")]
    [AutoGen("Query2Request", "Biwen.AutoClassGen.Models")]
    public interface IQueryRequest : IPager, IQuery
    {
    }

    /// <summary>
    /// 多租户请求
    /// </summary>
    [AutoGen("MyTenantRequest", "Biwen.AutoClassGen.Models")]
    public interface ITenantRealRequest : ITenantRequest
    {
    }

    //如果接口中有方法，需要定义一个partial类，实现接口中的方法
    //如果接口中没有方法，可以不定义partial类
    //当然partial类很重要,一般含有业务逻辑 根据需要自行决定
    public partial class QueryRequest
    {
        public string TestMethod(string arg1, int arg2)
        {
            return $"{arg1} {arg2}";
        }
    }
```
#### 3.Gen自动生成类

```c#

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Biwen.AutoClassGen.TestConsole.Interfaces;

#pragma warning disable
namespace Biwen.AutoClassGen.Models
{
    public partial class QueryRequest : IQueryRequest
    {
        /// <inheritdoc cref = "IPager.CurrentPage"/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.ComponentModel.DescriptionAttribute("页码,从0开始")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(0, 2147483647)]
        public int? CurrentPage { get; set; }

        /// <inheritdoc cref = "IPager.PageLen"/>
        [System.ComponentModel.DefaultValueAttribute(10)]
        [System.ComponentModel.DescriptionAttribute("每页项数,10-30之间")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(10, 30)]
        public int? PageLen { get; set; }

        /// <inheritdoc cref = "IQuery.KeyWord"/>
        [System.ComponentModel.DataAnnotations.StringLengthAttribute(100)]
        [System.ComponentModel.DescriptionAttribute("查询关键字")]
        public string? KeyWord { get; set; }
    }

    public partial class Query2Request : IQueryRequest
    {
        /// <inheritdoc cref = "IPager.CurrentPage"/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.ComponentModel.DescriptionAttribute("页码,从0开始")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(0, 2147483647)]
        public int? CurrentPage { get; set; }

        /// <inheritdoc cref = "IPager.PageLen"/>
        [System.ComponentModel.DefaultValueAttribute(10)]
        [System.ComponentModel.DescriptionAttribute("每页项数,10-30之间")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(10, 30)]
        public int? PageLen { get; set; }

        /// <inheritdoc cref = "IQuery.KeyWord"/>
        [System.ComponentModel.DataAnnotations.StringLengthAttribute(100)]
        [System.ComponentModel.DescriptionAttribute("查询关键字")]
        public string? KeyWord { get; set; }
    }

    public partial class MyTenantRequest : ITenantRealRequest
    {
        /// <inheritdoc cref = "ITenantRequest.TenantId"/>
        [System.ComponentModel.DataAnnotations.RequiredAttribute]
        [System.ComponentModel.DescriptionAttribute("租户ID")]
        [System.ComponentModel.DefaultValueAttribute("default")]
        [Microsoft.AspNetCore.Mvc.FromHeaderAttribute(Name = "tenant-id")]
        public string? TenantId { get; set; }
    }
}
#pragma warning restore

```

### 错误码

- GEN001: 标注特性[AutoGen]的接口必须继承至少一个接口
