using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning disable RS1036 // 指定分析器禁止的 API 强制设置
    public class SourceGenAnalyzer : DiagnosticAnalyzer
#pragma warning restore RS1036 // 指定分析器禁止的 API 强制设置
    {
        #region DiagnosticDescriptors

        const string helplink = "https://github.com/vipwan/Biwen.AutoClassGen#gen-error-code";

        public const string GEN001 = "GEN001";
        public const string GEN011 = "GEN011";
        public const string GEN021 = "GEN021";
        public const string GEN031 = "GEN031";//推荐生成


        /// <summary>
        /// 无法生成类的错误
        /// </summary>
#pragma warning disable RS2008 // 启用分析器发布跟踪
        private static readonly DiagnosticDescriptor InvalidDeclareError = new(id: GEN001,
#pragma warning restore RS2008 // 启用分析器发布跟踪
                                                                              title: "标注接口没有继承基础接口因此不能生成类",
#pragma warning disable RS1032 // 正确定义诊断消息
                                                                              messageFormat: "没有实现基础接口因此不能生成类,请删除标注的特性[AutoGen] or 继承相应的接口.",
#pragma warning restore RS1032 // 正确定义诊断消息
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              helpLinkUri: helplink,
                                                                              isEnabledByDefault: true);


        /// <summary>
        /// 重名错误
        /// </summary>
#pragma warning disable RS2008 // 启用分析器发布跟踪
        private static readonly DiagnosticDescriptor InvalidDeclareNameError = new(id: GEN011,
#pragma warning restore RS2008 // 启用分析器发布跟踪
                                                                              title: "生成类的类名称不可和接口名重名",
#pragma warning disable RS1032 // 正确定义诊断消息
                                                                              messageFormat: "生成类的类名称不可和接口名重名.",
#pragma warning restore RS1032 // 正确定义诊断消息
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              helpLinkUri: helplink,
                                                                              isEnabledByDefault: true);

        /// <summary>
        /// 命名空间规范警告
        /// </summary>
#pragma warning disable RS2008 // 启用分析器发布跟踪
        private static readonly DiagnosticDescriptor SuggestDeclareNameWarning = new(id: GEN021,
#pragma warning restore RS2008 // 启用分析器发布跟踪
                                                                              title: "推荐使用相同的命名空间",
#pragma warning disable RS1032 // 正确定义诊断消息
                                                                              messageFormat: "推荐使用相同的命名空间.",
#pragma warning restore RS1032 // 正确定义诊断消息
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Warning,
                                                                              helpLinkUri: helplink,
                                                                              isEnabledByDefault: true);


        /// <summary>
        /// 推荐使用自动生成
        /// </summary>
#pragma warning disable RS2008 // 启用分析器发布跟踪
        private static readonly DiagnosticDescriptor SuggestAutoGen = new(id: GEN031,
#pragma warning restore RS2008 // 启用分析器发布跟踪
                                                                              title: "使用[AutoGen]自动生成",
#pragma warning disable RS1032 // 正确定义诊断消息
                                                                              messageFormat: "使用[AutoGen]自动生成.",
#pragma warning restore RS1032 // 正确定义诊断消息
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Info,
                                                                              helpLinkUri: helplink,
                                                                              isEnabledByDefault: true);


        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            InvalidDeclareError,
            InvalidDeclareNameError,
            SuggestDeclareNameWarning,
            SuggestAutoGen
            );

        const string AttributeValueMetadataName = "AutoGen";

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) return;
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx =>
            {
                // Find implicitly typed interface declarations.
                var declaration = (InterfaceDeclarationSyntax)ctx.Node;
                if (declaration == null) return;

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

                            if (declaration.Parent is NamespaceDeclarationSyntax @namespace &&
                            @namespace?.Name.ToString() != arg1.GetText().ToString().Replace("\"", ""))
                            {
                                var location = arg1?.GetLocation();
                                // issue warning
                                ctx.ReportDiagnostic(Diagnostic.Create(SuggestDeclareNameWarning, location));
                            }
                        }
                    }
                }

                //suggest
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
            }, SyntaxKind.InterfaceDeclaration);
        }
    }
}