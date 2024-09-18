using System;
using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

/// <summary>
/// 分析Class是否可以转换为Record
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class ClassToRecordAnalyzer : DiagnosticAnalyzer
{

    public const string DiagnosticId = "GEN060";
    private static readonly LocalizableString Title = "Class可以转换为Record";
    private static readonly LocalizableString MessageFormat = "当前的Class可以转换为Record";
    private static readonly LocalizableString Description = "当前的Class可以转换为Record.";
    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor Rule = new(
    DiagnosticId, Title, MessageFormat, Category,
    DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];


    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            return;

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var syntaxTree = context.Node.SyntaxTree;

        // 获取C#语言版本
        var parseOptions = (CSharpParseOptions)syntaxTree.Options;
        var languageVersion = parseOptions.LanguageVersion;
        // 如果语言版本小于C#9则不转换
        if (languageVersion < LanguageVersion.CSharp9)
            return;

        // 如果是NETFramework项目则不转换
        // 获取Compilation
        var compilation = context.SemanticModel.Compilation;

        // 获取目标框架
        var targetFramework = compilation.Options.Platform;

        // 检查是否为 .NET Framework
        bool isDotNetFramework = compilation.ReferencedAssemblyNames
            .Any(assembly => assembly.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase));

        if (isDotNetFramework)
            return;

        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // 如果类已经继承了其他类，则不转换
        if (classDeclaration.BaseList?.Types.Any() ?? false)
            return;

        // 如果类为抽象类，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword))
            return;

        // 如果当前类已经包含了Record关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.RecordKeyword))
            return;

        // 如果当前类已经包含了Sealed关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.SealedKeyword))
            return;

        // 如果当前类已经包含了Static关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        // 如果当前类已经包含了Partial关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            return;

        // 如果当前类已经包含了Unsafe关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.UnsafeKeyword))
            return;

        // 如果当前类已经包含了Virtual关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.VirtualKeyword))
            return;

        // 如果当前类已经包含了Override关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            return;

        // 如果当前类已经包含了New关键字，则不转换
        if (classDeclaration.Modifiers.Any(SyntaxKind.NewKeyword))
            return;
        // 如果当前类标注了特性，则不转换
        if (classDeclaration.AttributeLists.Any())
            return;


        // 类中只有属性, 不存在其他成员则可以转换
        if (classDeclaration.Members.All(m => m is PropertyDeclarationSyntax))
        {
            var diagnostic = Diagnostic.Create(Rule, classDeclaration.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}