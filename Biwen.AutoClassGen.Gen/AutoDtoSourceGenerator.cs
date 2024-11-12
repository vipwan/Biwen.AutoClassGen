using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Biwen.AutoClassGen;

[Generator]
public class AutoDtoSourceGenerator : IIncrementalGenerator
{

    private const string AttributeMetadataNameDto = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute";
    private const string AttributeValueMetadataNameDto = "AutoDto";

    /// <summary>
    /// 泛型AutoDtoAttribute
    /// </summary>
    private const string GenericAutoDtoAttributeName = "Biwen.AutoClassGen.Attributes.AutoDtoAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        #region AutoDtoAttribute

        var nodesDto = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeMetadataNameDto,
            (context, _) => true,
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        var nodesDtoG = context.SyntaxProvider.ForAttributeWithMetadataName(
            GenericAutoDtoAttributeName,
            (context, attributeSyntax) => true,
            (syntaxContext, _) => syntaxContext.TargetNode).Collect();

        #endregion

        var join = nodesDto.Combine(nodesDtoG);
        var comp = context.CompilationProvider.Combine(join);

        context.RegisterSourceOutput(comp,
            (ctx, nodes) =>
            {
                var meta1 = GetMeta(nodes.Right.Left);
                var meta2 = GetMetaFromGeneric(nodes.Right.Right);
                GenSource(nodes.Left, ctx, [.. meta1, .. meta2]);
            });
    }

    private static List<AutoDtoMetadata> GetMeta(ImmutableArray<SyntaxNode> nodes)
    {
        List<AutoDtoMetadata> retn = [];
        if (nodes.Length == 0) return retn;
        foreach (var syntaxNode in nodes.AsEnumerable())
        {
            //Cast<ClassDeclarationSyntax>()
            //Cast<RecordDeclarationSyntax>()

            if (syntaxNode is not TypeDeclarationSyntax node)
            {
                continue;
            }

            //如果是Record类
            var isRecord = syntaxNode is RecordDeclarationSyntax;

            //如果不含partial关键字,则不生成
            if (!node.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
            {
                continue;
            }

            AttributeSyntax? attributeSyntax = null;
            foreach (var attr in node.AttributeLists.AsEnumerable())
            {
                var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                if (attrName == AttributeValueMetadataNameDto)
                {
                    attributeSyntax = attr.Attributes.First(x => x.Name.ToString() == AttributeValueMetadataNameDto);
                    break;
                }
            }
            if (attributeSyntax == null)
            {
                continue;
            }

            //转译的Entity类名
            var entityName = string.Empty;
            var eType = (attributeSyntax.ArgumentList!.Arguments[0].Expression as TypeOfExpressionSyntax)!.Type;
            if (eType.IsKind(SyntaxKind.IdentifierName))
            {
                entityName = (eType as IdentifierNameSyntax)!.Identifier.ValueText;
            }
            else if (eType.IsKind(SyntaxKind.QualifiedName))
            {
                entityName = (eType as QualifiedNameSyntax)!.ToString().Split(['.']).Last();
            }
            else if (eType.IsKind(SyntaxKind.AliasQualifiedName))
            {
                entityName = (eType as AliasQualifiedNameSyntax)!.ToString().Split(['.']).Last();
            }
            if (string.IsNullOrEmpty(entityName))
            {
                continue;
            }

            var rootNamespace = string.Empty;

            //获取文件范围的命名空间
            var filescopeNamespace = node.AncestorsAndSelf().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

            if (filescopeNamespace != null)
            {
                rootNamespace = filescopeNamespace.Name.ToString();
            }
            else
            {
                rootNamespace = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().Single().Name.ToString();
            }

            // 排除的属性
            HashSet<string> escapes = [];
            for (var i = 1; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
            {
                var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
                if (expressionSyntax.IsKind(SyntaxKind.InvocationExpression))
                {
                    var name = (expressionSyntax as InvocationExpressionSyntax)!.ArgumentList.DescendantNodes().First().ToString();
                    escapes.Add(name.Split(['.']).Last());
                }
                else if (expressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var name = (expressionSyntax as LiteralExpressionSyntax)!.Token.ValueText;
                    escapes.Add(name);
                }
            }

            retn.Add(new AutoDtoMetadata(entityName, node.Identifier.ValueText, escapes)
            {
                IsRecord = isRecord,
                RootNamespace = rootNamespace,
            });
        }
        return retn;
    }

    private static List<AutoDtoMetadata> GetMetaFromGeneric(ImmutableArray<SyntaxNode> nodes)
    {
        List<AutoDtoMetadata> retn = [];
        if (nodes.Length == 0) return retn;
        foreach (var syntaxNode in nodes.AsEnumerable())
        {
            //Cast<ClassDeclarationSyntax>()
            //Cast<RecordDeclarationSyntax>()
            if (syntaxNode is not TypeDeclarationSyntax node)
            {
                continue;
            }
            //如果是Record类
            var isRecord = syntaxNode is RecordDeclarationSyntax;
            //如果不含partial关键字,则不生成
            if (!node.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
            {
                continue;
            }
            AttributeSyntax? attributeSyntax = null;
            foreach (var attr in node.AttributeLists.AsEnumerable())
            {
                var attrName = attr.Attributes.FirstOrDefault()?.Name.ToString();
                if (attrName?.IndexOf(AttributeValueMetadataNameDto, System.StringComparison.Ordinal) == 0)
                {
                    attributeSyntax = attr.Attributes.First(x => x.Name.ToString().IndexOf(AttributeValueMetadataNameDto, System.StringComparison.Ordinal) == 0);
                    break;
                }
            }
            if (attributeSyntax == null)
            {
                continue;
            }
            //转译的Entity类名
            var entityName = string.Empty;
            string pattern = @"(?<=<)(?<type>\w+)(?=>)";
            var match = Regex.Match(attributeSyntax.ToString(), pattern);
            if (match.Success)
            {
                entityName = match.Groups["type"].Value.Split(['.']).Last();
            }
            else
            {
                continue;
            }

            var rootNamespace = string.Empty;

            //获取文件范围的命名空间
            var filescopeNamespace = node.AncestorsAndSelf().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

            if (filescopeNamespace != null)
            {
                rootNamespace = filescopeNamespace.Name.ToString();
            }
            else
            {
                rootNamespace = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().Single().Name.ToString();
            }

            // 排除的属性
            HashSet<string> escapes = [];

            if (attributeSyntax.ArgumentList?.Arguments.Count > 0)
            {
                for (var i = 0; i < attributeSyntax.ArgumentList?.Arguments.Count; i++)
                {
                    var expressionSyntax = attributeSyntax.ArgumentList.Arguments[i].Expression;
                    if (expressionSyntax.IsKind(SyntaxKind.InvocationExpression))
                    {
                        var name = (expressionSyntax as InvocationExpressionSyntax)!.ArgumentList.DescendantNodes().First().ToString();
                        escapes.Add(name.Split(['.']).Last());
                    }
                    else if (expressionSyntax.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var name = (expressionSyntax as LiteralExpressionSyntax)!.Token.ValueText;
                        escapes.Add(name);
                    }
                }
            }

            retn.Add(new AutoDtoMetadata(entityName, node.Identifier.ValueText, escapes)
            {
                IsRecord = isRecord,
                RootNamespace = rootNamespace,
            });
        }
        return retn;
    }

    private static void GenSource(Compilation compilation, SourceProductionContext context, IEnumerable<AutoDtoMetadata> metadatas)
    {
        if (!metadatas.Any()) return;

        StringBuilder envStringBuilder = new();

        envStringBuilder.AppendLine("// <auto-generated />");
        envStringBuilder.AppendLine("// author:vipwan@outlook.com 万雅虎");
        envStringBuilder.AppendLine("// issue:https://github.com/vipwan/Biwen.AutoClassGen/issues");
        envStringBuilder.AppendLine("// 如果你在使用中遇到问题,请第一时间issue,谢谢!");
        envStringBuilder.AppendLine("// This file is generated by Biwen.AutoClassGen.AutoDtoSourceGenerator");
        envStringBuilder.AppendLine();
        envStringBuilder.AppendLine("using System;");
        envStringBuilder.AppendLine("using System.Collections.Generic;");
        envStringBuilder.AppendLine("using System.Text;");
        envStringBuilder.AppendLine("using System.Threading.Tasks;");
        envStringBuilder.AppendLine("#pragma warning disable");

        foreach (var metadata in metadatas)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"//generate {metadata.FromClass}-{metadata.ToClass}");
            sb.AppendLine();
            sb.AppendLine("namespace $ni");
            sb.AppendLine("{");
            sb.AppendLine("$namespace");
            sb.AppendLine("$classes");
            sb.AppendLine("}");
            // sb.AppendLine("#pragma warning restore");
            string classTemp = $"partial $isRecord $className  {{ $body }}";
            classTemp = classTemp.Replace("$isRecord", metadata.IsRecord ? "record class" : "class");
            {
                var className = metadata.ToClass;

                StringBuilder bodyBuilder = new();
                List<string> namespaces = [];
                StringBuilder bodyInnerBuilder = new();
                StringBuilder mapperBodyBuilder = new();
                StringBuilder mapperBodyBuilder2 = new();

                bodyInnerBuilder.AppendLine();

                List<string> haveProps = [];

                // 生成属性
                void GenProperty(TypeSyntax @type)
                {
                    //无法将类型为“Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel.PropertySymbol”
                    //的对象强制转换为类型“Microsoft.CodeAnalysis.ITypeSymbol
                    var symbols = compilation.GetSymbolsWithName(@type.ToString(), SymbolFilter.Type);

                    foreach (ITypeSymbol symbol in symbols.Cast<ITypeSymbol>())
                    {
                        var fullNameSpace = symbol.ContainingNamespace.ToDisplayString();
                        // 命名空间
                        if (!namespaces.Contains(fullNameSpace))
                        {
                            namespaces.Add(fullNameSpace);
                        }
                        symbol.GetMembers().OfType<IPropertySymbol>().ToList().ForEach(prop =>
                        {
                            if (!metadata.Escapes.Contains(prop.Name))
                            {
                                // 如果存在同名属性,则不生成
                                if (haveProps.Contains(prop.Name))
                                {
                                    return;
                                }

                                haveProps.Add(prop.Name);

                                //如果是泛型属性,则不生成
                                if (prop.ContainingType.TypeParameters.Any(x => x.Name == prop.Type.Name))
                                {
                                    return;
                                }

                                // prop:
                                var raw = $"public {prop.Type.ToDisplayString()} {prop.Name} {{get;set;}}";
                                // body:
                                bodyInnerBuilder.AppendLine($"/// <inheritdoc cref=\"{@type}.{prop.Name}\" />");
                                bodyInnerBuilder.AppendLine($"{raw}");

                                // mapper:
                                // 只有public的属性才能赋值.且可写
                                if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                                {
                                    mapperBodyBuilder.AppendLine($"{prop.Name} = model.{prop.Name},");
                                }
                                // mapper2:
                                if (prop.GetMethod?.DeclaredAccessibility == Accessibility.Public &&
                                    prop.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                                {
                                    mapperBodyBuilder2.AppendLine($"{prop.Name} = model.{prop.Name},");
                                }
                            }
                        });
                    }
                }

                // 生成属性:
                var symbols = compilation.GetSymbolsWithName(metadata.FromClass, SymbolFilter.Type);
                var symbol = symbols.Cast<ITypeSymbol>().FirstOrDefault();

                //引用了其他库.
                if (symbol is null)
                    continue;

                GenProperty(SyntaxFactory.ParseTypeName(symbol.MetadataName));

                // 生成父类的属性:
                INamedTypeSymbol? baseType = symbol.BaseType;
                while (baseType != null)
                {
                    GenProperty(SyntaxFactory.ParseTypeName(baseType.MetadataName));
                    baseType = baseType.BaseType;
                }

                var rawClass = classTemp.Replace("$className", className);
                rawClass = rawClass.Replace("$body", bodyInnerBuilder.ToString());
                // append:
                bodyBuilder.AppendLine(rawClass);

                string rawNamespace = string.Empty;
                namespaces.ForEach(ns => rawNamespace += $"using {ns};\r\n");

                var source = sb.ToString();
                source = source.Replace("$namespace", rawNamespace);
                source = source.Replace("$classes", bodyBuilder.ToString());
                source = source.Replace("$ni", metadata.RootNamespace);

                // 生成Mapper
                var mapperSource = MapperTemplate.Replace("$namespace", namespaces.First());
                mapperSource = mapperSource.Replace("$ns", metadata.RootNamespace);
                mapperSource = mapperSource.Replace("$baseclass", metadata.FromClass);
                mapperSource = mapperSource.Replace("$dtoclass", className);
                mapperSource = mapperSource.Replace("$body", mapperBodyBuilder.ToString());
                mapperSource = mapperSource.Replace("$2body", mapperBodyBuilder2.ToString());

                // 合并
                source = $"{source}\r\n{mapperSource}";
                envStringBuilder.AppendLine(source);
            }
        }

        envStringBuilder.AppendLine("#pragma warning restore");
        var envSource = envStringBuilder.ToString();
        // format:
        envSource = envSource.FormatContent();
        context.AddSource($"Biwen.AutoClassGenDto.g.cs", SourceText.From(envSource, Encoding.UTF8));

    }

    private record AutoDtoMetadata(string FromClass, string ToClass, HashSet<string> Escapes)
    {
        public string? RootNamespace { get; set; }

        /// <summary>
        /// 是否记录类型
        /// </summary>
        public bool IsRecord { get; set; }
    }

    #region Template

    private const string MapperTemplate = $@"
namespace $namespace
{{
    using $ns ;
    public static partial class $baseclassTo$dtoclassExtentions
    {{
        /// <summary>
        /// custom mapper
        /// </summary>
        static partial void MapperToPartial($baseclass from, $dtoclass to);
        /// <summary>
        /// mapper to $dtoclass
        /// </summary>
        /// <returns></returns>
        public static $dtoclass MapperTo$dtoclass(this $baseclass model)
        {{
            var retn = new $dtoclass()
            {{
                $body
            }};
            MapperToPartial(model, retn);
            return retn;
        }}
    }}

    public static partial class $dtoclassTo$baseclassExtentions
    {{
        /// <summary>
        /// custom mapper
        /// </summary>
        static partial void MapperToPartial($dtoclass from, $baseclass to);
        /// <summary>
        /// mapper to $baseclass
        /// </summary>
        /// <returns></returns>
        public static $baseclass MapperTo$baseclass(this $dtoclass model)
        {{
            var retn = new $baseclass()
            {{
                $2body
            }};
            MapperToPartial(model, retn);
            return retn;
        }}
    }}
}}
";

    #endregion

}
