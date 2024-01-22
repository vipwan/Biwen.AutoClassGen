// <copyright file="SourceGenerator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Biwen.AutoClassGen
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using Desc = DiagnosticDescriptors;

    [Generator(LanguageNames.CSharp)]
    public class SourceGenerator : IIncrementalGenerator
    {

        private const string AttributeMetadataName = "Biwen.AutoClassGen.Attributes.AutoGenAttribute";
        private const string AttributeValueMetadataName = "AutoGen";

        private const string AttributeMetadataNameDto = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute";
        private const string AttributeValueMetadataNameDto = "AutoDto";

        /// <summary>
        /// 泛型AutoDtoAttribute
        /// </summary>
        private const string AttributeMetadataNameDtoG = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute`1";


        private const string AttributeMetadataNameDecor = "Biwen.AutoClassGen.Attributes.AutoDecorAttribute";

        //private const string AttributeValueMetadataNameDecor = "AutoDecor";
        /// <summary>
        /// 泛型AutoDecorAttribute
        /// </summary>
        private const string AttributeMetadataNameDecorG = "Biwen.AutoClassGen.Attributes.AutoDecorAttribute`1";




        public void Initialize(IncrementalGeneratorInitializationContext context)
        {


            #region AutoGenAttribute


            var nodes = context.SyntaxProvider.ForAttributeWithMetadataName(
              AttributeMetadataName,
              (context, attributeSyntax) => true,
              (syntaxContext, _) => syntaxContext.TargetNode).Collect();

            IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypes =
                context.CompilationProvider.Combine(nodes);

            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedNodes(source.Item1, source.Item2, spc));

            #endregion

            #region AutoDtoAttribute

            var nodesDto = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeMetadataNameDto,
                (context, attributeSyntax) => true,
                (syntaxContext, _) => syntaxContext.TargetNode).Collect();

            IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesDto =
                context.CompilationProvider.Combine(nodesDto);

            context.RegisterSourceOutput(compilationAndTypesDto, static (spc, source) => HandleAnnotatedNodesDto(source.Item1, source.Item2, spc));

            #endregion

            #region AutoDtoAttributeG

            var nodesDtoG = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeMetadataNameDtoG,
                (context, attributeSyntax) => true,
                (syntaxContext, _) => syntaxContext.TargetNode).Collect();

            IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesDtoG =
                context.CompilationProvider.Combine(nodesDtoG);

            context.RegisterSourceOutput(compilationAndTypesDtoG, static (spc, source) => HandleAnnotatedNodesDtoG(source.Item1, source.Item2, spc));

            #endregion


            #region AutoDecorAttribute

            var nodesDecor = context.SyntaxProvider.ForAttributeWithMetadataName(
                               AttributeMetadataNameDecor,
                               (context, attributeSyntax) => true,
                               (syntaxContext, _) => syntaxContext.TargetSymbol).Collect();

            var nodesDecorG = context.SyntaxProvider.ForAttributeWithMetadataName(
                   AttributeMetadataNameDecorG,
                   (context, attributeSyntax) => true,
                   (syntaxContext, _) => syntaxContext.TargetSymbol).Collect();

            IncrementalValueProvider<((Compilation, ImmutableArray<ISymbol>), ImmutableArray<ISymbol>)> compilationAndTypesDecor =
                context.CompilationProvider.Combine(nodesDecor).Combine(nodesDecorG);

            context.RegisterSourceOutput(compilationAndTypesDecor, static (spc, source) =>
            HandleAnnotatedNodesDecor(source.Item1.Item1, source.Item1.Item2, source.Item2, spc));

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
            envSource = FormatContent(envSource);
            context.AddSource($"Biwen.AutoClassGen.g.cs", SourceText.From(envSource, Encoding.UTF8));
        }

        /// <summary>
        /// Gen AutoDtoAttribute
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="nodes"></param>
        /// <param name="context"></param>
        private static void HandleAnnotatedNodesDto(Compilation compilation, ImmutableArray<SyntaxNode> nodes, SourceProductionContext context)
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

            foreach (ClassDeclarationSyntax node in nodes.AsEnumerable().Cast<ClassDeclarationSyntax>())
            {
                AttributeSyntax? attributeSyntax = null;
                foreach (var attr in node.AttributeLists.AsEnumerable())
                {
                    var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                    if (attrName == AttributeValueMetadataNameDto)
                    {
                        attributeSyntax = attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataNameDto);
                        break;
                    }
                }
                if (attributeSyntax == null)
                {
                    continue;
                }

                //转译的Entity类名
                var entityName = string.Empty;
                var eType = (attributeSyntax.ArgumentList!.Arguments[0].Expression as TypeOfExpressionSyntax)!.Type;
                if (eType.IsKind(SyntaxKind.IdentifierName))
                {
                    entityName = (eType as IdentifierNameSyntax)!.Identifier.ValueText;
                }
                else if (eType.IsKind(SyntaxKind.QualifiedName))
                {
                    entityName = (eType as QualifiedNameSyntax)!.ToString().Split(['.']).Last();
                }
                else if (eType.IsKind(SyntaxKind.AliasQualifiedName))
                {
                    entityName = (eType as AliasQualifiedNameSyntax)!.ToString().Split(['.']).Last();
                }
                if (string.IsNullOrEmpty(entityName))
                {
                    continue;
                }

                if (node.AttributeLists.AsEnumerable().Where(
                    x => x.Attributes[0].Name.ToString().IndexOf(AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0).Count() > 1)
                {
                    var location = node.GetLocation();
                    // issue error
                    context.ReportDiagnostic(Diagnostic.Create(Desc.MutiMarkedAutoDtoError, location));
                    continue;
                }

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"//generate {entityName}-{node.Identifier.ValueText}");
                sb.AppendLine();
                sb.AppendLine("namespace $ni");
                sb.AppendLine("{");
                sb.AppendLine("$namespace");
                sb.AppendLine("$classes");
                sb.AppendLine("}");
                // sb.AppendLine("#pragma warning restore");
                string classTemp = $"partial class $className  {{ $body }}";

                {
                    // 排除的属性
                    List<string> excapes = [];
                    for (var i = 1; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
                    {
                        var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
                        if (expressionSyntax.IsKind(SyntaxKind.InvocationExpression))
                        {
                            var name = (expressionSyntax as InvocationExpressionSyntax)!.ArgumentList.DescendantNodes().First().ToString();
                            excapes.Add(name.Split(['.']).Last());
                        }
                        else if (expressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
                        {
                            var name = (expressionSyntax as LiteralExpressionSyntax)!.Token.ValueText;
                            excapes.Add(name);
                        }
                    }

                    var className = node.Identifier.ValueText;
                    var rootNamespace = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().Single().Name.ToString();

                    StringBuilder bodyBuilder = new();
                    List<string> namespaces = [];
                    StringBuilder bodyInnerBuilder = new();
                    StringBuilder mapperBodyBuilder = new();

                    bodyInnerBuilder.AppendLine();

                    // 生成属性
                    void GenProperty(TypeSyntax @type)
                    {
                        var symbols = compilation.GetSymbolsWithName(type.ToString());
                        foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                        {
                            var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                            // 命名空间
                            if (!namespaces.Contains(fullNameSpace))
                            {
                                namespaces.Add(fullNameSpace);
                            }
                            symbol.GetMembers().OfType<IPropertySymbol>().ToList().ForEach(prop =>
                            {
                                if (!excapes.Contains(prop.Name))
                                {
                                    // prop:
                                    var raw = $"public {prop.Type.ToDisplayString()} {prop.Name} {{get;set;}}";
                                    // body:
                                    bodyInnerBuilder.AppendLine($"/// <inheritdoc cref=\"{@type}.{prop.Name}\" />");
                                    bodyInnerBuilder.AppendLine($"{raw}");

                                    // mapper:
                                    mapperBodyBuilder.AppendLine($"{prop.Name} = model.{prop.Name},");
                                }
                            });
                        }
                    }

                    // 生成属性:
                    var symbols = compilation.GetSymbolsWithName(entityName, SymbolFilter.Type);
                    var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();
                    GenProperty(SyntaxFactory.ParseTypeName(symbol.MetadataName));

                    // 生成父类的属性:
                    INamedTypeSymbol? baseType = symbol.BaseType;
                    while (baseType != null)
                    {
                        GenProperty(SyntaxFactory.ParseTypeName(baseType.MetadataName));
                        baseType = baseType.BaseType;
                    }

                    var rawClass = classTemp.Replace("$className", className);
                    rawClass = rawClass.Replace("$body", bodyInnerBuilder.ToString());
                    // append:
                    bodyBuilder.AppendLine(rawClass);

                    string rawNamespace = string.Empty;
                    namespaces.ForEach(ns => rawNamespace += $"using {ns};\r\n");

                    var source = sb.ToString();
                    source = source.Replace("$namespace", rawNamespace);
                    source = source.Replace("$classes", bodyBuilder.ToString());
                    source = source.Replace("$ni", rootNamespace);

                    // 生成Mapper
                    var mapperSource = MapperTemplate.Replace("$namespace", namespaces.First());
                    mapperSource = mapperSource.Replace("$ns", rootNamespace);
                    mapperSource = mapperSource.Replace("$baseclass", entityName);
                    mapperSource = mapperSource.Replace("$dtoclass", className);
                    mapperSource = mapperSource.Replace("$body", mapperBodyBuilder.ToString());

                    // 合并
                    source = $"{source}\r\n{mapperSource}";
                    envStringBuilder.AppendLine(source);
                }
            }

            envStringBuilder.AppendLine("#pragma warning restore");
            var envSource = envStringBuilder.ToString();
            // format:
            envSource = FormatContent(envSource);
            context.AddSource($"Biwen.AutoClassGenDto.g.cs", SourceText.From(envSource, Encoding.UTF8));
        }


        /// <summary>
        /// Gen AutoDtoAttribute G
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="nodes"></param>
        /// <param name="context"></param>
        private static void HandleAnnotatedNodesDtoG(Compilation compilation, ImmutableArray<SyntaxNode> nodes, SourceProductionContext context)
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

            foreach (ClassDeclarationSyntax node in nodes.AsEnumerable().Cast<ClassDeclarationSyntax>())
            {
                AttributeSyntax? attributeSyntax = null;
                foreach (var attr in node.AttributeLists.AsEnumerable())
                {
                    var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                    if (attrName?.IndexOf(AttributeValueMetadataNameDto, System.StringComparison.Ordinal) == 0)
                    {
                        attributeSyntax = attr.Attributes.First(x => x.Name.ToString().IndexOf(AttributeValueMetadataNameDto, System.StringComparison.Ordinal) == 0);
                        break;
                    }
                }
                if (attributeSyntax == null)
                {
                    continue;
                }


                //转译的Entity类名
                var entityName = string.Empty;

                string pattern = @"(?<=<)(?<type>\w+)(?=>)";
                var match = Regex.Match(attributeSyntax.ToString(), pattern);
                if (match.Success)
                {
                    entityName = match.Groups["type"].Value.Split(['.']).Last();
                }
                else
                {
                    continue;
                }

                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine($"//generate {entityName}-{node.Identifier.ValueText}");
                sb.AppendLine();
                sb.AppendLine("namespace $ni");
                sb.AppendLine("{");
                sb.AppendLine("$namespace");
                sb.AppendLine("$classes");
                sb.AppendLine("}");
                // sb.AppendLine("#pragma warning restore");
                string classTemp = $"partial class $className  {{ $body }}";

                {
                    // 排除的属性
                    List<string> excapes = [];

                    if (attributeSyntax.ArgumentList != null)
                    {
                        for (var i = 0; i < attributeSyntax.ArgumentList!.Arguments.Count; i++)
                        {
                            var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
                            if (expressionSyntax.IsKind(SyntaxKind.InvocationExpression))
                            {
                                var name = (expressionSyntax as InvocationExpressionSyntax)!.ArgumentList.DescendantNodes().First().ToString();
                                excapes.Add(name.Split(['.']).Last());
                            }
                            else if (expressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
                            {
                                var name = (expressionSyntax as LiteralExpressionSyntax)!.Token.ValueText;
                                excapes.Add(name);
                            }
                        }
                    }

                    var className = node.Identifier.ValueText;
                    var rootNamespace = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().Single().Name.ToString();

                    StringBuilder bodyBuilder = new();
                    List<string> namespaces = [];
                    StringBuilder bodyInnerBuilder = new();
                    StringBuilder mapperBodyBuilder = new();

                    bodyInnerBuilder.AppendLine();

                    // 生成属性
                    void GenProperty(TypeSyntax @type)
                    {
                        var symbols = compilation.GetSymbolsWithName(type.ToString());
                        foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                        {
                            var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                            // 命名空间
                            if (!namespaces.Contains(fullNameSpace))
                            {
                                namespaces.Add(fullNameSpace);
                            }
                            symbol.GetMembers().OfType<IPropertySymbol>().ToList().ForEach(prop =>
                            {
                                if (!excapes.Contains(prop.Name))
                                {
                                    // prop:
                                    var raw = $"public {prop.Type.ToDisplayString()} {prop.Name} {{get;set;}}";
                                    // body:
                                    bodyInnerBuilder.AppendLine($"/// <inheritdoc cref=\"{@type}.{prop.Name}\" />");
                                    bodyInnerBuilder.AppendLine($"{raw}");

                                    // mapper:
                                    // 只有public的属性才能赋值
                                    if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                                    {
                                        mapperBodyBuilder.AppendLine($"{prop.Name} = model.{prop.Name},");
                                    }
                                }
                            });
                        }
                    }

                    // 生成属性:
                    var symbols = compilation.GetSymbolsWithName(entityName, SymbolFilter.Type);
                    var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();
                    GenProperty(SyntaxFactory.ParseTypeName(symbol.MetadataName));

                    // 生成父类的属性:
                    INamedTypeSymbol? baseType = symbol.BaseType;
                    while (baseType != null)
                    {
                        GenProperty(SyntaxFactory.ParseTypeName(baseType.MetadataName));
                        baseType = baseType.BaseType;
                    }

                    var rawClass = classTemp.Replace("$className", className);
                    rawClass = rawClass.Replace("$body", bodyInnerBuilder.ToString());
                    // append:
                    bodyBuilder.AppendLine(rawClass);

                    string rawNamespace = string.Empty;
                    namespaces.ForEach(ns => rawNamespace += $"using {ns};\r\n");

                    var source = sb.ToString();
                    source = source.Replace("$namespace", rawNamespace);
                    source = source.Replace("$classes", bodyBuilder.ToString());
                    source = source.Replace("$ni", rootNamespace);

                    // 生成Mapper
                    var mapperSource = MapperTemplate.Replace("$namespace", namespaces.First());
                    mapperSource = mapperSource.Replace("$ns", rootNamespace);
                    mapperSource = mapperSource.Replace("$baseclass", entityName);
                    mapperSource = mapperSource.Replace("$dtoclass", className);
                    mapperSource = mapperSource.Replace("$body", mapperBodyBuilder.ToString());

                    // 合并
                    source = $"{source}\r\n{mapperSource}";
                    envStringBuilder.AppendLine(source);
                }
            }

            envStringBuilder.AppendLine("#pragma warning restore");
            var envSource = envStringBuilder.ToString();
            // format:
            envSource = FormatContent(envSource);
            context.AddSource($"Biwen.AutoClassGenDtoG.g.cs", SourceText.From(envSource, Encoding.UTF8));
        }


        private static void HandleAnnotatedNodesDecor(Compilation compilation, ImmutableArray<ISymbol> nodes, ImmutableArray<ISymbol> nodes2, SourceProductionContext context)
        {
            if ((nodes.Length + nodes2.Length) == 0) return;

            IList<KeyValuePair<string, string>> ofImpls = [];
            // (普通特性)获取所有实现类
            foreach (var node in nodes)
            {
                var tName = node.OriginalDefinition.ToDisplayString();
                foreach (var item in node.GetAttributes().Where(x => x.AttributeClass?.MetadataName == "AutoDecorAttribute"))
                {
                    var attributeSyntax = item?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

                    if (attributeSyntax?.ArgumentList?.Arguments[0].Expression is TypeOfExpressionSyntax implNameSyntax)
                    {
                        var implNameStr = implNameSyntax.Type.ToString();
                        var symbol = compilation.GetSymbolsWithName(implNameStr, SymbolFilter.Type);

                        if (symbol?.Any() is true)
                        {
                            var implName = symbol.First().ToDisplayString();
                            implNameStr = implName;

                            ofImpls.Add(new KeyValuePair<string, string>(tName, implName));
                        }
                    }
                }
            }
            // (泛型特性)获取所有实现类
            foreach (var node in nodes2)
            {
                var tName = node.OriginalDefinition.ToDisplayString();
                foreach (var item in node.GetAttributes().Where(x => x.AttributeClass?.MetadataName == "AutoDecorAttribute`1"))
                {
                    var attributeSyntax = item?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

                    if (attributeSyntax?.Name is GenericNameSyntax genericNameSyntax)
                    {
                        var implNameStr = genericNameSyntax.TypeArgumentList.Arguments[0].ToString();
                        var symbol = compilation.GetSymbolsWithName(genericNameSyntax.TypeArgumentList.Arguments[0].ToString(), SymbolFilter.Type);

                        if (symbol?.Any() is true)
                        {
                            var implName = symbol.First().ToDisplayString();
                            implNameStr = implName;

                            ofImpls.Add(new KeyValuePair<string, string>(tName, implName));
                        }
                    }
                }
            }

            if (ofImpls.Count == 0)
            {
                return;
            }

            StringBuilder envStringBuilder = new();
            envStringBuilder.AppendLine("// <auto-generated />");
            envStringBuilder.AppendLine("// author:vipwan@outlook.com 万雅虎");
            envStringBuilder.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
            envStringBuilder.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
            envStringBuilder.AppendLine("// This file is generated by Biwen.AutoClassGen.SourceGenerator");
            envStringBuilder.AppendLine();
            envStringBuilder.AppendLine("#pragma warning disable");
            envStringBuilder.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            envStringBuilder.AppendLine("{");
            envStringBuilder.AppendLine("public static class AutoDecorExtensions");
            envStringBuilder.AppendLine("{");
            envStringBuilder.AppendLine("/// <summary>");
            envStringBuilder.AppendLine("/// AddAutoDecor");
            envStringBuilder.AppendLine("/// </summary>");
            envStringBuilder.AppendLine("public static IServiceCollection AddAutoDecor(this IServiceCollection services)");
            envStringBuilder.AppendLine("{");
            // decor
            foreach (var item in ofImpls.Distinct())
            {
                envStringBuilder.AppendLine($"services.Decorate<{item.Key}, {item.Value}>();");
            }
            envStringBuilder.AppendLine("return services;");
            envStringBuilder.AppendLine("}");
            envStringBuilder.AppendLine("}");
            envStringBuilder.AppendLine("}");
            envStringBuilder.AppendLine("#pragma warning restore");

            var envSource = envStringBuilder.ToString();
            // format:
            envSource = FormatContent(envSource);
            context.AddSource($"Biwen.AutoClassGenDecor.g.cs", SourceText.From(envSource, Encoding.UTF8));
        }


        #region Template

        public static readonly string MapperTemplate = $@"
namespace $namespace
{{
    using $ns ;
    public static partial class $baseclassTo$dtoclassExtentions
    {{
        /// <summary>
        /// mapper to $dtoclass
        /// </summary>
        /// <returns></returns>
        public static $dtoclass MapperTo$dtoclass(this $baseclass model)
        {{
            return new $dtoclass()
            {{
                $body
            }};
        }}
    }}
}}
";

        #endregion

        /// <summary>
        /// 格式化代码
        /// </summary>
        /// <param name="csCode"></param>
        /// <returns></returns>
        private static string FormatContent(string csCode)
        {
            var tree = CSharpSyntaxTree.ParseText(csCode);
            var root = tree.GetRoot().NormalizeWhitespace();
            var ret = root.ToFullString();
            return ret;
        }
    }
}