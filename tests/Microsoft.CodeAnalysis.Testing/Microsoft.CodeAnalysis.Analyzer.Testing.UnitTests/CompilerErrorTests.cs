// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class CompilerErrorTests
    {
        [Fact]
        public async Task TestCSharpUndeclaredCompilerError()
        {
            var testCode = @"
class TestClass {
  void TestMethod() { throw null }
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest { TestCode = testCode }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// Test0.cs(3,34): error CS1002: ; expected" + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task TestCSharpExplicitCompilerError()
        {
            var testCode = @"
class TestClass {
  void TestMethod() { throw null }
}
";

            await new CSharpTest
            {
                TestCode = testCode,
                ExpectedDiagnostics = { DiagnosticResult.CompilerError("CS1002").WithLocation(3, 34).WithMessage("; expected") },
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpMarkupCompilerError()
        {
            var testCode = @"
class TestClass {
  void TestMethod() { throw null {|CS1002:|}}
}
";

            await new CSharpTest { TestCode = testCode }.RunAsync();
        }

        [Fact]
        public async Task TestVisualBasicUndeclaredCompilerError()
        {
            var testCode = @"
Class TestClass
  Sub Method)
  End Sub
End Class
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VisualBasicTest { TestCode = testCode }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// Test0.vb(3) : error BC30205: End of statement expected." + Environment.NewLine +
                Environment.NewLine;
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task TestVisualBasicExplicitCompilerError()
        {
            var testCode = @"
Class TestClass
  Sub Method)
  End Sub
End Class
";

            await new VisualBasicTest
            {
                TestCode = testCode,
                ExpectedDiagnostics = { DiagnosticResult.CompilerError("BC30205").WithLocation(3, 13).WithMessage("End of statement expected.") },
            }.RunAsync();
        }

        [Fact]
        public async Task TestVisualBasicMarkupCompilerError()
        {
            var testCode = @"
Class TestClass
  Sub Method{|BC30205:)|}
  End Sub
End Class
";

            await new VisualBasicTest { TestCode = testCode }.RunAsync();
        }

        private class CSharpTest : AnalyzerTest<DefaultVerifier>
        {
            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
                => new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new NoActionAnalyzer();
            }
        }

        private class VisualBasicTest : AnalyzerTest<DefaultVerifier>
        {
            public override string Language => LanguageNames.VisualBasic;

            protected override string DefaultFileExt => "vb";

            protected override CompilationOptions CreateCompilationOptions()
                => new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new NoActionAnalyzer();
            }
        }
    }
}
