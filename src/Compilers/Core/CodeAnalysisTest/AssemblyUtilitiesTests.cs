// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: alphaDll.Path, actual: results[0]);
        }

        [Fact]
        public void FindAssemblySet_TwoUnrelatedAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: alphaDll.Path, actual: results[0]);
        }

        [Fact]
        public void FindAssemblySet_SimpleDependency()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 2, actual: results.Length);
            Assert.Contains(alphaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(gammaDll.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindAssemblySet_TransitiveDependencies()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 3, actual: results.Length);
            Assert.Contains(alphaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(gammaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(deltaDll.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadMVid()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var assembly = Assembly.Load(File.ReadAllBytes(alphaDll.Path));

            var result = AssemblyUtilities.ReadMvid(alphaDll.Path);

            Assert.Equal(expected: assembly.ManifestModule.ModuleVersionId, actual: result);
        }

        [Fact]
        public void FindSatelliteAssemblies_None()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void FindSatelliteAssemblies_DoesNotIncludeFileInSameDirectory()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void FindSatelliteAssemblies_OneLevelDown()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: satelliteFile.Path, actual: results[0], comparer: StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_TwoLevelsDown()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateDirectory("de").CreateDirectory("FakeAssembly.resources").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: satelliteFile.Path, actual: results[0], comparer: StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_MultipleAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFileDE = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll");
            var satelliteFileFR = directory.CreateDirectory("fr").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 2, actual: results.Length);
            Assert.Contains(satelliteFileDE.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(satelliteFileFR.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_WrongIntermediateDirectoryName()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateDirectory("de").CreateDirectory("OtherAssembly.resources").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void IdentifyMissingDependencies_OnlyMscorlibMissing()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            var results = AssemblyUtilities.IdentifyMissingDependencies(alphaDll.Path, new[] { alphaDll.Path, gammaDll.Path, deltaDll.Path });

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: "mscorlib", actual: results[0].Name);
        }

        [Fact]
        public void IdentifyMissingDependencies_MultipleMissing()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var results = AssemblyUtilities.IdentifyMissingDependencies(alphaDll.Path, new[] { alphaDll.Path }).Select(identity => identity.Name);

            Assert.Equal(expected: 2, actual: results.Count());
            Assert.Contains("mscorlib", results);
            Assert.Contains("Gamma", results);
        }

        [Fact]
        public void GetAssemblyIdentity()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var result = AssemblyUtilities.GetAssemblyIdentity(alphaDll.Path);

            Assert.Equal(expected: "Alpha", actual: result.Name);
        }
    }
}
