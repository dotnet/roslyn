// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing.TestGenerators;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceGeneratorValidationTests
    {
        [Fact]
        public async Task AddSimpleFile()
        {
            await new CSharpSourceGeneratorTest<AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        ("Microsoft.CodeAnalysis.Testing.Utilities\\Microsoft.CodeAnalysis.Testing.TestGenerators.AddEmptyFile\\EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task MultipleFilesAllowEitherOrder()
        {
            await new CSharpSourceGeneratorTest<AddTwoEmptyFiles>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile1.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile2.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                    },
                },
            }.RunAsync();

            await new CSharpSourceGeneratorTest<AddTwoEmptyFiles>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile2.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                        (typeof(AddTwoEmptyFiles), "EmptyGeneratedFile1.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileByGeneratorType()
        {
            await new CSharpSourceGeneratorTest<AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", string.Empty),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileByGeneratorTypeWithEncoding()
        {
            await new CSharpSourceGeneratorTest<AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                        @"// Comment",
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileToEmptyProject()
        {
            await new CSharpSourceGeneratorTest<AddEmptyFile>
            {
                TestState =
                {
                    Sources =
                    {
                    },
                    GeneratedSources =
                    {
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", string.Empty),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task AddSimpleFileWithWrongExpectedEncoding()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpSourceGeneratorTest<AddEmptyFile>
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

            var expectedMessage = @"Context: Source generator application
Context: Verifying source generated files
encoding of 'Microsoft.CodeAnalysis.Testing.Utilities\Microsoft.CodeAnalysis.Testing.TestGenerators.AddEmptyFile\EmptyGeneratedFile.cs' was expected to be 'utf-16' but was 'utf-8'";
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task AddSimpleFileVerifiesCompilerDiagnostics_CSharp()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new CSharpSourceGeneratorTest<AddFileWithCompileError>
                {
                    TestState =
                    {
                        Sources =
                        {
                            @"class A {",
                        },
                        GeneratedSources =
                        {
                            (typeof(AddFileWithCompileError), "ErrorGeneratedFile.cs", @"class C {"),
                        },
                    },
                }.RunAsync();
            });

            var expectedMessage = @"Mismatch between number of diagnostics returned, expected ""0"" actual ""2""

Diagnostics:
// /0/Test0.cs(1,10): error CS1513: } expected
DiagnosticResult.CompilerError(""CS1513"").WithSpan(1, 10, 1, 10),
// Microsoft.CodeAnalysis.Testing.Utilities\Microsoft.CodeAnalysis.Testing.TestGenerators.AddFileWithCompileError\ErrorGeneratedFile.cs(1,10): error CS1513: } expected
DiagnosticResult.CompilerError(""CS1513"").WithSpan(""Microsoft.CodeAnalysis.Testing.Utilities\Microsoft.CodeAnalysis.Testing.TestGenerators.AddFileWithCompileError\ErrorGeneratedFile.cs"", 1, 10, 1, 10),

";
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task AddSimpleFileVerifiesCompilerDiagnostics_VisualBasic()
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await new VisualBasicSourceGeneratorTest<AddFileWithCompileError>
                {
                    TestState =
                    {
                        Sources =
                        {
                            "Class A",
                        },
                        GeneratedSources =
                        {
                            (typeof(AddFileWithCompileError), "ErrorGeneratedFile.vb", "Class C"),
                        },
                    },
                }.RunAsync();
            });

            var expectedMessage = @"Mismatch between number of diagnostics returned, expected ""0"" actual ""2""

Diagnostics:
// /0/Test0.vb(1) : error BC30481: 'Class' statement must end with a matching 'End Class'.
DiagnosticResult.CompilerError(""BC30481"").WithSpan(1, 1, 1, 8),
// Microsoft.CodeAnalysis.Testing.Utilities\Microsoft.CodeAnalysis.Testing.TestGenerators.AddFileWithCompileError\ErrorGeneratedFile.vb(1) : error BC30481: 'Class' statement must end with a matching 'End Class'.
DiagnosticResult.CompilerError(""BC30481"").WithSpan(""Microsoft.CodeAnalysis.Testing.Utilities\Microsoft.CodeAnalysis.Testing.TestGenerators.AddFileWithCompileError\ErrorGeneratedFile.vb"", 1, 1, 1, 8),

";
            new DefaultVerifier().EqualOrDiff(expectedMessage, exception.Message);
        }

        [Fact]
        public async Task AddSimpleFileWithDiagnostic()
        {
            await new CSharpSourceGeneratorTest<AddEmptyFileWithDiagnostic>
            {
                TestState =
                {
                    Sources =
                    {
                        @"{|#0:|}// Comment",
                    },
                    GeneratedSources =
                    {
                        ("Microsoft.CodeAnalysis.Testing.Utilities\\Microsoft.CodeAnalysis.Testing.TestGenerators.AddEmptyFileWithDiagnostic\\EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.UTF8)),
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
            await new CSharpSourceGeneratorTest<AddEmptyFileWithDiagnostic>
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

        private class CSharpSourceGeneratorTest<TSourceGenerator> : SourceGeneratorTest<DefaultVerifier>
            where TSourceGenerator : ISourceGenerator, new()
        {
            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override ParseOptions CreateParseOptions()
            {
                return new CSharpParseOptions(CSharp.LanguageVersion.Default, DocumentationMode.Diagnose);
            }

            protected override IEnumerable<Type> GetSourceGenerators()
            {
                yield return typeof(TSourceGenerator);
            }
        }

        private class VisualBasicSourceGeneratorTest<TSourceGenerator> : SourceGeneratorTest<DefaultVerifier>
            where TSourceGenerator : ISourceGenerator, new()
        {
            public override string Language => LanguageNames.VisualBasic;

            protected override string DefaultFileExt => "vb";

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override ParseOptions CreateParseOptions()
            {
                return new VisualBasicParseOptions(VisualBasic.LanguageVersion.Default, DocumentationMode.Diagnose);
            }

            protected override IEnumerable<Type> GetSourceGenerators()
            {
                yield return typeof(TSourceGenerator);
            }
        }
    }
}
