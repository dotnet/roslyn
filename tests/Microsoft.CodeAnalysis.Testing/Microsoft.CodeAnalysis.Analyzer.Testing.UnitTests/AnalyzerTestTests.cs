// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Assert.Equal(StateInheritanceMode.Explicit, test.TestState.InheritanceMode);
            Assert.Equal(MarkupMode.Allow, test.TestState.AllowMarkup);
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
            Assert.Equal("Test0.cs", test.TestState.Sources[0].filename);
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

            Assert.Equal("Test0.cs", test.TestState.Sources[0].filename);
            Assert.Equal("Test code", test.TestState.Sources[0].content.ToString());
            Assert.Null(test.TestState.Sources[0].content.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, test.TestState.Sources[0].content.ChecksumAlgorithm);

            Assert.Equal("Test1.cs", test.TestState.Sources[1].filename);
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

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new NoActionAnalyzer();
            }
        }
    }
}
