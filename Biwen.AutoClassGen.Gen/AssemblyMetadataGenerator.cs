// <copyright file="SourceGenCodeFixProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Biwen.AutoClassGen;

/// <summary>
/// 生成程序集元数据
/// </summary>
[Generator]
public class AssemblyMetadataGenerator : IIncrementalGenerator
{
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
        nameof(TargetFrameworkAttribute),
        "UserSecretsIdAttribute"];

    private const string AssemblyVersionAttributeMetadataName = "System.Reflection.AssemblyVersionAttribute";
    private const string ONamespace = "build_property.rootnamespace";//命名空间
    private const string ODir = "build_property.projectdir";//项目目录
    private const string ProjExt = ".csproj";//项目文件扩展名

    private readonly record struct AssemblyConstant(string Name, string Value);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //proj file
        var projInc = context.AnalyzerConfigOptionsProvider.Select((pvd, _) =>
        {
            //取得项目目录
            var flag = pvd.GlobalOptions.TryGetValue(ODir, out var root);
            if (!flag)
                return null;

            //取得命名空间
            pvd.GlobalOptions.TryGetValue(ONamespace, out var @namespace);

            //查找csproj文件
            var files = Directory.GetFiles(root, $"*{ProjExt}", SearchOption.TopDirectoryOnly);

            return files.Any() ? files[0] : null;
        });

        //获取根命名空间
        var rootNamespace = context.AnalyzerConfigOptionsProvider.Select((pvd, _) =>
        {
            pvd.GlobalOptions.TryGetValue(ONamespace, out var @namespace);
            return @namespace;
        });

        //获取程序集元数据
        var attrs = context.SyntaxProvider.ForAttributeWithMetadataName(
            AssemblyVersionAttributeMetadataName,
            (context, _) => context is CompilationUnitSyntax,
            (syntaxContext, _) =>
            {
                var attributes = syntaxContext.TargetSymbol.GetAttributes();
                var constants = new List<AssemblyConstant>();

                foreach (var attribute in attributes)
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
                        var value = argument.ToCSharpString() ?? string.Empty;

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
                        var value = valueArgument.ToCSharpString() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                            continue;

                        // prevent duplicates
                        if (constants.Any(c => c.Name == key))
                            continue;

                        var constant = new AssemblyConstant(key, value);
                        constants.Add(constant);
                    }
                }
                return constants;
            }).Where(c => c?.Count > 0);

        //合并数据
        var inc = attrs.Combine(rootNamespace).Combine(projInc);

        //生成源代码
        context.RegisterSourceOutput(inc, (ctx, info) =>
        {
            if (info.Right is not null)
            {
                // 读取文件
                var text = File.ReadAllText(info.Right);

                //<Biwen-AutoClassGen>gv=false;ga=false;</Biwen-AutoClassGen>
                //读取配置获取:Biwen-AutoClassGen.ga 如果等于false那么不生成:
                var flagMatch = new Regex(@"<Biwen-AutoClassGen>(.*?)</Biwen-AutoClassGen>", RegexOptions.Singleline).Match(text);

                if (flagMatch.Success)
                {
                    var flag = flagMatch.Groups[1].Value;
                    if (flag?.ToLower(System.Globalization.CultureInfo.CurrentCulture).Contains("ga=false") is true)
                        return;
                }
            }

            var constants = info.Left.Left;
            var rootNamespace = info.Left.Right;

            if (string.IsNullOrEmpty(rootNamespace))
                return;

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine($"namespace {rootNamespace}.Generated;");
            sb.AppendLine();
            sb.AppendLine($"[global::System.CodeDom.Compiler.GeneratedCode(\"{ThisAssembly.Product}\", \"{ThisAssembly.FileVersion}\")]");
            sb.AppendLine("public static class AssemblyMetadata");
            sb.AppendLine("{");
            foreach (var constant in constants)
            {
                sb.AppendLine($"public const string {constant.Name} = {constant.Value};");
            }
            sb.AppendLine("}");

            var source = sb.ToString().FormatContent();

            ctx.AddSource("AssemblyMetadata.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }
}