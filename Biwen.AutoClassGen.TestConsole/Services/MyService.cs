namespace Biwen.AutoClassGen.TestConsole.Services;

/// <summary>
/// 测试服务
/// </summary>

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

