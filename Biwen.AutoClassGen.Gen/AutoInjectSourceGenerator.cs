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

    /// <summary>
    /// 泛型AutoInjectAttribute
    /// </summary>
    private const string GenericAutoInjectAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectAttribute`1";

    /// <summary>
    /// 非泛型AutoInjectAttribute
    /// </summary>
    private const string AutoInjectAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectAttribute";


    private const string AutoInjectKeyedMetadataNameInject = "AutoInjectKeyed";

    /// <summary>
    /// .NET8.0以上支持Keyed
    /// </summary>
    private const string AutoInjectKeyedAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectKeyedAttribute`1";


    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        #region 非泛型

        var nodesAutoInject = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoInjectAttributeName,
            (context, _) =>
            {
                //必须是类,且不是抽象类:
                return context is ClassDeclarationSyntax cds &&
                       !cds.Modifiers.Any(SyntaxKind.AbstractKeyword);
            },
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInject =
            context.CompilationProvider.Combine(nodesAutoInject);

        #endregion

        #region 泛型

        var nodesAutoInjectG = context.SyntaxProvider.ForAttributeWithMetadataName(
GenericAutoInjectAttributeName,
            (context, _) =>
            {
                //必须是类,且不是抽象类:
                return context is ClassDeclarationSyntax cds &&
                       !cds.Modifiers.Any(SyntaxKind.AbstractKeyword);
            },
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInjectG =
            context.CompilationProvider.Combine(nodesAutoInjectG);

        #endregion

        #region Keyed

        var nodesAutoInjectKeyed = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoInjectKeyedAttributeName,
            (context, _) => true,
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInjectKeyed =
            context.CompilationProvider.Combine(nodesAutoInjectKeyed);

        #endregion

        var join = compilationAndTypesInject.Combine(compilationAndTypesInjectG).Combine(compilationAndTypesInjectKeyed);

        context.RegisterSourceOutput(join,
            (ctx, nodes) =>
            {
                //程序集命名空间
                var @namespace = nodes.Left.Left.Item1.AssemblyName ?? nodes.Left.Right.Item1.AssemblyName ?? nodes.Right.Item1.AssemblyName;

                var nodes1 = GetAnnotatedNodes(nodes.Left.Left.Item1, nodes.Left.Left.Item2, InjectAttributeType.Regular);
                var nodes2 = GetAnnotatedNodes(nodes.Left.Right.Item1, nodes.Left.Right.Item2, InjectAttributeType.Generic);
                var nodes3 = GetAnnotatedNodes(nodes.Right.Item1, nodes.Right.Item2, InjectAttributeType.Keyed);
                GenSource(ctx, [.. nodes1, .. nodes2, .. nodes3], @namespace);
            });
    }

    /// <summary>
    /// 提取注入元数据
    /// </summary>
    /// <param name="compilation">编译上下文</param>
    /// <param name="nodes">语法节点集合</param>
    /// <param name="injectType">注入类型</param>
    /// <returns>注入元数据列表</returns>
    private static List<AutoInjectMetadata> GetAnnotatedNodes(
        Compilation compilation,
        ImmutableArray<SyntaxNode> nodes,
        InjectAttributeType injectType)
    {
        if (nodes.Length == 0) return [];

        // 注册的服务
        List<AutoInjectMetadata> autoInjects = [];
        List<string> namespaces = [];

        // 确定属性名称
        string attributeMetadataName = injectType switch
        {
            InjectAttributeType.Regular => AttributeValueMetadataNameInject,
            InjectAttributeType.Generic => AttributeValueMetadataNameInject,
            InjectAttributeType.Keyed => AutoInjectKeyedMetadataNameInject,
            _ => AttributeValueMetadataNameInject,
        };

        // 确定生命周期前缀
        string lifetimePrefix = injectType == InjectAttributeType.Keyed ? "AddKeyed" : "Add";

        foreach (ClassDeclarationSyntax node in nodes.AsEnumerable().Cast<ClassDeclarationSyntax>())
        {
            AttributeSyntax? attributeSyntax = null;
            string? attrName = null;

            // 查找属性
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

                // 查找特定的属性
                if (injectType == InjectAttributeType.Regular)
                {
                    attributeSyntax = attr.Attributes.FirstOrDefault(x =>
                        x.Name.ToString().IndexOf(attributeMetadataName, StringComparison.Ordinal) == 0);

                    if (attributeSyntax is null) continue;
                }
                else
                {
#pragma warning disable CA1031 // 不捕获常规异常类型
                    try
                    {
                        attributeSyntax = attr.Attributes.First(x =>
                            x.Name.ToString().IndexOf(attributeMetadataName, StringComparison.Ordinal) == 0);
                    }
                    catch
                    {
                        continue;
                    }
#pragma warning restore CA1031 // 不捕获常规异常类型
                }

                if (attrName?.IndexOf(attributeMetadataName, StringComparison.Ordinal) == 0)
                {
                    // 提取基类名称
                    string baseTypeName = ExtractBaseTypeName(attributeSyntax, injectType);
                    if (string.IsNullOrEmpty(baseTypeName) && injectType != InjectAttributeType.Regular)
                    {
                        continue;
                    }

                    // 获取实现类名称
                    var implTypeName = node.Identifier.ValueText;
                    var symbols = compilation.GetSymbolsWithName(implTypeName);
                    foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                    {
                        implTypeName = symbol.ToDisplayString();
                        break;
                    }

                    // 如果是普通注入且没有指定基类，实现类就是基类
                    if (string.IsNullOrEmpty(baseTypeName) && injectType == InjectAttributeType.Regular)
                    {
                        baseTypeName = implTypeName;
                    }

                    // 获取完整的基类名称
                    var baseSymbols = compilation.GetSymbolsWithName(baseTypeName);
                    foreach (ITypeSymbol baseSymbol in baseSymbols.Cast<ITypeSymbol>())
                    {
                        baseTypeName = baseSymbol.ToDisplayString();
                        break;
                    }

                    // 处理Keyed注入的键
                    string? key = null;
                    if (injectType == InjectAttributeType.Keyed)
                    {
                        key = ExtractKeyFromAttribute(attributeSyntax);
                    }

                    // 确定生命周期
                    string lifeTime = DetermineLifetime(attributeSyntax, injectType, lifetimePrefix);

                    // 创建元数据
                    var metadata = new AutoInjectMetadata(
                        implTypeName,
                        baseTypeName,
                        lifeTime);

                    if (key != null)
                    {
                        metadata.Key = key;
                    }

                    autoInjects.Add(metadata);

                    // 添加命名空间
                    AddNamespaces(compilation, baseTypeName, namespaces);

                    break; // 找到了属性，跳出循环
                }
            }
        }

        return autoInjects;
    }

    /// <summary>
    /// 从属性中提取基类类型名称
    /// </summary>
    private static string ExtractBaseTypeName(AttributeSyntax attributeSyntax, InjectAttributeType injectType)
    {
        if (injectType == InjectAttributeType.Regular)
        {
            if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Count == 0)
            {
                return string.Empty; // 返回空，由调用方处理
            }

            if (attributeSyntax.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfExpression)
            {
                var eType = typeOfExpression.Type;
                if (eType.IsKind(SyntaxKind.IdentifierName))
                {
                    return (eType as IdentifierNameSyntax)!.Identifier.ValueText;
                }
                else if (eType.IsKind(SyntaxKind.QualifiedName))
                {
                    return (eType as QualifiedNameSyntax)!.ToString().Split(['.']).Last();
                }
                else if (eType.IsKind(SyntaxKind.AliasQualifiedName))
                {
                    return (eType as AliasQualifiedNameSyntax)!.ToString().Split(['.']).Last();
                }
            }

            return string.Empty;
        }
        else // Generic或Keyed
        {
            string pattern = @"(?<=<)(?<type>\w+)(?=>)";
            var match = Regex.Match(attributeSyntax.ToString(), pattern);
            if (match.Success)
            {
                return match.Groups["type"].Value.Split(['.']).Last();
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// 从Keyed属性中提取键
    /// </summary>
    private static string? ExtractKeyFromAttribute(AttributeSyntax attributeSyntax)
    {
        if (attributeSyntax.ArgumentList?.Arguments.Count > 0)
        {
            string? key = attributeSyntax.ArgumentList.Arguments[0].Expression.ToString();

            // 处理字符串形式的键
            string keyPattern1 = "\"(.*?)\"";
            if (Regex.IsMatch(key, keyPattern1))
            {
                return Regex.Match(key, keyPattern1).Groups[1].Value;
            }

            // 处理nameof形式的键
            string keyPattern2 = @"\((.*?)\)";
            if (Regex.IsMatch(key, keyPattern2))
            {
                key = Regex.Match(key, keyPattern2).Groups[1].Value;
                return key.Split(['.']).Last();
            }

            return key;
        }

        return null;
    }

    /// <summary>
    /// 确定服务生命周期
    /// </summary>
    private static string DetermineLifetime(AttributeSyntax attributeSyntax, InjectAttributeType injectType, string prefix)
    {
        string defaultLifetime = $"{prefix}Scoped";

        if (attributeSyntax.ArgumentList == null)
        {
            return defaultLifetime;
        }

        // Keyed注入从第二个参数开始查找生命周期
        int startIndex = injectType == InjectAttributeType.Keyed ? 1 : 0;

        for (var i = startIndex; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
        {
            var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
            if (expressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                var name = (expressionSyntax as MemberAccessExpressionSyntax)!.Name.Identifier.ValueText;
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

    /// <summary>
    /// 添加命名空间
    /// </summary>
    private static void AddNamespaces(Compilation compilation, string typeName, List<string> namespaces)
    {
        var symbols = compilation.GetSymbolsWithName(typeName);
        foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
        {
            var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
            if (!namespaces.Contains(fullNameSpace))
            {
                namespaces.Add(fullNameSpace);
            }
        }
    }

    /// <summary>
    /// 注入属性类型
    /// </summary>
    private enum InjectAttributeType
    {
        /// <summary>
        /// 普通注入
        /// </summary>
        Regular,

        /// <summary>
        /// 泛型注入
        /// </summary>
        Generic,

        /// <summary>
        /// 带键的注入
        /// </summary>
        Keyed,
    }



    //private static readonly object _lock = new();
    /// <summary>
    /// 所有的注入定义
    /// </summary>
    //private static List<AutoInjectDefine> _injectDefines = [];
    //private static List<string> _namespaces = [];

    private static void GenSource(SourceProductionContext context, IEnumerable<AutoInjectMetadata> metas, string? rootNamespace)
    {
        //如果没有任何注入定义,则不生成代码
        if (!metas.Any()) return;

        // 生成代码
        StringBuilder classes = new();
        metas.Distinct().ToList().ForEach(meta =>
        {
            //NET8.0以上支持Keyed
            if (meta.Key != null)
            {
                if (meta.ImplType != meta.BaseType)
                {
                    classes.AppendLine($@"services.{meta.LifeTime}<{meta.BaseType}, {meta.ImplType}>(""{meta.Key}"");");
                }
                else
                {
                    classes.AppendLine($@"services.{meta.LifeTime}<{meta.ImplType}>(""{meta.Key}"");");
                }
            }
            //非Keyed
            else
            {
                if (meta.ImplType != meta.BaseType)
                {
                    classes.AppendLine($@"services.{meta.LifeTime}<{meta.BaseType}, {meta.ImplType}>();");
                }
                else
                {
                    classes.AppendLine($@"services.{meta.LifeTime}<{meta.ImplType}>();");
                }
            }
        });

        string rawNamespace = string.Empty;
        if (rootNamespace != null)
        {
            rawNamespace += $"namespace {rootNamespace};\r\n";
        }

        //_namespaces.Distinct().ToList().ForEach(ns => rawNamespace += $"using {ns};\r\n");
        var envSource = Template.Replace("$services", classes.ToString());
        envSource = envSource.Replace("$namespaces", rawNamespace);
        envSource = envSource.Replace("$codegen", $"[global::System.CodeDom.Compiler.GeneratedCode(\"{ThisAssembly.Product}\", \"{ThisAssembly.FileVersion}\")]");
        // format:
        envSource = envSource.FormatContent();
        context.AddSource($"Biwen.AutoClassGenInject.g.cs", SourceText.From(envSource, Encoding.UTF8));

    }

    private record AutoInjectMetadata(string ImplType, string BaseType, string LifeTime)
    {
        /// <summary>
        /// 针对NET8.0以上的Keyed Service,默认为NULL
        /// </summary>
        public string? Key { get; set; }

    }

    #region tmpl

    private const string Template = """
        // <auto-generated />
        // author:vipwan@outlook.com 万雅虎
        // issue:https://github.com/vipwan/Biwen.AutoClassGen/issues
        // 如果你在使用中遇到问题,请第一时间issue,谢谢!
        // This file is generated by Biwen.AutoClassGen.AutoInjectSourceGenerator

        #pragma warning disable
        using Microsoft.Extensions.DependencyInjection;
        $namespaces
        $codegen
        public static partial class AutoInjectExtension
        {
            /// <summary>
            /// 自动注册标注的服务
            /// </summary>
            /// <param name="services"></param>
            /// <returns></returns>
            public static  Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutoInject(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            {
                $services
                return services;
            }
        }
        
        #pragma warning restore
        """;

    #endregion

}