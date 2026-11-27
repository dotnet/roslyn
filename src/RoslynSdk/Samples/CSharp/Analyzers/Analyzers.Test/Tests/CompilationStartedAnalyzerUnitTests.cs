// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
