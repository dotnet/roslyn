// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
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
                    GeneratedSources = { (typeof(GenerateSourceFile), "Generated.g.cs", "content not yet validated") },
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
                    GeneratedSources = { (typeof(GenerateSourceFile), "Generated.g.vb", "content not yet validated") },
                },
            }.RunAsync();
        }

        [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
        internal class GenerateSourceFile : ISourceGenerator
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
