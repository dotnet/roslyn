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

            AssertEx.ThrowsArgumentNull("fullPath", () => loader.AddDependencyLocation(null));
        }

        [Fact]
        public void ThrowsForMissingFile()
        {
            var path = Path.Combine(Temp.CreateDirectory().Path, Path.GetRandomFileName() + ".dll");

            var loader = new SimpleAnalyzerAssemblyLoader();

            AssertEx.Throws<Exception>(() => loader.LoadFromPath(path), allowDerived: true);
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
    }
}
