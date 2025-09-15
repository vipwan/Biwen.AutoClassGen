// <copyright file="AutoDtoAnalyzer.cs" company="vipwan">
// MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoDtoAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticIdGEN044 = "GEN044";
    public const string DiagnosticIdGEN045 = "GEN045";

    public const string DiagnosticIdGEN041 = "GEN041";
    public const string DiagnosticIdGEN042 = "GEN042";

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


    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        RuleGEN041,
        RuleGEN042,
        RuleGEN044,
        RuleGEN045];

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

        // 筛选出所有 AutoDto 相关的特性
        var autoDtoAttributes = attributeLists
            .SelectMany(x => x.Attributes)
            .Where(x => x.Name.ToString().IndexOf("AutoDto", StringComparison.Ordinal) == 0)
            .ToList();

        if (autoDtoAttributes.Count == 0)
            return;

        // 分别检查 AutoDto 和 AutoDtoComplex 特性
        var autoDtoOnlyAttributes = autoDtoAttributes
            .Where(x => 
            {
                var attrName = x.Name.ToString();
                return attrName == "AutoDto" || 
                       (x.Name is GenericNameSyntax && attrName.StartsWith("AutoDto<", StringComparison.Ordinal));
            })
            .ToList();

        var autoDtoComplexAttributes = autoDtoAttributes
            .Where(x => x.Name.ToString() == "AutoDtoComplex")
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

        // 遍历每个 AutoDto 特性，提取并验证实体类型
        foreach (var attribute in autoDtoOnlyAttributes)
        {
            TypeSyntax? targetTypeSyntax = null;

            // 处理泛型特性和非泛型特性
            if (attribute.Name is GenericNameSyntax genericName)
            {
                // 泛型特性: [AutoDto<EntityType>]
                targetTypeSyntax = genericName.TypeArgumentList.Arguments.FirstOrDefault();
            }
            else
            {
                // 非泛型特性: [AutoDto(typeof(EntityType))]
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

            // 如果成功提取了目标类型语法，使用语义模型验证类型
            if (targetTypeSyntax != null)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(targetTypeSyntax);
                var targetTypeSymbol = typeInfo.Type;

                if (targetTypeSymbol == null || targetTypeSymbol.TypeKind == TypeKind.Error)
                {
                    // 如果无法解析类型，报告诊断信息
                    context.ReportDiagnostic(Diagnostic.Create(RuleGEN044, attribute.GetLocation()));
                }
                else
                {
                    // 可选：检查类型的可访问性
                    if (targetTypeSymbol.DeclaredAccessibility != Accessibility.Public &&
                        targetTypeSymbol.DeclaredAccessibility != Accessibility.Internal)
                    {
                        // 如果类型不是公共或内部的，可能无法访问
                        context.ReportDiagnostic(Diagnostic.Create(RuleGEN044, attribute.GetLocation()));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取类型的简单名称（不包含命名空间）
    /// </summary>
    /// <param name="type">类型语法</param>
    /// <returns>简单类型名</returns>
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

            default:
                // 对于其他情况，转为字符串并尝试取最后一段
                var fullName = type.ToString();
                var lastDotIndex = fullName.LastIndexOf('.');
                return lastDotIndex >= 0 ? fullName.Substring(lastDotIndex + 1) : fullName;
        }
    }

}