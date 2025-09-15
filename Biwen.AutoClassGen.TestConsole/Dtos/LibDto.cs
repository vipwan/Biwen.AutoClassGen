// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App LibDto.cs 
// 2024-09-26 16:20:15  Biwen.AutoClassGen.TestConsole 万雅虎

namespace Biwen.AutoClassGen.TestConsole.Dtos;

using Biwen.AutoClassGen.TestLib;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

//当前已支持夸库DTO生成 20250903
[AutoDto<TestClass1>("hello")] //no warning
[AutoDtoComplex]
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

    [StringLength(100, MinimumLength = 5)]
    public string? Hello { get; set; }
}

public class TImplClass : TClass<string>
{
    [Required]
    [Description("hello world")]
    public string? World { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string HostUrl { get; set; } = "https://www.baidu.com";

    [Phone]
    public string PhoneNumber { get; set; } = "1234567890";

    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only alphanumeric characters are allowed.")]
    public string? RegText { get; set; }


}
