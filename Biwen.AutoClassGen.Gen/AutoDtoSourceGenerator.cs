// <copyright file="AutoDtoSourceGenerator.cs" company="vipwan">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Biwen.AutoClassGen;

[Generator]
public class AutoDtoSourceGenerator : IIncrementalGenerator
{

    private const string AttributeMetadataNameDto = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute";
    private const string AttributeValueMetadataNameDto = "AutoDto";

    /// <summary>
    /// 泛型AutoDtoAttribute
    /// </summary>
    private const string GenericAutoDtoAttributeName = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        #region AutoDtoAttribute

        var nodesDto = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeMetadataNameDto,
            // 添加初步过滤：必须是类型声明，且必须是 partial 类，且不能是抽象类
            (syntaxNode, _) =>
                syntaxNode is TypeDeclarationSyntax typeDecl &&
                typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                !typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        var nodesDtoG = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenericAutoDtoAttributeName,
            // 添加相同的初步过滤条件
            (syntaxNode, _) =>
                syntaxNode is TypeDeclarationSyntax typeDecl &&
                typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                !typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        #endregion

        var join = nodesDto.Combine(nodesDtoG);
        var comp = context.CompilationProvider.Combine(join);

        context.RegisterSourceOutput(comp,
            (ctx, nodes) =>
            {
                var meta1 = GetMeta(nodes.Right.Left);
                var meta2 = GetMeta(nodes.Right.Right, true);
                GenSource(nodes.Left, ctx, [.. meta1, .. meta2]);
            });
    }


    /// <summary>
    /// 从普通AutoDto或泛型AutoDto属性中提取元数据
    /// </summary>
    /// <param name="nodes">语法节点集合</param>
    /// <param name="isGeneric">是否为泛型属性</param>
    /// <returns>AutoDto元数据列表</returns>
    private static List<AutoDtoMetadata> GetMeta(ImmutableArray<SyntaxNode> nodes, bool isGeneric = false)
    {
        List<AutoDtoMetadata> retn = [];
        if (nodes.Length == 0) return retn;

        foreach (var syntaxNode in nodes.AsEnumerable())
        {
            // 已经确保是 TypeDeclarationSyntax，可以安全转换
            var node = (TypeDeclarationSyntax)syntaxNode;

            // 如果是Record类
            var isRecord = syntaxNode is RecordDeclarationSyntax;

            // 不再需要检查 partial 修饰符，因为在谓词函数中已经筛选过了
            AttributeSyntax? attributeSyntax = null;
            foreach (var attr in node.AttributeLists.AsEnumerable())
            {
                var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                if (isGeneric
                    ? attrName?.IndexOf(AttributeValueMetadataNameDto, System.StringComparison.Ordinal) == 0
                    : attrName == AttributeValueMetadataNameDto)
                {
                    attributeSyntax = attr.Attributes.First(x => isGeneric
                        ? x.Name.ToString().IndexOf(AttributeValueMetadataNameDto, System.StringComparison.Ordinal) == 0
                        : x.Name.ToString() == AttributeValueMetadataNameDto);
                    break;
                }
            }

            if (attributeSyntax == null)
            {
                continue;
            }

            // 提取实体名称（这是两个方法的主要区别）
            string entityName = ExtractEntityName(attributeSyntax, isGeneric);
            if (string.IsNullOrEmpty(entityName))
            {
                continue;
            }

            // 获取命名空间
            var rootNamespace = GetRootNamespace(node);

            // 提取需排除的属性
            HashSet<string> escapes = GetEscapeProperties(attributeSyntax, isGeneric);

            retn.Add(new AutoDtoMetadata(entityName, node.Identifier.ValueText, escapes)
            {
                IsRecord = isRecord,
                RootNamespace = rootNamespace,
            });
        }

        return retn;
    }

    /// <summary>
    /// 从属性中提取实体名称
    /// </summary>
    /// <summary>
    /// 从属性中提取实体名称
    /// </summary>
    private static string ExtractEntityName(AttributeSyntax attributeSyntax, bool isGeneric)
    {
        if (isGeneric)
        {
            // 使用 Roslyn 语法 API 来获取泛型类型参数
            if (attributeSyntax.Name is GenericNameSyntax genericName)
            {
                // 直接获取泛型参数列表中的第一个类型
                if (genericName.TypeArgumentList.Arguments.Count > 0)
                {
                    var typeArg = genericName.TypeArgumentList.Arguments[0];
                    return GetSimpleTypeName(typeArg);
                }
            }
            return string.Empty;
        }
        else
        {
            // 处理非泛型情况，即 typeof(Type) 形式的参数
            if (attributeSyntax.ArgumentList?.Arguments.Count > 0)
            {
                var expression = attributeSyntax.ArgumentList.Arguments[0].Expression;
                if (expression is TypeOfExpressionSyntax typeOfExpr)
                {
                    return GetSimpleTypeName(typeOfExpr.Type);
                }
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取简单类型名称
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static string GetSimpleTypeName(TypeSyntax type)
    {
        switch (type)
        {
            case IdentifierNameSyntax identifierName:
                return identifierName.Identifier.ValueText;

            case QualifiedNameSyntax qualifiedName:
                // 对于限定名，返回最后一部分
                return qualifiedName.Right.Identifier.ValueText;

            case AliasQualifiedNameSyntax aliasQualifiedName:
                return aliasQualifiedName.Name.Identifier.ValueText;

            case GenericNameSyntax genericName:
                // 对于泛型名称，只返回基础类型名（不含泛型参数）
                return genericName.Identifier.ValueText;

            case ArrayTypeSyntax arrayType:
                // 处理数组类型
                return GetSimpleTypeName(arrayType.ElementType);

            case NullableTypeSyntax nullableType:
                // 处理可空类型
                return GetSimpleTypeName(nullableType.ElementType);

            default:
                // 对于其他情况，转为字符串并尝试取最后一段
                var fullName = type.ToString();
                var lastDotIndex = fullName.LastIndexOf('.');
                return lastDotIndex >= 0 ? fullName.Substring(lastDotIndex + 1) : fullName;
        }
    }

    /// <summary>
    /// 获取根命名空间
    /// </summary>
    private static string GetRootNamespace(TypeDeclarationSyntax node)
    {
        // 获取文件范围的命名空间
        var filescopeNamespace = node.AncestorsAndSelf().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

        if (filescopeNamespace != null)
        {
            return filescopeNamespace.Name.ToString();
        }
        else
        {
            return node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().Single().Name.ToString();
        }
    }

    /// <summary>
    /// 获取需排除的属性
    /// </summary>
    private static HashSet<string> GetEscapeProperties(AttributeSyntax attributeSyntax, bool isGeneric)
    {
        HashSet<string> escapes = [];

        if (attributeSyntax.ArgumentList == null)
        {
            return escapes;
        }

        int startIndex = isGeneric ? 0 : 1;
        int count = attributeSyntax.ArgumentList.Arguments.Count;

        for (var i = startIndex; i < count; i++)
        {
            var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
            if (expressionSyntax.IsKind(SyntaxKind.InvocationExpression))
            {
                var name = (expressionSyntax as InvocationExpressionSyntax)!.ArgumentList.DescendantNodes().First().ToString();
                escapes.Add(name.Split(['.']).Last());
            }
            else if (expressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var name = (expressionSyntax as LiteralExpressionSyntax)!.Token.ValueText;
                escapes.Add(name);
            }
            else if (expressionSyntax.IsKind(SyntaxKind.IdentifierName))
            {
                // 处理标识符名称
                var name = (expressionSyntax as IdentifierNameSyntax)!.Identifier.ValueText;
                escapes.Add(name);
            }
        }

        return escapes;
    }

    /// <summary>
    /// 生成源代码
    /// </summary>
    private static void GenSource(Compilation compilation, SourceProductionContext context, IEnumerable<AutoDtoMetadata> metadatas)
    {
        if (!metadatas.Any()) return;

        StringBuilder envStringBuilder = new();

        envStringBuilder.AppendLine("// <auto-generated />");
        envStringBuilder.AppendLine("// author:vipwan@outlook.com 万雅虎");
        envStringBuilder.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
        envStringBuilder.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
        envStringBuilder.AppendLine("// This file is generated by Biwen.AutoClassGen.AutoDtoSourceGenerator");
        envStringBuilder.AppendLine();
        envStringBuilder.AppendLine("using System;");
        envStringBuilder.AppendLine("using System.Collections.Generic;");
        envStringBuilder.AppendLine("using System.Text;");
        envStringBuilder.AppendLine("using System.Threading.Tasks;");
        envStringBuilder.AppendLine("using System.ComponentModel;"); // 添加 Description 特性所需命名空间
        envStringBuilder.AppendLine("using System.ComponentModel.DataAnnotations;"); // 添加 Required 和 StringLength 特性所需命名空间
        envStringBuilder.AppendLine("#pragma warning disable");

        foreach (var metadata in metadatas)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"//generate {metadata.FromClass}-{metadata.ToClass}");
            sb.AppendLine();
            sb.AppendLine("namespace $ni");
            sb.AppendLine("{");
            sb.AppendLine("$namespace");
            sb.AppendLine("$classes");
            sb.AppendLine("}");
            // sb.AppendLine("#pragma warning restore");
            string classTemp = $"partial $isRecord $className  {{ $body }}";
            classTemp = classTemp.Replace("$isRecord", metadata.IsRecord ? "record class" : "class");
            {
                var className = metadata.ToClass;

                StringBuilder bodyBuilder = new();
                List<string> namespaces = [];
                StringBuilder bodyInnerBuilder = new();
                StringBuilder mapperBodyBuilder = new();
                StringBuilder mapperBodyBuilder2 = new();

                bodyInnerBuilder.AppendLine();

                List<string> haveProps = [];

                // 生成属性
                void GenProperty(TypeSyntax @type)
                {
                    //无法将类型为"Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.PropertySymbol"
                    //的对象强制转换为类型"Microsoft.CodeAnalysis.ITypeSymbol
                    var symbols = compilation.GetSymbolsWithName(@type.ToString(), SymbolFilter.Type);

                    foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                    {
                        var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                        // 命名空间
                        if (!namespaces.Contains(fullNameSpace))
                        {
                            namespaces.Add(fullNameSpace);
                        }

                        // 确保添加System.ComponentModel命名空间
                        if (!namespaces.Contains("System.ComponentModel"))
                        {
                            namespaces.Add("System.ComponentModel");
                        }

                        // 确保添加System.ComponentModel.DataAnnotations命名空间
                        if (!namespaces.Contains("System.ComponentModel.DataAnnotations"))
                        {
                            namespaces.Add("System.ComponentModel.DataAnnotations");
                        }

                        symbol.GetMembers().OfType<IPropertySymbol>().ToList().ForEach(prop =>
                        {
                            if (!metadata.Escapes.Contains(prop.Name))
                            {
                                // 如果存在同名属性,则不生成
                                if (haveProps.Contains(prop.Name))
                                {
                                    return;
                                }

                                haveProps.Add(prop.Name);

                                //如果是泛型属性,则不生成
                                if (prop.ContainingType.TypeParameters.Any(x => x.Name == prop.Type.Name))
                                {
                                    return;
                                }

                                // 处理属性特性
                                var attributesBuilder = new StringBuilder();
                                foreach (var attribute in prop.GetAttributes())
                                {
                                    // 查找Description特性
                                    if (attribute.AttributeClass?.Name == "DescriptionAttribute")
                                    {
                                        var description = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            attributesBuilder.AppendLine($"[Description(\"{description}\")]");
                                        }
                                    }
                                    // 查找Required特性
                                    else if (attribute.AttributeClass?.Name == "RequiredAttribute")
                                    {
                                        attributesBuilder.AppendLine("[Required]");
                                    }
                                    // 查找StringLength特性
                                    else if (attribute.AttributeClass?.Name == "StringLengthAttribute")
                                    {
                                        var maxLength = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
                                        if (!string.IsNullOrEmpty(maxLength))
                                        {
                                            // 检查是否有MinimumLength参数
                                            var minLength = attribute.NamedArguments
                                                .FirstOrDefault(na => na.Key == "MinimumLength")
                                                .Value.Value?.ToString();

                                            if (!string.IsNullOrEmpty(minLength))
                                            {
                                                attributesBuilder.AppendLine($"[StringLength({maxLength}, MinimumLength = {minLength})]");
                                            }
                                            else
                                            {
                                                attributesBuilder.AppendLine($"[StringLength({maxLength})]");
                                            }
                                        }
                                    }
                                    // 查找Range特性
                                    else if (attribute.AttributeClass?.Name == "RangeAttribute")
                                    {
                                        var min = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value?.ToString() : null;
                                        var max = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value?.ToString() : null;
                                        if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                                        {
                                            attributesBuilder.AppendLine($"[Range({min}, {max})]");
                                        }
                                    }
                                    // 查找Display特性
                                    else if (attribute.AttributeClass?.Name == "DisplayAttribute")
                                    {
                                        var name = attribute.NamedArguments.FirstOrDefault(na => na.Key == "Name").Value.Value?.ToString();
                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            attributesBuilder.AppendLine($"[Display(Name = \"{name}\")]");
                                        }
                                    }
                                    // EmailAddress
                                    else if (attribute.AttributeClass?.Name == "EmailAddressAttribute")
                                    {
                                        attributesBuilder.AppendLine("[EmailAddress]");
                                    }
                                    // Url
                                    else if (attribute.AttributeClass?.Name == "UrlAttribute")
                                    {
                                        attributesBuilder.AppendLine("[Url]");
                                    }
                                    // Phone
                                    else if (attribute.AttributeClass?.Name == "PhoneAttribute")
                                    {
                                        attributesBuilder.AppendLine("[Phone]");
                                    }
                                    // RegularExpression
                                    else if (attribute.AttributeClass?.Name == "RegularExpressionAttribute")
                                    {
                                        //pattern必须包含,ErrorMessage可能不存在
                                        var pattern = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
                                        var errorMessage = attribute.NamedArguments.FirstOrDefault(na => na.Key == "ErrorMessage").Value.Value?.ToString();
                                        if (!string.IsNullOrEmpty(pattern))
                                        {
                                            if (!string.IsNullOrEmpty(errorMessage))
                                            {
                                                attributesBuilder.AppendLine($"[RegularExpression(\"{pattern}\", ErrorMessage = \"{errorMessage}\")]");
                                            }
                                            else
                                            {
                                                attributesBuilder.AppendLine($"[RegularExpression(\"{pattern}\")]");
                                            }
                                        }
                                    }
                                    // 其他特性可以根据需要添加
                                }

                                // prop:
                                var raw = $"public {prop.Type.ToDisplayString()} {prop.Name} {{get;set;}}";
                                // body:
                                bodyInnerBuilder.AppendLine($"/// <inheritdoc cref=\"{@type}.{prop.Name}\" />");
                                // 添加特性
                                if (attributesBuilder.Length > 0)
                                {
                                    bodyInnerBuilder.Append(attributesBuilder.ToString());
                                }
                                bodyInnerBuilder.AppendLine($"{raw}");

                                // mapper:
                                // 只有public的属性才能赋值.且可写
                                if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                                {
                                    mapperBodyBuilder.AppendLine($"{prop.Name} = model.{prop.Name},");
                                }
                                // mapper2:
                                if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public &&
                                    prop.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                                {
                                    mapperBodyBuilder2.AppendLine($"{prop.Name} = model.{prop.Name},");
                                }
                            }
                        });
                    }
                }

                // 生成属性:
                var symbols = compilation.GetSymbolsWithName(metadata.FromClass, SymbolFilter.Type);
                var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();

                //引用了其他库.
                if (symbol is null)
                    continue;

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
                source = source.Replace("$ni", metadata.RootNamespace);

                // 生成Mapper
                var mapperSource = MapperTemplate.Replace("$namespace", namespaces.First());
                mapperSource = mapperSource.Replace("$ns", metadata.RootNamespace);
                mapperSource = mapperSource.Replace("$baseclass", metadata.FromClass);
                mapperSource = mapperSource.Replace("$dtoclass", className);
                mapperSource = mapperSource.Replace("$body", mapperBodyBuilder.ToString());
                mapperSource = mapperSource.Replace("$2body", mapperBodyBuilder2.ToString());

                // 合并
                source = $"{source}\r\n{mapperSource}";
                envStringBuilder.AppendLine(source);
            }
        }

        envStringBuilder.AppendLine("#pragma warning restore");
        var envSource = envStringBuilder.ToString();
        // format:
        envSource = envSource.FormatContent();
        context.AddSource($"Biwen.AutoClassGenDto.g.cs", SourceText.From(envSource, Encoding.UTF8));

    }

    private record AutoDtoMetadata(string FromClass, string ToClass, HashSet<string> Escapes)
    {
        public string? RootNamespace { get; set; }

        /// <summary>
        /// 是否记录类型
        /// </summary>
        public bool IsRecord { get; set; }
    }

    #region Template

    private const string MapperTemplate = $@"
namespace $namespace
{{
    using System.Linq;
    using $ns ;
    public static partial class $baseclassTo$dtoclassExtentions
    {{
        /// <summary>
        /// custom mapper
        /// </summary>
        static partial void MapperToPartial($baseclass from, $dtoclass to);
        /// <summary>
        /// mapper to $dtoclass
        /// </summary>
        /// <returns></returns>
        public static $dtoclass MapperTo$dtoclass(this $baseclass model)
        {{
            var retn = new $dtoclass()
            {{
                $body
            }};
            MapperToPartial(model, retn);
            return retn;
        }}

        /// <summary>
        /// ProjectTo $dtoclass
        /// </summary>
        public static IQueryable<$dtoclass> ProjectTo$dtoclass(this IQueryable<$baseclass> query)
        {{
            return query.Select(model => model.MapperTo$dtoclass());
        }}
        
    }}

    public static partial class $dtoclassTo$baseclassExtentions
    {{
        /// <summary>
        /// custom mapper
        /// </summary>
        static partial void MapperToPartial($dtoclass from, $baseclass to);
        /// <summary>
        /// mapper to $baseclass
        /// </summary>
        /// <returns></returns>
        public static $baseclass MapperTo$baseclass(this $dtoclass model)
        {{
            var retn = new $baseclass()
            {{
                $2body
            }};
            MapperToPartial(model, retn);
            return retn;
        }}
    }}
}}
";

    #endregion
}
