// <copyright file="SourceGenCodeFixProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Biwen.AutoClassGen.CodeFixs;

using Desc = DiagnosticDescriptors;

/// <summary>
/// 代码修补提供者.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoGenSourceGenCodeFixProvider))]
[Shared]
internal sealed class AutoGenSourceGenCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// GetFixAllProvider.
    /// </summary>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// FixableDiagnosticIds.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        Desc.GEN001,
        Desc.GEN011,
        Desc.GEN021,
        Desc.GEN031
    ];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            // Only allow code fixes for diagnostics with the matching id.
            if (diagnostic.Id == Desc.GEN021)
            {
                var diagnosticSpan = context.Span;

                // getInnerModeNodeForTie = true so we are replacing the string literal node and not the whole argument node
                var nodeToReplace = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
                if (nodeToReplace == null)
                {
                    return;
                }

                // AncestorsAndSelf 祖先和自己
                // var @namespace = root.Parent?.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().First().Name.ToString();
                var rootCompUnit = (CompilationUnitSyntax)root;
                var @namespace = (rootCompUnit.Members.Where(m => m.IsKind(SyntaxKind.NamespaceDeclaration)).FirstOrDefault() as NamespaceDeclarationSyntax)?.Name.ToString();
                if (string.IsNullOrEmpty(@namespace))
                {
                    @namespace = (rootCompUnit.Members.Where(m => m.IsKind(SyntaxKind.FileScopedNamespaceDeclaration)).FirstOrDefault() as FileScopedNamespaceDeclarationSyntax)?.Name.ToString();
                }

                if (!string.IsNullOrEmpty(@namespace))
                {
                    // Register a code action that will invoke the fix.
                    CodeAction action = CodeAction.Create(
                        "GEN:使用推荐的命名空间",
                        c => ReplaceWithNameOfAsync(context.Document, nodeToReplace, @namespace!, c),
                        equivalenceKey: nameof(AutoGenSourceGenCodeFixProvider));
                    context.RegisterCodeFix(action, diagnostic);
                }
            }
            else if (diagnostic.Id == Desc.GEN011)
            {
                var diagnosticSpan = context.Span;

                // getInnerModeNodeForTie = true so we are replacing the string literal node and not the whole argument node
                var nodeToReplace = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
                if (nodeToReplace == null)
                {
                    return;
                }

                // 移除第一个字母I eg."IRequest" -> "Request"
                var raw = nodeToReplace.GetText().ToString().Substring(2, nodeToReplace.GetText().ToString().Length - 3);
                CodeAction action = CodeAction.Create(
                    "GEN:使用推荐的类名称",
                    c => ReplaceWithNameOfAsync(context.Document, nodeToReplace, raw, c),
                    equivalenceKey: nameof(AutoGenSourceGenCodeFixProvider));
                context.RegisterCodeFix(action, diagnostic);
            }
            else if (diagnostic.Id == Desc.GEN001)
            {
                CodeAction action = CodeAction.Create(
                    "GEN:删除无意义的特性[AutoGen]",
                    async c =>
                    {
                        var root = await context.Document.GetSyntaxRootAsync(c).ConfigureAwait(false);
                        var nowRoot = root?.RemoveNode(root?.FindNode(context.Span)!, SyntaxRemoveOptions.KeepExteriorTrivia);
                        if (nowRoot == null)
                        {
                            return context.Document.WithSyntaxRoot(root!);
                        }

                        return context.Document.WithSyntaxRoot(nowRoot);
                    },
                    equivalenceKey: nameof(AutoGenSourceGenCodeFixProvider));
                context.RegisterCodeFix(action, diagnostic);
            }
            else if (diagnostic.Id == Desc.GEN031)
            {
                CodeAction action = CodeAction.Create(
                    "GEN:添加自动生成特性[AutoGen]",
                    c => AddAttributeAsync(context.Document, root?.FindNode(context.Span)!, "AutoGen", c),
                    equivalenceKey: nameof(AutoGenSourceGenCodeFixProvider));
                context.RegisterCodeFix(action, diagnostic);
            }
        }
    }

    /// <summary>
    /// 替换字符串.
    /// </summary>
    private static async Task<Document> ReplaceWithNameOfAsync(Document document, SyntaxNode nodeToReplace, string stringText, CancellationToken cancellationToken)
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

        // var nameOfExpression = generator.NameOfExpression(generator.IdentifierName(stringText))
        //    .WithTrailingTrivia(trailingTrivia)
        //    .WithLeadingTrivia(leadingTrivia);
        var newRoot = root?.ReplaceNode(nodeToReplace, textExpression);
        return document.WithSyntaxRoot(newRoot!);
    }

    /// <summary>
    /// 给接口添加特性.
    /// </summary>
    private static async Task<Document> AddAttributeAsync(Document document, SyntaxNode nodeToAddAttribute, string attributeName, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (nodeToAddAttribute is not InterfaceDeclarationSyntax)
        {
            return document.WithSyntaxRoot(root!);
        }

        var rootCompUnit = (CompilationUnitSyntax)root!;

        // 命名空间
        var @namespace = (rootCompUnit.Members.Where(
            m => m.IsKind(SyntaxKind.NamespaceDeclaration)).FirstOrDefault() as NamespaceDeclarationSyntax)?.Name.ToString();
        if (string.IsNullOrEmpty(@namespace))
        {
            @namespace = (rootCompUnit.Members.Where(
            m => m.IsKind(SyntaxKind.FileScopedNamespaceDeclaration)).FirstOrDefault() as FileScopedNamespaceDeclarationSyntax)?.Name.ToString();
        }

        // 类名
        var @class = "YourClassName";

        var trailingTrivia = nodeToAddAttribute.GetTrailingTrivia();
        var leadingTrivia = nodeToAddAttribute.GetLeadingTrivia();

        var argumentLis = SyntaxFactory.AttributeArgumentList(
             SyntaxFactory.SeparatedList(
                 [
                     SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,SyntaxFactory.Literal(@class))),
                     SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,SyntaxFactory.Literal(@namespace!))),
                 ]));

        var attributes = (nodeToAddAttribute as InterfaceDeclarationSyntax)!.AttributeLists.Add(
            SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName))
            .WithArgumentList(argumentLis)))
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