using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Biwen.AutoClassGen.TestConsole.Interfaces
{

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
}