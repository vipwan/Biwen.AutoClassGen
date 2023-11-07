using Biwen.AutoClassGen.TestConsole.Interfaces;

namespace Biwen.AutoClassGen.Models
{
    /// <summary>
    /// 分页请求
    /// </summary>
    [AutoGen("QueryRequest", "Biwen.AutoClassGen.Models")]
    [AutoGen("Query2Request", "Biwen.AutoClassGen.Models")]
    public interface IQueryRequest : IPager, IQuery
    {
    }

    [AutoGen("TenantRealRequest", "Biwen.AutoClassGen.Models")]
    public interface ITenantRealRequest : ITenantRequest
    {

    }
}