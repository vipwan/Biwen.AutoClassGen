using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

/// <summary>
/// 文件头部分析器
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class FileHeaderAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "GEN050";
    private static readonly LocalizableString Title = "文件缺少头部信息";
    private static readonly LocalizableString MessageFormat = "文件缺少头部信息";
    private static readonly LocalizableString Description = "每个文件应包含头部信息.";
    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            return;

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var firstToken = root.GetFirstToken();

        // 检查文件是否以注释开头
        var hasHeaderComment = firstToken.LeadingTrivia.Any(trivia =>
        trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
        trivia.IsKind(SyntaxKind.MultiLineCommentTrivia));

        if (hasHeaderComment)
            return;

        var diagnostic = Diagnostic.Create(Rule, Location.Create(context.Tree, TextSpan.FromBounds(0, 0)));
        context.ReportDiagnostic(diagnostic);
    }
}
