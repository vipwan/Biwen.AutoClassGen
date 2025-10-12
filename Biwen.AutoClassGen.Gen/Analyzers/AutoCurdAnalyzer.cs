// <copyright file="AutoDtoAnalyzer.cs" company="vipwan">
// MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

using System.Collections.Immutable;
using System.Linq;

namespace Biwen.AutoClassGen.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoCurdAnalyzer : DiagnosticAnalyzer
{
    //private const string AttributeOriginalDefinition = "Biwen.AutoClassGen.Attributes.AutoCurdAttribute`1";
    //private const string AttributeNonGenericMetadataName = "Biwen.AutoClassGen.Attributes.AutoCurdAttribute";
    public const string DiagnosticId = "GENCURD001";
    public const string DuplicateDiagnosticId = "GENCURD002";
    public const string MissingDbSetDiagnosticId = "GENCURD003";

    private static readonly DiagnosticDescriptor InvalidDbContextDescriptor = new(
        id: DiagnosticId,
        title: "AutoCurd generic argument must be a DbContext",
        messageFormat: "The generic type argument '{0}' of AutoCurdAttribute must derive from Microsoft.EntityFrameworkCore.DbContext",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateAutoCurdDescriptor = new(
        id: DuplicateDiagnosticId,
        title: "Cannot use multiple AutoCurdAttribute on the same type",
        messageFormat: "Do not apply more than one AutoCurdAttribute on the same type. Please keep only one form.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingDbSetDescriptor = new(
        id: MissingDbSetDiagnosticId,
        title: "Entity not registered in DbContext",
        messageFormat: "The entity '{0}' marked with AutoCurd is not exposed as a DbSet<{0}> in DbContext '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        =>
        [
        InvalidDbContextDescriptor,
        DuplicateAutoCurdDescriptor,
        MissingDbSetDescriptor
        ];

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
        var attributes = namedType.GetAttributes();

        // 检查是否存在多个 AutoCurdAttribute（不论泛型或非泛型形式）
        var autoCurdAttrs = attributes.Where(a => a.AttributeClass != null && a.AttributeClass.Name == "AutoCurdAttribute").ToList();
        if (autoCurdAttrs.Count > 1)
        {
            foreach (var attr in autoCurdAttrs)
            {
                Location? loc = null;
                if (attr.ApplicationSyntaxReference != null)
                {
                    var syntax = attr.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
                    loc = syntax.GetLocation();
                }
                var fallback = namedType.Locations.FirstOrDefault();
                context.ReportDiagnostic(Diagnostic.Create(DuplicateAutoCurdDescriptor, loc ?? fallback));
            }

            // don't proceed further when duplicate attributes present
            return;
        }

        foreach (var attr in autoCurdAttrs)
        {
            var attrClass = attr.AttributeClass;
            if (attrClass == null) continue;

            // Attribute is AutoCurdAttribute<T> or AutoCurdAttribute(typeof(...))
            ITypeSymbol? dbContextTypeSymbol = null;
            if (attrClass.TypeArguments.Length > 0)
            {
                dbContextTypeSymbol = attrClass.TypeArguments[0];
            }

            // 非泛型 typeof 形式：尝试从构造函数参数读取 typeof(...) 表达式
            if (dbContextTypeSymbol == null && attr.ConstructorArguments.Length > 0)
            {
                var first = attr.ConstructorArguments[0];
                if (first.Kind == TypedConstantKind.Type && first.Value is ITypeSymbol ts)
                {
                    dbContextTypeSymbol = ts;
                }
            }

            if (dbContextTypeSymbol == null) continue;

            // Look up Microsoft.EntityFrameworkCore.DbContext symbol
            var dbContextBaseSymbol = context.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
            if (dbContextBaseSymbol == null)
            {
                // If DbContext type not referenced in compilation, cannot validate
                continue;
            }

            // Walk base types of the provided DbContext type argument to see if it derives from DbContext
            bool derives = false;
            INamedTypeSymbol? dbts = dbContextTypeSymbol as INamedTypeSymbol;
            if (dbts != null)
            {
                var baseType = dbts;
                while (baseType != null)
                {
                    if (SymbolEqualityComparer.Default.Equals(baseType, dbContextBaseSymbol) ||
                        (baseType.OriginalDefinition != null && SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, dbContextBaseSymbol)))
                    {
                        derives = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }

            if (!derives)
            {
                // Report invalid dbcontext diagnostic (handled elsewhere)
                Location? loc = null;
                if (attr.ApplicationSyntaxReference != null)
                {
                    var syntax = attr.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
                    loc = syntax.GetLocation();
                }
                var fallbackLocation = namedType.Locations.FirstOrDefault();
                var diagnostic = Diagnostic.Create(InvalidDbContextDescriptor, loc ?? fallbackLocation, dbContextTypeSymbol.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
                continue;
            }

            // Now verify that the entity (namedType) is present as DbSet<TEntity> in the specified DbContext
            // Resolve DbSet<T> definition symbol
            var dbSetDef = context.Compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");

            bool found = false;
            if (dbts != null)
            {
                foreach (var member in dbts.GetMembers().OfType<IPropertySymbol>())
                {
                    if (member.Type is INamedTypeSymbol named && named.IsGenericType)
                    {
                        // Check against DbSet<> definition if available
                        if (dbSetDef != null)
                        {
                            if (!SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, dbSetDef)) continue;
                        }
                        else
                        {
                            // fallback: name check
                            if (named.Name != "DbSet") continue;
                        }

                        var arg = named.TypeArguments.FirstOrDefault();
                        if (arg != null && SymbolEqualityComparer.Default.Equals(arg, namedType))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                // Report diagnostic: entity not registered in DbContext
                Location? loc = null;
                if (attr.ApplicationSyntaxReference != null)
                {
                    var syntax = attr.ApplicationSyntaxReference.GetSyntax(context.CancellationToken);
                    loc = syntax.GetLocation();
                }
                var fallbackLocation = namedType.Locations.FirstOrDefault();
                var diagnostic = Diagnostic.Create(MissingDbSetDescriptor, loc ?? fallbackLocation, namedType.Name, dbts?.ToDisplayString() ?? dbContextTypeSymbol.ToDisplayString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}