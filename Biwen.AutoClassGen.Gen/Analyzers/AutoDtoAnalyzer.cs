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
    public const string DiagnosticId = "GEN044";

    private static readonly LocalizableString Title = "不可使用外部库生成DTO";
    private static readonly LocalizableString MessageFormat = "不可使用外部库生成DTO";
    private static readonly LocalizableString Description = "不可使用外部库生成DTO.";
    private const string Category = "GEN";
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
                        var diagnostic = Diagnostic.Create(Rule, attribute.GetLocation());
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
                        var diagnostic = Diagnostic.Create(Rule, attribute.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}