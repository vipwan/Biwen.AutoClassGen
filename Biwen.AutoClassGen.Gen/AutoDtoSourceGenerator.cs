// <copyright file="AutoDtoSourceGenerator.cs" company="vipwan">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Biwen.AutoClassGen;

[Generator]
public class AutoDtoSourceGenerator : IIncrementalGenerator
{
    /// <summary>
    /// 标记为AutoDto的DTO类
    /// </summary>
    private const string AttributeMetadataNameDto = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute";
    /// <summary>
    /// 需要忽略的属性标记
    /// </summary>
    private const string AutoDtoIgronedAttributeName = "AutoDtoIgroned";

    private const string AttributeValueMetadataNameDto = "AutoDto";

    /// <summary>
    /// 泛型AutoDtoAttribute
    /// </summary>
    private const string GenericAutoDtoAttributeName = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute`1";

    /// <summary>
    /// 复杂对象DTO特性
    /// </summary>
    private const string AutoDtoComplexAttributeName = "Biwen.AutoClassGen.Attributes.AutoDtoComplexAttribute";


    private record class ComplexNodeInfo(TypeDeclarationSyntax Node, AttributeData AttributeData);

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

        #region AutoDtoComplexAttribute

        // 检测复杂DTO特性
        var nodesComplex = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoDtoComplexAttributeName,
            // 添加初步过滤：必须是类型声明，且必须是 partial 类，且不能是抽象类
            (syntaxNode, _) =>
                syntaxNode is TypeDeclarationSyntax typeDecl &&
                typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                !typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            (syntaxContext, _) => new ComplexNodeInfo(
                (TypeDeclarationSyntax)syntaxContext.TargetNode,
                syntaxContext.Attributes.First()
            )).Collect();

        #endregion

        var join = nodesDto.Combine(nodesDtoG);
        var joinWithComplex = join.Combine(nodesComplex);
        var comp = context.CompilationProvider.Combine(joinWithComplex);

        // RegisterSourceOutput
        context.RegisterSourceOutput(comp,
            (ctx, nodes) =>
            {
                var meta1 = GetMeta(nodes.Right.Left.Left);
                var meta2 = GetMeta(nodes.Right.Left.Right, true);

                // 处理复杂DTO标记 - 修改处理方式，避免使用dynamic
                List<AutoDtoMetadata> metadataList = [.. meta1, .. meta2];

                // 直接在这里处理复杂DTO特性，使用强类型
                foreach (var complexNode in nodes.Right.Right)
                {
                    var typeDecl = complexNode.Node;
                    var className = typeDecl.Identifier.ValueText;

                    // 找到对应的元数据
                    var metadata = metadataList.FirstOrDefault(m => m.ToClass == className);
                    if (metadata != null)
                    {
                        metadata.IsComplex = true;

                        int maxLevel = 2; // 默认,只支持到第二层

                        // 尝试获取嵌套深度参数
                        var attributeData = complexNode.AttributeData;
                        if (attributeData.ConstructorArguments.Length > 0 &&
                            attributeData.ConstructorArguments[0].Value is int maxLevelValue)
                        {
                            maxLevel = maxLevelValue;
                        }
                        metadata.MaxNestingLevel = maxLevel;
                    }
                }

                GenSource(nodes.Left, ctx, metadataList);
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
                    ? attrName?.IndexOf(AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0
                    : attrName == AttributeValueMetadataNameDto)
                {
                    attributeSyntax = attr.Attributes.First(x => isGeneric
                        ? x.Name.ToString().IndexOf(AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0
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
                IsComplex = false, // 默认不是复杂DTO，除非明确标记了AutoDtoComplex特性
            });
        }

        return retn;
    }

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
                escapes.Add(name.Split(['.'], StringSplitOptions.RemoveEmptyEntries).Last());
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
    /// 判断一个类型是否为复杂类型（非原始类型）
    /// </summary>
    private static bool IsComplexType(ITypeSymbol type)
    {
        // 排除基本类型、字符串、枚举等
        if (type.IsValueType || type.SpecialType == SpecialType.System_String ||
            type.TypeKind == TypeKind.Enum || type.TypeKind == TypeKind.Array)
            return false;

        // 排除一些常见的系统类型
        var typeName = type.ToDisplayString();
        if (typeName.StartsWith("System.", StringComparison.Ordinal) &&
            !typeName.StartsWith("System.Collections.", StringComparison.Ordinal) &&
            !typeName.StartsWith("System.Linq.", StringComparison.Ordinal))
            return false;

        return true;
    }

    /// <summary>
    /// 判断一个类型是否为集合类型，并获取元素类型
    /// </summary>
    private static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;

        // 检查是否是数组
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        // 检查是否是泛型集合类型
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var genericTypeName = namedType.ConstructedFrom.ToDisplayString();
            if (genericTypeName == "System.Collections.Generic.List<T>" ||
                genericTypeName == "System.Collections.Generic.IList<T>" ||
                genericTypeName == "System.Collections.Generic.ICollection<T>" ||
                genericTypeName == "System.Collections.Generic.IEnumerable<T>")
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }
        }

        // 检查是否实现了IEnumerable<T>
        foreach (var interfaceType in type.AllInterfaces)
        {
            if (interfaceType.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
            {
                elementType = ((INamedTypeSymbol)interfaceType).TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试获取类型对应的DTO类型，如果不存在，则将其添加到待生成的集合中
    /// </summary>
    private static bool TryGetDtoType(Compilation compilation,
                                     ITypeSymbol type,
                                     string currentNamespace,
                                     bool isComplex, // 标识是否为复杂DTO
                                     out INamedTypeSymbol? dtoSymbol,
                                     out string dtoTypeName,
                                     Dictionary<string, AutoDtoMetadata> nestedDtoTypesToGenerate)
    {
        dtoSymbol = null;
        dtoTypeName = string.Empty;

        // 获取类型的简单名称
        string typeName = type.Name;

        // 搜索可能的DTO类型名称 (TypeName + "Dto")
        string possibleDtoName = typeName + "Dto";

        var dtoSymbols = compilation.GetSymbolsWithName(possibleDtoName, SymbolFilter.Type)
                                  .OfType<INamedTypeSymbol>()
                                  .Where(s => s.DeclaredAccessibility == Accessibility.Public)
                                  .ToList();

        if (dtoSymbols.Count > 0)
        {
            // 优先选择相同命名空间下的DTO
            var sameNamespaceDto = dtoSymbols.FirstOrDefault(s =>
                s.ContainingNamespace.ToDisplayString() == currentNamespace);

            if (sameNamespaceDto != null)
            {
                dtoSymbol = sameNamespaceDto;
                dtoTypeName = possibleDtoName;
                return true;
            }

            // 如果没有同命名空间的，选择第一个
            dtoSymbol = dtoSymbols[0];
            dtoTypeName = dtoSymbol.ToDisplayString();
            return true;
        }

        // 如果没有找到现有的DTO类型，只有在isComplex为true时才生成嵌套DTO
        if (isComplex && !nestedDtoTypesToGenerate.ContainsKey(typeName))
        {
            dtoTypeName = possibleDtoName;

            var nestedMetadata = new AutoDtoMetadata(
                typeName,
                possibleDtoName,
                [])
            {
                IsRecord = false, // 默认不使用record类型
                RootNamespace = currentNamespace,
                IsComplex = true, // 嵌套DTO也启用复杂类型支持
                MaxNestingLevel = 2, // 嵌套层级较浅，避免无限递归
            };

            nestedDtoTypesToGenerate.Add(typeName, nestedMetadata);
            return true; // 返回true，表示处理成功（会生成DTO）
        }
        else if (nestedDtoTypesToGenerate.ContainsKey(typeName))
        {
            // 已经在待生成列表中，直接返回类型名称
            dtoTypeName = possibleDtoName;
            return true;
        }

        // 如果不是复杂DTO且找不到现有DTO，返回false
        return false;
    }

    /// <summary>
    /// 递归处理类型的所有属性，收集需要生成的嵌套DTO类型
    /// </summary>
    private static void ProcessTypeProperties(Compilation compilation,
                                            ITypeSymbol type,
                                            string currentNamespace,
                                            int currentNestingLevel,
                                            int maxNestingLevel,
                                            bool isComplex,
                                            Dictionary<string, AutoDtoMetadata> nestedDtoTypesToGenerate)
    {
        if (currentNestingLevel >= maxNestingLevel)
            return;

        // 非复杂DTO不需要递归处理嵌套类型
        if (!isComplex && currentNestingLevel > 0)
            return;

        foreach (var prop in type.GetMembers().OfType<IPropertySymbol>())
        {
            // 检查属性是否被忽略
            if (HasAutoDtoIgronedAttribute(prop))
                continue;

            // 检查是否是复杂类型
            bool isCollection = IsCollectionType(prop.Type, out ITypeSymbol? elementType);
            ITypeSymbol typeToCheck = isCollection ? elementType! : prop.Type;

            if (typeToCheck != null && IsComplexType(typeToCheck))
            {
                // 尝试获取或创建对应的DTO类型
                if (TryGetDtoType(compilation, typeToCheck, currentNamespace, isComplex, out INamedTypeSymbol? dtoSymbol, out string dtoTypeName, nestedDtoTypesToGenerate))
                {
                    // 如果创建了新的元数据，递归处理该类型的属性
                    if (isComplex && nestedDtoTypesToGenerate.ContainsKey(typeToCheck.Name))
                    {
                        // 递归处理嵌套类型的属性
                        var nestedSymbols = compilation.GetSymbolsWithName(typeToCheck.Name, SymbolFilter.Type);
                        var nestedSymbol = nestedSymbols.FirstOrDefault() as ITypeSymbol;
                        if (nestedSymbol != null)
                        {
                            ProcessTypeProperties(compilation, nestedSymbol, currentNamespace,
                                currentNestingLevel + 1, maxNestingLevel, isComplex, nestedDtoTypesToGenerate);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 检查属性是否被标记了 AutoDtoIgronedAttribute 特性
    /// </summary>
    private static bool HasAutoDtoIgronedAttribute(IPropertySymbol prop)
    {
        foreach (var attribute in prop.GetAttributes())
        {
            // 检查特性的完全限定名
            if (attribute.AttributeClass?.ToDisplayString() == AutoDtoIgronedAttributeName ||
                attribute.AttributeClass?.Name == "AutoDtoIgronedAttribute" ||
                attribute.AttributeClass?.Name == "AutoDtoIgroned")
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 生成源代码
    /// </summary>
    private static void GenSource(Compilation compilation, SourceProductionContext context, IEnumerable<AutoDtoMetadata> metadatas)
    {
        if (!metadatas.Any()) return;

        // 记录需要生成的嵌套DTO类型
        Dictionary<string, AutoDtoMetadata> nestedDtoTypesToGenerate = new();

        // 第一次遍历：处理所有的主DTO元数据，收集需要生成的嵌套DTO
        foreach (var metadata in metadatas)
        {
#pragma warning disable CA1031 // 不捕获常规异常类型
            try
            {
                // 获取源类型
                var symbols = compilation.GetSymbolsWithName(metadata.FromClass, SymbolFilter.Type);
                var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();

                if (symbol != null)
                {
                    // 递归处理所有属性 - 仅当IsComplex为true时才处理嵌套级别
                    int maxNestingLevel = metadata.IsComplex ? metadata.MaxNestingLevel : 1;
                    ProcessTypeProperties(compilation, symbol, metadata.RootNamespace!, 0, maxNestingLevel, metadata.IsComplex, nestedDtoTypesToGenerate);
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "GEN099",
                        "AutoDto生成器发生异常",
                        $"AutoDto第一阶段处理 {metadata.FromClass} 时发生异常: {ex.Message}",
                        "AutoDto",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
#pragma warning restore CA1031 // 不捕获常规异常类型
        }

        // 合并所有需要生成的DTO元数据
        var allMetadatas = new List<AutoDtoMetadata>(metadatas);
        allMetadatas.AddRange(nestedDtoTypesToGenerate.Values);

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
        envStringBuilder.AppendLine("using System.Linq;");
        envStringBuilder.AppendLine("using System.ComponentModel;"); // 添加 Description 特性所需命名空间
        envStringBuilder.AppendLine("using System.ComponentModel.DataAnnotations;"); // 添加 Required 和 StringLength 特性所需命名空间
        envStringBuilder.AppendLine("#pragma warning disable");

        foreach (var metadata in allMetadatas)
        {
#pragma warning disable CA1031 // 不捕获常规异常类型
            try
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
                string classTemp = $"public partial $isRecord $className  {{ $body }}";
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
                    // 收集生成的所有属性名称
                    HashSet<string> generatedProperties = new();
                    // 存储嵌套对象的属性映射代码
                    Dictionary<string, string> nestedObjectMappers = new();
                    Dictionary<string, string> nestedObjectReverseMappers = new();
                    // 存储嵌套对象的属性名和类型信息
                    Dictionary<string, Tuple<string, string>> nestedObjects = new Dictionary<string, Tuple<string, string>>();

                    // 生成属性
                    void GenProperty(TypeSyntax @type, bool isBaseType = false, int currentNestingLevel = 0, bool shouldGenerateProperty = true)
                    {
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
                                // 检查属性是否被标记了 AutoDtoIgronedAttribute 或包含在忽略列表中
                                if (HasAutoDtoIgronedAttribute(prop) || metadata.Escapes.Contains(prop.Name))
                                {
                                    return; // 跳过被忽略的属性
                                }

                                // 确保属性确实属于当前类型，不是从嵌套类型获取的
                                if (prop.ContainingType.Name != type.ToString())
                                {
                                    return; // 跳过不属于当前类型的属性
                                }

                                if (!metadata.Escapes.Contains(prop.Name))
                                {
                                    // 如果存在同名属性,则不生成
                                    if (haveProps.Contains(prop.Name))
                                    {
                                        return;
                                    }

                                    if (!shouldGenerateProperty)
                                    {
                                        // 如果不应该生成属性，只处理复杂对象，但不生成属性
                                        // 非复杂DTO不需要处理嵌套层级
                                        if (!metadata.IsComplex)
                                            return;

                                        int maxLevel2 = metadata.MaxNestingLevel;
                                        if (currentNestingLevel < maxLevel2)
                                        {
                                            bool isCollection = IsCollectionType(prop.Type, out ITypeSymbol? elementType);
                                            ITypeSymbol typeToCheck = isCollection ? elementType! : prop.Type;

                                            if (typeToCheck != null && IsComplexType(typeToCheck))
                                            {
                                                if (TryGetDtoType(compilation, typeToCheck, metadata.RootNamespace!, metadata.IsComplex, out INamedTypeSymbol? dtoSymbol, out string dtoTypeName, nestedDtoTypesToGenerate))
                                                {
                                                    // 处理嵌套类型
                                                    var nestedSymbols = compilation.GetSymbolsWithName(typeToCheck.Name, SymbolFilter.Type);
                                                    var nestedSymbol = nestedSymbols.FirstOrDefault() as ITypeSymbol;
                                                    if (nestedSymbol != null && currentNestingLevel + 1 < maxLevel2)
                                                    {
                                                        // 递归处理嵌套类型，但不生成属性
                                                        GenProperty(SyntaxFactory.ParseTypeName(nestedSymbol.Name), false, currentNestingLevel + 1, false);
                                                    }
                                                }
                                            }
                                        }
                                        return;
                                    }

                                    haveProps.Add(prop.Name);
                                    generatedProperties.Add(prop.Name);

                                    //如果是泛型属性,则不生成
                                    if (prop.ContainingType.TypeParameters.Any(x => x.Name == prop.Type.Name))
                                    {
                                        return;
                                    }

                                    // 处理属性类型，检查是否需要使用DTO类型替换
                                    string propTypeName = prop.Type.ToDisplayString();
                                    string mapperCode = $"{prop.Name} = model.{prop.Name},";
                                    string reverseMapperCode = $"{prop.Name} = model.{prop.Name},";

                                    // 检查是否是嵌套的复杂类型，并根据 IsComplex 和 MaxNestingLevel 来决定是否处理嵌套对象
                                    int maxLevel = metadata.IsComplex ? metadata.MaxNestingLevel : 1;

                                    if (currentNestingLevel < maxLevel)
                                    {
                                        bool isCollection = IsCollectionType(prop.Type, out ITypeSymbol? elementType);
                                        ITypeSymbol typeToCheck = isCollection ? elementType! : prop.Type;

                                        if (typeToCheck != null && IsComplexType(typeToCheck))
                                        {
                                            // 尝试查找对应的DTO类型
                                            if (TryGetDtoType(compilation, typeToCheck, metadata.RootNamespace!, metadata.IsComplex, out INamedTypeSymbol? dtoSymbol, out string dtoTypeName, nestedDtoTypesToGenerate))
                                            {
                                                // 只有当是复杂DTO时才替换属性类型和映射代码
                                                if (metadata.IsComplex)
                                                {
                                                    if (isCollection)
                                                    {
                                                        // 集合类型，替换元素类型
                                                        string collectionType = propTypeName;
                                                        // 根据集合类型进行不同的处理
                                                        if (prop.Type is IArrayTypeSymbol)
                                                        {
                                                            propTypeName = $"{dtoTypeName}[]";
                                                        }
                                                        else
                                                        {
                                                            // 替换泛型参数
                                                            propTypeName = Regex.Replace(collectionType,
                                                                Regex.Escape(typeToCheck.ToDisplayString()),
                                                                dtoSymbol?.ToDisplayString() ?? dtoTypeName);
                                                        }

                                                        // 生成集合映射代码
                                                        mapperCode = $"{prop.Name} = model.{prop.Name} != null " +
                                                                    $"? model.{prop.Name}.Select(x => x?.MapperTo{dtoTypeName}()).ToList() " +
                                                                    $": null,";

                                                        reverseMapperCode = $"{prop.Name} = model.{prop.Name} != null " +
                                                                          $"? model.{prop.Name}.Select(x => x?.MapperTo{typeToCheck.Name}()).ToList() " +
                                                                          $": null,";

                                                        // 记录嵌套对象信息
                                                        nestedObjects[prop.Name] = Tuple.Create(propTypeName, prop.Name);
                                                    }
                                                    else
                                                    {
                                                        // 单一对象类型，直接替换
                                                        propTypeName = dtoSymbol?.ToDisplayString() ?? dtoTypeName;

                                                        // 生成对象映射代码
                                                        mapperCode = $"{prop.Name} = model.{prop.Name}?.MapperTo{dtoTypeName}(),";
                                                        reverseMapperCode = $"{prop.Name} = model.{prop.Name}?.MapperTo{typeToCheck.Name}(),";

                                                        // 记录嵌套对象信息
                                                        nestedObjects[prop.Name] = Tuple.Create(propTypeName, prop.Name);
                                                    }

                                                    // 确保引用了所需的命名空间
                                                    if (dtoSymbol != null && !namespaces.Contains(dtoSymbol.ContainingNamespace.ToDisplayString()))
                                                    {
                                                        namespaces.Add(dtoSymbol.ContainingNamespace.ToDisplayString());
                                                    }
                                                }

                                                // 只在复杂DTO模式下递归处理嵌套类型
                                                if (metadata.IsComplex && currentNestingLevel + 1 < maxLevel)
                                                {
                                                    var nestedSymbols = compilation.GetSymbolsWithName(typeToCheck.Name, SymbolFilter.Type);
                                                    var nestedSymbol = nestedSymbols.FirstOrDefault() as ITypeSymbol;
                                                    if (nestedSymbol != null)
                                                    {
                                                        // 递归处理嵌套类型，但不生成属性
                                                        GenProperty(SyntaxFactory.ParseTypeName(nestedSymbol.Name), false, currentNestingLevel + 1, false);
                                                    }
                                                }
                                            }
                                        }
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
                                    var raw = $"public {propTypeName} {prop.Name} {{get;set;}}";
                                    // body:
                                    bodyInnerBuilder.AppendLine($"/// <inheritdoc cref=\"{@type}.{prop.Name}\" />");
                                    // 添加特性
                                    if (attributesBuilder.Length > 0)
                                    {
                                        bodyInnerBuilder.Append(attributesBuilder.ToString());
                                    }
                                    bodyInnerBuilder.AppendLine($"{raw}");

                                    // 只有public的属性才能赋值.且可写
                                    if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                                    {
                                        mapperBodyBuilder.AppendLine(mapperCode);
                                    }

                                    // 如果是嵌套对象，保存映射代码以便处理嵌套属性
                                    if (nestedObjects.ContainsKey(prop.Name))
                                    {
                                        nestedObjectMappers[prop.Name] = mapperCode;
                                    }

                                    // mapper2:
                                    if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public &&
                                        prop.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                                    {
                                        mapperBodyBuilder2.AppendLine(reverseMapperCode);

                                        // 如果是嵌套对象，保存映射代码以便处理嵌套属性
                                        if (nestedObjects.ContainsKey(prop.Name))
                                        {
                                            nestedObjectReverseMappers[prop.Name] = reverseMapperCode;
                                        }
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

                    GenProperty(SyntaxFactory.ParseTypeName(symbol.MetadataName), false, 0, true);

                    // 生成父类的属性:
                    INamedTypeSymbol? baseType = symbol.BaseType;
                    while (baseType != null && baseType.Name != "Object" && baseType.ToDisplayString() != "object")
                    {
                        GenProperty(SyntaxFactory.ParseTypeName(baseType.MetadataName), true, 0, true);
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

                    // 为嵌套对象生成特殊的映射代码
                    // 例如，对于 Person.Address.Street，生成 Street = model.Address?.Street
                    var specialMapperBuilder = new StringBuilder();
                    var specialReverseMapperBuilder = new StringBuilder();

                    // 对于复杂类型，生成特殊映射逻辑
                    if (metadata.IsComplex)
                    {
                        // 处理嵌套对象的属性映射
                        foreach (var entry in nestedObjects)
                        {
                            string propName = entry.Key;

                            // 已经处理过的嵌套对象属性
                            if (nestedObjectMappers.ContainsKey(propName))
                            {
                                // 这里保留嵌套对象的映射代码
                                specialMapperBuilder.AppendLine(nestedObjectMappers[propName]);
                            }
                            if (nestedObjectReverseMappers.ContainsKey(propName))
                            {
                                // 这里保留嵌套对象的映射代码
                                specialReverseMapperBuilder.AppendLine(nestedObjectReverseMappers[propName]);
                            }
                        }
                    }

                    // 合并映射代码
                    var finalMapperBodyBuilder = new StringBuilder();
                    var finalReverseMapperBodyBuilder = new StringBuilder();

                    // 添加普通属性映射
                    foreach (var line in mapperBodyBuilder.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var propertyName = line.Trim().Split('=')[0].Trim();
                        // 确保不是嵌套对象映射，避免重复
                        if (!nestedObjectMappers.ContainsKey(propertyName))
                        {
                            finalMapperBodyBuilder.AppendLine(line);
                        }
                    }

                    // 添加嵌套对象映射
                    finalMapperBodyBuilder.Append(specialMapperBuilder.ToString());

                    // 添加普通属性反向映射
                    foreach (var line in mapperBodyBuilder2.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var propertyName = line.Trim().Split('=')[0].Trim();
                        // 确保不是嵌套对象映射，避免重复
                        if (!nestedObjectReverseMappers.ContainsKey(propertyName))
                        {
                            finalReverseMapperBodyBuilder.AppendLine(line);
                        }
                    }

                    // 添加嵌套对象反向映射
                    finalReverseMapperBodyBuilder.Append(specialReverseMapperBuilder.ToString());

                    var mapperSource = MapperTemplate.Replace("$namespace", namespaces.Count > 0 ? namespaces.First() : metadata.RootNamespace);
                    mapperSource = mapperSource.Replace("$ns", metadata.RootNamespace);
                    mapperSource = mapperSource.Replace("$baseclass", metadata.FromClass);
                    mapperSource = mapperSource.Replace("$dtoclass", className);
                    mapperSource = mapperSource.Replace("$body", finalMapperBodyBuilder.ToString());
                    mapperSource = mapperSource.Replace("$2body", finalReverseMapperBodyBuilder.ToString());

                    // 合并
                    source = $"{source}\r\n{mapperSource}";
                    envStringBuilder.AppendLine(source);
                }
            }
            catch (Exception ex)
            {
                // 报告诊断
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "GEN099",
                        "AutoDto生成器发生异常",
                        $"AutoDto生成器处理 {metadata.FromClass} 时发生异常: {ex.Message}",
                        "AutoDto",
                        DiagnosticSeverity.Error,
                        true),
                    Location.None));
            }
#pragma warning restore CA1031 // 不捕获常规异常类型
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

        /// <summary>
        /// 是否是复杂DTO
        /// </summary>
        public bool IsComplex { get; set; } // 默认不是复杂DTO，除非明确标记

        /// <summary>
        /// 最大嵌套层级
        /// </summary>
        public int MaxNestingLevel { get; set; } = 2; // 默认只支持2层嵌套
    }

    #region Template

    private const string MapperTemplate = $@"
namespace $namespace
{{
    using System.Linq;
    using $ns;
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
            if (model == null) return null;
            
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
            if (model == null) return null;
            
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
