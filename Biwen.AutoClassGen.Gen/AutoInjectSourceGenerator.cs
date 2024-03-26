using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace Biwen.AutoClassGen
{
    [Generator(LanguageNames.CSharp)]
    public class AutoInjectSourceGenerator : IIncrementalGenerator
    {
        private const string AttributeValueMetadataNameInject = "AutoInject";

        /// <summary>
        /// 泛型AutoInjectAttribute
        /// </summary>
        private const string GenericAutoInjectAttributeName = "Biwen.AutoClassGen.Attributes.AutoInjectAttribute`1";


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var nodesAutoInjectG = context.SyntaxProvider.ForAttributeWithMetadataName(
    GenericAutoInjectAttributeName,
    (context, attributeSyntax) => true,
    (syntaxContext, _) => syntaxContext.TargetNode).Collect();

            IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypesInjectG =
                context.CompilationProvider.Combine(nodesAutoInjectG);

            context.RegisterSourceOutput(compilationAndTypesInjectG, static (spc, source) => HandleGenericAnnotatedNodesInject(source.Item1, source.Item2, spc));
        }

        /// <summary>
        /// Gen AutoInjectAttribute G
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="nodes"></param>
        /// <param name="context"></param>
        private static void HandleGenericAnnotatedNodesInject(Compilation compilation, ImmutableArray<SyntaxNode> nodes, SourceProductionContext context)
        {
            if (nodes.Length == 0) return;
            // 注册的服务
            List<AutoInjectDefine> autoInjects = [];
            List<string> namespaces = [];

            foreach (ClassDeclarationSyntax node in nodes.AsEnumerable().Cast<ClassDeclarationSyntax>())
            {
                AttributeSyntax? attributeSyntax = null;
                foreach (var attr in node.AttributeLists.AsEnumerable())
                {
                    var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                    attributeSyntax = attr.Attributes.First(x => x.Name.ToString().IndexOf(AttributeValueMetadataNameInject, System.StringComparison.Ordinal) == 0);

                    if (attrName?.IndexOf(AttributeValueMetadataNameInject, System.StringComparison.Ordinal) == 0)
                    {
                        //转译的Entity类名
                        var baseTypeName = string.Empty;

                        string pattern = @"(?<=<)(?<type>\w+)(?=>)";
                        var match = Regex.Match(attributeSyntax.ToString(), pattern);
                        if (match.Success)
                        {
                            baseTypeName = match.Groups["type"].Value.Split(['.']).Last();
                        }
                        else
                        {
                            continue;
                        }

                        var implTypeName = node.Identifier.ValueText;
                        var rootNamespace = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().Single().Name.ToString();

                        var symbols = compilation.GetSymbolsWithName(implTypeName);
                        foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                        {
                            var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                            // 命名空间
                            if (!namespaces.Contains(fullNameSpace))
                            {
                                namespaces.Add(fullNameSpace);
                            }
                        }

                        string lifeTime = "AddScoped"; //default
                        {
                            if (attributeSyntax.ArgumentList != null)
                            {
                                for (var i = 0; i < attributeSyntax.ArgumentList!.Arguments.Count; i++)
                                {
                                    var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
                                    if (expressionSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                                    {
                                        var name = (expressionSyntax as MemberAccessExpressionSyntax)!.Name.Identifier.ValueText;
                                        lifeTime = name switch
                                        {
                                            "Singleton" => "AddSingleton",
                                            "Transient" => "AddTransient",
                                            "Scoped" => "AddScoped",
                                            _ => "AddScoped",
                                        };
                                        break;
                                    }
                                }
                            }

                            autoInjects.Add(new AutoInjectDefine
                            {
                                ImplType = implTypeName,
                                BaseType = baseTypeName,
                                LifeTime = lifeTime,
                            });

                            //命名空间
                            symbols = compilation.GetSymbolsWithName(baseTypeName);
                            foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                            {
                                var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                                // 命名空间
                                if (!namespaces.Contains(fullNameSpace))
                                {
                                    namespaces.Add(fullNameSpace);
                                }
                            }
                        }
                    }
                }
            }

            // 生成代码
            StringBuilder classes = new();
            foreach (var define in autoInjects)
            {
                if (define.ImplType != define.BaseType)
                {
                    classes.AppendLine($@"services.{define.LifeTime}<{define.BaseType}, {define.ImplType}>();");
                }
                else
                {
                    classes.AppendLine($@"services.{define.LifeTime}<{define.ImplType}>();");
                }
            }

            string rawNamespace = string.Empty;
            namespaces.ForEach(ns => rawNamespace += $"using {ns};\r\n");

            var envSource = Template.Replace("$services", classes.ToString());
            envSource = envSource.Replace("$namespaces", rawNamespace);
            // format:
            envSource = FormatContent(envSource);
            context.AddSource($"Biwen.AutoClassGenInjectG.g.cs", SourceText.From(envSource, Encoding.UTF8));
        }

        private class AutoInjectDefine
        {
            public string ImplType { get; set; } = null!;
            public string BaseType { get; set; } = null!;

            public string LifeTime { get; set; } = null!;
        }


        #region tmpl

        private const string Template = """
            // <auto-generated />
            // author:vipwan@outlook.com 万雅虎
            // issue:https://github.com/vipwan/Biwen.AutoClassGen/issues
            // 如果你在使用中遇到问题,请第一时间issue,谢谢!
            // This file is generated by Biwen.AutoClassGen.AutoInjectSourceGenerator

            #pragma warning disable
            namespace Microsoft.Extensions.DependencyInjection
            {
                $namespaces

                public static class ServiceCollectionExtension
                {
                    /// <summary>
                    /// 自动注册标注的服务
                    /// </summary>
                    /// <param name="services"></param>
                    /// <returns></returns>
                    public static IServiceCollection AddAutoInject(this IServiceCollection services)
                    {
                        $services
                        return services;
                    }
                }
            }
            #pragma warning restore
            """;

        #endregion


        /// <summary>
        /// 格式化代码
        /// </summary>
        /// <param name="csCode"></param>
        /// <returns></returns>
        private static string FormatContent(string csCode)
        {
            var tree = CSharpSyntaxTree.ParseText(csCode);
            var root = tree.GetRoot().NormalizeWhitespace();
            var ret = root.ToFullString();
            return ret;
        }

    }
}