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
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class AnalyzerConsistencyCheckerTests : TestBase
    {
        [Fact]
        public void MissingReference()
        {
            var directory = Temp.CreateDirectory();
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference("Alpha.dll"));
            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new InMemoryAssemblyLoader());

            Assert.False(result);
        }

        [Fact]
        public void AllChecksPassed()
        {
            var directory = Temp.CreateDirectory();
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Alpha.dll"),
                new CommandLineAnalyzerReference("Beta.dll"),
                new CommandLineAnalyzerReference("Gamma.dll"),
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new InMemoryAssemblyLoader());

            Assert.True(result);
        }

        [Fact]
        public void DifferingMvids()
        {
            var directory = Temp.CreateDirectory();

            // Load Beta.dll from the future Alpha.dll path to prime the assembly loader
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);

            var assemblyLoader = new InMemoryAssemblyLoader();
            var betaAssembly = assemblyLoader.LoadFromPath(alphaDll.Path);

            alphaDll.WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Alpha.dll"),
                new CommandLineAnalyzerReference("Gamma.dll"),
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, assemblyLoader);

            Assert.False(result);
        }

        [Fact]
        public void AssemblyLoadException()
        {
            var directory = Temp.CreateDirectory();
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            var analyzerReferences = ImmutableArray.Create(
                new CommandLineAnalyzerReference("Delta.dll"));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, TestAnalyzerAssemblyLoader.LoadNotImplemented);

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
                references: new MetadataReference[] { MetadataReference.CreateFromImage(TestMetadata.ResourcesNetStandard20.netstandard) },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var compFile = directory.CreateFile(name);
            comp.Emit(compFile.Path);


            var analyzerReferences = ImmutableArray.Create(new CommandLineAnalyzerReference(name));

            var result = AnalyzerConsistencyChecker.Check(directory.Path, analyzerReferences, new InMemoryAssemblyLoader());

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
