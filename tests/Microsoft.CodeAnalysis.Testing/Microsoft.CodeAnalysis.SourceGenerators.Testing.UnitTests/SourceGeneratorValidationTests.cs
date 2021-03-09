// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing.TestGenerators;
using Microsoft.CodeAnalysis.Text;
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
                },
                FixedState =
                {
                    Sources =
                    {
                        @"// Comment",
                        ("Microsoft.CodeAnalysis.SourceGenerators.Testing.UnitTests\\Microsoft.CodeAnalysis.Testing.TestGenerators.AddEmptyFile\\EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.UTF8)),
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
                },
                FixedState =
                {
                    Sources =
                    {
                        @"// Comment",
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
                },
                FixedState =
                {
                    Sources =
                    {
                        @"// Comment",
                        (typeof(AddEmptyFile), "EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.UTF8)),
                    },
                },
            }.RunAsync();
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
                        @"// Comment",
                    },
                },
                FixedState =
                {
                    Sources =
                    {
                        @"{|#0:|}// Comment",
                        ("Microsoft.CodeAnalysis.SourceGenerators.Testing.UnitTests\\Microsoft.CodeAnalysis.Testing.TestGenerators.AddEmptyFileWithDiagnostic\\EmptyGeneratedFile.cs", SourceText.From(string.Empty, Encoding.UTF8)),
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

            protected override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators)
            {
                return CSharpGeneratorDriver.Create(
                    sourceGenerators,
                    project.AnalyzerOptions.AdditionalFiles,
                    (CSharpParseOptions)project.ParseOptions!,
                    project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            }

            protected override ParseOptions CreateParseOptions()
            {
                return new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);
            }

            protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
            {
                yield return new TSourceGenerator();
            }
        }
    }
}
