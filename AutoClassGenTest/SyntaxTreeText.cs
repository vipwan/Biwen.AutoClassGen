using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoClassGenTest;

public class SyntaxTreeText
{
    public const string SourceText = """

        using System;

        namespace Biwen.QuickApi.DemoWeb;

        public interface IHelloService
        {
            string Hello(HelloService.HelloBody helloBody);
        }

        /// <summary>
        /// 测试服务
        /// </summary>

        [AutoInject<IHelloService>]
        [AutoInjectKeyed<IHelloService>("hello")]
        public partial class HelloService(ILogger<HelloService> logger) : IHelloService
        {
            public record HelloBody(string name, int age);

            /// <summary>
            /// 测试方法
            /// </summary>
            /// <param name="HelloBody">body</param>
            public string Hello(HelloBody helloBody)
            {
                var str = $"Hello {helloBody.name}";
                Console.WriteLine(str);

                Log.LogInfo(logger, helloBody);
                //logger.LogInformation($"Hello {helloBody.name}");
                return str;
            }

            static partial class Log
            {
                [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Hello {helloBody}")]
                public static partial void LogInfo(ILogger logger, [LogProperties] HelloBody helloBody);
            }

        }
        
        """;

    [Fact]
    public void 查询源代码中的所有Using引用()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();

        Assert.Single(usings);
        Assert.Equal("System", usings[0].Name!.ToFullString());
    }

    [Fact]
    public void 查询源代码中的所有命名空间()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();

        //命名空间存在两种形式, 一种是NamespaceDeclarationSyntax, 一种是FileScopedNamespaceDeclarationSyntax
        //并且NamespaceDeclarationSyntax可能存在多个, FileScopedNamespaceDeclarationSyntax只有一个

        var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();
        var fileScopedNamespaces = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>().ToList();

        Assert.Empty(namespaces);

        Assert.Single(fileScopedNamespaces);
        Assert.Equal("Biwen.QuickApi.DemoWeb", fileScopedNamespaces[0].Name.ToFullString());
    }

    [Fact]
    public void 查询源代码中的所有接口()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToList();
        Assert.Single(interfaces);
        Assert.Equal("IHelloService", interfaces[0].Identifier.Text);


        //查询接口返回类型
        var members = interfaces[0].Members;
        Assert.Single(members);
        var method = members[0] as MethodDeclarationSyntax;
        Assert.NotNull(method);
        Assert.Equal("Hello", method.Identifier.Text);
        Assert.Equal("string", method.ReturnType.ToString());


        //查询方法参数
        var parameters = method.ParameterList!.Parameters;
        Assert.Single(parameters);
        var parameter = parameters[0];
        Assert.Equal("HelloService.HelloBody", parameter.Type!.ToString());



    }

    [Fact]
    public void 查询源代码中的所有类()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        Assert.Equal(2, classes.Count);

        Assert.Equal("HelloService", classes[0].Identifier.Text);

        Assert.Equal("Log", classes[1].Identifier.Text);

    }

    [Fact]
    public void 查询类标注的特性()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var attributes = classes[0].AttributeLists.SelectMany(x => x.Attributes).ToList();
        Assert.Equal(2, attributes.Count);
        Assert.Equal("AutoInject<IHelloService>", attributes[0].Name.ToFullString());
        Assert.Equal("AutoInjectKeyed<IHelloService>", attributes[1].Name.ToFullString());
    }

    [Fact]
    public void 查询类有哪些修饰符()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        var modifiers = classes[1].Modifiers.Select(x => x.Text).ToList();

        Assert.Equal(2, modifiers.Count);

        Assert.Contains("static", modifiers);
        Assert.Contains("partial", modifiers);
    }

    [Fact]
    public void 查询类中有哪些方法()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        //查询的子孙节点
        var methods = classes[0].DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        Assert.Equal(2, methods.Count);

        Assert.Equal("Hello", methods[0].Identifier.Text);
        Assert.Equal("LogInfo", methods[1].Identifier.Text);
    }

    [Fact]
    public void 查询方法标注的特性()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var methods = classes[0].DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
        var attributes = methods[1].AttributeLists.SelectMany(x => x.Attributes).ToList();
        Assert.Single(attributes);
        Assert.Equal("LoggerMessage", attributes[0].Name.ToFullString());

        //查询特性的参数:
        var arguments = attributes[0].ArgumentList!.Arguments;
        Assert.Equal(3, arguments.Count);
        Assert.Equal("EventId = 0", arguments[0].ToFullString());
        Assert.Equal("Level = LogLevel.Information", arguments[1].ToFullString());
        Assert.Equal("Message = \"Hello {helloBody}\"", arguments[2].ToFullString());


        //查询特性参数的值的具体含义
        var argument1 = arguments[0];
        var argument2 = arguments[1];
        var argument3 = arguments[2];

        var argument1Name = argument1.NameEquals!.Name.Identifier.Text;
        var argument1Value = argument1.Expression.ToFullString();

        var argument2Name = argument2.NameEquals!.Name.Identifier.Text;
        var argument2Value = argument2.Expression.ToFullString();

        var argument3Name = argument3.NameEquals!.Name.Identifier.Text;
        var argument3Value = argument3.Expression.ToFullString();

        Assert.Equal("EventId", argument1Name);
        Assert.Equal("0", argument1Value);

        Assert.Equal("Level", argument2Name);
        Assert.Equal("LogLevel.Information", argument2Value);

        Assert.Equal("Message", argument3Name);
        Assert.Equal("\"Hello {helloBody}\"", argument3Value);


    }

    [Fact]
    public void 查询方法的注释信息()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText);
        var root = syntaxTree.GetRoot();
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var methods = classes[0].DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

        // 获取方法的注释
        var trivia = methods[0].GetLeadingTrivia()
                           .Select(i => i.GetStructure())
                           .OfType<DocumentationCommentTriviaSyntax>()
                           .FirstOrDefault();


        Assert.NotNull(trivia);

        // 查找summary节点
        var summarys = trivia.Content.OfType<XmlElementSyntax>();

        Assert.Equal(2, summarys.Count());

        var summary = summarys.First();

        Assert.Equal("summary", summary.StartTag.Name.LocalName.Text);

        // 获取summary的内容
        var summaryContent = summary.Content;

        //SyntaxToken XmlTextLiteralToken  测试方法
        var content = summaryContent[0].ChildTokens().FirstOrDefault(x => x.Text.Trim().Length > 0);
        Assert.Contains("测试方法", content.Text);

        // 获取param节点
        var param = summarys.FirstOrDefault(x => x.StartTag.Name.LocalName.Text == "param");

        Assert.NotNull(param);

        // 获取param的内容
        var paramContent = param.Content;
        Assert.Contains("body", paramContent.ToString());

        // 获取param的name属性
        var name = param.StartTag.Attributes.FirstOrDefault(x => x.Name.LocalName.Text == "name");
        Assert.NotNull(name);
        Assert.Contains("HelloBody", name.ToString());






    }

}
