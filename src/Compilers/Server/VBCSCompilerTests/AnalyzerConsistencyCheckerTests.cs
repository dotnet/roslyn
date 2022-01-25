// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            // Load Beta.dll from the future Alpha.dll path to prime the assembly loader
            var alphaDll = directory.CopyFile(TestFixture.Beta.Path, name: "Alpha.dll");

            var assemblyLoader = new InMemoryAssemblyLoader();
            var betaAssembly = assemblyLoader.LoadFromPath(alphaDll.Path);

            // now overwrite the {directory}/Alpha.dll file with the content from our Alpha.dll test resource
            alphaDll.CopyContentFrom(TestFixture.Alpha.Path);
            directory.CopyFile(TestFixture.Gamma.Path);
            directory.CopyFile(TestFixture.Delta1.Path);

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Alpha.dll"),
                new CommandLineAnalyzerReference("Gamma.dll"),
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader, Logger);

            Assert.False(result);
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
        public void NetstandardIgnored()
        {
            var directory = Temp.CreateDirectory();
            const string name = "netstandardRef";
            var comp = CSharpCompilation.Create(
                name,
                new[] { SyntaxFactory.ParseSyntaxTree(@"class C {}") },
                references: new MetadataReference[] { NetStandard20.netstandard },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: Diagnostic.MaxWarningLevel));
            var compFile = directory.CreateFile(name);
            comp.Emit(compFile.Path);


            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference(name));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new InMemoryAssemblyLoader(), Logger);

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
