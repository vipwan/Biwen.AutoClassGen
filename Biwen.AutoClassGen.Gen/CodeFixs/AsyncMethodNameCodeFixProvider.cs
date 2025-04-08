// <copyright file="SourceGenCodeFixProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Biwen.AutoClassGen.Analyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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
                createChangedSolution:
                c =>
                FixSolutionAsync(context.Document, diagnosticSpan, c),
                equivalenceKey: Title),
            diagnostic);

        return Task.CompletedTask;
    }

    private const string AsyncSuffix = "Async";

    private static async Task<Solution> FixSolutionAsync(Document document, TextSpan diagnosticSpan, CancellationToken c)
    {
        var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(c).ConfigureAwait(false);

        if (root == null || semanticModel == null)
            return document.Project.Solution;

        var node = root.FindNode(diagnosticSpan);

        if (node is not MethodDeclarationSyntax methodDeclaration)
            return document.Project.Solution;

        // 获取方法符号
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, c);
        if (methodSymbol == null)
            return document.Project.Solution;

        // 创建新的方法名
        var newName = $"{methodDeclaration.Identifier.Text}{AsyncSuffix}";

        // 使用Roslyn的重命名服务执行全面重命名
        // 这将处理所有引用、接口实现和方法调用
        var solution = document.Project.Solution;

        // 关键改进：添加日志以便调试
        System.Diagnostics.Debug.WriteLine($"开始重命名方法: {methodSymbol.Name} -> {newName}");
        System.Diagnostics.Debug.WriteLine($"方法所在类: {methodSymbol.ContainingType.Name}");

        // 查找所有接口方法和实现方法
        var allRelatedSymbols = new List<ISymbol> { methodSymbol };

        // 查找接口实现
        if (methodSymbol.IsOverride || methodSymbol.IsImplementation())
        {
            var implementations = await SymbolFinder.FindImplementationsAsync(methodSymbol, solution, cancellationToken: c).ConfigureAwait(false);
            foreach (var impl in implementations)
            {
                System.Diagnostics.Debug.WriteLine($"找到实现: {impl.Name} 在 {impl.ContainingType.Name}");
                allRelatedSymbols.Add(impl);
            }

            // 查找被实现的接口方法
            if (methodSymbol.IsImplementation())
            {
                var interfaceMember = methodSymbol.GetInterfaceMember();
                if (interfaceMember != null)
                {
                    System.Diagnostics.Debug.WriteLine($"找到接口: {interfaceMember.Name} 在 {interfaceMember.ContainingType.Name}");
                    allRelatedSymbols.Add(interfaceMember);
                }
            }
        }

        // 如果是接口方法，查找所有实现
        if (methodSymbol.ContainingType.TypeKind == TypeKind.Interface)
        {
            var implementations = SymbolFinder.FindImplementationsAsync(methodSymbol, solution, cancellationToken: c).ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (var impl in implementations)
            {
                System.Diagnostics.Debug.WriteLine($"找到实现: {impl.Name} 在 {impl.ContainingType.Name}");
                allRelatedSymbols.Add(impl);
            }
        }

        var symbolRenameOptions = new SymbolRenameOptions
        {
            RenameFile = false,      // 不重命名文件
            RenameInComments = true, // 支持注释重命名
            RenameInStrings = false, // 不在字符串中重命名
            RenameOverloads = false, // 不重命名重载
        };

        // 执行重命名操作
        var newSolution = Renamer.RenameSymbolAsync(
            solution,
            methodSymbol,
            symbolRenameOptions,
            newName,
            c).ConfigureAwait(false).GetAwaiter().GetResult();

        System.Diagnostics.Debug.WriteLine("重命名方法完成");
        return newSolution;
    }
}

// 扩展方法，用于检查方法是否是接口实现
#pragma warning disable SA1402 // File may only contain a single type
internal static class SymbolExtensions
#pragma warning restore SA1402 // File may only contain a single type
{
    public static bool IsImplementation(this IMethodSymbol method)
    {
        return method.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers())
            .OfType<IMethodSymbol>()
            .Any(m => SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(m), method) == true);
    }

    public static IMethodSymbol GetInterfaceMember(this IMethodSymbol method)
    {
        foreach (var iface in method.ContainingType.AllInterfaces)
        {
            foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(member), method))
                {
                    return member;
                }
            }
        }
        return null!;
    }
}
