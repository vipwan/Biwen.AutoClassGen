using System.Collections.Immutable;
using System.Linq;

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

        var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.Text);

        #region 如果项目是测试项目,则不检查

        //判断代码所在项目是否是测试项目,xUnit,NUnit,MSTest
        var istestProj = context.SemanticModel.Compilation.ReferencedAssemblyNames.Any(
            a =>
            a.Name == "MSTest.TestFramework" ||
            a.Name == "xunit.core" ||
            a.Name == "nunit.framework");

        if (istestProj)
            return;

        #endregion

        //如果方法是重写的方法则不检查
        if (methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
        {
            return;
        }

        if (methodDeclaration.Parent is ClassDeclarationSyntax parent)
        {
            #region 处理MVC Controller

            //如果parent是Controller则不检查,判断名称是否包含结尾Controller
            if (parent.Identifier.Text.EndsWith("Controller", StringComparison.Ordinal))
            {
                return;
            }

            //如果parent是ApiController则不检查
            if (parent.AttributeLists.SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString() == "ApiController"))
            {
                return;
            }

            //返回IActionResult的方法不检查
            if (methodDeclaration.ReturnType is IdentifierNameSyntax action && action.Identifier.Text == "IActionResult")
            {
                return;
            }
            //返回Task<IActionResult>的方法不检查
            if (methodDeclaration.ReturnType is GenericNameSyntax genericName && genericName.Identifier.Text == "Task" &&
                genericName.TypeArgumentList.Arguments.Count == 1)
            {
                var typeArgument = genericName.TypeArgumentList.Arguments[0];
                if (typeArgument is IdentifierNameSyntax gAction && gAction.Identifier.Text == "IActionResult")
                {
                    return;
                }
            }

            #endregion

            #region 不处理Microsoft.AspNetCore.SignalR.Hub

            if (context.ContainingSymbol!.ContainingSymbol is ITypeSymbol { } hubSymbol)
            {
                //如果parent是Hub 和 Hub<T>则不检查
                if (hubSymbol?.BaseType?.ToDisplayString().Contains("Microsoft.AspNetCore.SignalR.Hub") is true)
                {
                    return;
                }
            }

            #endregion

            //如果方法的父亲是类,且该方法是接口的实现方法则不检查
            if (context.ContainingSymbol!.ContainingSymbol is ITypeSymbol { } parentSymbol)
            {
                //查询parentSymbol的祖先符号.如果祖先符号存在当前方法则不检查
                var interfaceSymbols = parentSymbol.AllInterfaces;
                foreach (var interfaceSymbol in interfaceSymbols)
                {
                    if (interfaceSymbol.GetMembers(methodDeclaration.Identifier.Text)
                        .Any(m => m.Kind == SymbolKind.Method && m.Name == methodDeclaration.Identifier.Text))
                    {
                        return;
                    }
                }
            }
        }

        //如果包含Async关键字
        if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            if (!methodDeclaration.Identifier.Text.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(diagnostic);
                return;
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
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
            }
        }
        else if (returnType is GenericNameSyntax genericName && (genericName.Identifier.Text == "Task" || genericName.Identifier.Text == "ValueTask"))
        {
            if (!methodDeclaration.Identifier.Text.EndsWith(AsyncSuffix, StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }
}