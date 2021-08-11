// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    [CollectionDefinition(Name)]
    public class AssemblyLoadTestFixtureCollection : ICollectionFixture<AssemblyLoadTestFixture>
    {
        public const string Name = nameof(AssemblyLoadTestFixtureCollection);
        private AssemblyLoadTestFixtureCollection() { }
    }

    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public sealed class DefaultAnalyzerAssemblyLoaderTests : TestBase
    {
        private static readonly CSharpCompilationOptions s_dllWithMaxWarningLevel = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel);
        private readonly AssemblyLoadTestFixture _testFixture;
        public DefaultAnalyzerAssemblyLoaderTests(AssemblyLoadTestFixture testFixture)
        {
            _testFixture = testFixture;
        }

        [Fact, WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void LoadWithDependency()
        {
            var directory = Temp.CreateDirectory();
            var immutable = directory.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var microsoftCodeAnalysis = directory.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);

            var originalDirectory = directory.CreateDirectory("Analyzer");
            var analyzerDependencyFile = CreateAnalyzerDependency();
            var analyzerMainFile = CreateMainAnalyzerWithDependency(analyzerDependencyFile);
            var loader = new DefaultAnalyzerAssemblyLoader();
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

            TempFile CreateAnalyzerDependency()
            {
                var analyzerDependencySource = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class AbstractTestAnalyzer : DiagnosticAnalyzer
{
    protected static string SomeString = nameof(SomeString);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
    public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
}";

                var analyzerDependencyCompilation = CSharpCompilation.Create(
                    "AnalyzerDependency",
                    new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(analyzerDependencySource) },
                    new MetadataReference[]
                    {
                        TestMetadata.NetStandard20.mscorlib,
                        TestMetadata.NetStandard20.netstandard,
                        TestMetadata.NetStandard20.SystemRuntime,
                        MetadataReference.CreateFromFile(immutable.Path),
                        MetadataReference.CreateFromFile(microsoftCodeAnalysis.Path)
                    },
                    s_dllWithMaxWarningLevel);

                return originalDirectory.CreateFile("AnalyzerDependency.dll").WriteAllBytes(analyzerDependencyCompilation.EmitToArray());
            }

            TempFile CreateMainAnalyzerWithDependency(TempFile analyzerDependency)
            {
                var analyzerMainSource = @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAnalyzer : AbstractTestAnalyzer
{
    private static string SomeString2 = AbstractTestAnalyzer.SomeString;
}";
                var analyzerMainCompilation = CSharpCompilation.Create(
                   "AnalyzerMain",
                   new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(analyzerMainSource) },
                   new MetadataReference[]
                   {
                        TestMetadata.NetStandard20.mscorlib,
                        TestMetadata.NetStandard20.netstandard,
                        TestMetadata.NetStandard20.SystemRuntime,
                        MetadataReference.CreateFromFile(immutable.Path),
                        MetadataReference.CreateFromFile(microsoftCodeAnalysis.Path),
                        MetadataReference.CreateFromFile(analyzerDependency.Path)
                   },
                   s_dllWithMaxWarningLevel);

                return originalDirectory.CreateFile("AnalyzerMain.dll").WriteAllBytes(analyzerMainCompilation.EmitToArray());
            }
        }

        [Fact]
        public void AddDependencyLocationThrowsOnNull()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();

            Assert.Throws<ArgumentNullException>("fullPath", () => loader.AddDependencyLocation(null));
            Assert.Throws<ArgumentException>("fullPath", () => loader.AddDependencyLocation("a"));
        }

        [Fact]
        public void ThrowsForMissingFile()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");

            var loader = new DefaultAnalyzerAssemblyLoader();

            Assert.ThrowsAny<Exception>(() => loader.LoadFromPath(path));
        }

        [Fact]
        public void BasicLoad()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Alpha.Path);
            Assembly alpha = loader.LoadFromPath(_testFixture.Alpha.Path);

            Assert.NotNull(alpha);
        }

        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Alpha.Path);
            loader.AddDependencyLocation(_testFixture.Beta.Path);
            loader.AddDependencyLocation(_testFixture.Gamma.Path);
            loader.AddDependencyLocation(_testFixture.Delta1.Path);

            Assembly alpha = loader.LoadFromPath(_testFixture.Alpha.Path);

            var a = alpha.CreateInstance("Alpha.A")!;
            a.GetType().GetMethod("Write")!.Invoke(a, new object[] { sb, "Test A" });

            Assembly beta = loader.LoadFromPath(_testFixture.Beta.Path);

            var b = beta.CreateInstance("Beta.B")!;
            b.GetType().GetMethod("Write")!.Invoke(b, new object[] { sb, "Test B" });

            var expected = @"Delta: Gamma: Alpha: Test A
Delta: Gamma: Beta: Test B
";

            var actual = sb.ToString();

            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AssemblyLoading_AssemblyLocationNotAdded()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Gamma.Path);
            loader.AddDependencyLocation(_testFixture.Delta1.Path);
            Assert.Throws<FileNotFoundException>(() => loader.LoadFromPath(_testFixture.Beta.Path));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AssemblyLoading_DependencyLocationNotAdded()
        {
            StringBuilder sb = new StringBuilder();
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Gamma.Path);
            loader.AddDependencyLocation(_testFixture.Beta.Path);
            Assembly beta = loader.LoadFromPath(_testFixture.Beta.Path);

            var b = beta.CreateInstance("Beta.B")!;
            var writeMethod = b.GetType().GetMethod("Write")!;
            var exception = Assert.Throws<TargetInvocationException>(
                () => writeMethod.Invoke(b, new object[] { sb, "Test B" }));
            Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);

            var actual = sb.ToString();
            Assert.Equal(@"", actual);
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Gamma.Path);
            loader.AddDependencyLocation(_testFixture.Delta1.Path);
            loader.AddDependencyLocation(_testFixture.Epsilon.Path);
            loader.AddDependencyLocation(_testFixture.Delta2.Path);

            Assembly gamma = loader.LoadFromPath(_testFixture.Gamma.Path);
            var g = gamma.CreateInstance("Gamma.G")!;
            g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(_testFixture.Epsilon.Path);
            var e = epsilon.CreateInstance("Epsilon.E")!;
            e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

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
        public void AssemblyLoading_MultipleVersions_MissingVersion()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.Gamma.Path);
            loader.AddDependencyLocation(_testFixture.Delta1.Path);
            loader.AddDependencyLocation(_testFixture.Epsilon.Path);

            Assembly gamma = loader.LoadFromPath(_testFixture.Gamma.Path);
            var g = gamma.CreateInstance("Gamma.G")!;
            g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(_testFixture.Epsilon.Path);
            var e = epsilon.CreateInstance("Epsilon.E")!;
            var eWrite = e.GetType().GetMethod("Write")!;

            var actual = sb.ToString();
            if (ExecutionConditionUtil.IsCoreClr)
            {
                var exception = Assert.Throws<TargetInvocationException>(() => eWrite.Invoke(e, new object[] { sb, "Test E" }));
                Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);
            }
            else
            {
                eWrite.Invoke(e, new object[] { sb, "Test E" });
                Assert.Equal(
@"Delta: Gamma: Test G
",
                    actual);
            }
        }
    }
}
