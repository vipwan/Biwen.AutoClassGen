using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Biwen.AutoClassGen
{

    /// <summary>
    /// 代码修补提供者
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SourceGenCodeFixProvider)), Shared]
    public sealed class SourceGenCodeFixProvider : CodeFixProvider
    {
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            SourceGenAnalyzer.GEN001,
            SourceGenAnalyzer.GEN011,
            SourceGenAnalyzer.GEN021,
            SourceGenAnalyzer.GEN031
            );

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                // Only allow code fixes for diagnostics with the matching id.
                if (diagnostic.Id == SourceGenAnalyzer.GEN021)
                {
                    var diagnosticSpan = context.Span;

                    // getInnerModeNodeForTie = true so we are replacing the string literal node and not the whole argument node
                    var nodeToReplace = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
                    if (nodeToReplace == null)
                    {
                        return;
                    }

                    //AncestorsAndSelf 祖先和自己
                    //var @namespace = root.Parent?.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().First().Name.ToString();

                    var rootCompUnit = (CompilationUnitSyntax)root;
                    var @namespace = ((rootCompUnit.Members.Where(m => m.IsKind(SyntaxKind.NamespaceDeclaration)).Single()) as NamespaceDeclarationSyntax)?.Name.ToString();

                    // Register a code action that will invoke the fix.
                    CodeAction action = CodeAction.Create(
                        "GEN:使用推荐的命名空间",
                        c => ReplaceWithNameOfAsync(context.Document, nodeToReplace, @namespace!, c),
                        equivalenceKey: nameof(SourceGenCodeFixProvider));
                    context.RegisterCodeFix(action, diagnostic);
                }
                else if (diagnostic.Id == SourceGenAnalyzer.GEN011)
                {
                    var diagnosticSpan = context.Span;
                    // getInnerModeNodeForTie = true so we are replacing the string literal node and not the whole argument node
                    var nodeToReplace = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
                    if (nodeToReplace == null)
                    {
                        return;
                    }
                    //移除第一个字母I eg."IRequest" -> "Request"
                    var raw = nodeToReplace.GetText().ToString().Substring(2, nodeToReplace.GetText().ToString().Length - 3);
                    CodeAction action = CodeAction.Create(
                        "GEN:使用推荐的类名称",
                        c => ReplaceWithNameOfAsync(context.Document, nodeToReplace, raw, c),
                        equivalenceKey: nameof(SourceGenCodeFixProvider));
                    context.RegisterCodeFix(action, diagnostic);
                }
                else if (diagnostic.Id == SourceGenAnalyzer.GEN001)
                {
                    CodeAction action = CodeAction.Create(
                        "GEN:删除无意义的特性[AutoGen]", async c =>
                        {
                            var root = await context.Document.GetSyntaxRootAsync(c).ConfigureAwait(false);
                            var nowRoot = root?.RemoveNode(root?.FindNode(context.Span)!, SyntaxRemoveOptions.KeepExteriorTrivia);
                            if (nowRoot == null) return context.Document.WithSyntaxRoot(root!);
                            return context.Document.WithSyntaxRoot(nowRoot);
                        },
                        equivalenceKey: nameof(SourceGenCodeFixProvider));
                    context.RegisterCodeFix(action, diagnostic);
                }
                else if (diagnostic.Id == SourceGenAnalyzer.GEN031)
                {
                    CodeAction action = CodeAction.Create(
                        "GEN:添加自动生成特性[AutoGen]",
                        c => AddAttributeAsync(context.Document, root?.FindNode(context.Span)!, "AutoGen", c),
                        equivalenceKey: nameof(SourceGenCodeFixProvider));
                    context.RegisterCodeFix(action, diagnostic);
                }
            }
        }


        /// <summary>
        /// 替换字符串
        /// </summary>
        /// <param name="document"></param>
        /// <param name="nodeToReplace"></param>
        /// <param name="stringText"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<Document> ReplaceWithNameOfAsync(Document document, SyntaxNode nodeToReplace,
            string stringText, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (nodeToReplace is not LiteralExpressionSyntax)
            {
                return document.WithSyntaxRoot(root!);
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var trailingTrivia = nodeToReplace.GetTrailingTrivia();
            var leadingTrivia = nodeToReplace.GetLeadingTrivia();

            var textExpression = generator.LiteralExpression(stringText)
                 .WithTrailingTrivia(trailingTrivia)
                 .WithLeadingTrivia(leadingTrivia);

            //var nameOfExpression = generator.NameOfExpression(generator.IdentifierName(stringText))
            //    .WithTrailingTrivia(trailingTrivia)
            //    .WithLeadingTrivia(leadingTrivia);

            var newRoot = root?.ReplaceNode(nodeToReplace, textExpression);
            return document.WithSyntaxRoot(newRoot!);
        }

        /// <summary>
        /// 给接口添加特性
        /// </summary>
        /// <param name="document"></param>
        /// <param name="nodeToAddAttribute"></param>
        /// <param name="attributeName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<Document> AddAttributeAsync(Document document, SyntaxNode nodeToAddAttribute,
            string attributeName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (nodeToAddAttribute is not InterfaceDeclarationSyntax)
            {
                return document.WithSyntaxRoot(root!);
            }

            var rootCompUnit = (CompilationUnitSyntax)root!;
            //命名空间
            var @namespace = ((rootCompUnit.Members.Where(
                m => m.IsKind(SyntaxKind.NamespaceDeclaration)).Single()) as NamespaceDeclarationSyntax)?.Name.ToString();
            //类名
            var @class = "YourClassName";

            var trailingTrivia = nodeToAddAttribute.GetTrailingTrivia();
            var leadingTrivia = nodeToAddAttribute.GetLeadingTrivia();

            var argumentLis = SyntaxFactory.AttributeArgumentList(
                 SyntaxFactory.SeparatedList(
                     new AttributeArgumentSyntax[]
                     {
                         SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,SyntaxFactory.Literal(@class))),
                         SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,SyntaxFactory.Literal(@namespace!))),
                     }));

            var attributes = (nodeToAddAttribute as InterfaceDeclarationSyntax)!.AttributeLists.Add(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName))
                .WithArgumentList(argumentLis)
                ))
                .WithTrailingTrivia(trailingTrivia)
                .WithLeadingTrivia(leadingTrivia));

#pragma warning disable CS8604 // 引用类型参数可能为 null。

            return document.WithSyntaxRoot(
                root?.ReplaceNode(
                    oldNode: nodeToAddAttribute,
                    newNode: ((InterfaceDeclarationSyntax)nodeToAddAttribute)?.WithAttributeLists(attributes)));

#pragma warning restore CS8604 // 引用类型参数可能为 null。

        }
    }
}