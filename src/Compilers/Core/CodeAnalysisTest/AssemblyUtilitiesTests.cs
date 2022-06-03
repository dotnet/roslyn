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
        public void FindAssemblySet_SingleAssembly()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CopyFile(_testFixture.Alpha.Path);
            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            AssertEx.SetEqual(new[] { alphaDll.Path }, results);
        }

        [Fact]
        public void FindAssemblySet_TwoUnrelatedAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CopyFile(_testFixture.Alpha.Path);
            var betaDll = directory.CopyFile(_testFixture.Beta.Path);
            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            AssertEx.SetEqual(new[] { alphaDll.Path }, results);
        }

        [Fact]
        public void FindAssemblySet_SimpleDependency()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CopyFile(_testFixture.Alpha.Path);
            var gammaDll = directory.CopyFile(_testFixture.Gamma.Path);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            AssertEx.SetEqual(new[] { alphaDll.Path, gammaDll.Path }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindAssemblySet_TransitiveDependencies()
        {
            var results = AssemblyUtilities.FindAssemblySet(_testFixture.Alpha.Path);

            AssertEx.SetEqual(new[]
            {
                _testFixture.Alpha.Path,
                _testFixture.Gamma.Path,
                _testFixture.Delta1.Path
            }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ReadMVid()
        {
            var assembly = Assembly.Load(File.ReadAllBytes(_testFixture.Alpha.Path));

            var result = AssemblyUtilities.ReadMvid(_testFixture.Alpha.Path);

            Assert.Equal(expected: assembly.ManifestModule.ModuleVersionId, actual: result);
        }

        [Fact]
        public void FindSatelliteAssemblies_None()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Empty(results);
        }

        [Fact]
        public void FindSatelliteAssemblies_DoesNotIncludeFileInSameDirectory()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            Assert.Empty(results);
        }

        [Fact]
        public void FindSatelliteAssemblies_OneLevelDown()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            AssertEx.SetEqual(new[] { satelliteFile.Path }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_TwoLevelsDown()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFile = directory.CreateDirectory("de").CreateDirectory("FakeAssembly.resources").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            AssertEx.SetEqual(new[] { satelliteFile.Path }, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindSatelliteAssemblies_MultipleAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var assemblyFile = directory.CreateFile("FakeAssembly.dll");
            var satelliteFileDE = directory.CreateDirectory("de").CreateFile("FakeAssembly.resources.dll");
            var satelliteFileFR = directory.CreateDirectory("fr").CreateFile("FakeAssembly.resources.dll");

            var results = AssemblyUtilities.FindSatelliteAssemblies(assemblyFile.Path);

            AssertEx.SetEqual(new[] { satelliteFileDE.Path, satelliteFileFR.Path }, results, StringComparer.OrdinalIgnoreCase);
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
        public void IdentifyMissingDependencies_OnlyNetstandardMissing()
        {
            var results = AssemblyUtilities.IdentifyMissingDependencies(_testFixture.Alpha.Path, new[] { _testFixture.Alpha.Path, _testFixture.Gamma.Path, _testFixture.Delta1.Path });

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: "netstandard", actual: results[0].Name);
        }

        [Fact]
        public void IdentifyMissingDependencies_MultipleMissing()
        {
            var results = AssemblyUtilities.IdentifyMissingDependencies(_testFixture.Alpha.Path, new[] { _testFixture.Alpha.Path }).Select(identity => identity.Name);

            AssertEx.SetEqual(new[] { "netstandard", "Gamma" }, results);
        }

        [Fact]
        public void GetAssemblyIdentity()
        {
            var result = AssemblyUtilities.GetAssemblyIdentity(_testFixture.Alpha.Path);
            Assert.Equal(expected: "Alpha", actual: result.Name);
        }
    }
}
