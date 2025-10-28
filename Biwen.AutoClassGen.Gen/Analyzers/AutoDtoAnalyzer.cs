// <copyright file="AutoDtoAnalyzer.cs" company="vipwan">
// MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class AutoDtoAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticIdGEN044 = "GEN044";
    public const string DiagnosticIdGEN045 = "GEN045";

    public const string DiagnosticIdGEN041 = "GEN041";
    public const string DiagnosticIdGEN042 = "GEN042";
    public const string DiagnosticIdGEN046 = "GEN046"; // AutoDtoWithMapper 缺少或非法 mapper 参数
    public const string DiagnosticIdGEN047 = "GEN047"; // AutoDtoWithMapper mapper 泛型参数不匹配

    private static readonly LocalizableString Title = "无法解析目标类型";
    private static readonly LocalizableString MessageFormat = "无法解析目标类型，请确保引用了正确的程序集并且类型可访问";
    private static readonly LocalizableString Description = "无法解析目标类型，可能是因为缺少程序集引用或类型不可访问.";

    private static readonly LocalizableString Title2 = "标注的类必须是partial类";
    private static readonly LocalizableString MessageFormat2 = "标注的类必须是partial类";
    private static readonly LocalizableString Description2 = "标注的类必须是partial类.";

    private static readonly LocalizableString Title3 = "不可标注到abstract类";
    private static readonly LocalizableString MessageFormat3 = "不可标注到abstract类";
    private static readonly LocalizableString Description3 = "不可标注到abstract类.";

    private static readonly LocalizableString TitleGEN041GEN041 = "重复标注[AutoDto]";
    private static readonly LocalizableString MessageFormatGEN041 = "重复标注了[AutoDto],请删除多余的标注, 实际上 AutoDto可以和AutoDtoComplex并存, 请修改对应分析器的错误";
    private static readonly LocalizableString DescriptionGEN041 = "重复标注[AutoDto]. AutoDto 可以和 AutoDtoComplex 并存, 但不能有多个相同的 AutoDto 或 AutoDtoComplex 特性.";

    private static readonly LocalizableString TitleGEN046 = "AutoDtoWithMapper 缺少有效的 mapper 参数";
    private static readonly LocalizableString MessageFormatGEN046 = "[AutoDtoWithMapper] 需要提供有效的 mapper 类型参数 typeof(YourMapper) 且不能为空";
    private static readonly LocalizableString DescriptionGEN046 = "在 AutoDtoWithMapper 特性中未提供有效的 mapper 参数 (null 或缺失).";

    private static readonly LocalizableString TitleGEN047 = "AutoDtoWithMapper 的 IStaticAutoDtoMapper 泛型参数不匹配";
    private static readonly LocalizableString MessageFormatGEN047 = "提供的 mapper 类型未正确实现 IStaticAutoDtoMapper<源实体, 当前DTO> 接口或泛型参数不匹配: {0}";
    private static readonly LocalizableString DescriptionGEN047 = "检查 AutoDtoWithMapper 的 mapper 类型是否实现 IStaticAutoDtoMapper<TFrom,TTo> 且 TFrom 与特性泛型参数一致, TTo 为当前 DTO 类型.";

    private const string Category = "GEN";

    private static readonly DiagnosticDescriptor RuleGEN044 = new(
        DiagnosticIdGEN044, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    private static readonly DiagnosticDescriptor RuleGEN045 = new(
    DiagnosticIdGEN045, Title2, MessageFormat2, Category,
    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description2);

    private static readonly DiagnosticDescriptor RuleGEN041 = new(
    DiagnosticIdGEN041, TitleGEN041GEN041, MessageFormatGEN041, Category,
    DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionGEN041);


    private static readonly DiagnosticDescriptor RuleGEN042 = new(
    DiagnosticIdGEN042, Title3, MessageFormat3, Category,
    DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description3);

    private static readonly DiagnosticDescriptor RuleGEN046 = new(
    DiagnosticIdGEN046, TitleGEN046, MessageFormatGEN046, Category,
    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DescriptionGEN046);

    private static readonly DiagnosticDescriptor RuleGEN047 = new(
    DiagnosticIdGEN047, TitleGEN047, MessageFormatGEN047, Category,
    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DescriptionGEN047);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        RuleGEN041,
        RuleGEN042,
        RuleGEN044,
        RuleGEN045,
        RuleGEN046,
        RuleGEN047];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            return;
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTree, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeTree(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var attributeLists = classDeclaration.AttributeLists;

        if (attributeLists.Count == 0)
            return;

        // 筛选出所有 AutoDto* 相关的特性
        var autoDtoAttributes = attributeLists
            .SelectMany(x => x.Attributes)
            .Where(x => x.Name.ToString().IndexOf("AutoDto", StringComparison.Ordinal) == 0)
            .ToList();

        if (autoDtoAttributes.Count == 0)
            return;

        // 分别检查 AutoDto 和 AutoDtoComplex 特性 (不含 AutoDtoWithMapper)
        var autoDtoOnlyAttributes = autoDtoAttributes
            .Where(x =>
            {
                var attrName = x.Name.ToString();
                return (attrName == "AutoDto" ||
                       (x.Name is GenericNameSyntax && attrName.StartsWith("AutoDto<", StringComparison.Ordinal))) &&
                       !attrName.StartsWith("AutoDtoWithMapper", StringComparison.Ordinal);
            })
            .ToList();

        var autoDtoComplexAttributes = autoDtoAttributes
            .Where(x => x.Name.ToString() == "AutoDtoComplex")
            .ToList();

        var autoDtoWithMapperAttributes = autoDtoAttributes
            .Where(x => x.Name.ToString().StartsWith("AutoDtoWithMapper", StringComparison.Ordinal))
            .ToList();

        // 检查是否有多个 AutoDto 特性（不包括 AutoDtoComplex）
        if (autoDtoOnlyAttributes.Count > 1)
        {
            var location = Location.Create(classDeclaration.SyntaxTree,
                TextSpan.FromBounds(classDeclaration.Modifiers.FullSpan.Start, classDeclaration.Identifier.Span.End));
            context.ReportDiagnostic(Diagnostic.Create(RuleGEN041, location));
        }

        // 检查是否有多个 AutoDtoComplex 特性
        if (autoDtoComplexAttributes.Count > 1)
        {
            var location = Location.Create(classDeclaration.SyntaxTree,
                TextSpan.FromBounds(classDeclaration.Modifiers.FullSpan.Start, classDeclaration.Identifier.Span.End));
            context.ReportDiagnostic(Diagnostic.Create(RuleGEN041, location));
        }

        // 检查是否含有 partial 关键字
        if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            var location = Location.Create(classDeclaration.SyntaxTree,
                TextSpan.FromBounds(classDeclaration.Modifiers.FullSpan.Start, classDeclaration.Identifier.Span.End));
            context.ReportDiagnostic(Diagnostic.Create(RuleGEN045, location));
        }

        // 检查是否是抽象类
        if (classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)))
        {
            var location = Location.Create(classDeclaration.SyntaxTree,
                TextSpan.FromBounds(classDeclaration.Modifiers.FullSpan.Start, classDeclaration.Identifier.Span.End));
            context.ReportDiagnostic(Diagnostic.Create(RuleGEN042, location));
        }

        // 处理普通 AutoDto/泛型 AutoDto 的实体类型解析诊断
        foreach (var attribute in autoDtoOnlyAttributes)
        {
            TypeSyntax? targetTypeSyntax = null;

            if (attribute.Name is GenericNameSyntax genericName)
            {
                targetTypeSyntax = genericName.TypeArgumentList.Arguments.FirstOrDefault();
            }
            else
            {
                var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                if (argument != null)
                {
                    var expression = argument.Expression;
                    if (expression is TypeOfExpressionSyntax typeOfExpr)
                    {
                        targetTypeSyntax = typeOfExpr.Type;
                    }
                }
            }

            if (targetTypeSyntax != null)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(targetTypeSyntax);
                var targetTypeSymbol = typeInfo.Type;

                if (targetTypeSymbol == null || targetTypeSymbol.TypeKind == TypeKind.Error)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleGEN044, attribute.GetLocation()));
                }
                else
                {
                    if (targetTypeSymbol.DeclaredAccessibility != Accessibility.Public &&
                        targetTypeSymbol.DeclaredAccessibility != Accessibility.Internal)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(RuleGEN044, attribute.GetLocation()));
                    }
                }
            }
        }

        // 处理 AutoDtoWithMapper 特性
        if (autoDtoWithMapperAttributes.Count > 0)
        {
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
            if (classSymbol == null) return;

            foreach (var attribute in autoDtoWithMapperAttributes)
            {
                // 获取泛型实体类型
                ITypeSymbol? entityTypeSymbol = null;
                if (attribute.Name is GenericNameSyntax gname && gname.TypeArgumentList.Arguments.Count > 0)
                {
                    var entTypeSyntax = gname.TypeArgumentList.Arguments[0];
                    entityTypeSymbol = context.SemanticModel.GetTypeInfo(entTypeSyntax).Type;
                }

                // 获取 mapper 参数 (第一个参数 或 命名 mapper = ...)
                AttributeArgumentSyntax? mapperArg = null;
                if (attribute.ArgumentList != null)
                {
                    // 优先寻找命名参数 mapper =
                    mapperArg = attribute.ArgumentList.Arguments.FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "mapper");
                    if (mapperArg == null && attribute.ArgumentList.Arguments.Count > 0)
                    {
                        mapperArg = attribute.ArgumentList.Arguments[0];
                    }
                }

                // 验证 mapper 参数是否缺失/为 null/不是 typeof
                bool invalidMapper = false;
                ITypeSymbol? mapperTypeSymbol = null;
                if (mapperArg == null)
                {
                    invalidMapper = true;
                }
                else
                {
                    var expr = mapperArg.Expression;
                    if (expr.IsKind(SyntaxKind.NullLiteralExpression))
                    {
                        invalidMapper = true;
                    }
                    else if (expr is TypeOfExpressionSyntax typeOfExpr)
                    {
                        mapperTypeSymbol = context.SemanticModel.GetTypeInfo(typeOfExpr.Type).Type;
                        if (mapperTypeSymbol == null || mapperTypeSymbol.TypeKind == TypeKind.Error)
                        {
                            invalidMapper = true;
                        }
                    }
                    else
                    {
                        invalidMapper = true; // 不是 typeof(...) 形式
                    }
                }

                if (invalidMapper)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleGEN046, attribute.GetLocation()));
                    continue; // 没有 mapper 无法继续泛型匹配
                }

                // 如果无法解析实体类型, 不再继续匹配 IStaticAutoDtoMapper
                if (entityTypeSymbol == null || entityTypeSymbol.TypeKind == TypeKind.Error)
                {
                    // 这里沿用已有 GEN044 规则 (实体类型解析失败)
                    context.ReportDiagnostic(Diagnostic.Create(RuleGEN044, attribute.GetLocation()));
                    continue;
                }

                // 检查 mapper 是否实现 IStaticAutoDtoMapper<TFrom,TTo>
                bool foundInterface = false;
                bool genericMatch = false;
                if (mapperTypeSymbol is INamedTypeSymbol namedMapper)
                {
                    foreach (var iface in namedMapper.AllInterfaces)
                    {
                        if (iface.Name == "IStaticAutoDtoMapper" && iface.TypeArguments.Length == 2)
                        {
                            foundInterface = true;
                            var fromArg = iface.TypeArguments[0];
                            var toArg = iface.TypeArguments[1];
                            if (SymbolEqualityComparer.Default.Equals(fromArg, entityTypeSymbol) &&
                                SymbolEqualityComparer.Default.Equals(toArg, classSymbol))
                            {
                                genericMatch = true;
                                break;
                            }
                        }
                    }
                }

                if (!foundInterface || !genericMatch)
                {
                    // 给出 mapper 类型名 (可能为空)
                    var mapperDisplay = mapperTypeSymbol?.ToDisplayString() ?? "<unknown>";
                    context.ReportDiagnostic(Diagnostic.Create(RuleGEN047, attribute.GetLocation(), mapperDisplay));
                }
            }
        }
    }

    // 获取类型的简单名称（不包含命名空间） - 当前未使用保留
    private static string GetSimpleTypeName(TypeSyntax type)
    {
        switch (type)
        {
            case IdentifierNameSyntax identifierName:
                return identifierName.Identifier.ValueText;
            case QualifiedNameSyntax qualifiedName:
                return qualifiedName.Right.Identifier.ValueText;
            case AliasQualifiedNameSyntax aliasQualifiedName:
                return aliasQualifiedName.Name.Identifier.ValueText;
            case GenericNameSyntax genericName:
                return genericName.Identifier.ValueText;
            default:
                var fullName = type.ToString();
                var lastDotIndex = fullName.LastIndexOf('.');
                if (lastDotIndex >= 0 && lastDotIndex < fullName.Length - 1)
                {
                    return fullName.Substring(lastDotIndex + 1);
                }
                return fullName;
        }
    }
}