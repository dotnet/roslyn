// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ConsistencyCheckerTests : TestBase
    {
        [Fact]
        public void BadFileOnDisk()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var alphaAssembly = Assembly.Load(File.ReadAllBytes(alphaDll.Path));
            var badFile = directory.CreateFile("Alpha.dll.bad").WriteAllText("This is not an assembly");

            var consistencyChecker = new ConsistencyChecker(Enumerable.Empty<string>());
            var analyzerAssemblies = new Dictionary<string, Assembly>
            {
                { badFile.Path, alphaAssembly }
            };

            var errors = consistencyChecker.CheckAssemblies(analyzerAssemblies);

            Assert.Equal(expected: 1, actual: errors.Length);
            Assert.Equal(expected: ConsistencyErrorKind.UnableToReadFile, actual: errors[0].Kind);
        }

        [Fact]
        public void MissingReference()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var alphaAssembly = Assembly.Load(File.ReadAllBytes(alphaDll.Path));

            var consistencyChecker = new ConsistencyChecker(Enumerable.Empty<string>());
            var analyzerAssemblies = new Dictionary<string, Assembly>
            {
                { alphaDll.Path, alphaAssembly }
            };

            var errors = consistencyChecker.CheckAssemblies(analyzerAssemblies);

            Assert.Equal(expected: 2, actual: errors.Length);
            Assert.Equal(expected: ConsistencyErrorKind.MissingReference, actual: errors[0].Kind);
            Assert.Equal(expected: ConsistencyErrorKind.MissingReference, actual: errors[1].Kind);
        }

        [Fact]
        public void WhiteListPreventsMissingReferenceErrors()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var alphaAssembly = Assembly.Load(File.ReadAllBytes(alphaDll.Path));

            var consistencyChecker = new ConsistencyChecker(new[] { "mscorlib", "Gamma" });
            var analyzerAssemblies = new Dictionary<string, Assembly>
            {
                { alphaDll.Path, alphaAssembly }
            };

            var errors = consistencyChecker.CheckAssemblies(analyzerAssemblies);

            Assert.Equal(expected: 0, actual: errors.Length);
        }

        [Fact]
        public void LoadedAssemblyIsDifferent()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Beta);
            var betaAssembly = Assembly.Load(File.ReadAllBytes(betaDll.Path));

            var consistencyChecker = new ConsistencyChecker(new[] { "mscorlib", "Gamma" });
            var analyzerAssemblies = new Dictionary<string, Assembly>
            {
                { alphaDll.Path, betaAssembly }
            };

            var errors = consistencyChecker.CheckAssemblies(analyzerAssemblies);

            Assert.Equal(expected: 1, actual: errors.Length);
            Assert.Equal(expected: ConsistencyErrorKind.LoadedAssemblyDiffers, actual: errors[0].Kind);
        }

        [Fact]
        public void MultipleAssemblies()
        {
            var directory = Temp.CreateDirectory();

            var alphaDll = Temp.CreateDirectory().CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var betaDll = Temp.CreateDirectory().CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Beta);
            var gammaDll = Temp.CreateDirectory().CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Gamma);
            var deltaDll = Temp.CreateDirectory().CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Delta);

            var analyzerAssemblies = new Dictionary<string, Assembly>
            {
                { alphaDll.Path, Assembly.Load(File.ReadAllBytes(alphaDll.Path)) },
                { betaDll.Path, Assembly.Load(File.ReadAllBytes(betaDll.Path)) },
                { gammaDll.Path, Assembly.Load(File.ReadAllBytes(gammaDll.Path)) },
                { deltaDll.Path, Assembly.Load(File.ReadAllBytes(deltaDll.Path)) }
            };

            var consistencyChecker = new ConsistencyChecker(new[] { "mscorlib" });

            var errors = consistencyChecker.CheckAssemblies(analyzerAssemblies);

            Assert.Equal(expected: 0, actual: errors.Length);
        }
    }
}
