// <copyright file="AutoDtoSourceGenerator.cs" company="vipwan">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Text;

namespace Biwen.AutoClassGen;

[Generator]
internal class AutoGurdSourceGenerator : IIncrementalGenerator
{
    private const string GenericAutoCurdAttributeName = "Biwen.AutoClassGen.Attributes.AutoCurdAttribute`1";
    private const string NonGenericAutoCurdAttributeName = "Biwen.AutoClassGen.Attributes.AutoCurdAttribute";
    private const string AttributeValueName = "AutoCurd";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 支持泛型形式和非泛型 typeof(...) 形式
        var nodesGeneric = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenericAutoCurdAttributeName,
            predicate: (node, ct) => node is TypeDeclarationSyntax,
            transform: (ctx, ct) => ctx).Collect();

        var nodesNonGeneric = context.SyntaxProvider.ForAttributeWithMetadataName(
            NonGenericAutoCurdAttributeName,
            predicate: (node, ct) => node is TypeDeclarationSyntax,
            transform: (ctx, ct) => ctx).Collect();

        var join = nodesGeneric.Combine(nodesNonGeneric);
        var comp = context.CompilationProvider.Combine(join);

        context.RegisterSourceOutput(comp, (spc, items) =>
        {
            var compilation = items.Left;
            var nodesPair = items.Right; // Left: nodesGeneric, Right: nodesNonGeneric
            var nodesG = nodesPair.Left;
            var nodesN = nodesPair.Right;

            if (nodesG.IsDefaultOrEmpty && nodesN.IsDefaultOrEmpty) return;

            // Track generated entities to avoid duplicates in case of mixed attributes
            var generated = new HashSet<string>();

            void ProcessAttrContext(GeneratorAttributeSyntaxContext attrContext, bool isGenericProvider)
            {
                if (attrContext.TargetNode is not TypeDeclarationSyntax node) return;

                // Find the specific AutoCurd attribute syntax instance on this node that matches provider type
                AttributeSyntax? matchingAttr = null;
                foreach (var al in node.AttributeLists)
                {
                    foreach (var a in al.Attributes)
                    {
                        var name = a.Name.ToString();
                        if (name.IndexOf(AttributeValueName, System.StringComparison.Ordinal) < 0) continue;

                        // Decide if this attribute instance corresponds to generic or non-generic provider
                        if (isGenericProvider && a.Name is GenericNameSyntax) { matchingAttr = a; break; }
                        if (!isGenericProvider && !(a.Name is GenericNameSyntax)) { matchingAttr = a; break; }
                    }
                    if (matchingAttr != null) break;
                }
                if (matchingAttr == null) return;

                var semanticModel = attrContext.SemanticModel;

                // Resolve DbContext symbol from either generic type argument or typeof(...) expression
                ITypeSymbol? dbContextSymbol = null;
                if (matchingAttr.Name is GenericNameSyntax gname && gname.TypeArgumentList.Arguments.Count > 0)
                {
                    var typeArg = gname.TypeArgumentList.Arguments[0];
                    var typeInfo = semanticModel.GetTypeInfo(typeArg);
                    dbContextSymbol = typeInfo.Type;
                }
                else if (matchingAttr.ArgumentList != null && matchingAttr.ArgumentList.Arguments.Count > 0)
                {
                    // first argument might be typeof(MyDbContext)
                    var firstExpr = matchingAttr.ArgumentList.Arguments[0].Expression;
                    if (firstExpr is TypeOfExpressionSyntax tof)
                    {
                        var typeInfo = semanticModel.GetTypeInfo(tof.Type);
                        dbContextSymbol = typeInfo.Type;
                    }
                }

                // parse namespace parameter
                string targetNamespace = string.Empty;
                if (matchingAttr.ArgumentList != null && matchingAttr.ArgumentList.Arguments.Count > 0)
                {
                    // if first arg is typeof, namespace may be second
                    int idx = 0;
                    if (matchingAttr.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax) idx = 1;
                    if (matchingAttr.ArgumentList.Arguments.Count > idx)
                    {
                        var expr = matchingAttr.ArgumentList.Arguments[idx].Expression;
                        var constVal = semanticModel.GetConstantValue(expr);
                        if (constVal.HasValue && constVal.Value is string s)
                        {
                            targetNamespace = s;
                        }
                        else
                        {
                            targetNamespace = expr.ToString().Trim('"');
                        }
                    }
                }

                if (string.IsNullOrEmpty(targetNamespace)) targetNamespace = compilation.AssemblyName ?? "GeneratedServices";

                if (node is not ClassDeclarationSyntax classNode) return;
                if (semanticModel.GetDeclaredSymbol(classNode) is not INamedTypeSymbol entitySymbol) return;

                var fq = entitySymbol.ToDisplayString();
                if (generated.Contains(fq)) return;
                generated.Add(fq);

                GenerateForEntity(spc, compilation, targetNamespace, dbContextSymbol, entitySymbol);
            }

            foreach (var ctx in nodesG)
            {
                ProcessAttrContext(ctx, true);
            }
            foreach (var ctx in nodesN)
            {
                ProcessAttrContext(ctx, false);
            }
        });
    }

    private static void GenerateForEntity(SourceProductionContext context, Compilation compilation, string ns, ITypeSymbol? dbContextSymbol, INamedTypeSymbol entity)
    {
        var entityName = entity.Name;
        var serviceInterfaceName = $"I{entityName}CurdService";
        var serviceClassName = $"{entityName}CurdService";
        var dbContextTypeName = dbContextSymbol != null ? dbContextSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) : "DbContext";

        var entityNamespace = entity.ContainingNamespace.IsGlobalNamespace ? string.Empty : entity.ContainingNamespace.ToDisplayString();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// author:vipwan@outlook.com 万雅虎");
        sb.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
        sb.AppendLine("// This file is generated by Biwen.AutoClassGen.AutoGurdSourceGenerator");
        sb.AppendLine();
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        if (!string.IsNullOrEmpty(entityNamespace)) sb.AppendLine($"using {entityNamespace};");
        sb.AppendLine();

        var useFileScoped = (compilation as CSharpCompilation)?.LanguageVersion >= LanguageVersion.CSharp10;
        if (useFileScoped)
        {
            sb.AppendLine($"namespace {ns};");
        }
        else
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        var indent = useFileScoped ? string.Empty : "    ";

        // Interface summary
        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"{ThisAssembly.Product}\", \"{ThisAssembly.FileVersion}\")] ");
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// 自动生成的 CURD 服务接口，针对实体 {entityName}");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public partial interface {serviceInterfaceName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 创建一个新的 {entityName} 实体并保存到数据库");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"entity\">要创建的实体</param>");
        sb.AppendLine($"{indent}    /// <returns>创建后的实体</returns>");
        sb.AppendLine($"{indent}    Task<{entityName}> CreateAsync({entityName} entity);");
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 更新指定的 {entityName} 实体并保存到数据库");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"entity\">要更新的实体</param>");
        sb.AppendLine($"{indent}    Task UpdateAsync({entityName} entity);");
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 删除指定的 {entityName} 实体并保存到数据库");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"entity\">要删除的实体</param>");
        sb.AppendLine($"{indent}    Task DeleteAsync({entityName} entity);");
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 根据主键查找 {entityName} 实体");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"ids\">主键值集合</param>");
        sb.AppendLine($"{indent}    /// <returns>找到的实体或 null</returns>");
        sb.AppendLine($"{indent}    Task<{entityName}?> GetAsync(params object[] ids);");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // Class summary
        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"{ThisAssembly.Product}\", \"{ThisAssembly.FileVersion}\")] ");
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// 自动生成的 CURD 服务实现，针对实体 {entityName}");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public partial class {serviceClassName} : {serviceInterfaceName}");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    private readonly ILogger<{serviceClassName}> _logger;");
        sb.AppendLine($"{indent}    private readonly {dbContextTypeName} _dbContext;");
        sb.AppendLine();
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 构造函数");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public {serviceClassName}(ILogger<{serviceClassName}> logger, {dbContextTypeName} context)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        _logger = logger;");
        sb.AppendLine($"{indent}        _dbContext = context;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // Create
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 创建一个新的 {entityName} 实体并保存到数据库");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"entity\">要创建的实体</param>");
        sb.AppendLine($"{indent}    /// <returns>创建后的实体</returns>");
        sb.AppendLine($"{indent}    public virtual async Task<{entityName}> CreateAsync({entityName} entity)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        _dbContext.Set<{entityName}>().Add(entity);");
        sb.AppendLine($"{indent}        await _dbContext.SaveChangesAsync();");
        sb.AppendLine($"{indent}        return entity;");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // Delete
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 删除指定的 {entityName} 实体并保存到数据库");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"entity\">要删除的实体</param>");
        sb.AppendLine($"{indent}    public virtual async Task DeleteAsync({entityName} entity)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        _dbContext.Set<{entityName}>().Remove(entity);");
        sb.AppendLine($"{indent}        await _dbContext.SaveChangesAsync();");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // Get
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 根据主键查找 {entityName} 实体");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"ids\">主键值集合</param>");
        sb.AppendLine($"{indent}    /// <returns>找到的实体或 null</returns>");
        sb.AppendLine($"{indent}    public virtual async Task<{entityName}?> GetAsync(params object[] ids)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return await _dbContext.Set<{entityName}>().FindAsync(ids);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine();

        // Update
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// 更新指定的 {entityName} 实体并保存到数据库");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    /// <param name=\"entity\">要更新的实体</param>");
        sb.AppendLine($"{indent}    public virtual async Task UpdateAsync({entityName} entity)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        _dbContext.Set<{entityName}>().Update(entity);");
        sb.AppendLine($"{indent}        await _dbContext.SaveChangesAsync();");
        sb.AppendLine($"{indent}    }}");

        sb.AppendLine($"{indent}}}");

        if (!useFileScoped)
        {
            sb.AppendLine("}");
        }

        sb.AppendLine("#pragma warning restore");

        var source = sb.ToString().FormatContent();
        var hintName = $"Biwen.AutoClassGen.Curd.{entityName}.g.cs";
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }
}

// helper extension to enumerate all named types in global namespace (simple)
#pragma warning disable SA1402 // File may only contain a single type
internal static class RoslynExtensions
#pragma warning restore SA1402 // File may only contain a single type
{
    public static List<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol ns)
    {
        var list = new List<INamedTypeSymbol>();
        Traverse(ns, list);
        return list;

        static void Traverse(INamespaceSymbol n, List<INamedTypeSymbol> acc)
        {
            foreach (var m in n.GetTypeMembers())
            {
                acc.Add(m);
            }
            foreach (var child in n.GetNamespaceMembers())
            {
                Traverse(child, acc);
            }
        }
    }
}