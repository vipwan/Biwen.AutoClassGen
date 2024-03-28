namespace Biwen.AutoClassGen.TestConsole.Services2;

/// <summary>
/// 测试服务
/// </summary>
[AutoInject]
[AutoInject]
public class MyService
{
    public string Hello(string name)
    {
        var str = $"Hello {name}";
        Console.WriteLine(str);
        return str;
    }
}