// <copyright file="AutoDtoAnalyzer.cs" company="vipwan">
// MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace Biwen.AutoClassGen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoDtoAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticIdGEN044 = "GEN044";
    public const string DiagnosticIdGEN045 = "GEN045";

    private static readonly LocalizableString Title = "不可使用外部库生成DTO";
    private static readonly LocalizableString MessageFormat = "不可使用外部库生成DTO";
    private static readonly LocalizableString Description = "不可使用外部库生成DTO.";

    private static readonly LocalizableString Title2 = "标注的类必须是partial类";
    private static readonly LocalizableString MessageFormat2 = "标注的类必须是partial类";
    private static readonly LocalizableString Description2 = "标注的类必须是partial类.";

    private const string Category = "GEN";

    private static readonly DiagnosticDescriptor RuleGEN044 = new(
        DiagnosticIdGEN044, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    private static readonly DiagnosticDescriptor RuleGEN045 = new(
    DiagnosticIdGEN045, Title2, MessageFormat2, Category,
    DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description2);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleGEN044, RuleGEN045];

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

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (!attribute.Name.ToString().Contains("AutoDto"))
                    continue;

                //如果被标记的类不含partial关键字,则不生成:
                if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    //定位到类的第一行 0~class关键字结束位置:
                    var location = Location.Create(classDeclaration.SyntaxTree,
                        TextSpan.FromBounds(classDeclaration.Modifiers.FullSpan.Start,
                        classDeclaration.Identifier.Span.End));

                    var diagnostic = Diagnostic.Create(RuleGEN045, location);
                    context.ReportDiagnostic(diagnostic);
                }


                if (attribute.Name is not GenericNameSyntax)
                {
                    var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (argument is null)
                        return;

                    var attributeSyntax = argument.Expression;
                    string pattern = @"(?<=<)(?<type>\w+)(?=>)";
                    var match = Regex.Match(attributeSyntax.ToString(), pattern);

                    //转译的Entity类名
                    string? entityName;
                    if (match.Success)
                    {
                        entityName = match.Groups["type"].Value.Split(['.']).Last();
                    }
                    else
                    {
                        continue;
                    }

                    // 查找对象是否属于当前编译库:
                    var symbols = context.SemanticModel.Compilation.GetSymbolsWithName(entityName, SymbolFilter.Type);
                    var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();

                    if (symbol is null)
                    {
                        //如果没有找到对应的类,则报错
                        var diagnostic = Diagnostic.Create(RuleGEN044, attribute.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                else
                {
                    //泛型语法:
                    var genericName = (GenericNameSyntax)attribute.Name;
                    var entityName = genericName.TypeArgumentList.Arguments.FirstOrDefault()?.ToString();
                    if (entityName is null)
                        return;

                    // 查找对象是否属于当前编译库:
                    var symbols = context.SemanticModel.Compilation.GetSymbolsWithName(entityName, SymbolFilter.Type);
                    var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();

                    if (symbol is null)
                    {
                        //如果没有找到对应的类,则报错
                        var diagnostic = Diagnostic.Create(RuleGEN044, attribute.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}