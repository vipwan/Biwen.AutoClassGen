// Licensed to the Test Console App under one or more agreements.
// The Test Console App licenses this file to you under the MIT license. 
// See the LICENSE file in the project root for more information.13456789
// This is a test console App
// Test Console App Program.cs 
// 2025-04-09 03:32:13  Biwen.AutoClassGen.TestConsole 万雅虎

using Biwen.AutoClassGen;
using Biwen.AutoClassGen.TestConsole.Decors;
using Biwen.AutoClassGen.TestConsole.Dtos;
using Biwen.AutoClassGen.TestConsole.Entitys;
using Biwen.AutoClassGen.TestConsole.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder();

// get version
Console.WriteLine(Biwen.AutoClassGen.TestConsole.Generated.Version.FileVersion);
Console.WriteLine(Biwen.AutoClassGen.TestConsole.Generated.Version.Current);
Console.WriteLine(Biwen.AutoClassGen.TestConsole.Generated.Version.AssemblyVersion);

// get assembly info
Console.WriteLine(Biwen.AutoClassGen.TestConsole.Generated.AssemblyMetadata.TargetFramework);

// Add services to the container.
builder.Services.AddLogging(config =>
{
    config.AddConsole();
});


builder.Services.AddScoped<IHelloService, HelloService>();
builder.Services.AddScoped<ClassService>();

// add auto inject
Biwen.AutoClassGen.TestConsole.AutoInjectExtension.AddAutoInject(builder.Services);

// add auto decor
builder.Services.AddAutoDecor();

var app = builder.Build();


using var scope = app.Services.CreateScope();
// get IHelloService
var svc = scope.ServiceProvider.GetRequiredService<IHelloService>();
var result1 = svc.SayHello("IHelloService");
Console.WriteLine(result1);

// get ClassService
var svc2 = scope.ServiceProvider.GetRequiredService<ClassService>();
var result2 = svc2.SayHello("ClassService");
Console.WriteLine(result2);


// get auto inject svc
//var svcInject = scope.ServiceProvider.GetRequiredService<ITest2Service>();
//var result3 = svcInject.Say2("from auto inject");
//Console.WriteLine(result3);


// get auto inject svc
var myService = scope.ServiceProvider.GetRequiredService<Biwen.AutoClassGen.TestConsole.Services2.MyService>();
var result4 = myService.Hello("from my service");
Console.WriteLine(result4);

// get keyed service
var keyedService = scope.ServiceProvider.GetRequiredKeyedService<ITest2Service>("test2");
var result5 = keyedService.Say2("from keyed service");
Console.WriteLine(result5);


// auto decorate for 
var forService = scope.ServiceProvider.GetRequiredService<IHelloServiceFor>();
var result6 = forService.SayHello("from IHelloServiceFor");
Console.WriteLine(result6);




Biwen.AutoClassGen.Models.QueryRequest queryRequest = new()
{
    PageLen = 10,
    CurrentPage = 1,
    KeyWord = "biwen"
};



Biwen.AutoClassGen.TestConsole.Dtos.UserDto userDto2 = new()
{
    FirstName = "biwen",
    LastName = "wan",
    Age = 18,
};


var user = new User
{
    Age = 18,
    Email = "vipwan@ms.co.ltd",
    FirstName = "biwen",
    LastName = "wan",
    Id = "001",
    Remark = "this is a test",
};

// mapper to UserDto
var userDto = user.MapperToUserDto();
// mapper to User2Dto
var user2Dto = user.MapperToUser2Dto();

// from [AutoDto<T>(params string?[])]
var user3Dto = user.MapperToUser3Dto();


var user2 = user3Dto.MapperToUser();


var venue = new Venue
{
    Address = "No1 street",
    BusinessId = "123456",
    Images = [new VenueImage { OrderId = 1234, Url = "img1" }],
    Name = "test"
};

var venuesDto = venue.MapperToVenueDto();



Console.WriteLine($"{queryRequest.KeyWord}");
Console.WriteLine($"I`m {nameof(userDto)} {userDto.FirstName} {userDto.LastName} I`m {userDto.Age} years old");
Console.WriteLine($"I`m {nameof(user2Dto)} {user2Dto.FirstName} {user2Dto.LastName} I`m {user2Dto.Age} years old");
Console.WriteLine($"I`m {nameof(user3Dto)} {user3Dto.FirstName} {user3Dto.LastName} I`m {user3Dto.Age} years old");

Console.WriteLine($"I`m {nameof(user)}2 {user2.FirstName} {user2.LastName} I`m {user2.Age} years old");

Console.WriteLine($"I`m {nameof(venuesDto)} {venuesDto.Name} {venuesDto.Address} I have {venuesDto.Images?.Count} images");


// project to dto

var user3 = new User
{
    Age = 20,
    Email = "vipwan2@ms.co.ltd",
    FirstName = "biwen2",
    LastName = "wan2",
    Id = "001",
    Remark = "this is a test",
};

var users = new List<User> { user, user3 };
var list = users.AsQueryable().ProjectToUserDto().ToList();

Console.WriteLine("project to dto:");
Console.WriteLine(JsonSerializer.Serialize(list, options: new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
}));

//comolex dto
var person = new Person
{
    Address = new Address
    {
        City = "Shenzhen",
        State = "Shenzhen",
        Street = "No1 street",
        ZipCode = "100000",
    },
    Age = 18,
    Name = "万雅虎",
    Igrone = "这不重要",
    Igrone2 = "这也不重要",
    Hobbies =
    [
        new Hobby
        {
            Name = "basketball",
            Description = "I like basketball",
            Extend = new HobbyExtend
            {
                Extend1 = "extend1",
                Extend2 = "extend2",
                Extend3 = new InnerExtend{
                  InnerExtendMsg = "inner extend msg",
                }
            }
        },
        new Hobby
        {
            Name = "football",
            Description = "I like football",
            Extend =new HobbyExtend{
                Extend1 = "extend11",
                Extend2 = "extend21",
            }
        }
    ],
};

var personDto = person.MapperToPersonDto();
//当前因为没有实现多层嵌套，所以二级一下属性没有映射到DTO
personDto.Hobbies[0].Extend.Extend1 = "extend1 ex5555555";
personDto.Hobbies[0].Extend.Extend3.InnerExtendMsg = "hhhhhhhhh";

Console.WriteLine($"I`m {personDto.Address.GetType().FullName} {personDto.Name} I`m {personDto.Age} years old");
Console.WriteLine(JsonSerializer.Serialize(personDto, options: new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
}));


// complex project to dto
var personComplexDto = person.MapperToPersonComplexDto();
//支持到多层嵌套,因此内部复杂对象的属性也会映射到DTO
personComplexDto.Hobbies[0].Extend.Extend1 = "extend1 ex43432423";
personComplexDto.Hobbies[0].Extend.Extend3.InnerExtendMsg = "hhhhhhhhh";


Console.WriteLine($"I`m {personComplexDto.Address.GetType().FullName} {personComplexDto.Name} I`m {personComplexDto.Age} years old");
Console.WriteLine(JsonSerializer.Serialize(personComplexDto, options: new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
}));




//提供对AutoDescriptionAttribute的支持
var colorEnum = Biwen.AutoClassGen.TestConsole.ColorEnum.Red;
var colorEnum2 = Biwen.AutoClassGen.TestConsole.ColorEnum.LightBlue;

Console.WriteLine($"From Description: {colorEnum.Description()}, Default: {colorEnum2.Description()}");


Console.ReadLine();