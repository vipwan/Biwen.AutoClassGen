using System;
using System.Collections.Immutable;

namespace Biwen.AutoClassGen.Analyzers;

/// <summary>
/// 将异步方法名改为以Async结尾
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncMethodNameAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "GEN051";
    private static readonly LocalizableString Title = "将异步方法名改为以Async结尾";
    private static readonly LocalizableString MessageFormat = "将异步方法名改为以Async结尾";
    private static readonly LocalizableString Description = "将异步方法名改为以Async结尾.";
    private const string Category = "Documentation";

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
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }


    private const string AsyncSuffix = "Async";

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        //如果包含Async关键字
        if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            if (!methodDeclaration.Identifier.Text.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }

        var returnType = methodDeclaration.ReturnType;

        //如果返回类型为Task或者Task<T>,或者ValueTask<T>,ValueTask 则方法名应该以Async结尾
        // 判断返回类型是否为 Task 或 ValueTask
        if (returnType is IdentifierNameSyntax identifierName)
        {
            if (identifierName.Identifier.Text == "Task" || identifierName.Identifier.Text == "ValueTask")
            {
                if (!methodDeclaration.Identifier.Text.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        else if (returnType is GenericNameSyntax genericName && (genericName.Identifier.Text == "Task" || genericName.Identifier.Text == "ValueTask"))
        {
            if (!methodDeclaration.Identifier.Text.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}