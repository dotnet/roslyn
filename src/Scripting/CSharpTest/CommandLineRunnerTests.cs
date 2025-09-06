// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.TestUtilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests
{
    using static TestCompilationFactory;

    public class CommandLineRunnerTests : CSharpScriptTestBase
    {
        private static readonly string s_compilerVersion = CommonCompiler.GetProductVersion(typeof(CSharpInteractiveCompiler));

        private string LogoAndHelpPrompt => $@"{string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion)}
{CSharpScriptingResources.LogoLine2}

{ScriptingResources.HelpPrompt}";

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Await()
        {
            var runner = CreateRunner(input:
@"async Task<int[]> GetStuffAsync()
{
  return new int[] { 1, 2, 3, 4, 5 };
}
from x in await GetStuffAsync()
where x > 2
select x * x
");
            runner.RunInteractive();

            var iteratorType = RuntimeUtilities.IsCoreClr9OrHigherRuntime
                ? "ArrayWhereSelectIterator"
                : "WhereSelectArrayIterator";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> async Task<int[]> GetStuffAsync()
. {{
.   return new int[] {{ 1, 2, 3, 4, 5 }};
. }}
> from x in await GetStuffAsync()
. where x > 2
. select x * x
Enumerable.{iteratorType}<int, int> {{ 9, 16, 25 }}
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "",
                runner.Console.Error.ToString());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17043")]
        [WorkItem("http://github.com/dotnet/roslyn/issues/7133")]
        public void TestDisplayResultsWithCurrentUICulture1()
        {
            // logoOutput needs to be retrieved before the runner is started, because the runner changes the culture to de-DE. 
            var logoOutput = $@"{string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion)}
{CSharpScriptingResources.LogoLine2}

{ScriptingResources.HelpPrompt}";
            var runner = CreateRunner(input:
@"using System.Globalization;
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""en-GB"", useUserOverride: false)
Math.PI
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""de-DE"", useUserOverride: false)
Math.PI
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{logoOutput}
> using System.Globalization;
> CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""en-GB"", useUserOverride: false)
[en-GB]
> Math.PI
3.1415926535897931
> CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""de-DE"", useUserOverride: false)
[de-DE]
> Math.PI
3,1415926535897931
>", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30924")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33564")]
        [WorkItem("http://github.com/dotnet/roslyn/issues/7133")]
        public void TestDisplayResultsWithCurrentUICulture2()
        {
            // logoOutput needs to be retrieved before the runner is started, because the runner changes the culture to de-DE. 
            var logoOutput = $@"{string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion)}
{CSharpScriptingResources.LogoLine2}

{ScriptingResources.HelpPrompt}";
            // Tests that DefaultThreadCurrentUICulture is respected and not DefaultThreadCurrentCulture.
            var runner = CreateRunner(input:
@"using System.Globalization;
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""en-GB"", useUserOverride: false)
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(""en-GB"", useUserOverride: false)
Math.PI
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(""de-DE"", useUserOverride: false)
Math.PI
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{logoOutput}
> using System.Globalization;
> CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""en-GB"", useUserOverride: false)
[en-GB]
> CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(""en-GB"", useUserOverride: false)
[en-GB]
> Math.PI
3.1415926535897931
> CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(""de-DE"", useUserOverride: false)
[de-DE]
> Math.PI
3.1415926535897931
>", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Void()
        {
            var runner = CreateRunner(input:
@"Print(1);
Print(2)
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> Print(1);
1
> Print(2)
2
> ", runner.Console.Out.ToString());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18479")]
        public void Tuples()
        {
            var runner = CreateRunner(input: "(1,2)");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> (1,2)
(1, 2)
> ", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Exception()
        {
            var runner = CreateRunner(input:
@"int div(int a, int b) => a/b;
div(10, 2)
div(10, 0)
");
            Assert.Equal(0, runner.RunInteractive());

            var exception = new DivideByZeroException();
            Assert.Equal(
$@"{LogoAndHelpPrompt}
> int div(int a, int b) => a/b;
> div(10, 2)
5
> div(10, 0)
«Red»
{exception.GetType()}: {exception.Message}
  + Submission#0.div(int, int)
«Gray»
> ", runner.Console.Out.ToString());

            Assert.Equal(
$@"{exception.GetType()}: {exception.Message}
  + Submission#0.div(int, int)
", runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void ExceptionInGeneric()
        {
            var runner = CreateRunner(input:
@"static class C<T> { public static int div<U>(int a, int b) => a/b; }
C<string>.div<bool>(10, 2)
C<string>.div<bool>(10, 0)
");
            Assert.Equal(0, runner.RunInteractive());

            var exception = new DivideByZeroException();
            Assert.Equal(
$@"{LogoAndHelpPrompt}
> static class C<T> {{ public static int div<U>(int a, int b) => a/b; }}
> C<string>.div<bool>(10, 2)
5
> C<string>.div<bool>(10, 0)
«Red»
{exception.GetType()}: {exception.Message}
  + Submission#0.C<T>.div<U>(int, int)
«Gray»
> ", runner.Console.Out.ToString());

            Assert.Equal(
$@"{exception.GetType()}: {exception.Message}
  + Submission#0.C<T>.div<U>(int, int)
", runner.Console.Error.ToString());
        }

        [Fact]
        public void Args_Interactive1()
        {
            var runner = CreateRunner(
                args: ["-i"],
                input: "1+1");

            runner.RunInteractive();

            Assert.Equal(
$@"{LogoAndHelpPrompt}
> 1+1
2
> ", runner.Console.Out.ToString());
        }

        [Fact]
        public void Args_Interactive2()
        {
            var runner = CreateRunner(
                args: ["/u:System", "/i", "--", "@arg1", "/arg2", "-arg3", "--arg4"],
                input: "foreach (var arg in Args) Print(arg);");

            runner.RunInteractive();

            var error = $@"error CS2001: {string.Format(CSharpResources.ERR_FileNotFound, Path.Combine(AppContext.BaseDirectory, "@arg1"))}";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        public void Args_InteractiveWithScript1()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var rsp = Temp.CreateFile(extension: ".rsp").WriteAllText($@"
/u:System
/i
""{script.Path}""
@arg1
/arg2
-arg3
--arg4");

            var runner = CreateRunner(
                args: [$@"@""{rsp.Path}""", "/arg5", "--", "/arg7"],
                input: "1");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"""@arg1""
""/arg2""
""-arg3""
""--arg4""
""/arg5""
""--""
""/arg7""
> 1
1
> ", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        public void Args_Script1()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: [script.Path, "arg1", "arg2", "arg3"]);

            Assert.True(runner.RunInteractive() == 0, userMessage: runner.Console.Error.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""arg1""
""arg2""
""arg3""
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        public void Args_Script2()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: [script.Path, "@arg1", "@arg2", "@arg3"]);

            Assert.True(runner.RunInteractive() == 0, userMessage: runner.Console.Error.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""@arg1""
""@arg2""
""@arg3""
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem("https://github.com/dotnet/roslyn/issues/15860")]
        public void Args_Script3()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var rsp = Temp.CreateFile(extension: ".rsp").WriteAllText($@"
/u:System
{script.Path}
--
@arg1
/arg2
-arg3
--arg4");

            var runner = CreateRunner(
                args: [$"@{rsp.Path}", "/arg5", "--", "/arg7"],
                input: "foreach (var arg in Args) Print(arg);");

            Assert.True(runner.RunInteractive() == 0, userMessage: runner.Console.Error.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""--""
""@arg1""
""/arg2""
""-arg3""
""--arg4""
""/arg5""
""--""
""/arg7""
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Args_Script4()
        {
            var script = Temp.CreateFile(prefix: "@", extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: ["--", script.Path, "@arg1", "@arg2", "@arg3"]);

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""@arg1""
""@arg2""
""@arg3""
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Args_Script5()
        {
            var dir = Temp.CreateDirectory();
            var script = dir.CreateFile("--").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: ["--", "--", "-", "--", "-"],
                workingDirectory: dir.Path);

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""-""
""--""
""-""
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Script_NonExistingFile()
        {
            var runner = CreateRunner(["a + b"]);

            Assert.Equal(1, runner.RunInteractive());

            var error = $@"error CS2001: {string.Format(CSharpResources.ERR_FileNotFound, Path.Combine(AppContext.BaseDirectory, "a + b"))}";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [Fact]
        public void Help()
        {
            var runner = CreateRunner(["/help"]);

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion)}
{CSharpScriptingResources.LogoLine2}

{CSharpScriptingResources.InteractiveHelp}
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Version()
        {
            var runner = CreateRunner(["/version"]);
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());

            runner = CreateRunner(["/version", "/help"]);
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());

            runner = CreateRunner(["/version", "/r:somefile"]);
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());

            runner = CreateRunner(["/version", "/nologo"]);
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Script_BadUsings()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("WriteLine(42);");

            var runner = CreateRunner(
            [
                GacFileResolver.IsAvailable ? null : "/r:System.Console",
                "/u:System.Console;Alpha.Beta",
                script.Path
            ]);

            Assert.Equal(1, runner.RunInteractive());

            var error = $@"error CS0246: {string.Format(CSharpResources.ERR_SingleTypeNameNotFound, "Alpha")}";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/29908")]
        public void Script_NoHostNamespaces()
        {
            var runner = CreateRunner(input: "nameof(Microsoft.Missing)");

            runner.RunInteractive();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> nameof(Microsoft.Missing)
«Red»
(1,8): error CS0234: {string.Format(CSharpResources.ERR_DottedTypeNameNotFoundInNS, "Missing", "Microsoft")}
«Gray»
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $"(1,8): error CS0234: {string.Format(CSharpResources.ERR_DottedTypeNameNotFoundInNS, "Missing", "Microsoft")}",
                runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void RelativePath()
        {
            using (var directory = new DisposableDirectory(Temp))
            {
                const string scriptName = "c.csx";
                var script = directory.CreateFile(scriptName).WriteAllText("Print(3);");
                var scriptPath = PathUtilities.CombinePathsUnchecked(PathUtilities.GetFileName(directory.Path), scriptName);
                var workingDirectory = PathUtilities.GetDirectoryName(directory.Path);
                Assert.False(PathUtilities.IsAbsolute(scriptPath));
                var runner = CreateRunner(
                    args: [scriptPath],
                    workingDirectory: workingDirectory);
                runner.RunInteractive();
                AssertEx.AssertEqualToleratingWhitespaceDifferences("3", runner.Console.Out.ToString());
            }
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData(null, null)]
        [InlineData("c:", null)]
        [InlineData("c:\\", null)]
        [InlineData("c:\\first", "c:\\")]
        [InlineData("c:\\first\\", "c:\\first")]
        [InlineData("c:\\first\\second", "c:\\first")]
        [InlineData("c:\\first\\second\\", "c:\\first\\second")]
        [InlineData("c:\\first\\second\\third", "c:\\first\\second")]
        [InlineData("\\", null)]
        [InlineData("\\first", "\\")]
        [InlineData("\\first\\", "\\first")]
        [InlineData("\\first\\second", "\\first")]
        [InlineData("\\first\\second\\", "\\first\\second")]
        [InlineData("\\first\\second\\third", "\\first\\second")]
        [InlineData("first", "")]
        [InlineData("first\\", "first")]
        [InlineData("first\\second", "first")]
        [InlineData("first\\second\\", "first\\second")]
        [InlineData("first\\second\\third", "first\\second")]
        public void TestGetDirectoryName_Windows(string path, string expectedOutput)
        {
            Assert.Equal(expectedOutput, PathUtilities.GetDirectoryName(path, isUnixLike: false));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void SourceSearchPaths1()
        {
            var main = Temp.CreateFile(extension: ".csx").WriteAllText(@"
#load ""1.csx""
#load ""2.csx""
#load ""3.csx""
Print(4);
");

            var dir1 = Temp.CreateDirectory();
            dir1.CreateFile("1.csx").WriteAllText(@"Print(1);");

            var dir2 = Temp.CreateDirectory();
            dir2.CreateFile("2.csx").WriteAllText(@"Print(2);");

            var dir3 = Temp.CreateDirectory();
            dir3.CreateFile("3.csx").WriteAllText(@"Print(3);");

            var runner = CreateRunner([$"/loadpath:{dir1.Path}", $"/loadpaths:{dir2.Path};{dir3.Path}", main.Path]);

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
1
2
3
4
", runner.Console.Out.ToString());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/26510")]
        public void ReferenceSearchPaths1()
        {
            var main = Temp.CreateFile(extension: ".csx").WriteAllText(@"
#r ""1.dll""
#r ""2.dll""
#r ""3.dll""
Print(new C1());
Print(new C2());
Print(new C3());
Print(new C4());
");

            var dir1 = Temp.CreateDirectory();
            dir1.CreateFile("1.dll").WriteAllBytes(CreateCSharpCompilationWithCorlib("public class C1 {}", "1").EmitToArray());

            var dir2 = Temp.CreateDirectory();
            dir2.CreateFile("2.dll").WriteAllBytes(CreateCSharpCompilationWithCorlib("public class C2 {}", "2").EmitToArray());

            var dir3 = Temp.CreateDirectory();
            dir3.CreateFile("3.dll").WriteAllBytes(CreateCSharpCompilationWithCorlib("public class C3 {}", "3").EmitToArray());

            var dir4 = Temp.CreateDirectory();
            dir4.CreateFile("4.dll").WriteAllBytes(CreateCSharpCompilationWithCorlib("public class C4 {}", "4").EmitToArray());

            var runner = CreateRunner(["/r:4.dll", $"/lib:{dir1.Path}", $"/libpath:{dir2.Path}", $"/libpaths:{dir3.Path};{dir4.Path}", main.Path]);

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
C1 { }
C2 { }
C3 { }
C4 { }
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void SourceSearchPaths_Change1()
        {
            var dir = Temp.CreateDirectory();
            var main = dir.CreateFile("a.csx").WriteAllText("int X = 1;");

            var runner = CreateRunner(input:
$@"SourcePaths
#load ""a.csx""
SourcePaths.Add(@""{dir.Path}"")
#load ""a.csx""
X
");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
{LogoAndHelpPrompt}
> SourcePaths
SearchPaths {{ }}
> #load ""a.csx""
«Red»
(1,7): error CS1504: {string.Format(CSharpResources.ERR_NoSourceFile, "a.csx", CSharpResources.CouldNotFindFile)}
«Gray»
> SourcePaths.Add(@""{dir.Path}"")
> #load ""a.csx""
> X
1
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"(1,7): error CS1504: {string.Format(CSharpResources.ERR_NoSourceFile, "a.csx", CSharpResources.CouldNotFindFile)}",
                runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void ReferenceSearchPaths_Change1()
        {
            var dir = Temp.CreateDirectory();
            var main = dir.CreateFile("C.dll").WriteAllBytes(TestResources.General.C1);

            var runner = CreateRunner(input:
$@"ReferencePaths
#r ""C.dll""
ReferencePaths.Add(@""{dir.Path}"")
#r ""C.dll""
new C()
");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
{LogoAndHelpPrompt}
> ReferencePaths
SearchPaths {{ }}
> #r ""C.dll""
«Red»
(1,1): error CS0006: {string.Format(CSharpResources.ERR_NoMetadataFile, "C.dll")}
«Gray»
> ReferencePaths.Add(@""{dir.Path}"")
> #r ""C.dll""
> new C()
C {{ }}
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"(1,1): error CS0006: {string.Format(CSharpResources.ERR_NoMetadataFile, "C.dll")}",
                runner.Console.Error.ToString());
        }

        [Fact]
        public void ResponseFile()
        {
            var rsp = Temp.CreateFile().WriteAllText(@"
/r:System
/r:System.Core
/r:System.Data
/r:System.Data.DataSetExtensions
/r:System.Xml
/r:System.Xml.Linq
/r:Microsoft.CSharp
/u:System
/u:System.Collections.Generic
/u:System.Linq
/u:System.Text");

            var csi = CreateRunner(["b.csx"], responseFile: rsp.Path);
            var arguments = ((CSharpInteractiveCompiler)csi.Compiler).Arguments;

            AssertEx.Equal(new[]
            {
                "System",
                "System.Core",
                "System.Data",
                "System.Data.DataSetExtensions",
                "System.Xml",
                "System.Xml.Linq",
                "Microsoft.CSharp",
            }, arguments.MetadataReferences.Select(r => r.Reference));

            AssertEx.Equal(new[]
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
            }, arguments.CompilationOptions.Usings.AsEnumerable());
        }

        [Fact]
        public void InitialScript1()
        {
            var init = Temp.CreateFile(extension: ".csx").WriteAllText(@"
int X = 1;
");
            var runner = CreateRunner(["/i", init.Path], input:
@"X");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
> X
1
> 
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void InitialScript_Error()
        {
            var reference = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.General.C1);

            var init = Temp.CreateFile(extension: ".csx").WriteAllText(@"
1 1
");
            var runner = CreateRunner([$@"/r:""{reference.Path}""", "/i", init.Path], input:
@"new C()");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
«Red»
{init.Path}(2,3): error CS1002: {CSharpResources.ERR_SemicolonExpected}
«Gray»
> new C()
C {{ }}
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"{init.Path}(2,3): error CS1002: {CSharpResources.ERR_SemicolonExpected}",
                runner.Console.Error.ToString());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/24402")]
        public void HelpCommand()
        {
            var runner = CreateRunner(input:
@"#help
");
            runner.RunInteractive();

            Assert.Equal(
$@"{LogoAndHelpPrompt}
> #help
{ScriptingResources.HelpText}
> ", runner.Console.Out.ToString());
        }

        [Fact]
        public void LangVersions()
        {
            var runner = CreateRunner(["/langversion:?"]);
            Assert.Equal(0, runner.RunInteractive());

            var expected = Enum.GetValues<LanguageVersion>()
                .Select(v => v.ToDisplayString());

            var actual = runner.Console.Out.ToString();
            var acceptableSurroundingChar = new[] { '\r', '\n', '(', ')', ' ' };
            foreach (var version in expected)
            {
                if (version == "latest")
                    continue;

                var foundIndex = actual.IndexOf(version);
                Assert.True(foundIndex > 0, $"Missing version '{version}'");
                Assert.True(Array.IndexOf(acceptableSurroundingChar, actual[foundIndex - 1]) >= 0);
                Assert.True(Array.IndexOf(acceptableSurroundingChar, actual[foundIndex + version.Length]) >= 0);
            }
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void SharedLibCopy_Different()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = TestCompilationFactory.CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var libBase2 = TestCompilationFactory.CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var lib1 = TestCompilationFactory.CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = TestCompilationFactory.CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var runner = CreateRunner(input:
$@"#r ""{file1.Path}""
var l1 = new Lib1();
#r ""{file2.Path}""
var l2 = new Lib2();
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> #r ""{file1.Path}""
> var l1 = new Lib1();
> #r ""{file2.Path}""
> var l2 = new Lib2();
«Red»
{string.Format(ScriptingResources.AssemblyAlreadyLoaded, libBaseName, "0.0.0.0", fileBase1.Path, fileBase2.Path)}
«Gray»
> ", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/6580")]
        public void PreservingDeclarationsOnException()
        {
            var runner = CreateRunner(input:
@"int i = 100;
int j = 20; throw new System.Exception(""Bang!""); int k = 3;
i + j + k
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> int i = 100;
> int j = 20; throw new System.Exception(""Bang!""); int k = 3;
«Yellow»
(1,58): warning CS0162: {CSharpResources.WRN_UnreachableCode}
«Red»
System.Exception: Bang!
«Gray»
> i + j + k
120
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"(1,58): warning CS0162: {CSharpResources.WRN_UnreachableCode}
System.Exception: Bang!",
                runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/21327")]
        public void DefaultLiteral()
        {
            var runner = CreateRunner(input:
@"int i = default;
Print(i);
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> int i = default;
> Print(i);
0
> ", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/21327")]
        public void InferredTupleNames()
        {
            var runner = CreateRunner(input:
@"var a = 1;
var t = (a, 2);
Print(t.a);
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion)}
{CSharpScriptingResources.LogoLine2}
{ScriptingResources.HelpPrompt}
> var a = 1;
> var t = (a, 2);
> Print(t.a);
1
> ", runner.Console.Out.ToString());
        }
    }
}
