using Biwen.AutoClassGen;
using Biwen.AutoClassGen.TestConsole.Decors;
using Biwen.AutoClassGen.TestConsole.Dtos;
using Biwen.AutoClassGen.TestConsole.Entitys;
using Biwen.AutoClassGen.TestConsole.Services;

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
var svcInject = scope.ServiceProvider.GetRequiredService<ITest2Service>();
var result3 = svcInject.Say2("from auto inject");
Console.WriteLine(result3);


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


Console.WriteLine($"I`m {nameof(venuesDto)} {venuesDto.Name} {venuesDto.Address} I have {venuesDto.Images?.Count} images");




Console.ReadLine();