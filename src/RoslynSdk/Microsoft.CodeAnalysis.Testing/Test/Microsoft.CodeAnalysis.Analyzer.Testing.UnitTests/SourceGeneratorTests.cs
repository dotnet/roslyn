// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing.TestAnalyzers;
using Microsoft.CodeAnalysis.Testing.TestGenerators;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerWithSourceGeneratorTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.SourceGeneratorTests.GenerateSourceFile>;
using VisualBasicTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.VisualBasicAnalyzerWithSourceGeneratorTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.Testing.SourceGeneratorTests.GenerateSourceFile>;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceGeneratorTests
    {
        [Fact]
        public async Task TestValidateAddedSourceCSharp()
        {
            await new CSharpTest
            {
                TestState =
                {
                    Sources = { "class MainClass : TestClass { }" },
                    GeneratedSources = { (typeof(GenerateSourceFile), "Generated.g.cs", CreateExpectedGeneratedSource("class TestClass { }")) },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task TestValidateAddedSourceVisualBasic()
        {
            await new VisualBasicTest
            {
                TestState =
                {
                    Sources = { "Class MainClass : Inherits TestClass : End Class" },
                    GeneratedSources = { (typeof(GenerateSourceFile), "Generated.g.vb", CreateExpectedGeneratedSource("Class TestClass : End Class")) },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFile()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task MultipleFilesAllowEitherOrder()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddTwoEmptyFiles>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile1.cs", CreateExpectedGeneratedSource(string.Empty)),
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile2.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                },
            }.RunAsync();

            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddTwoEmptyFiles>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile2.cs", CreateExpectedGeneratedSource(string.Empty)),
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile1.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileByGeneratorType()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileByGeneratorTypeWithEncoding()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileToEmptyProject()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileWithWrongExpectedEncoding()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFile>
                {
                    TestState =
                    {
                        GeneratedSources =
                        {
                            (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.Unicode)),
                        },
                    },
                }.RunAsync();
            });

            var generatedFilePath = GetGeneratedFilePath(typeof(AddEmptyFile), "EmptyGeneratedFile.cs");
            var expectedMessage =
                $$"""
                Context: Source generator application
                Context: Verifying source generated files
                encoding of '{{generatedFilePath}}' was expected to be 'utf-16' but was 'utf-8'
                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());
        }

        [Fact]
        [UseCulture("en-US")]
        public async Task AddSimpleFileVerifiesCompilerDiagnostics_CSharp()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddFileWithCompileError>
                {
                    TestState =
                    {
                        Sources =
                        {
                            @"class A {",
                        },
                        GeneratedSources =
                        {
                            (typeof(AddFileWithCompileError), "ErrorGeneratedFile.cs", CreateExpectedGeneratedSource(@"class C {")),
                        },
                    },
                }.RunAsync();
            });

            var expectedMessage =
                $$"""
                Mismatch between number of diagnostics returned, expected "0" actual "2"

                Diagnostics:
                // /0/Test0.cs(1,10): error CS1513: } expected
                DiagnosticResult.CompilerError("CS1513").WithSpan(1, 10, 1, 10),
                // {{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.cs")}}(1,10): error CS1513: } expected
                DiagnosticResult.CompilerError("CS1513").WithSpan("{{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.cs")}}", 1, 10, 1, 10),


                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());
        }

        [Fact]
        [UseCulture("en-US")]
        public async Task AddSimpleFileVerifiesCompilerDiagnosticsEvenWhenSourceGeneratorOutputsSkipped_CSharp()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddFileWithCompileError>
                {
                    TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                    TestState =
                    {
                        Sources =
                        {
                            @"class A {",
                        },
                    },
                }.RunAsync();
            });

            var expectedMessage =
                $$"""
                Mismatch between number of diagnostics returned, expected "0" actual "2"

                Diagnostics:
                // /0/Test0.cs(1,10): error CS1513: } expected
                DiagnosticResult.CompilerError("CS1513").WithSpan(1, 10, 1, 10),
                // {{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.cs")}}(1,10): error CS1513: } expected
                DiagnosticResult.CompilerError("CS1513").WithSpan("{{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.cs")}}", 1, 10, 1, 10),


                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());
        }

        [Fact]
        [UseCulture("en-US")]
        public async Task AddSimpleFileVerifiesCompilerDiagnostics_VisualBasic()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VisualBasicAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddFileWithCompileError>
                {
                    TestState =
                    {
                        Sources =
                        {
                            "Class A",
                        },
                        GeneratedSources =
                        {
                            (typeof(AddFileWithCompileError), "ErrorGeneratedFile.vb", CreateExpectedGeneratedSource("Class C")),
                        },
                    },
                }.RunAsync();
            });

            var expectedMessage =
                $$"""
                Mismatch between number of diagnostics returned, expected "0" actual "2"

                Diagnostics:
                // /0/Test0.vb(1) : error BC30481: 'Class' statement must end with a matching 'End Class'.
                DiagnosticResult.CompilerError("BC30481").WithSpan(1, 1, 1, 8),
                // {{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.vb")}}(1) : error BC30481: 'Class' statement must end with a matching 'End Class'.
                DiagnosticResult.CompilerError("BC30481").WithSpan("{{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.vb")}}", 1, 1, 1, 8),


                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());
        }

        [Fact]
        [UseCulture("en-US")]
        public async Task AddSimpleFileVerifiesCompilerDiagnosticsEvenWhenSourceGeneratorOutputsSkipped_VisualBasic()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VisualBasicAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddFileWithCompileError>
                {
                    TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                    TestState =
                    {
                        Sources =
                        {
                            "Class A",
                        },
                    },
                }.RunAsync();
            });

            var expectedMessage =
                $$"""
                Mismatch between number of diagnostics returned, expected "0" actual "2"

                Diagnostics:
                // /0/Test0.vb(1) : error BC30481: 'Class' statement must end with a matching 'End Class'.
                DiagnosticResult.CompilerError("BC30481").WithSpan(1, 1, 1, 8),
                // {{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.vb")}}(1) : error BC30481: 'Class' statement must end with a matching 'End Class'.
                DiagnosticResult.CompilerError("BC30481").WithSpan("{{GetGeneratedFilePath(typeof(AddFileWithCompileError), "ErrorGeneratedFile.vb")}}", 1, 1, 1, 8),


                """;
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message.ReplaceLineEndings());
        }

        [Fact]
        public async Task AddSimpleFileWithDiagnostic()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFileWithDiagnostic>
            {
                TestState =
                {
                    Sources =
                    {
                        @"{|#0:|}// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFileWithDiagnostic), "EmptyGeneratedFile.cs", CreateExpectedGeneratedSource(string.Empty)),
                    },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(1,1): warning SG0001: Message
                        new DiagnosticResult(AddEmptyFileWithDiagnostic.Descriptor).WithLocation(0),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddImplicitSimpleFileWithDiagnostic()
        {
            await new CSharpAnalyzerWithSourceGeneratorTest<EmptyDiagnosticAnalyzer, AddEmptyFileWithDiagnostic>
            {
                TestBehaviors = TestBehaviors.SkipGeneratedSourcesCheck,
                TestState =
                {
                    Sources =
                    {
                        @"{|#0:|}// Comment",
                    },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(1,1): warning SG0001: Message
                        new DiagnosticResult(AddEmptyFileWithDiagnostic.Descriptor).WithLocation(0),
                    },
                },
            }.RunAsync();
        }

        private static string GetGeneratedFilePath(Type sourceGeneratorType, string fileName)
            => Path.Combine(sourceGeneratorType.Assembly.GetName().Name!, sourceGeneratorType.FullName!, fileName);

        private static SourceText CreateExpectedGeneratedSource(string source)
            => SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256);

        [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
#pragma warning disable RS1042 // Do not implement
        internal class GenerateSourceFile : ISourceGenerator
#pragma warning restore RS1042 // Do not implement
        {
            private const string CSharpSource = @"class TestClass { }";
            private const string VisualBasicSource = @"Class TestClass : End Class";

            public void Execute(GeneratorExecutionContext context)
            {
                var (source, hintName) = context.Compilation.Language == LanguageNames.CSharp
                    ? (source: CSharpSource, hintName: "Generated.g.cs")
                    : (source: VisualBasicSource, hintName: "Generated.g.vb");

                context.AddSource(hintName, source);
            }

            public void Initialize(GeneratorInitializationContext context)
            {
            }
        }
    }
}
