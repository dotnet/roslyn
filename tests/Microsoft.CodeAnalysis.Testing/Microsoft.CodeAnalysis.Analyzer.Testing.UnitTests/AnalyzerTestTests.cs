// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class AnalyzerTestTests
    {
        [Fact]
        public void TestDefaults()
        {
            var test = new CSharpTest();
            Assert.Null(test.TestState.InheritanceMode);
            Assert.Null(test.TestState.MarkupHandling);
            Assert.Empty(test.TestState.Sources);
            Assert.Empty(test.TestState.AdditionalFiles);
            Assert.Empty(test.TestState.AdditionalFilesFactories);
            Assert.Empty(test.TestState.ExpectedDiagnostics);
        }

        [Fact]
        public void TestSetTestCode()
        {
            var test = new CSharpTest { TestCode = "Test code" };
            Assert.Single(test.TestState.Sources);
            Assert.Equal("/0/Test0.cs", test.TestState.Sources[0].filename);
            Assert.Equal("Test code", test.TestState.Sources[0].content.ToString());
            Assert.Null(test.TestState.Sources[0].content.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, test.TestState.Sources[0].content.ChecksumAlgorithm);
        }

        [Fact]
        public void TestSetTestCodeTwice()
        {
            var test = new CSharpTest { TestCode = "Test code" };
            test.TestCode = "Test code";
            Assert.Equal(2, test.TestState.Sources.Count);

            Assert.Equal("/0/Test0.cs", test.TestState.Sources[0].filename);
            Assert.Equal("Test code", test.TestState.Sources[0].content.ToString());
            Assert.Null(test.TestState.Sources[0].content.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, test.TestState.Sources[0].content.ChecksumAlgorithm);

            Assert.Equal("/0/Test1.cs", test.TestState.Sources[1].filename);
            Assert.Equal("Test code", test.TestState.Sources[1].content.ToString());
            Assert.Null(test.TestState.Sources[1].content.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, test.TestState.Sources[1].content.ChecksumAlgorithm);
        }

        private class CSharpTest : AnalyzerTest<DefaultVerifier>
        {
            public override string Language => LanguageNames.CSharp;

            protected override string DefaultFileExt => "cs";

            protected override CompilationOptions CreateCompilationOptions()
                => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override ParseOptions CreateParseOptions()
                => new CSharpParseOptions(LanguageVersion.Default, DocumentationMode.Diagnose);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new NoActionAnalyzer();
            }
        }
    }
}
