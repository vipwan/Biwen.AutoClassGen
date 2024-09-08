namespace Biwen.AutoClassGen;

internal static class StringExtensions
{
    /// <summary>
    /// 格式化代码
    /// </summary>
    /// <param name="csCode"></param>
    /// <returns></returns>
    public static string FormatContent(this string csCode)
    {
        var tree = CSharpSyntaxTree.ParseText(csCode);
        var root = tree.GetRoot().NormalizeWhitespace();
        var ret = root.ToFullString();
        return ret;
    }
}
