## Biwen.AutoClassGen
Usage scenario: In many cases, we will have a lot of request objects, 
such as GetIdRequest, GetUserRequest, etc..., and these requests may have a large number of the same fields.
For example, the multi-tenant Id, the number of pages, and these attribute fields may have validation rules, binding rules, and Swagger descriptions.
If all this code needs to be written, it will add a lot of work, so Biwen.AutoClassGen came into being to solve this pain point...

[中文](https://github.com/vipwan/Biwen.AutoClassGen/blob/master/README-zh.md)

### Useage

#### 1.Define Interface

```c#
    /// <summary>
    /// Pager Interface
    /// </summary>
    public interface IPager
    {
        /// <summary>
        /// current page
        /// </summary>
        [DefaultValue(0), Description("start 0 to int.max")]
        [Range(0, int.MaxValue)]
        int? CurrentPage { get; set; }
        /// <summary>
        /// length of page 
        /// </summary>
        [DefaultValue(10), Description("between 10 an 30")]
        [Range(10, 30)]
        int? PageLen { get; set; }
    }
    /// <summary>
    /// Query Interface
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// KeyWord
        /// </summary>
        [StringLength(100), Description("Keyword for search")]
        string? KeyWord { get; set; }
    }
    /// <summary>
    /// Tenant Request
    /// </summary>
    public interface ITenantRequest
    {
        /// <summary>
        /// TenantId
        /// </summary>
        [Required, Description("Tenant ID"), DefaultValue("default")]
        [FromHeader(Name = "tenant-id")]
        string? TenantId { get; set; }
    }
```
#### 2.Inherent interface and mark [AutoGen] Attribute

```c#

    //can add multi AutoGen Attribute
    [AutoGen("QueryRequest", "Biwen.AutoClassGen.Models")]
    [AutoGen("Query2Request", "Biwen.AutoClassGen.Models")]
    public interface IQueryRequest : IPager, IQuery
    {
    }

    /// <summary>
    /// MyTenantRequest
    /// </summary>
    [AutoGen("MyTenantRequest", "Biwen.AutoClassGen.Models")]
    public interface ITenantRealRequest : ITenantRequest
    {
    }

    //partial class for your logic
    public partial class QueryRequest
    {
        public string TestMethod(string arg1, int arg2)
        {
            return $"{arg1} {arg2}";
        }
    }
```
#### 3.Enjoy!!! finally auto generated class for you

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
        [System.ComponentModel.DescriptionAttribute("start 0 to int.max")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(0, 2147483647)]
        public int? CurrentPage { get; set; }

        /// <inheritdoc cref = "IPager.PageLen"/>
        [System.ComponentModel.DefaultValueAttribute(10)]
        [System.ComponentModel.DescriptionAttribute("between 10 an 30")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(10, 30)]
        public int? PageLen { get; set; }

        /// <inheritdoc cref = "IQuery.KeyWord"/>
        [System.ComponentModel.DataAnnotations.StringLengthAttribute(100)]
        [System.ComponentModel.DescriptionAttribute("Keyword for search")]
        public string? KeyWord { get; set; }
    }

    public partial class Query2Request : IQueryRequest
    {
        /// <inheritdoc cref = "IPager.CurrentPage"/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [System.ComponentModel.DescriptionAttribute("start 0 to int.max")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(0, 2147483647)]
        public int? CurrentPage { get; set; }

        /// <inheritdoc cref = "IPager.PageLen"/>
        [System.ComponentModel.DefaultValueAttribute(10)]
        [System.ComponentModel.DescriptionAttribute("between 10 an 30")]
        [System.ComponentModel.DataAnnotations.RangeAttribute(10, 30)]
        public int? PageLen { get; set; }

        /// <inheritdoc cref = "IQuery.KeyWord"/>
        [System.ComponentModel.DataAnnotations.StringLengthAttribute(100)]
        [System.ComponentModel.DescriptionAttribute("Keyword for search")]
        public string? KeyWord { get; set; }
    }

    public partial class MyTenantRequest : ITenantRealRequest
    {
        /// <inheritdoc cref = "ITenantRequest.TenantId"/>
        [System.ComponentModel.DataAnnotations.RequiredAttribute]
        [System.ComponentModel.DescriptionAttribute("Tenant ID")]
        [System.ComponentModel.DefaultValueAttribute("default")]
        [Microsoft.AspNetCore.Mvc.FromHeaderAttribute(Name = "tenant-id")]
        public string? TenantId { get; set; }
    }
}
#pragma warning restore

```

### Gen Error Code

- GEN001: The interface marked [AutoGen] should be inherent one or more interface
