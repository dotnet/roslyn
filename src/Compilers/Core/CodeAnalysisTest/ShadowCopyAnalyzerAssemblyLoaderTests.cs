// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public sealed class ShadowCopyAnalyzerAssemblyLoaderTests : TestBase
    {
        private static readonly CSharpCompilationOptions s_dllWithMaxWarningLevel = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel);
        private readonly AssemblyLoadTestFixture _testFixture;
        public ShadowCopyAnalyzerAssemblyLoaderTests(AssemblyLoadTestFixture testFixture)
        {
            _testFixture = testFixture;
        }

        [Fact, WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void LoadWithDependency()
        {
            var analyzerDependencyFile = _testFixture.AnalyzerDependency;
            var analyzerMainFile = _testFixture.AnalyzerWithDependency;
            var loader = new ShadowCopyAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(analyzerDependencyFile.Path);

            var analyzerMainReference = new AnalyzerFileReference(analyzerMainFile.Path, loader);
            analyzerMainReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);
            var analyzerDependencyReference = new AnalyzerFileReference(analyzerDependencyFile.Path, loader);
            analyzerDependencyReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);

            var analyzers = analyzerMainReference.GetAnalyzersForAllLanguages();
            Assert.Equal(1, analyzers.Length);
            Assert.Equal("TestAnalyzer", analyzers[0].ToString());

            Assert.Equal(0, analyzerDependencyReference.GetAnalyzersForAllLanguages().Length);

            Assert.NotNull(analyzerDependencyReference.GetAssembly());
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new ShadowCopyAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Gamma.Path);
            loader.AddDependencyLocation(_testFixture.Delta1.Path);
            loader.AddDependencyLocation(_testFixture.Epsilon.Path);
            loader.AddDependencyLocation(_testFixture.Delta2.Path);

            Assembly gamma = loader.LoadFromPath(_testFixture.Gamma.Path);
            var g = gamma.CreateInstance("Gamma.G");
            g!.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(_testFixture.Epsilon.Path);
            var e = epsilon.CreateInstance("Epsilon.E");
            e!.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

            var actual = sb.ToString();
            if (ExecutionConditionUtil.IsCoreClr)
            {
                Assert.Equal(
@"Delta: Gamma: Test G
Delta.2: Epsilon: Test E
",
                    actual);
            }
            else
            {
                Assert.Equal(
@"Delta: Gamma: Test G
Delta: Epsilon: Test E
",
                    actual);
            }
        }

        [Fact]
        public void AssemblyLoading_Delete()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new ShadowCopyAnalyzerAssemblyLoader();

            var tempDir = Temp.CreateDirectory();
            var gammaCopy = tempDir.CreateFile("Gamma.dll").CopyContentFrom(_testFixture.Gamma.Path);
            var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(_testFixture.Delta1.Path);
            loader.AddDependencyLocation(deltaCopy.Path);
            loader.AddDependencyLocation(gammaCopy.Path);

            Assembly gamma = loader.LoadFromPath(gammaCopy.Path);
            var g = gamma.CreateInstance("Gamma.G");
            g!.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            File.Delete(gammaCopy.Path);
            File.Delete(deltaCopy.Path);

            var actual = sb.ToString();
            Assert.Equal(
@"Delta: Gamma: Test G
",
                actual);
        }
    }
}
