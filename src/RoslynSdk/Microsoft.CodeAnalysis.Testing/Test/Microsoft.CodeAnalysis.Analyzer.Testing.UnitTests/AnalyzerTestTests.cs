// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Xunit;
using CSharpTest = Microsoft.CodeAnalysis.Testing.TestAnalyzers.CSharpAnalyzerTest<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer>;

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
    }
}
