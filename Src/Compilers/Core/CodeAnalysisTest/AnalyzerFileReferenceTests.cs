// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AnalyzerFileReferenceTests : TestBase
    {
        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();
            var directory = Temp.CreateDirectory();

            EventHandler<AnalyzerAssemblyLoadEventArgs> handler = (e, args) =>
            {
                var relativePath = args.Path.Substring(directory.Path.Length);
                sb.AppendFormat("Assembly {0} loaded from {1}", args.LoadedAssembly.FullName, relativePath);
                sb.AppendLine();
            };

            AnalyzerFileReference.AssemblyLoad += handler;

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Beta);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Delta);

            AnalyzerFileReference alphaReference = new AnalyzerFileReference(alphaDll.Path);
            Assembly alpha = alphaReference.GetAssembly();
            File.Delete(alphaDll.Path);

            var a = alpha.CreateInstance("Alpha.A");
            a.GetType().GetMethod("Write").Invoke(a, new object[] { sb, "Test A" });

            File.Delete(gammaDll.Path);
            File.Delete(deltaDll.Path);

            AnalyzerFileReference betaReference = new AnalyzerFileReference(betaDll.Path);
            Assembly beta = betaReference.GetAssembly();
            var b = beta.CreateInstance("Beta.B");
            b.GetType().GetMethod("Write").Invoke(b, new object[] { sb, "Test B" });

            var expected = @"Assembly Alpha, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Alpha.dll
Assembly Gamma, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Gamma.dll
Assembly Delta, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Delta.dll
Delta: Gamma: Alpha: Test A
Assembly Beta, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Beta.dll
Delta: Gamma: Beta: Test B
";

            var actual = sb.ToString();

            Assert.Equal(expected, actual);

            AnalyzerFileReference.AssemblyLoad -= handler;
        }
    }
}
