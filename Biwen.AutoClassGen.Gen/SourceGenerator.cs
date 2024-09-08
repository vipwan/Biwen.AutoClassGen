﻿// <copyright file="SourceGenerator.cs" company="vipwan">
//MIT Copyright (c) vipwan. All rights reserved.
// </copyright>
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Biwen.AutoClassGen;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{

    private const string AttributeMetadataName = "Biwen.AutoClassGen.Attributes.AutoGenAttribute";
    private const string AttributeValueMetadataName = "AutoGen";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        #region AutoGenAttribute


        var nodes = context.SyntaxProvider.ForAttributeWithMetadataName(
          AttributeMetadataName,
          (context, _) => true,
          (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypes =
            context.CompilationProvider.Combine(nodes);

        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedNodes(source.Item1, source.Item2, spc));

        #endregion
    }

    /// <summary>
    /// Gen AutoGenAttribute
    /// </summary>
    /// <param name="compilation"></param>
    /// <param name="nodes"></param>
    /// <param name="context"></param>
    private static void HandleAnnotatedNodes(Compilation compilation, ImmutableArray<SyntaxNode> nodes, SourceProductionContext context)
    {
        if (nodes.Length == 0) return;

        StringBuilder envStringBuilder = new();

        envStringBuilder.AppendLine("// <auto-generated />");
        envStringBuilder.AppendLine("// author:vipwan@outlook.com 万雅虎");
        envStringBuilder.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
        envStringBuilder.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
        envStringBuilder.AppendLine("// This file is generated by Biwen.AutoClassGen.SourceGenerator");
        envStringBuilder.AppendLine();
        envStringBuilder.AppendLine("using System;");
        envStringBuilder.AppendLine("using System.Collections.Generic;");
        envStringBuilder.AppendLine("using System.Text;");
        envStringBuilder.AppendLine("using System.Threading.Tasks;");
        envStringBuilder.AppendLine("#pragma warning disable");
        envStringBuilder.AppendLine();

        string classTemp = $"public partial class $className : $interfaceName {{ $body }}";

        foreach (InterfaceDeclarationSyntax node in nodes.AsEnumerable().Cast<InterfaceDeclarationSyntax>())
        {

            if (node.BaseList == null || !node.BaseList.Types.Any())
            {
                // 当前使用分析器SourceGenAnalyzer

                // issue error
                // context.ReportDiagnostic(Diagnostic.Create(InvalidDeclareError, node.GetLocation()));
                continue;
            }

            // var attributes = (node.AttributeLists.AsEnumerable().First(
            //    x => x.Attributes.Any(x => x.Name.ToFullString() == AttributeValueMetadataName))
            //    as AttributeListSyntax).Attributes;

            List<AttributeSyntax> attributeSyntaxes = [];
            foreach (var attr in node.AttributeLists.AsEnumerable())
            {
                var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                if (attrName == AttributeValueMetadataName)
                {
                    attributeSyntaxes.Add(attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataName));
                }
            }
            if (attributeSyntaxes.Count == 0)
            {
                continue;
            }


            var sb = new StringBuilder();
            sb.AppendLine("namespace $ni");
            sb.AppendLine("{");

            sb.AppendLine("$namespace");
            sb.AppendLine();

            sb.AppendLine("$classes");
            sb.AppendLine("}");

            foreach (var attribute in attributeSyntaxes)
            {
                var className = attribute.ArgumentList!.Arguments[0].ToString();
                var rootNamespace = attribute.ArgumentList!.Arguments[1].ToString();

                StringBuilder bodyBuilder = new();
                List<string> namespaces = [];
                StringBuilder bodyInnerBuilder = new();

                // 每个接口生成属性
                void GenProperty(TypeSyntax @interfaceType)
                {
                    var interfaceName = @interfaceType.ToString();

                    var symbols = compilation.GetSymbolsWithName(interfaceName);
                    foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                    {
                        var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                        //命名空间
                        if (!namespaces.Contains(fullNameSpace))
                        {
                            namespaces.Add(fullNameSpace);
                        }
                        symbol.GetMembers().OfType<IPropertySymbol>().ToList().ForEach(prop =>
                        {
                            //attributes:
                            var attributes = prop.GetAttributes();
                            string rawAttributes = string.Empty;
                            attributes.ToList().ForEach(attr =>
                            {
                                rawAttributes += $"[{attr}]\r\n";
                            });

                            //prop:
                            var raw = $"public {prop.Type.ToDisplayString()} {prop.Name} {{get;set;}}";
                            //body:
                            bodyInnerBuilder.AppendLine($"/// <inheritdoc cref=\"{interfaceName}.{prop.Name}\" />");
                            bodyInnerBuilder.AppendLine($"{rawAttributes}{raw}");
                        });
                    }
                }

                // 获取所有父接口
                List<TypeSyntax> allInterface = [];
                foreach (var baseType in node.BaseList.Types)
                {
                    allInterface.Add(baseType.Type);
                    var symbols = compilation.GetSymbolsWithName(baseType.Type.ToString());
                    var symbol = symbols.FirstOrDefault();
                    if (symbol.Kind == SymbolKind.NamedType)
                    {
                        foreach (var item in (symbol as ITypeSymbol)!.AllInterfaces.AsEnumerable())
                        {
                            allInterface.Add(SyntaxFactory.ParseTypeName(item.MetadataName));
                        }
                    }
                }

                // 所有父接口生成属性:
                allInterface.ForEach(GenProperty);

                var rawClass = classTemp.Replace("$className", className.Replace("\"", ""));
                rawClass = rawClass.Replace("$interfaceName", node.Identifier.ToString());
                rawClass = rawClass.Replace("$body", bodyInnerBuilder.ToString());
                // append:
                bodyBuilder.AppendLine(rawClass);

                string rawNamespace = string.Empty;
                namespaces.ForEach(ns => rawNamespace += $"using {ns};\r\n");

                var source = sb.ToString();
                source = source.Replace("$namespace", rawNamespace);
                source = source.Replace("$classes", bodyBuilder.ToString());
                source = source.Replace("$ni", rootNamespace.Replace("\"", ""));
                envStringBuilder.AppendLine(source);
            }
        }
        envStringBuilder.AppendLine("#pragma warning restore");
        var envSource = envStringBuilder.ToString();
        // format:
        envSource = envSource.FormatContent();
        context.AddSource($"Biwen.AutoClassGen.g.cs", SourceText.From(envSource, Encoding.UTF8));
    }

}