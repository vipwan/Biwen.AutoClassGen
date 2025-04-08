// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App ComplexDto.cs 
// 2024-09-19 11:24:20  Biwen.AutoClassGen.TestConsole 万雅虎

using System.ComponentModel.DataAnnotations;

namespace Biwen.AutoClassGen.TestConsole.Dtos;

//嵌套属性测试用例:

/// <summary>
/// 模拟复杂对象
/// </summary>
public class Person
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, Range(0, 200)]
    public int Age { get; set; }

    //模拟嵌套
    public Address Address { get; set; } = new Address();

    //模拟集合
    public List<Hobby> Hobbies { get; set; } = [];

    /// <summary>
    /// 这是一个忽略的字段
    /// </summary>
    public string Igrone { get; set; } = string.Empty;


    [AutoDtoIgroned]
    public string Igrone2 { get; set; } = null!;

}

public class Address
{
    [Required]
    public string Street { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public string ZipCode { get; set; } = string.Empty;
}

public class Hobby
{
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;

    public HobbyExtend Extend { get; set; } = new HobbyExtend();

}

public class HobbyExtend
{
    public string Extend1 { get; set; } = string.Empty;

    public string Extend2 { get; set; } = string.Empty;

    public InnerExtend Extend3 { get; set; } = new InnerExtend();

}

public class InnerExtend
{
    public string InnerExtendMsg { get; set; } = string.Empty;
}




/// <summary>
/// 模拟的复杂DTO
/// </summary>
[AutoDto<Person>(nameof(Person.Igrone))]
[AutoDtoComplex(3)]
public partial record PersonComplexDto;

/// <summary>
/// 没有复杂属性嵌套的DTO
/// </summary>
[AutoDto<Person>(nameof(Person.Igrone))]
public partial record PersonDto;
