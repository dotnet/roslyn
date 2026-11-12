// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Sample.Analyzers.CompilationStartedAnalyzerWithCompilationWideAnalysis, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Sample.Analyzers.Test
{
    public class CompilationStartedAnalyzerWithCompilationWideAnalysisUnitTests
    {
        [Fact]
        public async Task CompilationStartedAnalyzerWithCompilationWideAnalysisTest()
        {
            string test = @"
namespace MyNamespace
{
    public class UnsecureMethodAttribute : System.Attribute { }

    public interface ISecureType { }

    public interface IUnsecureInterface
    {
        [UnsecureMethodAttribute]
        void F();
    }

    class MyInterfaceImpl1 : IUnsecureInterface
    {
        public void F() {}
    }

    class MyInterfaceImpl2 : IUnsecureInterface, ISecureType
    {
        public void F() {}
    }

    class MyInterfaceImpl3 : ISecureType
    {
        public void F() {}
    }
}";
            DiagnosticResult expected = Verify.Diagnostic().WithArguments("MyInterfaceImpl2", CompilationStartedAnalyzerWithCompilationWideAnalysis.SecureTypeInterfaceName, "IUnsecureInterface").WithLocation(19, 11);
            await Verify.VerifyAnalyzerAsync(test, expected);
        }
    }
}
