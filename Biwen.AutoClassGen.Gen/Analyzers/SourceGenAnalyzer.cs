using System;
using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

using Desc = DiagnosticDescriptors;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class SourceGenAnalyzer : DiagnosticAnalyzer
{

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        Desc.InvalidDeclareError,
        Desc.InvalidDeclareNameError,
        Desc.SuggestDeclareNameWarning,
        Desc.SuggestAutoGen,
        Desc.MutiMarkedAutoDtoError,
        Desc.MarkedAbstractAutoDtoError,
        Desc.MarkedAutoDecorError,
    ];

    private const string AttributeValueMetadataName = "AutoGen";
    /// <summary>
    /// Dto特性名称,注意存在泛型的情况
    /// </summary>
    private const string AttributeValueMetadataNameDto = "AutoDto";

    /// <summary>
    /// AutoDecor,注意存在泛型的情况
    /// </summary>

    private const string AttributeValueMetadataNameDecor = "AutoDecor";



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
                        // autoGen
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

                        // decor
                        if (attr.Attributes.Where(x => x.Name.ToString().IndexOf(
                            AttributeValueMetadataNameDecor, StringComparison.Ordinal) == 0).Any())
                        {
                            foreach (var at in attr.Attributes)
                            {
                                if (at.ArgumentList != null && at.ArgumentList.Arguments.Any())
                                {
                                    var arg0 = at.ArgumentList.Arguments[0];
                                    if (arg0.Expression is TypeOfExpressionSyntax express)
                                    {
                                        var implNameStr = express.Type.ToString();
                                        var symbol = ctx.Compilation.GetSymbolsWithName(implNameStr, SymbolFilter.Type);
                                        if (symbol.Any())
                                        {
                                            var implName = symbol.First();
                                            //IHelloService
                                            var interfaceName = declaration.Identifier.ValueText;

                                            if ((implName.OriginalDefinition as INamedTypeSymbol)?.AllInterfaces.Any(x => x.Name == interfaceName) is not true)
                                            {
                                                var location = at?.GetLocation();
                                                // issue error
                                                ctx.ReportDiagnostic(Diagnostic.Create(Desc.MarkedAutoDecorError, location));
                                            }
                                        }
                                    }
                                }
                                if (at?.Name is GenericNameSyntax genericNameSyntax)
                                {
                                    var implNameStr = genericNameSyntax.TypeArgumentList.Arguments[0].ToString();
                                    var symbol = ctx.Compilation.GetSymbolsWithName(genericNameSyntax.TypeArgumentList.Arguments[0].ToString(), SymbolFilter.Type);
                                    if (symbol.Any())
                                    {
                                        var implName = symbol.First();
                                        var interfaceName = declaration.Identifier.ValueText;
                                        if ((implName.OriginalDefinition as INamedTypeSymbol)?.AllInterfaces.Any(x => x.Name == interfaceName) is not true)
                                        {
                                            var location = at?.GetLocation();
                                            // issue error
                                            ctx.ReportDiagnostic(Diagnostic.Create(Desc.MarkedAutoDecorError, location));
                                        }
                                    }
                                }
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

                        if (attr.Attributes.Where(x => x.Name.ToString().IndexOf(
                            AttributeValueMetadataNameDecor, StringComparison.Ordinal) == 0).Any())
                        {
                            foreach (var at in attr.Attributes)
                            {
                                if (at.ArgumentList != null && at.ArgumentList.Arguments.Any())
                                {
                                    var arg0 = at.ArgumentList.Arguments[0];
                                    if (arg0.Expression is TypeOfExpressionSyntax express)
                                    {
                                        var implNameStr = express.Type.ToString();
                                        var symbol = ctx.Compilation.GetSymbolsWithName(implNameStr, SymbolFilter.Type);
                                        if (symbol.Any())
                                        {
                                            var implName = symbol.First();
                                            if (declaration.BaseList?.Types.Any(x => x.Type.ToString() == implName.Name) is not true
                                            && declaration.Identifier.Text != (implName as ITypeSymbol)?.BaseType?.Name)
                                            {
                                                var location = at?.GetLocation();
                                                // issue error
                                                ctx.ReportDiagnostic(Diagnostic.Create(Desc.MarkedAutoDecorError, location));
                                            }
                                        }
                                    }
                                }
                                if (at?.Name is GenericNameSyntax genericNameSyntax)
                                {
                                    var implNameStr = genericNameSyntax.TypeArgumentList.Arguments[0].ToString();
                                    var symbol = ctx.Compilation.GetSymbolsWithName(genericNameSyntax.TypeArgumentList.Arguments[0].ToString(), SymbolFilter.Type);

                                    if (symbol.Any())
                                    {
                                        var implName = symbol.First();
                                        if (declaration.BaseList?.Types.Any(x => x.Type.ToString() == implName.Name) is not true
                                        && declaration.Identifier.Text != (implName as ITypeSymbol)?.BaseType?.Name)
                                        {
                                            var location = at?.GetLocation();
                                            // issue error
                                            ctx.ReportDiagnostic(Diagnostic.Create(Desc.MarkedAutoDecorError, location));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        },
        SyntaxKind.InterfaceDeclaration,
        SyntaxKind.ClassDeclaration
        );
    }
}