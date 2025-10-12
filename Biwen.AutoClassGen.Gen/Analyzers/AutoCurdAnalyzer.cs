// <copyright file="AutoDtoAnalyzer.cs" company="vipwan">
// MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoCurdAnalyzer : DiagnosticAnalyzer
{
    private const string AttributeOriginalDefinition = "Biwen.AutoClassGen.Attributes.AutoCurdAttribute`1";
    public const string DiagnosticId = "GENCURD001";

    private static readonly DiagnosticDescriptor InvalidDbContextDescriptor = new(
        id: DiagnosticId,
        title: "AutoCurd generic argument must be a DbContext",
        messageFormat: "The generic type argument '{0}' of AutoCurdAttribute must derive from Microsoft.EntityFrameworkCore.DbContext",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [InvalidDbContextDescriptor];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            return;

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        foreach (var attr in namedType.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null) continue;

            // More robust detection: name + arity OR known metadata name
            bool isAutoCurdAttr = false;
            if (attrClass.Name == "AutoCurdAttribute" && attrClass.Arity == 1)
            {
                isAutoCurdAttr = true;
            }
            else
            {
                var orig = attrClass.OriginalDefinition?.ToDisplayString();
                if (orig == AttributeOriginalDefinition)
                    isAutoCurdAttr = true;
            }

            if (!isAutoCurdAttr) continue;

            // Attribute is AutoCurdAttribute<T>
            // The generic type argument is available on the attribute class type arguments
            ITypeSymbol? typeArg = null;
            if (attrClass.TypeArguments.Length > 0)
            {
                typeArg = attrClass.TypeArguments[0];
            }

            // If can't determine type arg, try constructor arguments (typeof(X) passed)
            if (typeArg == null && attr.ConstructorArguments.Length > 0)
            {
                var first = attr.ConstructorArguments[0];
                if (first.Kind == TypedConstantKind.Type && first.Value is ITypeSymbol ts)
                {
                    typeArg = ts;
                }
            }

            // If still can't determine type arg, skip
            if (typeArg == null) continue;

            // Look up Microsoft.EntityFrameworkCore.DbContext symbol
            var dbContextSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            if (dbContextSymbol == null)
            {
                // If DbContext type not referenced in compilation, cannot validate
                continue;
            }

            // Walk base types of the provided type argument to see if it derives from DbContext
            bool derives = false;
            if (typeArg is INamedTypeSymbol nts)
            {
                var baseType = nts;
                while (baseType != null)
                {
                    if (SymbolEqualityComparer.Default.Equals(baseType, dbContextSymbol) ||
                        (baseType.OriginalDefinition != null && SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, dbContextSymbol)))
                    {
                        derives = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }

            if (!derives)
            {
                // Report diagnostic on the attribute location if available, otherwise on the type declaration
                Location? loc = null;
                if (attr.ApplicationSyntaxReference != null)
                {
                    var syntax = attr.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
                    loc = syntax.GetLocation();
                }

                var fallbackLocation = namedType.Locations.FirstOrDefault();
                var diagnostic = Diagnostic.Create(InvalidDbContextDescriptor, loc ?? fallbackLocation, typeArg.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}