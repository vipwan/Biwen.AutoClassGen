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
        [DefaultValue(10), Description("每页项数,10-20之间")]
        [Range(10, 20)]
        int? PageLen { get; set; }


    }
}