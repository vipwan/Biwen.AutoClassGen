using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

/// <summary>
/// 推荐使用文件范围命名空间,命名空间后方键入分号[;]即可自动转换
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class FileScopeNamespaceAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "GEN052";
    private static readonly LocalizableString Title = "使用文件范围命名空间";
    private static readonly LocalizableString MessageFormat = "使用文件范围命名空间";
    private static readonly LocalizableString Description = "使用文件范围命名空间.";
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
        context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration);
    }

    private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node.SyntaxTree;

        //针对Codefirst模式生成的,cs源代码路径包含:`Migrations`的不检查
        if (syntaxTree.FilePath.Split(['/', '\\']).Any(x => x == "Migrations"))
            return;

        //不检测Program.cs
        if (syntaxTree.FilePath.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase))
            return;


        // 获取C#语言版本
        var parseOptions = (CSharpParseOptions)syntaxTree.Options;
        var languageVersion = parseOptions.LanguageVersion;
        // 小于C#10不支持文件范围命名空间
        if (languageVersion < LanguageVersion.CSharp10)
            return;

        var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;

        var root = namespaceDeclaration.SyntaxTree.GetRoot(context.CancellationToken);

        var namespaces = root.DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>();

        // 如果没有命名空间则不检查
        if (!namespaces.Any())
            return;

        // 如果有多个命名空间则不检查
        if (namespaces.Count() != 1)
            return;

        // 仅在命名空间上方提醒
        var location = namespaceDeclaration.SyntaxTree.GetLocation(
            TextSpan.FromBounds(namespaceDeclaration.SpanStart, namespaceDeclaration.Name.Span.End));

        var diagnostic = Diagnostic.Create(Rule, location);
        context.ReportDiagnostic(diagnostic);
    }
}