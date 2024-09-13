// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App Classes.cs 
// 2024-09-08 13:37:04  Biwen.AutoClassGen.TestConsole 万雅虎


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


    public class MyClass
    {
        public string? Id { get; set; }
        public string? Email { get; set; }

        public Task Get()
        {
            return Task.CompletedTask;
        }
        public async Task Get2()
        {
            await Task.CompletedTask;
        }
    }

}