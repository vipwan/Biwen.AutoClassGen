using System.Collections.Immutable;
using System.Text;

namespace Biwen.AutoClassGen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class EncodingUTF8Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "GEN053";
    private static readonly LocalizableString Title = "使用UTF8编码";
    private static readonly LocalizableString MessageFormat = "使用UTF8编码";
    private static readonly LocalizableString Description = "使用UTF8编码.";
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
        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var encoding = context.Tree.Encoding;

        // 检查是否为 UTF-8 编码（包括带 BOM 和不带 BOM 的情况）
        var isUtf8 = encoding != null &&
            (encoding == Encoding.UTF8 ||
             encoding == new UTF8Encoding(true) ||
             encoding == new UTF8Encoding(false));

        if (!isUtf8)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);
            var location = Location.Create(context.Tree, TextSpan.FromBounds(0, root.FullSpan.End));

            var diagnostic = Diagnostic.Create(Rule, location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}