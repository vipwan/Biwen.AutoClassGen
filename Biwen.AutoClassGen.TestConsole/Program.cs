using Biwen.AutoClassGen;
using Biwen.AutoClassGen.TestConsole.Decors;
using Biwen.AutoClassGen.TestConsole.Dtos;
using Biwen.AutoClassGen.TestConsole.Entitys;


var builder = WebApplication.CreateBuilder();


builder.Services.AddScoped<IHelloService, HelloService>();
builder.Services.AddScoped<ClassService>();

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


Console.WriteLine($"{queryRequest.KeyWord}");
Console.WriteLine($"I`m {nameof(userDto)} {userDto.FirstName} {userDto.LastName} I`m {userDto.Age} years old");
Console.WriteLine($"I`m {nameof(user2Dto)} {user2Dto.FirstName} {user2Dto.LastName} I`m {user2Dto.Age} years old");
Console.WriteLine($"I`m {nameof(user3Dto)} {user3Dto.FirstName} {user3Dto.LastName} I`m {user3Dto.Age} years old");



Console.ReadLine();