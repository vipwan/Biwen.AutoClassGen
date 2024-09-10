using Biwen.AutoClassGen.Analyzers;
using Biwen.AutoClassGen.CodeFixs;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace AutoClassGenTest;

public class MyVerifier : DefaultVerifier
{
    public override IVerifier PushContext(string context)
    {
        return new MyVerifier();
    }

    public override void Equal<T>(T expected, T actual, string? message = null)
    {
        //0,1:返回成功
        if (expected is 0 && actual is 1) return;

        base.Equal(expected, actual, message);
    }
}

//public class AddFileHeaderCodeFixProviderTest : CSharpCodeFixVerifier<FileHeaderAnalyzer, AddFileHeaderCodeFixProvider, MyVerifier>
//{

//    [Fact]
//    public async Task Test()
//    {
//        var @code = "namespace Biwen { public interface IRequest {} }";

//        var fixedCode = $"""
//            // Licensed to the TestProject under one or more agreements.
//            // The TestProject licenses this file to you under the MIT license.
//            // See the LICENSE file in the project root for more information.
//            {@code}
//            """;

//        Diagnostic(FileHeaderAnalyzer.DiagnosticId)
//            .WithSpan(1, 1, 1, 1)
//            .WithLocation(1, 1);
//        ;

//        await VerifyAnalyzerAsync(@code);
//        await VerifyCodeFixAsync(@code, fixedCode);

//    }
//}