using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Biwen.AutoClassGen;

[Generator]
public class AutoInjectSourceGenerator : IIncrementalGenerator
{
    private const string AttributeValueMetadataNameInject = "AutoInject";
    private const string GenericAutoInjectAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectAttribute`1";
    private const string AutoInjectAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectAttribute";
    private const string AutoInjectKeyedMetadataNameInject = "AutoInjectKeyed";
    private const string AutoInjectKeyedAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectKeyedAttribute`1";
    private const string AutoInjectKeyedAttributeNonGenericName = "Biwen.AutoClassGen.Attributes.AutoInjectKeyedAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        #region 非泛型
        var nodesAutoInject = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoInjectAttributeName,
            (syntaxContext, _) => syntaxContext is ClassDeclarationSyntax cds && !cds.Modifiers.Any(SyntaxKind.AbstractKeyword),
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();
        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInject =
            context.CompilationProvider.Combine(nodesAutoInject);
        #endregion

        #region 泛型
        var nodesAutoInjectG = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenericAutoInjectAttributeName,
            (syntaxContext, _) => syntaxContext is ClassDeclarationSyntax cds && !cds.Modifiers.Any(SyntaxKind.AbstractKeyword),
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();
        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInjectG =
            context.CompilationProvider.Combine(nodesAutoInjectG);
        #endregion

        #region Keyed (generic)
        var nodesAutoInjectKeyedGeneric = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoInjectKeyedAttributeName,
            (syntaxContext, _) => true,
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();
        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInjectKeyedGeneric =
            context.CompilationProvider.Combine(nodesAutoInjectKeyedGeneric);
        #endregion

        #region Keyed (non generic)
        var nodesAutoInjectKeyedNonGeneric = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoInjectKeyedAttributeNonGenericName,
            (syntaxContext, _) => true,
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();
        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInjectKeyedNonGeneric =
            context.CompilationProvider.Combine(nodesAutoInjectKeyedNonGeneric);
        #endregion

        // Combine keyed generic + non generic first
        var keyedJoin = compilationAndTypesInjectKeyedGeneric.Combine(compilationAndTypesInjectKeyedNonGeneric);
        // Combine all
        var join = compilationAndTypesInject.Combine(compilationAndTypesInjectG).Combine(keyedJoin);

        context.RegisterSourceOutput(join,
            (ctx, nodes) =>
            {
                var compilation = nodes.Left.Left.Item1; // base compilation
                var @namespace = compilation.AssemblyName ??
                                  nodes.Left.Right.Item1.AssemblyName ??
                                  nodes.Right.Left.Item1.AssemblyName ??
                                  nodes.Right.Right.Item1.AssemblyName;

                var nodes1 = GetAnnotatedNodes(nodes.Left.Left.Item1, nodes.Left.Left.Item2, InjectAttributeType.Regular);
                var nodes2 = GetAnnotatedNodes(nodes.Left.Right.Item1, nodes.Left.Right.Item2, InjectAttributeType.Generic);
                var nodes3 = GetAnnotatedNodes(nodes.Right.Left.Item1, nodes.Right.Left.Item2, InjectAttributeType.Keyed);
                var nodes4 = GetAnnotatedNodes(nodes.Right.Right.Item1, nodes.Right.Right.Item2, InjectAttributeType.Keyed); // non generic keyed
                GenSource(ctx, [.. nodes1, .. nodes2, .. nodes3, .. nodes4], @namespace, compilation);
            });
    }

    private static List<AutoInjectMetadata> GetAnnotatedNodes(
        Compilation compilation,
        ImmutableArray<SyntaxNode> nodes,
        InjectAttributeType injectType)
    {
        if (nodes.Length == 0) return [];
        List<AutoInjectMetadata> autoInjects = [];
        List<string> namespaces = [];
        string attributeMetadataName = injectType switch
        {
            InjectAttributeType.Regular => AttributeValueMetadataNameInject,
            InjectAttributeType.Generic => AttributeValueMetadataNameInject,
            InjectAttributeType.Keyed => AutoInjectKeyedMetadataNameInject,
            _ => AttributeValueMetadataNameInject,
        };
        string lifetimePrefix = injectType == InjectAttributeType.Keyed ? "AddKeyed" : "Add";

        foreach (ClassDeclarationSyntax node in nodes.AsEnumerable().Cast<ClassDeclarationSyntax>())
        {
            AttributeSyntax? attributeSyntax = null;
            string? attrName = null;
            foreach (var attr in node.AttributeLists.AsEnumerable())
            {
                if (injectType == InjectAttributeType.Keyed)
                {
                    attrName = attr.Attributes.FirstOrDefault(x =>
                        x.Name.ToString().Contains(attributeMetadataName))?.Name.ToString();
                    if (attrName is null) continue;
                }
                else
                {
                    attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                }

                if (injectType == InjectAttributeType.Regular)
                {
                    attributeSyntax = attr.Attributes.FirstOrDefault(x =>
                        x.Name.ToString().IndexOf(attributeMetadataName, StringComparison.Ordinal) == 0);
                    if (attributeSyntax is null) continue;
                }
                else
                {
                    try
                    {
                        attributeSyntax = attr.Attributes.First(x =>
                            x.Name.ToString().IndexOf(attributeMetadataName, StringComparison.Ordinal) == 0);
                    }
                    catch { continue; }
                }

                if (attrName?.IndexOf(attributeMetadataName, StringComparison.Ordinal) == 0)
                {
                    string baseTypeName = ExtractBaseTypeName(attributeSyntax, injectType);
                    if (string.IsNullOrEmpty(baseTypeName) && injectType != InjectAttributeType.Regular)
                    {
                        continue;
                    }
                    var implTypeName = node.Identifier.ValueText;
                    var symbols = compilation.GetSymbolsWithName(implTypeName);
                    foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>()) { implTypeName = symbol.ToDisplayString(); break; }
                    if (string.IsNullOrEmpty(baseTypeName) && injectType == InjectAttributeType.Regular) baseTypeName = implTypeName;
                    var baseSymbols = compilation.GetSymbolsWithName(baseTypeName);
                    foreach (ITypeSymbol baseSymbol in baseSymbols.Cast<ITypeSymbol>()) { baseTypeName = baseSymbol.ToDisplayString(); break; }
                    string? key = null;
                    if (injectType == InjectAttributeType.Keyed) key = ExtractKeyFromAttribute(attributeSyntax);
                    string lifeTime = DetermineLifetime(attributeSyntax, injectType, lifetimePrefix);
                    var metadata = new AutoInjectMetadata(implTypeName, baseTypeName, lifeTime);
                    if (key != null) metadata.Key = key;
                    autoInjects.Add(metadata);
                    AddNamespaces(compilation, baseTypeName, namespaces);
                    break;
                }
            }
        }
        return autoInjects;
    }

    private static string ExtractBaseTypeName(AttributeSyntax attributeSyntax, InjectAttributeType injectType)
    {
        if (injectType == InjectAttributeType.Regular)
        {
            if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Count == 0) return string.Empty;
            if (attributeSyntax.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                var eType = typeOfExpression.Type;
                if (eType.IsKind(SyntaxKind.IdentifierName)) return ((IdentifierNameSyntax)eType).Identifier.ValueText;
                if (eType.IsKind(SyntaxKind.QualifiedName)) return ((QualifiedNameSyntax)eType).ToString().Split(['.']).Last();
                if (eType.IsKind(SyntaxKind.AliasQualifiedName)) return ((AliasQualifiedNameSyntax)eType).ToString().Split(['.']).Last();
            }
            return string.Empty;
        }
        else
        {
            // 1. 先尝试解析泛型形式: [AutoInjectKeyed<IFoo>("key", ...)]
            string pattern = @"(?<=<)(?<type>\w+)(?=>)";
            var match = Regex.Match(attributeSyntax.ToString(), pattern);
            if (match.Success)
            {
                return match.Groups["type"].Value.Split(['.']).Last();
            }

            // 2. 兼容非泛型 Keyed: [AutoInjectKeyed("key", typeof(IFoo), ServiceLifetime.Scoped)]
            if (attributeSyntax.ArgumentList is { Arguments.Count: > 0 })
            {
                // Keyed 非泛型: 第一个参数是 key, 后续寻找 typeof 表达式
                foreach (var arg in attributeSyntax.ArgumentList.Arguments)
                {
                    if (arg.Expression is TypeOfExpressionSyntax typeOfExpression)
                    {
                        var t = typeOfExpression.Type;
                        if (t.IsKind(SyntaxKind.IdentifierName)) return ((IdentifierNameSyntax)t).Identifier.ValueText;
                        if (t.IsKind(SyntaxKind.QualifiedName)) return ((QualifiedNameSyntax)t).ToString().Split(['.']).Last();
                        if (t.IsKind(SyntaxKind.AliasQualifiedName)) return ((AliasQualifiedNameSyntax)t).ToString().Split(['.']).Last();
                    }
                }
            }

            return string.Empty;
        }
    }

    private static string? ExtractKeyFromAttribute(AttributeSyntax attributeSyntax)
    {
        if (attributeSyntax.ArgumentList?.Arguments.Count > 0)
        {
            // Key 一定是第一个参数
            string? key = attributeSyntax.ArgumentList.Arguments[0].Expression.ToString();
            string keyPattern1 = "\"(.*?)\"";
            if (Regex.IsMatch(key, keyPattern1)) return Regex.Match(key, keyPattern1).Groups[1].Value;
            string keyPattern2 = @"\((.*?)\)";
            if (Regex.IsMatch(key, keyPattern2)) { key = Regex.Match(key, keyPattern2).Groups[1].Value; return key.Split(['.']).Last(); }
            return key;
        }
        return null;
    }

    private static string DetermineLifetime(AttributeSyntax attributeSyntax, InjectAttributeType injectType, string prefix)
    {
        string defaultLifetime = $"{prefix}Scoped";
        if (attributeSyntax.ArgumentList == null) return defaultLifetime;
        int startIndex = injectType == InjectAttributeType.Keyed ? 1 : 0; // Keyed: index0 是 key
        for (var i = startIndex; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
        {
            var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
            if (expressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var name = ((MemberAccessExpressionSyntax)expressionSyntax).Name.Identifier.ValueText;
                return name switch
                {
                    "Singleton" => $"{prefix}Singleton",
                    "Transient" => $"{prefix}Transient",
                    "Scoped" => $"{prefix}Scoped",
                    _ => defaultLifetime,
                };
            }
        }
        return defaultLifetime;
    }

    private static void AddNamespaces(Compilation compilation, string typeName, List<string> namespaces)
    {
        var symbols = compilation.GetSymbolsWithName(typeName);
        foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
        {
            var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
            if (!namespaces.Contains(fullNameSpace)) namespaces.Add(fullNameSpace);
        }
    }

    private enum InjectAttributeType { Regular, Generic, Keyed }

    private static void GenSource(SourceProductionContext context, IEnumerable<AutoInjectMetadata> metas, string? rootNamespace, Compilation compilation)
    {
        if (!metas.Any()) return;
        var languageVersion = (compilation as CSharpCompilation)?.LanguageVersion ?? LanguageVersion.Default;
        bool useFileScoped = languageVersion >= LanguageVersion.CSharp10;

        StringBuilder registrations = new();
        foreach (var meta in metas.Distinct())
        {
            if (meta.Key != null)
            {
                if (meta.ImplType != meta.BaseType)
                    registrations.AppendLine($"services.{meta.LifeTime}<{meta.BaseType}, {meta.ImplType}>(\"{meta.Key}\");");
                else
                    registrations.AppendLine($"services.{meta.LifeTime}<{meta.ImplType}>(\"{meta.Key}\");");
            }
            else
            {
                if (meta.ImplType != meta.BaseType)
                    registrations.AppendLine($"services.{meta.LifeTime}<{meta.BaseType}, {meta.ImplType}>();");
                else
                    registrations.AppendLine($"services.{meta.LifeTime}<{meta.ImplType}>();");
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// author:vipwan@outlook.com 万雅虎");
        sb.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
        sb.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
        sb.AppendLine("// This file is generated by Biwen.AutoClassGen.AutoInjectSourceGenerator");
        sb.AppendLine();
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");

        string indent = string.Empty;
        if (!string.IsNullOrEmpty(rootNamespace))
        {
            if (useFileScoped)
            {
                sb.AppendLine($"namespace {rootNamespace};");
            }
            else
            {
                sb.AppendLine($"namespace {rootNamespace}");
                sb.AppendLine("{");
                indent = "    ";
            }
        }

        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"{ThisAssembly.Product}\", \"{ThisAssembly.FileVersion}\")]");
        sb.AppendLine($"{indent}public static partial class AutoInjectExtension");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 自动注册标注的服务");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutoInject(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine($"{indent}    {{");
        foreach (var line in registrations.ToString().Split(['\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            sb.AppendLine($"{indent}        {line}");
        }
        sb.AppendLine($"{indent}        return services;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        if (!useFileScoped && !string.IsNullOrEmpty(rootNamespace)) sb.AppendLine("}");
        sb.AppendLine("#pragma warning restore");

        var source = sb.ToString().FormatContent();
        context.AddSource("Biwen.AutoClassGenInject.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private record AutoInjectMetadata(string ImplType, string BaseType, string LifeTime)
    {
        /// <summary>
        /// 针对Microsoft.Extensions.DependencyInjection 8.0以上的Keyed Service,默认为NULL
        /// </summary>
        public string? Key { get; set; }
    }
}