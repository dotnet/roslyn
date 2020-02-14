// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                "DiagnosticResult.CompilerError(\"CS1002\").WithSpan(3, 34, 3, 35)" + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
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
        public async Task TestCSharpExplicitCompilerErrorWithExplicitInterfaceSymbol()
        {
            var testCode = @"using System;

class TestClass {
  void IDisposable.Dispose() { }
}
";

            await new CSharpTest
            {
                TestCode = testCode,
                ExpectedDiagnostics =
                {
                    // Test0.cs(4,8): error CS0540: 'TestClass.IDisposable.Dispose()': containing type does not implement interface 'IDisposable'
                    DiagnosticResult.CompilerError("CS0540").WithSpan(4, 8, 4, 19).WithArguments("TestClass.System.IDisposable.Dispose()", "System.IDisposable"),
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpReorderedExplicitCompilerErrorWithExplicitInterfaceSymbol()
        {
            var testCode = @"using System.Collections.Generic;

class TestClass : IEnumerable<int> {
}
";

            await new CSharpTest
            {
                TestCode = testCode,
                ExpectedDiagnostics =
                {
                    // Test0.cs(3,19): error CS0535: 'TestClass' does not implement interface member 'IEnumerable<int>.GetEnumerator()'
                    DiagnosticResult.CompilerError("CS0535").WithSpan(3, 19, 3, 35).WithArguments("TestClass", "System.Collections.Generic.IEnumerable<int>.GetEnumerator()"),

                    // Test0.cs(3,19): error CS0535: 'TestClass' does not implement interface member 'IEnumerable.GetEnumerator()'
                    DiagnosticResult.CompilerError("CS0535").WithLocation(3, 19).WithArguments("TestClass", "System.Collections.IEnumerable.GetEnumerator()"),
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpMarkupCompilerError()
        {
            var testCode = @"
class TestClass {
  void TestMethod() { throw null {|CS1002:}|}
}
";

            await new CSharpTest { TestCode = testCode }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpCompilerWarning()
        {
            var testCode = @"
class TestClass {
  int value = 3;
}
";

            // By default the warning is ignored
            Assert.Equal(CompilerDiagnostics.Errors, new CSharpTest { TestCode = testCode }.CompilerDiagnostics);
            await new CSharpTest { TestCode = testCode }.RunAsync();

            // The warning is checked with explicit configuration
            await new CSharpTest
            {
                TestCode = testCode,
                ExpectedDiagnostics = { DiagnosticResult.CompilerWarning("CS0414").WithSpan(3, 7, 3, 12).WithArguments("TestClass.value") },
                CompilerDiagnostics = CompilerDiagnostics.Warnings,
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpCompilerWarningDeclaredWithWrongArgument()
        {
            var testCode = @"
class TestClass {
  int value = 3;
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    ExpectedDiagnostics = { DiagnosticResult.CompilerWarning("CS0414").WithSpan(3, 7, 3, 12).WithArguments("TestClass2.value") },
                    CompilerDiagnostics = CompilerDiagnostics.Warnings,
                }.RunAsync();
            });

            var expected =
                "Expected diagnostic message arguments to match" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostic:" + Environment.NewLine +
                "    // Test0.cs(3,7): warning CS0414: The field 'TestClass.value' is assigned but its value is never used" + Environment.NewLine +
                "DiagnosticResult.CompilerWarning(\"CS0414\").WithSpan(3, 7, 3, 12).WithArguments(\"TestClass.value\")" + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestCSharpCompilerWarningNotDeclared()
        {
            var testCode = @"
class TestClass {
  int value = 3;
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest { TestCode = testCode, CompilerDiagnostics = CompilerDiagnostics.Warnings }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// Test0.cs(3,7): warning CS0414: The field 'TestClass.value' is assigned but its value is never used" + Environment.NewLine +
                "DiagnosticResult.CompilerWarning(\"CS0414\").WithSpan(3, 7, 3, 12).WithArguments(\"TestClass.value\")" + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
        }

        [Fact]
        public async Task TestCSharpCompilerHidden()
        {
            var testCode = @"
using System;

class TestClass {
}
";

            // By default the warning is ignored
            Assert.Equal(CompilerDiagnostics.Errors, new CSharpTest { TestCode = testCode }.CompilerDiagnostics);
            await new CSharpTest { TestCode = testCode }.RunAsync();

            // The warning is ignored at Warning severity
            await new CSharpTest { TestCode = testCode, CompilerDiagnostics = CompilerDiagnostics.Warnings }.RunAsync();

            // The warning is ignored at Suggestions severity
            await new CSharpTest { TestCode = testCode, CompilerDiagnostics = CompilerDiagnostics.Suggestions }.RunAsync();

            // The warning is checked with explicit configuration
            await new CSharpTest
            {
                TestCode = testCode,
                ExpectedDiagnostics = { new DiagnosticResult("CS8019", DiagnosticSeverity.Hidden).WithSpan(2, 1, 2, 14).WithMessage("Unnecessary using directive.") },
                CompilerDiagnostics = CompilerDiagnostics.All,
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpCompilerHiddenNotDeclared()
        {
            var testCode = @"
using System;

class TestClass {
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest { TestCode = testCode, CompilerDiagnostics = CompilerDiagnostics.All }.RunAsync();
            });

            var expected =
                "Mismatch between number of diagnostics returned, expected \"0\" actual \"1\"" + Environment.NewLine +
                Environment.NewLine +
                "Diagnostics:" + Environment.NewLine +
                "// Test0.cs(2,1): hidden CS8019: Unnecessary using directive." + Environment.NewLine +
                "new DiagnosticResult(\"CS8019\", DiagnosticSeverity.Hidden).WithSpan(2, 1, 2, 14)" + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
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
                "DiagnosticResult.CompilerError(\"BC30205\").WithSpan(3, 13, 3, 14)" + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
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

        [Fact]
        public async Task TestCSharpValueTupleUsageNet46()
        {
            var testCode = @"
class TestClass {
  (int x, int y) TestMethod() { return (0, 1); }
}
";

            await new CSharpTest
            {
                TestState =
                {
                    Sources = { testCode },
                },
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net46.Default
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("System.ValueTuple", "4.5.0"))),
            }.RunAsync();
        }

        [Fact]
        public async Task TestCSharpValueTupleUsageNet472()
        {
            var testCode = @"
class TestClass {
  (int x, int y) TestMethod() { return (0, 1); }
}
";

            await new CSharpTest
            {
                TestCode = testCode,
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Default,
            }.RunAsync();
        }

        [Fact]
        public async Task TestRoslynCompilerUsage_1()
        {
            var testCode = @"
using Microsoft.CodeAnalysis.CSharp;
class TestClass {
  SyntaxKind TestMethod() => SyntaxKind.CloseBraceToken;
}
";

            await new CSharpTest
            {
                TestState =
                {
                    Sources = { testCode },
                },
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net452.Default
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "1.0.1"))),
            }.RunAsync();
        }

        [Fact]
        public async Task TestRoslynCompilerUsage_2()
        {
            var testCode = @"
using Microsoft.CodeAnalysis.CSharp;
class TestClass {
  SyntaxKind TestMethod() => SyntaxKind.TupleType;
}
";

            await new CSharpTest
            {
                TestState =
                {
                    Sources = { testCode },
                },
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net46.Default
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "2.8.2"))),
            }.RunAsync();
        }

        [Fact]
        public async Task TestRoslynCompilerUsage_3()
        {
            var testCode = @"
using Microsoft.CodeAnalysis.CSharp;
class TestClass {
  SyntaxKind TestMethod() => SyntaxKind.DotDotToken;
}
";

            await new CSharpTest
            {
                TestState =
                {
                    Sources = { testCode },
                },
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net472.Default
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "3.3.1"))),
            }.RunAsync();
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
