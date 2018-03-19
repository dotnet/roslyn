// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyUtilitiesTests : TestBase
    {
        [Fact]
        public void FindAssemblySet_SingleAssembly()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: alphaDll.Path, actual: results[0]);
        }

        [Fact]
        public void FindAssemblySet_TwoUnrelatedAssemblies()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            CodeAnalysis.Test.Utilities.TempFile betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: alphaDll.Path, actual: results[0]);
        }

        [Fact]
        public void FindAssemblySet_SimpleDependency()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            CodeAnalysis.Test.Utilities.TempFile gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 2, actual: results.Length);
            Assert.Contains(alphaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(gammaDll.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindAssemblySet_TransitiveDependencies()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            CodeAnalysis.Test.Utilities.TempFile gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            CodeAnalysis.Test.Utilities.TempFile deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 3, actual: results.Length);
            Assert.Contains(alphaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(gammaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(deltaDll.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadMVid()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var assembly = Assembly.Load(File.ReadAllBytes(alphaDll.Path));

            Guid result = AssemblyUtilities.ReadMvid(alphaDll.Path);

            Assert.Equal(expected: assembly.ManifestModule.ModuleVersionId, actual: result);
        }

        [Fact]
        public void FindSatelliteAssemblies_None()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile assemblyFile = directory.CreateFile("FakeAssembly.dll");

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void FindSatelliteAssemblies_DoesNotIncludeFileInSameDirectory()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile assemblyFile = directory.CreateFile("FakeAssembly.dll");
            CodeAnalysis.Test.Utilities.TempFile satelliteFile = directory.CreateFile("FakeAssembly.resources.dll");

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void FindSatelliteAssemblies_OneLevelDown()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile assemblyFile = directory.CreateFile("FakeAssembly.dll");
            CodeAnalysis.Test.Utilities.TempFile satelliteFile = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll");

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: satelliteFile.Path, actual: results[0], comparer: StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_TwoLevelsDown()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile assemblyFile = directory.CreateFile("FakeAssembly.dll");
            CodeAnalysis.Test.Utilities.TempFile satelliteFile = directory.CreateDirectory("de").CreateDirectory("FakeAssembly.resources").CreateFile("FakeAssembly.resources.dll");

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: satelliteFile.Path, actual: results[0], comparer: StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_MultipleAssemblies()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile assemblyFile = directory.CreateFile("FakeAssembly.dll");
            CodeAnalysis.Test.Utilities.TempFile satelliteFileDE = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll");
            CodeAnalysis.Test.Utilities.TempFile satelliteFileFR = directory.CreateDirectory("fr").CreateFile("FakeAssembly.resources.dll");

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 2, actual: results.Length);
            Assert.Contains(satelliteFileDE.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(satelliteFileFR.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_WrongIntermediateDirectoryName()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile assemblyFile = directory.CreateFile("FakeAssembly.dll");
            CodeAnalysis.Test.Utilities.TempFile satelliteFile = directory.CreateDirectory("de").CreateDirectory("OtherAssembly.resources").CreateFile("FakeAssembly.resources.dll");

            System.Collections.Immutable.ImmutableArray<string> results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void IdentifyMissingDependencies_OnlyMscorlibMissing()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            CodeAnalysis.Test.Utilities.TempFile gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            CodeAnalysis.Test.Utilities.TempFile deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            System.Collections.Immutable.ImmutableArray<AssemblyIdentity> results = AssemblyUtilities.IdentifyMissingDependencies(alphaDll.Path, new[] { alphaDll.Path, gammaDll.Path, deltaDll.Path });

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: "mscorlib", actual: results[0].Name);
        }

        [Fact]
        public void IdentifyMissingDependencies_MultipleMissing()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            IEnumerable<string> results = AssemblyUtilities.IdentifyMissingDependencies(alphaDll.Path, new[] { alphaDll.Path }).Select(identity => identity.Name);

            Assert.Equal(expected: 2, actual: results.Count());
            Assert.Contains("mscorlib", results);
            Assert.Contains("Gamma", results);
        }

        [Fact]
        public void GetAssemblyIdentity()
        {
            CodeAnalysis.Test.Utilities.TempDirectory directory = Temp.CreateDirectory();

            CodeAnalysis.Test.Utilities.TempFile alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            AssemblyIdentity result = AssemblyUtilities.GetAssemblyIdentity(alphaDll.Path);

            Assert.Equal(expected: "Alpha", actual: result.Name);
        }
    }
}
