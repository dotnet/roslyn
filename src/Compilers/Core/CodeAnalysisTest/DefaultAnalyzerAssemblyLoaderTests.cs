// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class DefaultAnalyzerAssemblyLoaderTests : TestBase
    {
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
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var loader = new DefaultAnalyzerAssemblyLoader();

            Assembly alpha = loader.LoadFromPath(alphaDll.Path);

            Assert.NotNull(alpha);
        }

        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta1);

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(alphaDll.Path);
            loader.AddDependencyLocation(betaDll.Path);
            loader.AddDependencyLocation(gammaDll.Path);
            loader.AddDependencyLocation(deltaDll.Path);

            Assembly alpha = loader.LoadFromPath(alphaDll.Path);

            var a = alpha.CreateInstance("Alpha.A");
            a.GetType().GetMethod("Write").Invoke(a, new object[] { sb, "Test A" });

            Assembly beta = loader.LoadFromPath(betaDll.Path);

            var b = beta.CreateInstance("Beta.B");
            b.GetType().GetMethod("Write").Invoke(b, new object[] { sb, "Test B" });

            var expected = @"Delta: Gamma: Alpha: Test A
Delta: Gamma: Beta: Test B
";

            var actual = sb.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions()
        {
            StringBuilder sb = new StringBuilder();

            var path1 = Temp.CreateDirectory();
            var gammaDll = path1.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var delta1Dll = path1.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta1);

            var path2 = Temp.CreateDirectory();
            var epsilonDll = path2.CreateFile("Epsilon.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Epsilon);
            var delta2Dll = path2.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta2);

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(gammaDll.Path);
            loader.AddDependencyLocation(delta1Dll.Path);
            loader.AddDependencyLocation(epsilonDll.Path);
            loader.AddDependencyLocation(delta2Dll.Path);

            Assembly gamma = loader.LoadFromPath(gammaDll.Path);
            var g = gamma.CreateInstance("Gamma.G");
            g.GetType().GetMethod("Write").Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(epsilonDll.Path);
            var e = epsilon.CreateInstance("Epsilon.E");
            e.GetType().GetMethod("Write").Invoke(e, new object[] { sb, "Test E" });

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
    }
}
