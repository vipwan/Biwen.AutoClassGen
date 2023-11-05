using Biwen.AutoClassGen;
using Biwen.AutoClassGen.TestConsole.Entitys;


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

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

//mapper to UserDto
var userDto = user.MapperToUserDto();
//mapper to User2Dto
var user2Dto = user.MapperToUser2Dto();

Console.WriteLine($"{queryRequest.KeyWord}");
Console.WriteLine($"I`m {userDto.FirstName} {userDto.LastName} I`m {userDto.Age} years old");

Console.ReadLine();