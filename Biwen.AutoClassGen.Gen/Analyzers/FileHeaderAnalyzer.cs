﻿using Microsoft.CodeAnalysis;
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
        var root = (CompilationUnitSyntax)context.Tree.GetRoot(context.CancellationToken);

        if (root is null)
            return;

        //针对Codefirst模式生成的,cs源代码路径包含:`Migrations`的不检查
        if (root.SyntaxTree.FilePath.Split(['/', '\\']).Any(x => x == "Migrations"))
            return;

        //如果cs代码不包含编译信息,直接返回
        if (root.AttributeLists.Count == 0 && root.Usings.Count == 0 && root.Members.Count == 0)
            return;

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
