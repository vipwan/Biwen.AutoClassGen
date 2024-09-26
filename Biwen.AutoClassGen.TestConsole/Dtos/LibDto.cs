// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App LibDto.cs 
// 2024-09-26 16:20:15  Biwen.AutoClassGen.TestConsole 万雅虎

namespace Biwen.AutoClassGen.TestConsole.Dtos;

using Biwen.AutoClassGen.TestLib;

//非同一代码库下,不会生成
[AutoDto<TestClass1>] //warning
public partial class LibDto
{
}

[AutoDto<TestClass1>] //warning
public class LibDto2 //warning
{
}


[AutoDto<TImplClass>]
public partial class TDto
{
}

public class TClass<T>
{
    public T? Id { get; set; }

    public string? Hello { get; set; }
}

public class TImplClass : TClass<string>
{
    public string? World { get; set; }
}
