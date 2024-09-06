using Biwen.AutoClassGen.DiagnosticAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Versioning;
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


    private static readonly HashSet<string> _attributes = [
    nameof(AssemblyCompanyAttribute),
        nameof(AssemblyConfigurationAttribute),
        nameof(AssemblyCopyrightAttribute),
        nameof(AssemblyCultureAttribute),
        nameof(AssemblyDelaySignAttribute),
        nameof(AssemblyDescriptionAttribute),
        nameof(AssemblyFileVersionAttribute),
        nameof(AssemblyInformationalVersionAttribute),
        nameof(AssemblyKeyFileAttribute),
        nameof(AssemblyKeyNameAttribute),
        nameof(AssemblyMetadataAttribute),
        nameof(AssemblyProductAttribute),
        nameof(AssemblySignatureKeyAttribute),
        nameof(AssemblyTitleAttribute),
        nameof(AssemblyTrademarkAttribute),
        nameof(AssemblyVersionAttribute),
        nameof(NeutralResourcesLanguageAttribute),
        nameof(TargetFrameworkAttribute)];

    private readonly record struct AssemblyConstant(string Name, string Value);

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

        document.Project.TryGetCompilation(out var compilation);
        var constants = new List<AssemblyConstant>();

        //当Assembly为Microsoft.CodeAnalysis.TypedConstant时不做处理

        if (!compilation?.AssemblyName?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) is true)
        {
            var assemblyAttributes = compilation?.Assembly.GetAttributes();
            if (assemblyAttributes is not null)
            {
                foreach (var attribute in assemblyAttributes)
                {
                    var name = attribute.AttributeClass?.Name;
                    if (name == null || !_attributes.Contains(name))
                        continue;

                    if (attribute.ConstructorArguments.Length == 1)
                    {
                        // remove Assembly
                        if (name.Length > 8 && name.StartsWith("Assembly", StringComparison.Ordinal))
                            name = name.Substring(8);

                        // remove Attribute
                        if (name.Length > 9)
                            name = name.Substring(0, name.Length - 9);

                        var argument = attribute.ConstructorArguments.FirstOrDefault();
                        var value = argument.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(value))
                            continue;

                        var constant = new AssemblyConstant(name, value);
                        constants.Add(constant);
                    }
                    else if (name == nameof(AssemblyMetadataAttribute) && attribute.ConstructorArguments.Length == 2)
                    {
                        var nameArgument = attribute.ConstructorArguments[0];
                        var key = nameArgument.Value?.ToString() ?? string.Empty;

                        var valueArgument = attribute.ConstructorArguments[1];
                        var value = valueArgument.ToString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                            continue;

                        // prevent duplicates
                        if (constants.Any(c => c.Name == key))
                            continue;

                        var constant = new AssemblyConstant(key, value);
                        constants.Add(constant);
                    }
                }
            }
            foreach (var constant in constants)
            {
                var key = constant.Name;
                var value = constant.Value;

                switch (key)
                {
                    case "Author":
                        author = value;
                        break;
                    case "Company":
                        company = value;
                        break;
                    case "Configuration":
                        break;
                    case "Copyright":
                        copyright = value;
                        break;
                    case "Culture":
                        break;
                    case "Description":
                        description = value;
                        break;
                    case "FileVersion":
                        break;
                    case "InformationalVersion":
                        break;
                    case "Product":
                        product = value;
                        break;
                    case "Title":
                        title = value;
                        break;
                    case "Trademark":
                        break;
                    case "Version":
                        version = value;
                        break;
                    case "TargetFramework":
                        targetFramework = value;
                        break;
                    default:
                        break;
                }
            }
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
