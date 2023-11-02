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

        /// <summary>
        /// 无法生成类的错误
        /// </summary>
#pragma warning disable RS2008 // 启用分析器发布跟踪
        private static readonly DiagnosticDescriptor InvalidDeclareError = new(id: "GEN001",
#pragma warning restore RS2008 // 启用分析器发布跟踪
                                                                              title: "标注接口没有继承基础接口因此不能生成类",
#pragma warning disable RS1032 // 正确定义诊断消息
                                                                              messageFormat: "没有实现基础接口因此不能生成类,请删除标注的特性[AutoGen] or 继承相应的接口.",
#pragma warning restore RS1032 // 正确定义诊断消息
                                                                              category: typeof(SourceGenerator).Assembly.GetName().Name,
                                                                              DiagnosticSeverity.Error,
                                                                              isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(InvalidDeclareError);

        const string AttributeValueMetadataName = "AutoGen";

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) return;
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx =>
            {
                // Find implicitly typed interface declarations.
                InterfaceDeclarationSyntax declaration = (InterfaceDeclarationSyntax)ctx.Node;

                if (declaration == null) return;
                if (declaration.AttributeLists.Count == 0) return;

                foreach (var attr in declaration.AttributeLists.AsEnumerable())
                {
                    if (attr.Attributes.Any(x => x.Name.ToString() == AttributeValueMetadataName))
                    {
                        if (declaration.BaseList == null || !declaration.BaseList.Types.Any())
                        {
                            // issue error
                            ctx.ReportDiagnostic(Diagnostic.Create(InvalidDeclareError, attr.GetLocation()));
                        }
                    }
                }
            }, SyntaxKind.InterfaceDeclaration);
        }
    }
}