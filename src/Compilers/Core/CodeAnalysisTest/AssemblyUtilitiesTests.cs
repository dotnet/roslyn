// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public class AssemblyUtilitiesTests : TestBase
    {
        private readonly AssemblyLoadTestFixture _testFixture;

        public AssemblyUtilitiesTests(AssemblyLoadTestFixture testFixture)
        {
            _testFixture = testFixture;
        }

        [Fact]
        public void ReadMVid()
        {
            var assembly = Assembly.Load(File.ReadAllBytes(_testFixture.Alpha));

            var result = AssemblyUtilities.ReadMvid(_testFixture.Alpha);

            Assert.Equal(expected: assembly.ManifestModule.ModuleVersionId, actual: result);
        }

        [Fact]
        public void FindSatelliteAssemblies_None()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll").Path;

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile);

            Assert.Empty(results);
        }

        [Fact]
        public void FindSatelliteAssemblies_DoesNotIncludeFileInSameDirectory()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll").Path;
            var satelliteFile = directory.CreateFile("FakeAssembly.resources.dll").Path;

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile);

            Assert.Empty(results);
        }

        [Fact]
        public void FindSatelliteAssemblies_OneLevelDown()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll").Path;
            var satelliteFile = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll").Path;

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile);

            AssertEx.SetEqual(new[] { satelliteFile }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_TwoLevelsDown()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll").Path;
            var satelliteFile = directory.CreateDirectory("de").CreateDirectory("FakeAssembly.resources").CreateFile("FakeAssembly.resources.dll").Path;

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile);

            AssertEx.SetEqual(new[] { satelliteFile }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_MultipleAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll").Path;
            var satelliteFileDE = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll").Path;
            var satelliteFileFR = directory.CreateDirectory("fr").CreateFile("FakeAssembly.resources.dll").Path;

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile);

            AssertEx.SetEqual(new[] { satelliteFileDE, satelliteFileFR }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_WrongIntermediateDirectoryName()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll").Path;
            var satelliteFile = directory.CreateDirectory("de").CreateDirectory("OtherAssembly.resources").CreateFile("FakeAssembly.resources.dll").Path;

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile);

            Assert.Equal(expected: 0, actual: results.Length);
        }

        [Fact]
        public void IdentifyMissingDependencies_OnlyNetstandardMissing()
        {
            var results = AssemblyUtilities.IdentifyMissingDependencies(_testFixture.Alpha, new[] { _testFixture.Alpha, _testFixture.Gamma, _testFixture.Delta1 });

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: "netstandard", actual: results[0].Name);
        }

        [Fact]
        public void IdentifyMissingDependencies_MultipleMissing()
        {
            var results = AssemblyUtilities.IdentifyMissingDependencies(_testFixture.Alpha, new[] { _testFixture.Alpha }).Select(identity => identity.Name);

            AssertEx.SetEqual(new[] { "netstandard", "Gamma" }, results);
        }

        [Fact]
        public void GetAssemblyIdentity()
        {
            var result = AssemblyUtilities.GetAssemblyIdentity(_testFixture.Alpha);
            Assert.Equal(expected: "Alpha", actual: result.Name);
        }
    }
}
