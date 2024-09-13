using Biwen.AutoClassGen.Analyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Biwen.AutoClassGen.CodeFixs;

/// <summary>
/// 将异步方法名改为以Async结尾
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncMethodNameCodeFixProvider))]
[Shared]
internal class AsyncMethodNameCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [AsyncMethodNameAnalyzer.DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    private const string Title = "将异步方法名改为以Async结尾";


    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument:
                c =>
                FixDocumentAsync(context.Document, diagnostic, c),
                equivalenceKey: Title),
            diagnostic);

        return Task.CompletedTask;

    }

    private const string AsyncSuffix = "Async";

    private static async Task<Document> FixDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken c)
    {
        var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

        if (root == null)
            return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var methodDeclaration = (MethodDeclarationSyntax)node;
        var newName = $"{methodDeclaration.Identifier.Text}{AsyncSuffix}";
        var newRoot = root.ReplaceNode(methodDeclaration, methodDeclaration.WithIdentifier(SyntaxFactory.Identifier(newName)));
        return document.WithSyntaxRoot(newRoot);
    }
}
