// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias PortableTestUtils;

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using AssertEx = PortableTestUtils::Roslyn.Test.Utilities.AssertEx;
using TestBase = PortableTestUtils::Roslyn.Test.Utilities.TestBase;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting.UnitTests
{
    public class CsiTests : TestBase
    {
        private static readonly string s_compilerVersion = CommonCompiler.GetProductVersion(typeof(Csi));
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

            var expected = $@"
{string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion)}
{CSharpScriptingResources.LogoLine2}

{ScriptingResources.HelpPrompt}
> > > > > > 1
> C {{ }}
> 
";
            // The German translation (and possibly others) contains an en dash (0x2013),
            // but csi.exe outputs it as a hyphen-minus (0x002d). We need to fix up the 
            // expected string before we can compare it to the actual output.
            expected = expected.Replace((char)0x2013, (char)0x002d); // EN DASH -> HYPHEN-MINUS
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, result.Output);

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
(1,7): error CS1504: {string.Format(CSharpResources.ERR_NoSourceFile, "a.csx", CSharpResources.CouldNotFindFile)}
(1,1): error CS0006: {string.Format(CSharpResources.ERR_NoMetadataFile, "C.dll")}
", result.Errors);

            Assert.Equal(0, result.ExitCode);
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

            var result = ProcessUtilities.Run(CsiPath, "/r:C.dll a.csx", workingDirectory: cwd.Path, additionalEnvironmentVars: new[] { KeyValuePairUtil.Create("LIB", dir.Path) });

            // error CS0006: Metadata file 'C.dll' could not be found
            Assert.True(result.Errors.StartsWith("error CS0006", StringComparison.Ordinal));
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

        [Fact]
        public void LineNumber_Information_On_Exception()
        {
            var source = @"Console.WriteLine(""OK"");
throw new Exception(""Error!"");
";

            var cwd = Temp.CreateDirectory();
            cwd.CreateFile("a.csx").WriteAllText(source);

            var result = ProcessUtilities.Run(CsiPath, "a.csx", workingDirectory: cwd.Path);

            Assert.True(result.ContainsErrors);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("OK", result.Output);
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
System.Exception: Error!
   + <Initialize>.MoveNext(){string.Format(ScriptingResources.AtFileLine, $"{cwd}{Path.DirectorySeparatorChar}a.csx", "2")}
", result.Errors);
        }
    }
}
