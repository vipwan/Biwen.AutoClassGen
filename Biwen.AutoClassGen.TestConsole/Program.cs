using Biwen.AutoClassGen;


// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Biwen.AutoClassGen.Models.QueryRequest queryRequest = new()
{
    PageLen = 10,
    CurrentPage = 1,
    KeyWord = "biwen"
};

Console.WriteLine($"{queryRequest.KeyWord}");


Console.ReadLine();