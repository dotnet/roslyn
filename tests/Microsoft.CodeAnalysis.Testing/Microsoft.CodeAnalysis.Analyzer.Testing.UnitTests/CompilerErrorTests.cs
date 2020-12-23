// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>;
using VisualBasicTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.VisualBasicAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>;

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
                "// /0/Test0.cs(3,34): error CS1002: ; expected" + Environment.NewLine +
                "DiagnosticResult.CompilerError(\"CS1002\").WithSpan(3, 34, 3, 35)," + Environment.NewLine +
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
        public async Task TestMultipleErrorsMatchQuality()
        {
            var testCode = @"class TestClass {
  void IDisposable.Dispose() { }
}
";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpTest
                {
                    TestCode = testCode,
                    ExpectedDiagnostics =
                    {
                        // Test0.cs(2,8): error CS0246: The type or namespace name 'IDisposable' could not be found (are you missing a using directive or an assembly reference?)
                        DiagnosticResult.CompilerError("CS0246").WithSpan(2, 8, 2, 21).WithArguments("IDisposable"),

                        // Test0.cs(2,8): error CS0538: 'IDisposable' in explicit interface declaration is not an interface
                        DiagnosticResult.CompilerError("CS0538").WithSpan(2, 8, 2, 20).WithArguments("IDisposable"),
                    },
                }.RunAsync();
            });

            var expected =
                "Expected diagnostic to end at column \"21\" was actually at column \"19\"" + Environment.NewLine +
                Environment.NewLine +
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(2,8,2,21): error CS0246" + Environment.NewLine +
                "DiagnosticResult.CompilerError(\"CS0246\").WithSpan(2, 8, 2, 21).WithArguments(\"IDisposable\")," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(2,8): error CS0246: The type or namespace name 'IDisposable' could not be found (are you missing a using directive or an assembly reference?)" + Environment.NewLine +
                "DiagnosticResult.CompilerError(\"CS0246\").WithSpan(2, 8, 2, 19).WithArguments(\"IDisposable\")," + Environment.NewLine +
                Environment.NewLine;
            new DefaultVerifier().EqualOrDiff(expected, exception.Message);
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
                "Expected diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(3,7,3,12): warning CS0414" + Environment.NewLine +
                "DiagnosticResult.CompilerWarning(\"CS0414\").WithSpan(3, 7, 3, 12).WithArguments(\"TestClass2.value\")," + Environment.NewLine +
                Environment.NewLine +
                "Actual diagnostic:" + Environment.NewLine +
                "    // /0/Test0.cs(3,7): warning CS0414: The field 'TestClass.value' is assigned but its value is never used" + Environment.NewLine +
                "DiagnosticResult.CompilerWarning(\"CS0414\").WithSpan(3, 7, 3, 12).WithArguments(\"TestClass.value\")," + Environment.NewLine +
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
                "// /0/Test0.cs(3,7): warning CS0414: The field 'TestClass.value' is assigned but its value is never used" + Environment.NewLine +
                "DiagnosticResult.CompilerWarning(\"CS0414\").WithSpan(3, 7, 3, 12).WithArguments(\"TestClass.value\")," + Environment.NewLine +
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
                "// /0/Test0.cs(2,1): hidden CS8019: Unnecessary using directive." + Environment.NewLine +
                "new DiagnosticResult(\"CS8019\", DiagnosticSeverity.Hidden).WithSpan(2, 1, 2, 14)," + Environment.NewLine +
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
                "// /0/Test0.vb(3) : error BC30205: End of statement expected." + Environment.NewLine +
                "DiagnosticResult.CompilerError(\"BC30205\").WithSpan(3, 13, 3, 14)," + Environment.NewLine +
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

        [Theory]
        [InlineData("netstandard1.1", Skip = "https://github.com/dotnet/roslyn-sdk/issues/471")]
        [InlineData("netstandard2.0", Skip = "https://github.com/dotnet/roslyn-sdk/issues/471")]
        [InlineData("netstandard2.1", Skip = "https://github.com/dotnet/roslyn-sdk/issues/471")]
        [InlineData("net452")]
        [InlineData("net472")]
        public async Task TestRoslynCompilerUsage_1(string targetFramework)
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
                ReferenceAssemblies = MetadataReferenceTests.ReferenceAssembliesForTargetFramework(targetFramework)
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "1.0.1"))),
            }.RunAsync();
        }

        [Theory]
        [InlineData("netstandard1.3")]
        [InlineData("netstandard2.0")]
        [InlineData("netstandard2.1")]
        [InlineData("net46")]
        [InlineData("net472")]
        [InlineData("netcoreapp3.0")]
        [InlineData("netcoreapp3.1")]
#if !(NETCOREAPP1_1 || NET46)
        [InlineData("net5.0")]
#endif
        public async Task TestRoslynCompilerUsage_2(string targetFramework)
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
                ReferenceAssemblies = MetadataReferenceTests.ReferenceAssembliesForTargetFramework(targetFramework)
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "2.8.2"))),
            }.RunAsync();
        }

        [Theory]
        [InlineData("netstandard2.0")]
        [InlineData("netstandard2.1")]
        [InlineData("net472")]
        [InlineData("netcoreapp3.1")]
#if !(NETCOREAPP1_1 || NET46)
        [InlineData("net5.0")]
#endif
        public async Task TestRoslynCompilerUsage_3(string targetFramework)
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
                ReferenceAssemblies = MetadataReferenceTests.ReferenceAssembliesForTargetFramework(targetFramework)
                    .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "3.3.1"))),
            }.RunAsync();
        }

#if !(NETCOREAPP1_1 || NET46)
        [Fact]
        public async Task TestTopLevelStatements()
        {
            await new CSharpTest
            {
                TestState =
                {
                    OutputKind = OutputKind.ConsoleApplication,
                    Sources =
                    {
                        @"return 0;",
                    },
                },
            }.RunAsync();
        }
#endif
    }
}
