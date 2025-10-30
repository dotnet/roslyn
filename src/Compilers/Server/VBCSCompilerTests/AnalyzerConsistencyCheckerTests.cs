// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    [CollectionDefinition(Name)]
    public class AssemblyLoadTestFixtureCollection : ICollectionFixture<AssemblyLoadTestFixture>
    {
        public const string Name = nameof(AssemblyLoadTestFixtureCollection);
        private AssemblyLoadTestFixtureCollection() { }
    }

    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public class AnalyzerConsistencyCheckerTests : TestBase
    {
        private ICompilerServerLogger Logger { get; }
        private AssemblyLoadTestFixture TestFixture { get; }

        public AnalyzerConsistencyCheckerTests(ITestOutputHelper testOutputHelper, AssemblyLoadTestFixture testFixture)
        {
            Logger = new XunitCompilerServerLogger(testOutputHelper);
            TestFixture = testFixture;
        }

        private TempFile CreateNetStandardDll(TempDirectory directory, string assemblyName, string version, ImmutableArray<byte> publicKey, string? extraSource = null)
        {
            var source = $$"""
                using System;
                using System.Reflection;

                [assembly: AssemblyVersion("{{version}}")]
                [assembly: AssemblyFileVersion("{{version}}")]
                """;

            var sources = extraSource is null
                ? new[] { CSharpTestSource.Parse(source) }
                : new[] { CSharpTestSource.Parse(source), CSharpTestSource.Parse(extraSource) };

            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                warningLevel: Diagnostic.MaxWarningLevel,
                cryptoPublicKey: publicKey,
                deterministic: true,
                publicSign: true);

            var comp = CSharpCompilation.Create(
                assemblyName,
                sources,
                references: NetStandard20.References.All,
                options: options);

            var file = directory.CreateFile($"{assemblyName}.dll");
            var emitResult = comp.Emit(file.Path);
            Assert.Empty(emitResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error));
            return file;
        }

        /// <summary>
        /// Must support loading a DLL without all of it's references being present. It is common for analyzer
        /// assemblies to have missing references because customers often pair code fixers and analyzers into
        /// the same assembly.Here Alpha depends on Gamma which is not included 
        /// </summary>
        [Fact]
        public void LoadLibraryWithMissingReference()
        {
            var directory = Temp.CreateDirectory();
            _ = directory.CopyFile(TestFixture.Alpha);
            var assemblyLoader = AnalyzerAssemblyLoader.CreateNonLockingLoader(directory.CreateDirectory("shadow").Path);

            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference("Alpha.dll"));
            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);
            Assert.True(result);
        }

        [Fact]
        public void LoadLibraryAll()
        {
            var directory = Temp.CreateDirectory();
            var assemblyLoader = AnalyzerAssemblyLoader.CreateNonLockingLoader(directory.CreateDirectory("shadow").Path);
            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Alpha.dll"),
                new CommandLineAnalyzerReference("Beta.dll"),
                new CommandLineAnalyzerReference("Gamma.dll"),
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(Path.GetDirectoryName(TestFixture.Alpha)!, analyzerReferences, assemblyLoader, Logger);
            Assert.True(result);
        }

        [Fact]
        public void DifferingMvidsDifferentDirectory()
        {
            var directory = Temp.CreateDirectory();
            var assemblyLoader = AnalyzerAssemblyLoader.CreateNonLockingLoader(directory.CreateDirectory("shadow").Path);

            var key = NetStandard20.References.netstandard.GetAssemblyIdentity().PublicKey;
            var mvidAlpha1 = CreateNetStandardDll(directory.CreateDirectory("mvid1"), "MvidAlpha", "1.0.0.0", key, "class C { }");
            var mvidAlpha2 = CreateNetStandardDll(directory.CreateDirectory("mvid2"), "MvidAlpha", "1.0.0.0", key, "class D { }");

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference(mvidAlpha1.Path),
                new CommandLineAnalyzerReference(mvidAlpha2.Path));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);

#if NETFRAMEWORK
            Assert.False(result);
#else
            // In .NET Core assembly loading is partitioned per directory so it's possible to load the same 
            // simple name with different MVID
            Assert.True(result);
#endif
        }

        [Fact]
        public void DifferingMvidsSameDirectory()
        {
            var directory = Temp.CreateDirectory();
            var assemblyLoader = AnalyzerAssemblyLoader.CreateNonLockingLoader(directory.CreateDirectory("shadow").Path);

            var key = NetStandard20.References.netstandard.GetAssemblyIdentity().PublicKey;
            var mvidAlpha1 = CreateNetStandardDll(directory, "MvidAlpha", "1.0.0.0", key, "class C { }");

            var result = AnalyzerConsistencyChecker.Check(
                directory.Path,
                ImmutableArray.Create(new CommandLineAnalyzerReference(mvidAlpha1.Path)),
                assemblyLoader,
                Logger);
            Assert.True(result);

            File.Delete(mvidAlpha1.Path);
            var mvidAlpha2 = CreateNetStandardDll(directory, "MvidAlpha", "1.0.0.0", key, "class D { }");
            Assert.Equal(mvidAlpha1.Path, mvidAlpha2.Path);
            result = AnalyzerConsistencyChecker.Check(
                directory.Path,
                ImmutableArray.Create(new CommandLineAnalyzerReference(mvidAlpha2.Path)),
                assemblyLoader,
                Logger,
                out List<string>? errorMessages);
            Assert.False(result);
            Assert.NotNull(errorMessages);

            // Both the original and failed paths need to appear in the message, not the shadow copy 
            // paths
            var errorMessage = errorMessages!.Single();
            Assert.Contains(mvidAlpha1.Path, errorMessage);
            Assert.Contains(mvidAlpha2.Path, errorMessage);
        }

        /// <summary>
        /// A differing MVID is okay when it's loading a DLL from the compiler directory. That is 
        /// considered an exchange type. For example if an analyzer has a reference to System.Memory
        /// it will always load the copy the compiler used and that is not a consistency issue.
        /// </summary>
        [Fact]
        [WorkItem(64826, "https://github.com/dotnet/roslyn/issues/64826")]
        public void LoadingLibraryFromCompiler()
        {
            var directory = Temp.CreateDirectory();
            _ = CreateNetStandardDll(directory, "System.Memory", "2.0.0.0", NetStandard20.References.netstandard.GetAssemblyIdentity().PublicKey);

            // This test must use the DefaultAnalyzerAssemblyLoader as we want assembly binding redirects
            // to take affect here.
            var assemblyLoader = new AnalyzerAssemblyLoader();
            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("System.Memory.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);

            Assert.True(result);
        }

        /// <summary>
        /// A differing MVID is okay when it's loading a DLL from the GAC. There is no reason that 
        /// falling back to csc would change the load result.
        /// </summary>
        [Fact]
        [WorkItem(64826, "https://github.com/dotnet/roslyn/issues/64826")]
        public void LoadingLibraryFromRuntime()
        {
            var directory = Temp.CreateDirectory();
            var dllFile = directory.CreateFile("System.Core.dll");
            dllFile.WriteAllBytes(NetStandard20.Resources.SystemCore);

            // This test must use the DefaultAnalyzerAssemblyLoader as we want assembly binding redirects
            // to take affect here.
            var assemblyLoader = new AnalyzerAssemblyLoader();
            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("System.Core.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);

#if NETFRAMEWORK
            Assert.True(assemblyLoader.LoadFromPath(dllFile.Path).GlobalAssemblyCache);
#endif

            Assert.True(result);
        }

        [Fact]
        public void AssemblyLoadException()
        {
            var directory = Temp.CreateDirectory();
            directory.CopyFile(TestFixture.Delta1);

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new ThrowingLoader(), Logger);

            Assert.False(result);
        }

        [Fact]
        public void LoadingSimpleLibrary()
        {
            var directory = Temp.CreateDirectory();
            var key = NetStandard20.References.netstandard.GetAssemblyIdentity().PublicKey;
            var compFile = CreateNetStandardDll(directory, "netstandardRef", "1.0.0.0", key);

            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference(compFile.Path));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new AnalyzerAssemblyLoader(), Logger);

            Assert.True(result);
        }
    }

    file sealed class ThrowingLoader : IAnalyzerAssemblyLoaderInternal
    {
        public void AddDependencyLocation(string fullPath) { }
        public bool IsHostAssembly(Assembly assembly) => false;
        public Assembly LoadFromPath(string fullPath) => throw new Exception();
        public string? GetOriginalDependencyLocation(AssemblyName assembly) => throw new Exception();
        public void Dispose() { }
    }
}
