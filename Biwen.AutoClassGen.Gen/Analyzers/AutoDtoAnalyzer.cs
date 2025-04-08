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

    private static readonly LocalizableString Title = "不可使用外部库生成DTO";
    private static readonly LocalizableString MessageFormat = "不可使用外部库生成DTO";
    private static readonly LocalizableString Description = "不可使用外部库生成DTO.";

    private static readonly LocalizableString Title2 = "标注的类必须是partial类";
    private static readonly LocalizableString MessageFormat2 = "标注的类必须是partial类";
    private static readonly LocalizableString Description2 = "标注的类必须是partial类.";

    private static readonly LocalizableString Title3 = "不可标注到abstract类";
    private static readonly LocalizableString MessageFormat3 = "不可标注到abstract类";
    private static readonly LocalizableString Description3 = "不可标注到abstract类.";

    private static readonly LocalizableString TitleGEN041GEN041 = "重复标注[AutoDto]";
    private static readonly LocalizableString MessageFormatGEN041 = "重复标注了[AutoDto],请删除多余的标注";
    private static readonly LocalizableString DescriptionGEN041 = "重复标注[AutoDto].";





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

        // 检查是否重复标注 [AutoDto]
        if (autoDtoAttributes.Count > 1)
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
        foreach (var attribute in autoDtoAttributes)
        {
            string? entityName = null;

            // 处理泛型特性和非泛型特性
            if (attribute.Name is GenericNameSyntax genericName)
            {
                // 泛型特性: [AutoDto<EntityType>]
                var typeArg = genericName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg != null)
                {
                    entityName = GetSimpleTypeName(typeArg);
                }
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
                        entityName = GetSimpleTypeName(typeOfExpr.Type);
                    }
                }
            }

            // 如果成功提取了实体名称，验证它是否存在于当前编译中
            if (!string.IsNullOrEmpty(entityName))
            {
                var symbols = context.SemanticModel.Compilation.GetSymbolsWithName(entityName!, SymbolFilter.Type);
                var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();

                if (symbol is null)
                {
                    // 如果没有找到对应的类，报错
                    context.ReportDiagnostic(Diagnostic.Create(RuleGEN044, attribute.GetLocation()));
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