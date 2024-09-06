using Biwen.AutoClassGen.DiagnosticAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Biwen.AutoClassGen.CodeFixProviders;

/// <summary>
/// 自动给文件添加头部注释
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddFileHeaderCodeFixProvider))]
[Shared]
public class AddFileHeaderCodeFixProvider : CodeFixProvider
{
    private const string Title = "添加文件头部信息";
    private const string ConfigFileName = "Biwen.AutoClassGen.Comment";

    private const string DefaultComment = """
        // Licensed to the {Product} under one or more agreements.
        // The {Product} licenses this file to you under the MIT license.
        // See the LICENSE file in the project root for more information.
        """;

    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return [FileHeaderAnalyzer.DiagnosticId]; }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

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

    private static async Task<Document> FixDocumentAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        //从项目配置中获取文件头部信息
        var projFilePath = document.Project.FilePath ?? "C:\\test.csproj";//单元测试时没有文件路径,因此使用默认路径

        var projectDirectory = Path.GetDirectoryName(projFilePath);
        var configFilePath = Path.Combine(projectDirectory, ConfigFileName);

        var comment = DefaultComment;

        string? copyright = "MIT";
        string? author = Environment.UserName;
        string? title = document.Project.Name;
        string? version = document.Project.Version.ToString();
        string? product = document.Project.AssemblyName;
        string? file = Path.GetFileName(document.FilePath);
#pragma warning disable CA1305 // 指定 IFormatProvider
        string? date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
#pragma warning restore CA1305 // 指定 IFormatProvider

        if (File.Exists(configFilePath))
        {
            comment = File.ReadAllText(configFilePath, System.Text.Encoding.UTF8);
        }

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
                "Copyright" => copyright,
                "File" => file,
                _ => m.Value,
            };
        });

        var headerComment = SyntaxFactory.Comment(comment + Environment.NewLine);
        var newRoot = root?.WithLeadingTrivia(headerComment);
        if (newRoot == null)
        {
            return document;
        }
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument;
    }
}
