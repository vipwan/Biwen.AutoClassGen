using Biwen.AutoClassGen;


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Biwen.AutoClassGen.Models.QueryRequest queryRequest = new()
{
    PageLen = 10,
    CurrentPage = 1,
    KeyWord = "biwen"
};

Biwen.AutoClassGen.TestConsole.Dtos.UserDto userDto = new()
{
    FirstName = "biwen",
    LastName = "wan",
    Age = 18,
};


Console.WriteLine($"{queryRequest.KeyWord}");
Console.WriteLine($"I`m {userDto.FirstName} {userDto.LastName} I`m {userDto.Age} years old");

Console.ReadLine();