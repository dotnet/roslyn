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
    public sealed class SimpleAnalyzerAssemblyLoaderTests : TestBase
    {
        [Fact]
        public void AddDependencyLocationThrowsOnNull()
        {
            var loader = new SimpleAnalyzerAssemblyLoader();

            Assert.Throws<ArgumentNullException>("fullPath", () => loader.AddDependencyLocation(null));
        }

        [Fact]
        public void ThrowsForMissingFile()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");

            var loader = new SimpleAnalyzerAssemblyLoader();

            Assert.ThrowsAny<Exception>(() => loader.LoadFromPath(path));
        }

        [Fact]
        public void BasicLoad()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var loader = new SimpleAnalyzerAssemblyLoader();

            Assembly alpha = loader.LoadFromPath(alphaDll.Path);

            Assert.NotNull(alpha);
        }

        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();
            var directory = Temp.CreateDirectory();

            var alphaDll = Temp.CreateDirectory().CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var betaDll = Temp.CreateDirectory().CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);
            var gammaDll = Temp.CreateDirectory().CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = Temp.CreateDirectory().CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta);

            var loader = new SimpleAnalyzerAssemblyLoader();
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
    }
}
