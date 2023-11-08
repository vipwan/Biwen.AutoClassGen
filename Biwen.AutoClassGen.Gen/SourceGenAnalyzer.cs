namespace Biwen.AutoClassGen
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using Desc = DiagnosticDescriptors;


    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SourceGenAnalyzer : DiagnosticAnalyzer
    {


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Desc.InvalidDeclareError,
            Desc.InvalidDeclareNameError,
            Desc.SuggestDeclareNameWarning,
            Desc.SuggestAutoGen,
            Desc.MutiMarkedAutoDtoError,
            Desc.MarkedAbstractAutoDtoError);

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
                                    ctx.ReportDiagnostic(Diagnostic.Create(Desc.InvalidDeclareError, attr.GetLocation()));
                                }

                                var arg0 = attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataName)
                                .ArgumentList!.Arguments[0];

                                var arg1 = attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataName)
                                .ArgumentList!.Arguments[1];

                                if (declaration.Identifier.Text == arg0.GetText().ToString().Replace("\"", ""))
                                {
                                    var location = arg0?.GetLocation();
                                    // issue error
                                    ctx.ReportDiagnostic(Diagnostic.Create(Desc.InvalidDeclareNameError, location));
                                }

                                // NamespaceDeclarationSyntax
                                if (declaration.Parent is NamespaceDeclarationSyntax @namespace &&
                                @namespace?.Name.ToString() != arg1?.GetText().ToString().Replace("\"", ""))
                                {
                                    var location = arg1?.GetLocation();
                                    // issue warning
                                    ctx.ReportDiagnostic(Diagnostic.Create(Desc.SuggestDeclareNameWarning, location));
                                }
                                // FileScopedNamespaceDeclaration
                                if (declaration.Parent is FileScopedNamespaceDeclarationSyntax @namespace2 &&
                                @namespace2?.Name.ToString() != arg1?.GetText().ToString().Replace("\"", ""))
                                {
                                    var location = arg1?.GetLocation();
                                    // issue warning
                                    ctx.ReportDiagnostic(Diagnostic.Create(Desc.SuggestDeclareNameWarning, location));
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
                            ctx.ReportDiagnostic(Diagnostic.Create(Desc.SuggestAutoGen, location));
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
                                ctx.ReportDiagnostic(Diagnostic.Create(Desc.MutiMarkedAutoDtoError, location));
                            }

                            if (attr.Attributes.Where(x => x.Name.ToString().IndexOf(
                                AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0).Any())
                            {
                                if (declaration.Modifiers.Any(x => x.ValueText == "abstract"))
                                {
                                    var location = attr.Attributes.Where(
                                        x => x.Name.ToString().IndexOf(AttributeValueMetadataNameDto, StringComparison.Ordinal) == 0).First().GetLocation();
                                    // issue error
                                    ctx.ReportDiagnostic(Diagnostic.Create(Desc.MarkedAbstractAutoDtoError, location));
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