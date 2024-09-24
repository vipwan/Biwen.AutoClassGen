using Biwen.AutoClassGen.Analyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Biwen.AutoClassGen.CodeFixs;

/// <summary>
/// 自动给文件添加头部注释
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddFileHeaderCodeFixProvider))]
[Shared]
internal class AddFileHeaderCodeFixProvider : CodeFixProvider
{
    private const string Title = "添加文件头部信息";
    private const string ConfigFileName = "Biwen.AutoClassGen.Comment";
    private const string VarPrefix = "$";//变量前缀

    private const string DefaultComment = """
        // Licensed to the {Product} under one or more agreements.
        // The {Product} licenses this file to you under the MIT license.
        // See the LICENSE file in the project root for more information.
        """;

    #region regex

    private const RegexOptions ROptions = RegexOptions.Compiled | RegexOptions.Singleline;
    private static readonly Regex VersionRegex = new(@"<Version>(.*?)</Version>", ROptions);
    private static readonly Regex CopyrightRegex = new(@"<Copyright>(.*?)</Copyright>", ROptions);
    private static readonly Regex CompanyRegex = new(@"<Company>(.*?)</Company>", ROptions);
    private static readonly Regex DescriptionRegex = new(@"<Description>(.*?)</Description>", ROptions);
    private static readonly Regex AuthorsRegex = new(@"<Authors>(.*?)</Authors>", ROptions);
    private static readonly Regex ProductRegex = new(@"<Product>(.*?)</Product>", ROptions);
    private static readonly Regex TargetFrameworkRegex = new(@"<TargetFramework>(.*?)</TargetFramework>", ROptions);
    private static readonly Regex TargetFrameworksRegex = new(@"<TargetFrameworks>(.*?)</TargetFrameworks>", ROptions);
    private static readonly Regex ImportRegex = new(@"<Import Project=""(.*?)""", ROptions);

    #endregion

    private readonly record struct AssemblyConstant(string Name, string Value);

    public sealed override ImmutableArray<string> FixableDiagnosticIds => [FileHeaderAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => FixDocumentAsync(context.Document, diagnosticSpan, c),
                equivalenceKey: Title),
            diagnostic);

        return Task.CompletedTask;
    }

#pragma warning disable IDE0060 // 删除未使用的参数
    private static async Task<Document> FixDocumentAsync(Document document, TextSpan span, CancellationToken ct)
#pragma warning restore IDE0060 // 删除未使用的参数
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);

        if (root == null)
            return document;

        //从项目配置中获取文件头部信息
        var projFilePath = document.Project.FilePath ?? "C:\\test.csproj";//单元测试时没有文件路径,因此使用默认路径

        var projectDirectory = Path.GetDirectoryName(projFilePath);
        var configFilePath = Path.Combine(projectDirectory, ConfigFileName);

        var comment = DefaultComment;

        string? copyright = "MIT";
        string? author = Environment.UserName;
        string? company = string.Empty;
        string? description = string.Empty;
        string? title = document.Project.Name;
        string? version = document.Project.Version.ToString();
        string? product = document.Project.AssemblyName;
        string? file = Path.GetFileName(document.FilePath);
        string? targetFramework = string.Empty;
#pragma warning disable CA1305 // 指定 IFormatProvider
        string? date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
#pragma warning restore CA1305 // 指定 IFormatProvider


        if (File.Exists(configFilePath))
        {
            comment = File.ReadAllText(configFilePath, System.Text.Encoding.UTF8);
        }

        #region 查找程序集元数据

        // 加载项目文件:
        var text = File.ReadAllText(projFilePath, System.Text.Encoding.UTF8);
        // 载入Import的文件,例如 : <Import Project="..\Version.props" />
        // 使用正则表达式匹配Project:
        var importMatchs = ImportRegex.Matches(text);
        foreach (Match importMatch in importMatchs)
        {
            var importFile = Path.Combine(projectDirectory, importMatch.Groups[1].Value);
            if (File.Exists(importFile))
            {
                text += File.ReadAllText(importFile);
            }
        }

        //存在变量引用的时候,需要解析
        string RawVal(string old, string @default)
        {
            if (old == null)
                return @default;

            //当取得的版本号为变量引用:$(Version)的时候,需要再次解析
            if (old.StartsWith(VarPrefix, StringComparison.Ordinal))
            {
                var varName = old.Substring(2, old.Length - 3);
                var varMatch = new Regex($@"<{varName}>(.*?)</{varName}>", RegexOptions.Singleline).Match(text);
                if (varMatch.Success)
                {
                    return varMatch.Groups[1].Value;
                }
                //未找到变量引用,返回默
                return @default;
            }
            return old;
        }

        var versionMatch = VersionRegex.Match(text);
        var copyrightMath = CopyrightRegex.Match(text);
        var companyMatch = CompanyRegex.Match(text);
        var descriptionMatch = DescriptionRegex.Match(text);
        var authorsMatch = AuthorsRegex.Match(text);
        var productMatch = ProductRegex.Match(text);
        var targetFrameworkMatch = TargetFrameworkRegex.Match(text);
        var targetFrameworksMatch = TargetFrameworksRegex.Match(text);

        if (versionMatch.Success)
        {
            version = RawVal(versionMatch.Groups[1].Value, version);
        }
        if (copyrightMath.Success)
        {
            copyright = RawVal(copyrightMath.Groups[1].Value, copyright);
        }
        if (companyMatch.Success)
        {
            company = RawVal(companyMatch.Groups[1].Value, company);
        }
        if (descriptionMatch.Success)
        {
            description = RawVal(descriptionMatch.Groups[1].Value, description);
        }
        if (authorsMatch.Success)
        {
            author = RawVal(authorsMatch.Groups[1].Value, author);
        }
        if (productMatch.Success)
        {
            product = RawVal(productMatch.Groups[1].Value, product);
        }
        if (targetFrameworkMatch.Success)
        {
            targetFramework = RawVal(targetFrameworkMatch.Groups[1].Value, targetFramework);
        }
        if (targetFrameworksMatch.Success)
        {
            targetFramework = RawVal(targetFrameworksMatch.Groups[1].Value, targetFramework);
        }

        #endregion

        //使用正则表达式替换
        comment = Regex.Replace(comment, @"\{(?<key>[^}]+)\}", m =>
        {
            var key = m.Groups["key"].Value;
            return key switch
            {
                "Product" => product,
                "Title" => title,
                "Version" => version,
                "Date" => date,
                "Author" => author,
                "Company" => company,
                "Copyright" => copyright,
                "File" => file,
                "Description" => description,
                "TargetFramework" => targetFramework,
                _ => m.Value,
            };
        }, RegexOptions.Singleline);

        var newRoot = root.WithLeadingTrivia(SyntaxFactory.Comment(comment + Environment.NewLine));
        return document.WithSyntaxRoot(newRoot);
    }
}
