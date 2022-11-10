// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFRAMEWORK
#nullable disable

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
                references: NetStandard20.All,
                options: options);

            var file = directory.CreateFile($"{assemblyName}.dll");
            var emitResult = comp.Emit(file.Path);
            Assert.Empty(emitResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error));
            return file;
        }

        [Fact]
        public void MissingReference()
        {
            var directory = Temp.CreateDirectory();
            var alphaDll = directory.CopyFile(TestFixture.Alpha.Path);

            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference("Alpha.dll"));
            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new InMemoryAssemblyLoader(), Logger);

            Assert.True(result);
        }

        [Fact]
        public void AllChecksPassed()
        {
            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Alpha.dll"),
                new CommandLineAnalyzerReference("Beta.dll"),
                new CommandLineAnalyzerReference("Gamma.dll"),
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(Path.GetDirectoryName(TestFixture.Alpha.Path), analyzerReferences, new InMemoryAssemblyLoader(), Logger);
            Assert.True(result);
        }

        [Fact]
        public void DifferingMvids()
        {
            var directory = Temp.CreateDirectory();

            var key = NetStandard20.netstandard.GetAssemblyIdentity().PublicKey;
            var mvidAlpha1 = CreateNetStandardDll(directory.CreateDirectory("mvid1"), "MvidAlpha", "1.0.0.0", key, "class C { }");
            var mvidAlpha2 = CreateNetStandardDll(directory.CreateDirectory("mvid2"), "MvidAlpha", "1.0.0.0", key, "class D { }");

            // Can't use InMemoryAssemblyLoader because that uses the None context which fakes paths
            // to always be the currently executing application. That makes it look like everything 
            // is in the same directory
            var assemblyLoader = new DefaultAnalyzerAssemblyLoader();
            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference(mvidAlpha1.Path),
                new CommandLineAnalyzerReference(mvidAlpha2.Path));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);
            Assert.False(result);
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
            var dllFile = CreateNetStandardDll(directory, "System.Memory", "2.0.0.0", NetStandard20.netstandard.GetAssemblyIdentity().PublicKey);

            // This test must use the DefaultAnalyzerAssemblyLoader as we want assembly binding redirects
            // to take affect here.
            var assemblyLoader = new DefaultAnalyzerAssemblyLoader();
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
        public void LoadingLibraryFromGAC()
        {
            var directory = Temp.CreateDirectory();
            var dllFile = directory.CreateFile("System.Core.dll");
            dllFile.WriteAllBytes(NetStandard20.Resources.SystemCore);

            // This test must use the DefaultAnalyzerAssemblyLoader as we want assembly binding redirects
            // to take affect here.
            var assemblyLoader = new DefaultAnalyzerAssemblyLoader();
            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("System.Core.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);

            Assert.True(result);
        }

        [Fact]
        public void AssemblyLoadException()
        {
            var directory = Temp.CreateDirectory();
            directory.CopyFile(TestFixture.Delta1.Path);

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, TestAnalyzerAssemblyLoader.LoadNotImplemented, Logger);

            Assert.False(result);
        }

        [Fact]
        public void LoadingSimpleLibrary()
        {
            var directory = Temp.CreateDirectory();
            var key = NetStandard20.netstandard.GetAssemblyIdentity().PublicKey;
            var compFile = CreateNetStandardDll(directory, "netstandardRef", "1.0.0.0", key);

            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference(compFile.Path));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new DefaultAnalyzerAssemblyLoader(), Logger);

            Assert.True(result);
        }

        private class InMemoryAssemblyLoader : IAnalyzerAssemblyLoader
        {
            private readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                Assembly assembly;
                if (!_assemblies.TryGetValue(fullPath, out assembly))
                {
                    var bytes = File.ReadAllBytes(fullPath);
                    assembly = Assembly.Load(bytes);
                    _assemblies[fullPath] = assembly;
                }

                return assembly;
            }
        }
    }
}
#endif
