// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

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
        private readonly ITestOutputHelper _output;
        private readonly AssemblyLoadTestFixture _testFixture;
        public DefaultAnalyzerAssemblyLoaderTests(ITestOutputHelper output, AssemblyLoadTestFixture testFixture)
        {
            _output = output;
            _testFixture = testFixture;
        }

        [Fact, WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void LoadWithDependency()
        {
            var analyzerDependencyFile = _testFixture.AnalyzerDependency;
            var analyzerMainFile = _testFixture.AnalyzerWithDependency;
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
            // We don't pass Alpha's path to AddDependencyLocation here, and therefore expect
            // calling Beta.B.Write to fail.
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

#if NETCOREAPP
            var alcs = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader);
            Assert.Equal(2, alcs.Length);

            Assert.Equal(new[] {
                ("Delta", "1.0.0.0", _testFixture.Delta1.Path),
                ("Gamma", "0.0.0.0", _testFixture.Gamma.Path)
            }, alcs[0].Assemblies.Select(a => (a.GetName().Name!, a.GetName().Version!.ToString(), a.Location)).Order());

            Assert.Equal(new[] {
                ("Delta", "2.0.0.0", _testFixture.Delta2.Path),
                ("Epsilon", "0.0.0.0", _testFixture.Epsilon.Path)
            }, alcs[1].Assemblies.Select(a => (a.GetName().Name!, a.GetName().Version!.ToString(), a.Location)).Order());
#endif

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
        public void AssemblyLoading_MultipleVersions_MultipleLoaders()
        {
            StringBuilder sb = new StringBuilder();

            var loader1 = new DefaultAnalyzerAssemblyLoader();
            loader1.AddDependencyLocation(_testFixture.Gamma.Path);
            loader1.AddDependencyLocation(_testFixture.Delta1.Path);

            var loader2 = new DefaultAnalyzerAssemblyLoader();
            loader2.AddDependencyLocation(_testFixture.Epsilon.Path);
            loader2.AddDependencyLocation(_testFixture.Delta2.Path);

            Assembly gamma = loader1.LoadFromPath(_testFixture.Gamma.Path);
            var g = gamma.CreateInstance("Gamma.G")!;
            g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader2.LoadFromPath(_testFixture.Epsilon.Path);
            var e = epsilon.CreateInstance("Epsilon.E")!;
            e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

#if NETCOREAPP
            var alcs1 = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader1);
            Assert.Equal(1, alcs1.Length);

            Assert.Equal(new[] {
                ("Delta", "1.0.0.0", _testFixture.Delta1.Path),
                ("Gamma", "0.0.0.0", _testFixture.Gamma.Path)
            }, alcs1[0].Assemblies.Select(a => (a.GetName().Name!, a.GetName().Version!.ToString(), a.Location)).Order());

            var alcs2 = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader2);
            Assert.Equal(1, alcs2.Length);

            Assert.Equal(new[] {
                ("Delta", "2.0.0.0", _testFixture.Delta2.Path),
                ("Epsilon", "0.0.0.0", _testFixture.Epsilon.Path)
            }, alcs2[0].Assemblies.Select(a => (a.GetName().Name!, a.GetName().Version!.ToString(), a.Location)).Order());
#endif

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

        [Fact]
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_01()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.UserSystemCollectionsImmutable.Path);
            loader.AddDependencyLocation(_testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);

            Assembly analyzerAssembly = loader.LoadFromPath(_testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);
            var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;

            if (ExecutionConditionUtil.IsCoreClr)
            {
                var ex = Assert.ThrowsAny<Exception>(() => analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb }));
                Assert.True(ex is MissingMethodException or TargetInvocationException, $@"Unexpected exception type: ""{ex.GetType()}""");
            }
            else
            {
                analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
                Assert.Equal("42", sb.ToString());
            }
        }

        [Fact]
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_02()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.UserSystemCollectionsImmutable.Path);
            loader.AddDependencyLocation(_testFixture.AnalyzerReferencesSystemCollectionsImmutable2.Path);

            Assembly analyzerAssembly = loader.LoadFromPath(_testFixture.AnalyzerReferencesSystemCollectionsImmutable2.Path);
            var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
            analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
            Assert.Equal(ExecutionConditionUtil.IsCoreClr ? "1" : "42", sb.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly), typeof(CoreClrOnly))]
        public void AssemblyLoading_NativeDependency()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testFixture.AnalyzerWithNativeDependency.Path);

            Assembly analyzerAssembly = loader.LoadFromPath(_testFixture.AnalyzerWithNativeDependency.Path);
            var analyzer = analyzerAssembly.CreateInstance("Class1")!;
            var result = analyzer.GetType().GetMethod("GetFileAttributes")!.Invoke(analyzer, new[] { _testFixture.AnalyzerWithNativeDependency.Path });
            Assert.Equal(0, Marshal.GetLastWin32Error());
            Assert.Equal(FileAttributes.Archive, (FileAttributes)result!);
        }

        [Fact]
        public void AssemblyLoading_Delete()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();

            var tempDir = Temp.CreateDirectory();
            var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(_testFixture.Delta1.Path);
            loader.AddDependencyLocation(deltaCopy.Path);
            Assembly delta = loader.LoadFromPath(deltaCopy.Path);

            try
            {
                File.Delete(deltaCopy.Path);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            // The above call may or may not throw depending on the platform configuration.
            // If it doesn't throw, we might as well check that things are still functioning reasonably.

            var d = delta.CreateInstance("Delta.D");
            d!.GetType().GetMethod("Write")!.Invoke(d, new object[] { sb, "Test D" });

            var actual = sb.ToString();
            Assert.Equal(
@"Delta: Test D
",
                actual);
        }

#if NETCOREAPP
        [Fact]
        public void VerifyCompilerAssemblySimpleNames()
        {
            var caAssembly = typeof(Microsoft.CodeAnalysis.SyntaxNode).Assembly;
            var caReferences = caAssembly.GetReferencedAssemblies();
            var allReferenceSimpleNames = ArrayBuilder<string>.GetInstance();
            allReferenceSimpleNames.Add(caAssembly.GetName().Name ?? throw new InvalidOperationException());
            foreach (var reference in caReferences)
            {
                allReferenceSimpleNames.Add(reference.Name ?? throw new InvalidOperationException());
            }

            var csAssembly = typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode).Assembly;
            allReferenceSimpleNames.Add(csAssembly.GetName().Name ?? throw new InvalidOperationException());
            var csReferences = csAssembly.GetReferencedAssemblies();
            foreach (var reference in csReferences)
            {
                var name = reference.Name ?? throw new InvalidOperationException();
                if (!allReferenceSimpleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    allReferenceSimpleNames.Add(name);
                }
            }

            var vbAssembly = typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode).Assembly;
            var vbReferences = vbAssembly.GetReferencedAssemblies();
            allReferenceSimpleNames.Add(vbAssembly.GetName().Name ?? throw new InvalidOperationException());
            foreach (var reference in vbReferences)
            {
                var name = reference.Name ?? throw new InvalidOperationException();
                if (!allReferenceSimpleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    allReferenceSimpleNames.Add(name);
                }
            }

            if (!DefaultAnalyzerAssemblyLoader.CompilerAssemblySimpleNames.SetEquals(allReferenceSimpleNames))
            {
                allReferenceSimpleNames.Sort();
                var allNames = string.Join(",\r\n                ", allReferenceSimpleNames.Select(name => $@"""{name}"""));
                _output.WriteLine("        internal static readonly ImmutableHashSet<string> CompilerAssemblySimpleNames =");
                _output.WriteLine("            ImmutableHashSet.Create(");
                _output.WriteLine("                StringComparer.OrdinalIgnoreCase,");
                _output.WriteLine($"                {allNames});");
                allReferenceSimpleNames.Free();
                Assert.True(false, $"{nameof(DefaultAnalyzerAssemblyLoader)}.{nameof(DefaultAnalyzerAssemblyLoader.CompilerAssemblySimpleNames)} is not up to date. Paste in the standard output of this test to update it.");
            }
            else
            {
                allReferenceSimpleNames.Free();
            }
        }
#endif
    }
}
