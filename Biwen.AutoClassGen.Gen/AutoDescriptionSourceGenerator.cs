// <copyright file="AutoDescriptionSourceGenerator.cs" company="vipwan">
//MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Biwen.AutoClassGen;

[Generator]
public class AutoDescriptionSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 注册提供程序以获取所有包含AutoDescriptionAttribute的枚举
        var enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, ct) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 将枚举类型声明合并到单个输出
        var compilationAndEnums = context.CompilationProvider.Combine(enumDeclarations.Collect());

        // 生成代码
        context.RegisterSourceOutput(compilationAndEnums, static (spc, source) =>
        Execute(spc, source.Left, source.Right));
    }

    /// <summary>
    /// 判断是否是需要处理的语法节点（枚举声明）
    /// </summary>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
        => node is EnumDeclarationSyntax { AttributeLists.Count: > 0 };

    /// <summary>
    /// 获取语义模型目标，确认是否有AutoDescriptionAttribute特性
    /// </summary>
    private static EnumDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var enumDeclarationSyntax = (EnumDeclarationSyntax)context.Node;

        // 检查枚举是否标记了AutoDescriptionAttribute
        foreach (var attributeList in enumDeclarationSyntax.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                string attributeName = attribute.Name.ToString();

                // 直接名称匹配，支持多种形式
                if (attributeName == "AutoDescription" ||
                    attributeName == "AutoDescriptionAttribute")
                {
                    return enumDeclarationSyntax;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 执行代码生成
    /// </summary>
    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<EnumDeclarationSyntax?> enums)
    {
        if (enums.IsDefaultOrEmpty)
        {
            return;
        }

        // 收集所有标记了AutoDescriptionAttribute的枚举类型及其成员信息
        var enumsToProcess = new List<EnumToProcess>();

        foreach (var enumDeclaration in enums)
        {
            if (enumDeclaration == null) continue;
            var semanticModel = compilation.GetSemanticModel(enumDeclaration.SyntaxTree);
            var enumSymbol = semanticModel.GetDeclaredSymbol(enumDeclaration);
            if (enumSymbol == null) continue;

            var enumNamespace = GetNamespace(enumSymbol);
            var enumName = enumSymbol.Name;
            var enumMembers = new List<EnumMemberInfo>();

            // 处理枚举的每个成员
            foreach (var member in enumSymbol.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.ConstantValue != null)
                {
                    string description = GetEnumMemberDescription(member);
                    enumMembers.Add(new EnumMemberInfo(member.Name, description));
                }
            }

            enumsToProcess.Add(new EnumToProcess(enumSymbol, enumNamespace, enumName, enumMembers));
        }

        // 生成单一文件的枚举扩展类
        if (enumsToProcess.Count > 0)
        {
            var langVersion = (compilation as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.Default;
            GenerateUnifiedExtensionFile(context, enumsToProcess, langVersion);
        }
    }

    /// <summary>
    /// 获取枚举成员的描述
    /// </summary>
    private static string GetEnumMemberDescription(IFieldSymbol member)
    {
        // 尝试获取 System.ComponentModel.DescriptionAttribute
        var descAttr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute");

        if (descAttr != null && descAttr.ConstructorArguments.Length > 0)
        {
            return descAttr.ConstructorArguments[0].Value?.ToString() ?? member.Name;
        }

        // 尝试获取 System.ComponentModel.DataAnnotations.DisplayAttribute
        var displayAttr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.ComponentModel.DataAnnotations.DisplayAttribute");

        if (displayAttr != null)
        {
            foreach (var namedArg in displayAttr.NamedArguments)
            {
                if (namedArg.Key == "Name" && namedArg.Value.Value != null)
                {
                    return namedArg.Value.Value.ToString() ?? member.Name;
                }
            }
        }

        // 如果没有找到描述特性，返回枚举成员名称
        return member.Name;
    }

    /// <summary>
    /// 获取符号的命名空间
    /// </summary>
    private static string GetNamespace(ISymbol symbol)
    {
        string result = string.Empty;
        INamespaceSymbol? namespaceSymbol = symbol.ContainingNamespace;

        while (namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace)
        {
            result = namespaceSymbol.Name + (string.IsNullOrEmpty(result) ? "" : ".") + result;
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return result;
    }

    /// <summary>
    /// 生成统一的枚举扩展类文件
    /// 根据语言版本选择是否使用 switch 表达式 (>= C#8) 或 传统 switch 语句 (小于 C#8)
    /// </summary>
    private static void GenerateUnifiedExtensionFile(SourceProductionContext context, List<EnumToProcess> enumsToProcess, LanguageVersion languageVersion)
    {
        var sourceBuilder = new StringBuilder();

        sourceBuilder.AppendLine("// <auto-generated/>");
        sourceBuilder.AppendLine("// author:vipwan@outlook.com 万雅虎");
        sourceBuilder.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
        sourceBuilder.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
        sourceBuilder.AppendLine("// This file is generated by Biwen.AutoClassGen.AutoDescriptionSourceGenerator");
        sourceBuilder.AppendLine("using System;");

        HashSet<string> namespaces = [];
        foreach (var enumInfo in enumsToProcess)
        {
            if (!string.IsNullOrEmpty(enumInfo.Namespace))
            {
                namespaces.Add(enumInfo.Namespace);
            }
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("namespace System");
        sourceBuilder.AppendLine("{");
        sourceBuilder.AppendLine("    /// <summary>");
        sourceBuilder.AppendLine("    /// Auto-generated extensions for enum descriptions");
        sourceBuilder.AppendLine("    /// </summary>");
        sourceBuilder.AppendLine($"    [global::System.CodeDom.Compiler.GeneratedCode(\"Biwen.AutoClassGen\", \"{ThisAssembly.FileVersion}\")]");
        sourceBuilder.AppendLine("    public static partial class EnumDescriptionExtensions");
        sourceBuilder.AppendLine("    {");

        bool useSwitchExpression = languageVersion >= LanguageVersion.CSharp8;

        foreach (var enumInfo in enumsToProcess.OrderBy(e => e.Namespace).ThenBy(e => e.Name))
        {
            var fullEnumName = string.IsNullOrEmpty(enumInfo.Namespace)
                ? enumInfo.Name
                : $"{enumInfo.Namespace}.{enumInfo.Name}";

            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine($"        /// Gets the description for {fullEnumName} value");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        /// <param name=\"val\">The enum value</param>");
            sourceBuilder.AppendLine("        /// <returns>The description string</returns>");

            if (useSwitchExpression)
            {
                // C#8+ 版本使用 switch 表达式
                sourceBuilder.AppendLine($"        public static string Description(this {fullEnumName} val) => val switch");
                sourceBuilder.AppendLine("        {");
                foreach (var member in enumInfo.Members)
                {
                    sourceBuilder.AppendLine($"            {fullEnumName}.{member.Name} => \"{member.Description}\",");
                }
                sourceBuilder.AppendLine("            _ => val.ToString(),");
                sourceBuilder.AppendLine("        };");
            }
            else
            {
                // C#8 以下版本使用传统 switch 语句，避免 CS8370 (如递归模式 / switch 表达式 不可用)
                sourceBuilder.AppendLine($"        public static string Description(this {fullEnumName} val)");
                sourceBuilder.AppendLine("        {");
                sourceBuilder.AppendLine("            switch (val)");
                sourceBuilder.AppendLine("            {");
                foreach (var member in enumInfo.Members)
                {
                    sourceBuilder.AppendLine($"                case {fullEnumName}.{member.Name}: return \"{member.Description}\";");
                }
                sourceBuilder.AppendLine("                default: return val.ToString();");
                sourceBuilder.AppendLine("            }");
                sourceBuilder.AppendLine("        }");
            }
        }

        sourceBuilder.AppendLine("    }");
        sourceBuilder.AppendLine("}");

        var source = sourceBuilder.ToString().FormatContent();
        context.AddSource("EnumDescriptionExtensions.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    // 枚举信息记录类型
    private record EnumToProcess(INamedTypeSymbol Symbol, string Namespace, string Name, List<EnumMemberInfo> Members);
    private record EnumMemberInfo(string Name, string Description);
}
