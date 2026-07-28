// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.CompilationStartedAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class CompilationStartedAnalyzerUnitTests
    {
        [Fact]
        public async Task CompilationStartedAnalyzerTest()
        {
            string test = @"
namespace MyInterfaces
{
    public interface Interface {}
    class MyInterfaceImpl : Interface
    {
    }
    class MyInterfaceImpl2 : Interface
    {
    }
}";
            DiagnosticResult expected = Verify.Diagnostic().WithArguments("MyInterfaceImpl2", CompilationStartedAnalyzer.DontInheritInterfaceTypeName).WithLocation(8, 11);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
