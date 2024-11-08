// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App Classes.cs 
// 2024-09-08 13:37:04  Biwen.AutoClassGen.TestConsole 万雅虎


using Biwen.AutoClassGen.TestConsole.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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


    public interface IMyInterface
    {
        string? Id { get; set; }
        string? Email { get; set; }

        Task MyMethod();
    }


    public class MyClass : IMyInterface
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

        //继承的接口中有同名方法，分析器会排除掉这个方法
        public Task MyMethod()
        {
            return Task.CompletedTask;
        }
    }

    //分析器会排除掉这个类
    public class MyController
    {
        public Task MyMethod()
        {
            return Task.CompletedTask;
        }
    }

    //分析器会排除掉这个类
    [ApiController]
    public class MyCtor
    {
        public Task MyMethod()
        {
            return Task.CompletedTask;
        }
    }

    //分析器会排除掉这个类
    public class MyHub : Hub
    {
        public Task MyMethod()
        {
            return Task.CompletedTask;
        }
    }

    //分析器会排除掉这个类
    public class MyHub2 : Hub<MyCtor>
    {
        public Task MyMethod()
        {
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return string.Format("{0}", "MyHub2");
        }
    }

}