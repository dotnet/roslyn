// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PortableTestUtils;

using System;
using System.Reflection;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using AssertEx = PortableTestUtils::Roslyn.Test.Utilities.AssertEx;
using TestBase = PortableTestUtils::Roslyn.Test.Utilities.TestBase;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests
{
    public class CsiTests : TestBase
    {
        private static readonly string s_compilerVersion = typeof(Csi).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        private string CsiPath => typeof(Csi).GetTypeInfo().Assembly.Location;

        /// <summary>
        /// csi should use the current working directory of its environment to resolve relative paths specified on command line.
        /// </summary>
        [Fact]
        public void CurrentWorkingDirectory1()
        {
            var dir = Temp.CreateDirectory();
            dir.CreateFile("a.csx").WriteAllText(@"Console.Write(Environment.CurrentDirectory + ';' + typeof(C).Name);");
            dir.CreateFile("C.dll").WriteAllBytes(TestResources.General.C1);

            var result = ProcessUtilities.Run(CsiPath, "/r:C.dll a.csx", workingDirectory: dir.Path);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(dir.Path + ";C", result.Output);
            Assert.False(result.ContainsErrors);
        }

        [Fact]
        public void CurrentWorkingDirectory_Change()
        {
            var dir = Temp.CreateDirectory();
            dir.CreateFile("a.csx").WriteAllText(@"int X = 1;");
            dir.CreateFile("C.dll").WriteAllBytes(TestResources.General.C1);

            var result = ProcessUtilities.Run(CsiPath, "", stdInput:
$@"#load ""a.csx""
#r ""C.dll""
Directory.SetCurrentDirectory(@""{dir.Path}"")
#load ""a.csx""
#r ""C.dll""
X
new C()
Environment.Exit(0)
");

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> (1,7): error CS1504: Source file 'a.csx' could not be opened -- Could not find file.
> (1,1): error CS0006: Metadata file 'C.dll' could not be found
> > > > 1
> C {{ }}
> 
", result.Output);

            Assert.False(result.ContainsErrors);
        }

        /// <summary>
        /// csi does NOT use LIB environment variable to populate reference search paths.
        /// </summary>
        [Fact]
        public void ReferenceSearchPaths_LIB()
        {
            var cwd = Temp.CreateDirectory();
            cwd.CreateFile("a.csx").WriteAllText(@"Console.Write(typeof(C).Name);");

            var dir = Temp.CreateDirectory();
            dir.CreateFile("C.dll").WriteAllBytes(TestResources.General.C1);

            var result = ProcessUtilities.Run(CsiPath, "/r:C.dll a.csx", workingDirectory: cwd.Path, additionalEnvironmentVars: new[] { KeyValuePair.Create("LIB", dir.Path) });

            // error CS0006: Metadata file 'C.dll' could not be found
            Assert.True(result.Output.StartsWith("error CS0006", StringComparison.Ordinal));
            Assert.True(result.ContainsErrors);
        }

        /// <summary>
        /// csi does use SDK path (FX dir)
        /// </summary>
        [Fact]
        public void ReferenceSearchPaths_Sdk()
        {
            var cwd = Temp.CreateDirectory();
            cwd.CreateFile("a.csx").WriteAllText(@"Console.Write(typeof(DataSet).Name);");

            var result = ProcessUtilities.Run(CsiPath, "/r:System.Data.dll /u:System.Data;System a.csx", workingDirectory: cwd.Path);

            AssertEx.AssertEqualToleratingWhitespaceDifferences("DataSet", result.Output);
            Assert.False(result.ContainsErrors);
        }

        [Fact]
        public void DefaultUsings()
        {
            var source = @"
dynamic d = new ExpandoObject();
Process p = new Process();
Expression<Func<int>> e = () => 1;
var squares = from x in new[] { 1, 2, 3 } select x * x;
var sb = new StringBuilder();
var list = new List<int>();
var stream = new MemoryStream();
await Task.Delay(10);

Console.Write(""OK"");
";

            var cwd = Temp.CreateDirectory();
            cwd.CreateFile("a.csx").WriteAllText(source);

            var result = ProcessUtilities.Run(CsiPath, "a.csx", workingDirectory: cwd.Path);

            AssertEx.AssertEqualToleratingWhitespaceDifferences("OK", result.Output);
            Assert.False(result.ContainsErrors);
        }
    }
}
