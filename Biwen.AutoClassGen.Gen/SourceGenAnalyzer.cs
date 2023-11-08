namespace Biwen.AutoClassGen
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SourceGenAnalyzer : DiagnosticAnalyzer
    {
        #region DiagnosticDescriptors

        private const string Helplink = "https://github.com/vipwan/Biwen.AutoClassGen#gen-error-code";

        public const string GEN001 = "GEN001";
        public const string GEN011 = "GEN011";
        public const string GEN021 = "GEN021";
        public const string GEN031 = "GEN031"; // 推荐生成
        public const string GEN041 = "GEN041"; // 重复标注
        public const string GEN042 = "GEN042"; // 不可用于abstract基类

        /// <summary>
        /// 无法生成类的错误
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidDeclareError = new(id: GEN001,
                                                                              title: "标注接口没有继承基础接口因此不能生成类",
                                                                              messageFormat: "没有实现基础接口因此不能生成类,请删除标注的特性[AutoGen] or 继承相应的接口",
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              helpLinkUri: Helplink,
                                                                              isEnabledByDefault: true);


        /// <summary>
        /// 重名错误
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidDeclareNameError = new(id: GEN011,
                                                                              title: "生成类的类名称不可和接口名重名",
                                                                              messageFormat: "生成类的类名称不可和接口名重名",
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              helpLinkUri: Helplink,
                                                                              isEnabledByDefault: true);

        /// <summary>
        /// 命名空间规范警告
        /// </summary>
        public static readonly DiagnosticDescriptor SuggestDeclareNameWarning = new(id: GEN021,
                                                                              title: "推荐使用相同的命名空间",
                                                                              messageFormat: "推荐使用相同的命名空间",
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Warning,
                                                                              helpLinkUri: Helplink,
                                                                              isEnabledByDefault: true);

        /// <summary>
        /// 推荐使用自动生成
        /// </summary>
        public static readonly DiagnosticDescriptor SuggestAutoGen = new(id: GEN031,
                                                                              title: "使用[AutoGen]自动生成",
                                                                              messageFormat: "使用[AutoGen]自动生成",
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Info,
                                                                              helpLinkUri: Helplink,
                                                                              isEnabledByDefault: true);


        /// <summary>
        /// Dto特性重复标注
        /// </summary>
        public static readonly DiagnosticDescriptor MutiMarkedAutoDtoError = new(id: GEN041,
                                                                              title: "重复标注[AutoDto]",
                                                                              messageFormat: "重复标注了[AutoDto],请删除多余的标注",
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              helpLinkUri: Helplink,
                                                                              isEnabledByDefault: true);


        /// <summary>
        /// Dto错误标注
        /// </summary>
        public static readonly DiagnosticDescriptor MarkedAbstractAutoDtoError = new(id: GEN042,
                                                                              title: "不可在abstract类上标注[AutoDto]",
                                                                              messageFormat: "不可在abstract类上标注[AutoDto]",
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              helpLinkUri: Helplink,
                                                                              isEnabledByDefault: true);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            InvalidDeclareError,
            InvalidDeclareNameError,
            SuggestDeclareNameWarning,
            SuggestAutoGen,
            MutiMarkedAutoDtoError,
            MarkedAbstractAutoDtoError);

        private const string AttributeValueMetadataName = "AutoGen";
        /// <summary>
        /// Dto特性名称,注意存在泛型的情况
        /// </summary>
        private const string AttributeValueMetadataNameDto = "AutoDto";


        public override void Initialize(AnalysisContext context)
        {
            if (context == null) return;
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx =>
            {
                var kind = ctx.Node.Kind();

                // InterfaceDeclarationSyntax
                if (kind == SyntaxKind.InterfaceDeclaration)
                {
                    var declaration = (InterfaceDeclarationSyntax)ctx.Node;

                    if (declaration.AttributeLists.Count > 0)
                    {
                        foreach (var attr in declaration.AttributeLists.AsEnumerable())
                        {
                            if (attr.Attributes.Any(x => x.Name.ToString() == AttributeValueMetadataName))
                            {
                                if (declaration.BaseList == null || !declaration.BaseList.Types.Any())
                                {
                                    // issue error
                                    ctx.ReportDiagnostic(Diagnostic.Create(InvalidDeclareError, attr.GetLocation()));
                                }

                                var arg0 = attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataName)
                                .ArgumentList!.Arguments[0];

                                var arg1 = attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataName)
                                .ArgumentList!.Arguments[1];

                                if (declaration.Identifier.Text == arg0.GetText().ToString().Replace("\"", ""))
                                {
                                    var location = arg0?.GetLocation();
                                    // issue error
                                    ctx.ReportDiagnostic(Diagnostic.Create(InvalidDeclareNameError, location));
                                }

                                // NamespaceDeclarationSyntax
                                if (declaration.Parent is NamespaceDeclarationSyntax @namespace &&
                                @namespace?.Name.ToString() != arg1?.GetText().ToString().Replace("\"", ""))
                                {
                                    var location = arg1?.GetLocation();
                                    // issue warning
                                    ctx.ReportDiagnostic(Diagnostic.Create(SuggestDeclareNameWarning, location));
                                }
                                // FileScopedNamespaceDeclaration
                                if (declaration.Parent is FileScopedNamespaceDeclarationSyntax @namespace2 &&
                                @namespace2?.Name.ToString() != arg1?.GetText().ToString().Replace("\"", ""))
                                {
                                    var location = arg1?.GetLocation();
                                    // issue warning
                                    ctx.ReportDiagnostic(Diagnostic.Create(SuggestDeclareNameWarning, location));
                                }
                            }
                        }
                    }
                    // suggest
                    if (declaration.BaseList != null && declaration.BaseList.Types.Any(x => x.IsKind(SyntaxKind.SimpleBaseType)))
                    {
                        var haveAttr = declaration.AttributeLists.Any(x => x.Attributes.Any(x => x.Name.ToString() == AttributeValueMetadataName));
                        if (!haveAttr)
                        {
                            var location = declaration.GetLocation();
                            // issue suggest
                            ctx.ReportDiagnostic(Diagnostic.Create(SuggestAutoGen, location));
                        }
                    }
                }

                // ClassDeclarationSyntax
                if (kind == SyntaxKind.ClassDeclaration)
                {
                    var declaration = (ClassDeclarationSyntax)ctx.Node;
                    if (declaration.AttributeLists.Count > 0)
                    {
                        foreach (var attr in declaration.AttributeLists.AsEnumerable())
                        {
                            if (attr.Attributes.Where(x => x.Name.ToString().IndexOf(
                                AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0).Count() > 1)
                            {
                                var location = attr.GetLocation();
                                // issue error
                                ctx.ReportDiagnostic(Diagnostic.Create(MutiMarkedAutoDtoError, location));
                            }

                            if (attr.Attributes.Where(x => x.Name.ToString().IndexOf(
                                AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0).Any())
                            {
                                if (declaration.Modifiers.Any(x => x.ValueText == "abstract"))
                                {
                                    var location = attr.Attributes.Where(
                                        x => x.Name.ToString().IndexOf(AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0).First().GetLocation();
                                    // issue error
                                    ctx.ReportDiagnostic(Diagnostic.Create(MarkedAbstractAutoDtoError, location));
                                }
                            }
                        }
                    }
                }
            },
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.ClassDeclaration);

        }
    }
}