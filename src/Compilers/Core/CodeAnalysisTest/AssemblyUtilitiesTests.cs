// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: alphaDll.Path, actual: results[0]);
        }

        [Fact]
        public void FindAssemblySet_TwoUnrelatedAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Beta);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 1, actual: results.Length);
            Assert.Equal(expected: alphaDll.Path, actual: results[0]);
        }

        [Fact]
        public void FindAssemblySet_SimpleDependency()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Gamma);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 2, actual: results.Length);
            Assert.Contains(alphaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(gammaDll.Path, results, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void FindAssemblySet_TransitiveDependencies()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Delta);

            var results = AssemblyUtilities.FindAssemblySet(alphaDll.Path);

            Assert.Equal(expected: 3, actual: results.Length);
            Assert.Contains(alphaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(gammaDll.Path, results, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(deltaDll.Path, results, StringComparer.OrdinalIgnoreCase);
        }
    }
}
