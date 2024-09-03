// <copyright file="AutoDecorSourceGenerator.cs" company="vipwan">
//MIT Copyright (c) vipwan. All rights reserved.
// </copyright>

namespace Biwen.AutoClassGen
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public class AutoDecorSourceGenerator : IIncrementalGenerator
    {
        private const string AttributeMetadataNameDecor = "Biwen.AutoClassGen.Attributes.AutoDecorAttribute";

        private const string GenericAutoDecorAttributeName = "Biwen.AutoClassGen.Attributes.AutoDecorAttribute`1";

        #region DecorFor

        private const string AutoDecorForAttrbuteName = "AutoDecorFor";
        //private const string AttributeMetadataNameDecorFor = "Biwen.AutoClassGen.Attributes.AutoDecorForAttribute";
        //private const string GenericAutoDecorForAttributeName = "Biwen.AutoClassGen.Attributes.AutoDecorForAttribute`1";
        #endregion


        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            #region AutoDecorAttribute

            var nodesDecor = context.SyntaxProvider.ForAttributeWithMetadataName(
                               AttributeMetadataNameDecor,
                               (context, _) => true,
                               (syntaxContext, _) => syntaxContext.TargetSymbol).Collect();

            var nodesDecorG = context.SyntaxProvider.ForAttributeWithMetadataName(
                   GenericAutoDecorAttributeName,
                   (context, _) => true,
                   (syntaxContext, _) => syntaxContext.TargetSymbol).Collect();

            var forNode = context.SyntaxProvider.CreateSyntaxProvider((node, _) =>
            {
                if (!IsNotAbstractClass(node))
                    return false;
                //包含AutoDecorForAttribute的类
                if (node is ClassDeclarationSyntax cds)
                {
                    return cds.AttributeLists.Any(x =>
                    x.Attributes.Any(x => x.Name.ToFullString() == AutoDecorForAttrbuteName));
                }
                return false;
            }, (ctx, _) => ctx.Node).Collect();

            var forNodeG = context.SyntaxProvider.CreateSyntaxProvider((node, _) =>
            {
                if (!IsNotAbstractClass(node))
                    return false;
                //包含AutoDecorForAttribute的类
                if (node is ClassDeclarationSyntax cds)
                {
                    //当前是泛型特性
                    return cds.AttributeLists.Any(x =>
                    x.Attributes.Any(x =>
                    x.Name is GenericNameSyntax &&
                    x.Name.ToFullString().StartsWith(AutoDecorForAttrbuteName, StringComparison.Ordinal)
                    ));
                }
                return false;
            }, (ctx, _) => ctx.Node).Collect();

            var compilationAndTypesDecor = context.CompilationProvider
                .Combine(nodesDecor)
                .Combine(nodesDecorG)
                .Combine(forNode)
                .Combine(forNodeG);

            context.RegisterSourceOutput(compilationAndTypesDecor,
                static (spc, source) =>

                HandleAnnotatedNodesDecor(
                    source.Left.Left.Left.Left,
                    source.Left.Left.Left.Right,
                    source.Left.Left.Right,
                    source.Left.Right,
                    source.Right,
                    spc));

            #endregion
        }

        /// <summary>
        /// SyntaxNode是类,且不是抽象类
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool IsNotAbstractClass(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax cds && !cds.Modifiers.Any(SyntaxKind.AbstractKeyword);
        }

        private static void HandleAnnotatedNodesDecor(
            Compilation compilation,
            ImmutableArray<ISymbol> nodes,
            ImmutableArray<ISymbol> nodes2,
            ImmutableArray<SyntaxNode> nodes3,//for
            ImmutableArray<SyntaxNode> nodes4,//forG

            SourceProductionContext context)
        {
            if ((nodes.Length + nodes2.Length + nodes3.Length + nodes4.Length) == 0) return;

            IList<KeyValuePair<string, string>> ofImpls = [];
            // (普通特性)获取所有实现类
            foreach (var node in nodes)
            {
                var tName = node.OriginalDefinition.ToDisplayString();
                foreach (var item in node.GetAttributes().Where(x => x.AttributeClass?.MetadataName == "AutoDecorAttribute"))
                {
                    var attributeSyntax = item?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

                    if (attributeSyntax?.ArgumentList?.Arguments[0].Expression is TypeOfExpressionSyntax implNameSyntax)
                    {
                        var implNameStr = implNameSyntax.Type.ToString();
                        var symbol = compilation.GetSymbolsWithName(implNameStr, SymbolFilter.Type);

                        if (symbol?.Any() is true)
                        {
                            var implName = symbol.First().ToDisplayString();
                            implNameStr = implName;

                            ofImpls.Add(new KeyValuePair<string, string>(tName, implName));
                        }
                    }
                }
            }
            // (泛型特性)获取所有实现类
            foreach (var node in nodes2)
            {
                var tName = node.OriginalDefinition.ToDisplayString();
                foreach (var item in node.GetAttributes().Where(x => x.AttributeClass?.MetadataName == "AutoDecorAttribute`1"))
                {
                    var attributeSyntax = item?.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

                    if (attributeSyntax?.Name is GenericNameSyntax genericNameSyntax)
                    {
                        var implNameStr = genericNameSyntax.TypeArgumentList.Arguments[0].ToString();
                        var symbol = compilation.GetSymbolsWithName(genericNameSyntax.TypeArgumentList.Arguments[0].ToString(), SymbolFilter.Type);

                        if (symbol?.Any() is true)
                        {
                            var implName = symbol.First().ToDisplayString();
                            implNameStr = implName;

                            ofImpls.Add(new KeyValuePair<string, string>(tName, implName));
                        }
                    }
                }
            }

            //for:
            // (普通特性)获取所有实现类
            foreach (var node in nodes3)
            {
                if (node is ClassDeclarationSyntax cds)
                {
                    //查找特性DecorForAttribute:
                    var attrList = cds.AttributeLists.First(
                        x =>
                        x.Attributes.Any(
                            x =>
                            x.Name is not GenericNameSyntax &&
                            x.Name.ToFullString() == AutoDecorForAttrbuteName));

                    var attr = attrList.Attributes.First(x =>
                    x.Name is not GenericNameSyntax &&
                    x.Name.ToFullString() == AutoDecorForAttrbuteName);

                    if (attr.ArgumentList!.Arguments[0].Expression is not TypeOfExpressionSyntax attributeSyntax)
                        continue;

                    var tImpl = compilation.GetSymbolsWithName(cds.Identifier.ValueText, SymbolFilter.Type).FirstOrDefault()?.ToDisplayString();

                    if (string.IsNullOrEmpty(tImpl))
                        continue;

                    var implNameStr = attributeSyntax!.Type.ToString();
                    var symbol = compilation.GetSymbolsWithName(implNameStr, SymbolFilter.Type);
                    if (symbol?.Any() is true)
                    {
                        var forName = symbol.First().ToDisplayString();
                        //for是相反的:
                        ofImpls.Add(new KeyValuePair<string, string>(forName, tImpl!));
                    }
                }
            }
            // (泛型特性)获取所有实现类
            foreach (var node in nodes4)
            {
                if (node is ClassDeclarationSyntax cds)
                {
                    //查找特性DecorForAttribute:
                    var attrList = cds.AttributeLists.First(
                        x =>
                        x.Attributes.Any(
                            x =>
                            x.Name is GenericNameSyntax &&
                            x.Name.ToFullString().StartsWith(AutoDecorForAttrbuteName, StringComparison.Ordinal)));

                    var attr = attrList.Attributes.First(x =>
                    x.Name is GenericNameSyntax &&
                    x.Name.ToFullString().StartsWith(AutoDecorForAttrbuteName, StringComparison.Ordinal));

                    var attrSyntax = attr.Name as GenericNameSyntax;

                    var forNameStr = (attrSyntax!.TypeArgumentList.Arguments[0] as IdentifierNameSyntax)!.Identifier.ValueText;

                    var tImpl = compilation.GetSymbolsWithName(cds.Identifier.ValueText, SymbolFilter.Type).FirstOrDefault()?.ToDisplayString();
                    if (string.IsNullOrEmpty(tImpl))
                        continue;

                    var symbol = compilation.GetSymbolsWithName(forNameStr, SymbolFilter.Type);
                    if (symbol?.Any() is true)
                    {
                        var forName = symbol.First().ToDisplayString();
                        //for是相反的:
                        ofImpls.Add(new KeyValuePair<string, string>(forName, tImpl!));
                    }
                }
            }

            if (ofImpls.Count == 0)
            {
                return;
            }

            //去除重复的Key,只保留最后一项:
            ofImpls = ofImpls.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.Last().Value).ToList();

            StringBuilder envStringBuilder = new();
            envStringBuilder.AppendLine("// <auto-generated />");
            envStringBuilder.AppendLine("// author:vipwan@outlook.com 万雅虎");
            envStringBuilder.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
            envStringBuilder.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
            envStringBuilder.AppendLine("// This file is generated by Biwen.AutoClassGen.SourceGenerator");
            envStringBuilder.AppendLine();
            envStringBuilder.AppendLine("#pragma warning disable");
            //envStringBuilder.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            //envStringBuilder.AppendLine("{");
            envStringBuilder.AppendLine("public static partial class AutoDecorExtensions");
            envStringBuilder.AppendLine("{");

            //append decorate method:

            envStringBuilder.AppendLine(DecorateString);
            envStringBuilder.AppendLine();

            envStringBuilder.AppendLine("/// <summary>");
            envStringBuilder.AppendLine("/// AddAutoDecor");
            envStringBuilder.AppendLine("/// </summary>");
            envStringBuilder.AppendLine("public static IServiceCollection AddAutoDecor(this IServiceCollection services)");
            envStringBuilder.AppendLine("{");
            // decor
            foreach (var item in ofImpls.Distinct())
            {
                envStringBuilder.AppendLine($"services.Decorate<{item.Key}, {item.Value}>();");
            }
            envStringBuilder.AppendLine("return services;");
            envStringBuilder.AppendLine("}");
            envStringBuilder.AppendLine("}");
            //envStringBuilder.AppendLine("}");
            envStringBuilder.AppendLine("#pragma warning restore");

            var envSource = envStringBuilder.ToString();
            // format:
            envSource = envSource.FormatContent();
            context.AddSource($"Biwen.AutoClassGenDecor.g.cs", SourceText.From(envSource, Encoding.UTF8));
        }

        #region Template

        /// <summary>
        /// decorate method
        /// </summary>
        private const string DecorateString = """

            /// <summary>
            /// 装饰服务,<typeparamref name="TImpl"/>必须有一个构造函数接受<typeparamref name="TService"/>类型的参数
            /// </summary>
            /// <param name="services"></param>
            /// <param name="parameters"></param>
            /// <returns></returns>
            public static bool Decorate<TService, TImpl>(this IServiceCollection services, params object[] parameters)
            {
                var existingService = services.SingleOrDefault(s => s.ServiceType == typeof(TService));
                if (existingService is null)
                    throw new ArgumentException($"No service of type {typeof(TService).Name} found.");

                ServiceDescriptor? newService;

                if (existingService.ImplementationType is not null)
                {
                    newService = new ServiceDescriptor(existingService.ServiceType,
                        sp =>
                        {
                            TService inner =
                                (TService)ActivatorUtilities.CreateInstance(sp, existingService.ImplementationType);

                            if (inner is null)
                                throw new Exception(
                                    $"Unable to instantiate decorated type via implementation type {existingService.ImplementationType.Name}.");

                            var parameters2 = new object[parameters.Length + 1];
                            Array.Copy(parameters, 0, parameters2, 1, parameters.Length);
                            parameters2[0] = inner;

                            return ActivatorUtilities.CreateInstance<TImpl>(sp, parameters2)!;
                        },
                        existingService.Lifetime);
                }
                else if (existingService.ImplementationInstance is not null)
                {
                    newService = new ServiceDescriptor(existingService.ServiceType,
                        sp =>
                        {
                            TService inner = (TService)existingService.ImplementationInstance;
                            return ActivatorUtilities.CreateInstance<TImpl>(sp, inner, parameters)!;
                        },
                        existingService.Lifetime);
                }
                else if (existingService.ImplementationFactory is not null)
                {
                    newService = new ServiceDescriptor(existingService.ServiceType,
                        sp =>
                        {
                            TService inner = (TService)existingService.ImplementationFactory(sp);
                            if (inner is null)
                                throw new Exception(
                                    "Unable to instantiate decorated type via implementation factory.");

                            return ActivatorUtilities.CreateInstance<TImpl>(sp, inner, parameters)!;
                        },
                        existingService.Lifetime);
                }
                else
                {
                    throw new Exception(
                        "Unable to instantiate decorated type.");
                }

                services.Remove(existingService);
                services.Add(newService);

                return true;
            }


            """;

        #endregion

    }
}