// <copyright file="OptionsFieldTypeAnalyzer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Linq;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Biwen.AutoClassGen.Analyzers.BiwenQuickApi
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class OptionsFieldTypeAnalyzer : DiagnosticAnalyzer
    {
        // 诊断ID
        public const string DiagnosticId = "BWN001";

        // 诊断信息
        private static readonly LocalizableString Title = "泛型参数必须是枚举类型";
        private static readonly LocalizableString MessageFormat = "类型 '{0}' 的泛型参数 T 必须是枚举类型";
        private static readonly LocalizableString Description = "OptionsFieldType<T> 和 OptionsMultiFieldType<T> 的泛型参数 T 必须是枚举类型。.";
        private const string Category = "Usage";

        // 需要验证的类型
        private static readonly string[] TargetTypeNames = ["OptionsFieldType", "OptionsMultiFieldType"];

        // 创建诊断规则
        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                return;

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // 注册对泛型名称的分析
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.GenericName);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var genericNameSyntax = (GenericNameSyntax)context.Node;

            // 检查是否是目标类型
            if (!TargetTypeNames.Contains(genericNameSyntax.Identifier.Text))
                return;

            // 获取泛型参数的类型
            if (genericNameSyntax.TypeArgumentList.Arguments.Count != 1)
                return;

            var typeArgument = genericNameSyntax.TypeArgumentList.Arguments[0];
            var typeSymbol = context.SemanticModel.GetTypeInfo(typeArgument).Type;

            // 验证类型参数是否为枚举
            if (typeSymbol == null || typeSymbol.TypeKind != TypeKind.Enum)
            {
                // 报告诊断
                var diagnostic = Diagnostic.Create(
                    Rule,
                    genericNameSyntax.GetLocation(),
                    genericNameSyntax.Identifier.Text);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
