// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests
{
    using static TestCompilationFactory;

    public class CommandLineRunnerTests : TestBase
    {
        private static readonly string s_compilerVersion = CommonCompiler.GetProductVersion(typeof(CSharpInteractiveCompiler));

        private string LogoAndHelpPrompt => $@"{ string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion) }
{CSharpScriptingResources.LogoLine2}

{ScriptingResources.HelpPrompt}";

        // default csi.rsp
        private static readonly string[] s_defaultArgs = new[]
        {
            "/r:" + string.Join(";", GetReferences()),
            "/u:System;System.IO;System.Collections.Generic;System.Diagnostics;System.Dynamic;System.Linq;System.Linq.Expressions;System.Text;System.Threading.Tasks",
        };

        private static IEnumerable<string> GetReferences()
        {
            if (GacFileResolver.IsAvailable)
            {
                // keep in sync with list in csi.rsp
                yield return "System";
                yield return "System.Core";
                yield return "Microsoft.CSharp";
            }
            else
            {
                // keep in sync with list in core csi.rsp
                yield return "System.Collections";
                yield return "System.Collections.Concurrent";
                yield return "System.Console";
                yield return "System.Diagnostics.Debug";
                yield return "System.Diagnostics.Process";
                yield return "System.Diagnostics.StackTrace";
                yield return "System.Globalization";
                yield return "System.IO";
                yield return "System.IO.FileSystem";
                yield return "System.IO.FileSystem.Primitives";
                yield return "System.Reflection";
                yield return "System.Reflection.Extensions";
                yield return "System.Reflection.Primitives";
                yield return "System.Runtime";
                yield return "System.Runtime.Extensions";
                yield return "System.Runtime.InteropServices";
                yield return "System.Text.Encoding";
                yield return "System.Text.Encoding.CodePages";
                yield return "System.Text.Encoding.Extensions";
                yield return "System.Text.RegularExpressions";
                yield return "System.Threading";
                yield return "System.Threading.Tasks";
                yield return "System.Threading.Tasks.Parallel";
                yield return "System.Threading.Thread";
                yield return "System.Linq";
                yield return "System.Linq.Expressions";
                yield return "System.Runtime.Numerics";
                yield return "System.Dynamic.Runtime";
                yield return "Microsoft.CSharp";
            }
        }

        private static CommandLineRunner CreateRunner(
            string[] args = null,
            string input = "",
            string responseFile = null,
            string workingDirectory = null)
        {
            var io = new TestConsoleIO(input);
            var clientDir = Path.GetDirectoryName(RuntimeUtilities.GetAssemblyLocation(typeof(CommandLineRunnerTests)));
            var buildPaths = new BuildPaths(
                clientDir: clientDir,
                workingDir: workingDirectory ?? clientDir,
                sdkDir: null,
                tempDir: Path.GetTempPath());

            var compiler = new CSharpInteractiveCompiler(
                responseFile,
                buildPaths,
                args?.Where(a => a != null).ToArray() ?? s_defaultArgs,
                new NotImplementedAnalyzerLoader());

            return new CommandLineRunner(io, compiler, CSharpScriptCompiler.Instance, CSharpObjectFormatter.Instance);
        }

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

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> async Task<int[]> GetStuffAsync()
. {{
.   return new int[] {{ 1, 2, 3, 4, 5 }};
. }}
«Yellow»
(1,19): warning CS1998: { CSharpResources.WRN_AsyncLacksAwaits }
«Gray»
> from x in await GetStuffAsync()
. where x > 2
. select x * x
Enumerable.WhereSelectArrayIterator<int, int> {{ 9, 16, 25 }}
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"(1,19): warning CS1998: { CSharpResources.WRN_AsyncLacksAwaits }",
                runner.Console.Error.ToString());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17043")]
        [WorkItem(7133, "http://github.com/dotnet/roslyn/issues/7133")]
        public void TestDisplayResultsWithCurrentUICulture1()
        {
            // logoOutput needs to be retrieved before the runner is started, because the runner changes the culture to de-DE. 
            var logoOutput = $@"{ string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion) }
{ CSharpScriptingResources.LogoLine2}

{ ScriptingResources.HelpPrompt}";
            var runner = CreateRunner(input:
@"using System.Globalization;
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""en-GB"", useUserOverride: false)
Math.PI
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(""de-DE"", useUserOverride: false)
Math.PI
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{ logoOutput }
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

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30924")]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        [WorkItem(7133, "http://github.com/dotnet/roslyn/issues/7133")]
        public void TestDisplayResultsWithCurrentUICulture2()
        {
            // logoOutput needs to be retrieved before the runner is started, because the runner changes the culture to de-DE. 
            var logoOutput = $@"{ string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion) }
{ CSharpScriptingResources.LogoLine2}

{ ScriptingResources.HelpPrompt}";
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
$@"{ logoOutput }
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

        [Fact]
        [WorkItem(18479, "https://github.com/dotnet/roslyn/issues/18479")]
        public void Tuples()
        {
            var runner = CreateRunner(input: "(1,2)");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{LogoAndHelpPrompt}
> (1,2)
[(1, 2)]
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
                args: new[] { "-i" },
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
                args: new[] { "/u:System", "/i", "--", "@arg1", "/arg2", "-arg3", "--arg4" },
                input: "foreach (var arg in Args) Print(arg);");

            runner.RunInteractive();

            var error = $@"error CS2001: { string.Format(CSharpResources.ERR_FileNotFound, Path.Combine(AppContext.BaseDirectory, "@arg1"))}";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(15860, "https://github.com/dotnet/roslyn/issues/15860")]
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
                args: new[] { $@"@""{rsp.Path}""", "/arg5", "--", "/arg7" },
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

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(15860, "https://github.com/dotnet/roslyn/issues/15860")]
        public void Args_Script1()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: new[] { script.Path, "arg1", "arg2", "arg3" });

            Assert.True(runner.RunInteractive() == 0, userMessage: runner.Console.Error.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""arg1""
""arg2""
""arg3""
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(15860, "https://github.com/dotnet/roslyn/issues/15860")]
        public void Args_Script2()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: new[] { script.Path, "@arg1", "@arg2", "@arg3" });

            Assert.True(runner.RunInteractive() == 0, userMessage: runner.Console.Error.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""@arg1""
""@arg2""
""@arg3""
", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(15860, "https://github.com/dotnet/roslyn/issues/15860")]
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
                args: new[] { $"@{rsp.Path}", "/arg5", "--", "/arg7" },
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
                args: new[] { "--", script.Path, "@arg1", "@arg2", "@arg3" });

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
                args: new[] { "--", "--", "-", "--", "-" },
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
            var runner = CreateRunner(new[] { "a + b" });

            Assert.Equal(1, runner.RunInteractive());

            var error = $@"error CS2001: { string.Format(CSharpResources.ERR_FileNotFound, Path.Combine(AppContext.BaseDirectory, "a + b")) }";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [Fact]
        public void Help()
        {
            var runner = CreateRunner(new[] { "/help" });

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{ string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion) }
{CSharpScriptingResources.LogoLine2}

{CSharpScriptingResources.InteractiveHelp}
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Version()
        {
            var runner = CreateRunner(new[] { "/version" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());

            runner = CreateRunner(new[] { "/version", "/help" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());

            runner = CreateRunner(new[] { "/version", "/r:somefile" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());

            runner = CreateRunner(new[] { "/version", "/nologo" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void Script_BadUsings()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("WriteLine(42);");

            var runner = CreateRunner(new[]
            {
                GacFileResolver.IsAvailable ? null : "/r:System.Console",
                "/u:System.Console;Alpha.Beta",
                script.Path
            });

            Assert.Equal(1, runner.RunInteractive());

            var error = $@"error CS0246: { string.Format(CSharpResources.ERR_SingleTypeNameNotFound, "Alpha") }";
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
(1,8): error CS0234: { string.Format(CSharpResources.ERR_DottedTypeNameNotFoundInNS, "Missing", "Microsoft") }
«Gray»
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $"(1,8): error CS0234: { string.Format(CSharpResources.ERR_DottedTypeNameNotFoundInNS, "Missing", "Microsoft") }",
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
                    args: new[] { scriptPath },
                    workingDirectory: workingDirectory);
                runner.RunInteractive();
                AssertEx.AssertEqualToleratingWhitespaceDifferences("3", runner.Console.Out.ToString());
            }
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

            var runner = CreateRunner(new[] { $"/loadpath:{dir1.Path}", $"/loadpaths:{dir2.Path};{dir3.Path}", main.Path });

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

            var runner = CreateRunner(new[] { "/r:4.dll", $"/lib:{dir1.Path}", $"/libpath:{dir2.Path}", $"/libpaths:{dir3.Path};{dir4.Path}", main.Path });

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
(1,7): error CS1504: { string.Format(CSharpResources.ERR_NoSourceFile, "a.csx", CSharpResources.CouldNotFindFile) }
«Gray»
> SourcePaths.Add(@""{dir.Path}"")
> #load ""a.csx""
> X
1
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"(1,7): error CS1504: { string.Format(CSharpResources.ERR_NoSourceFile, "a.csx", CSharpResources.CouldNotFindFile) }",
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
(1,1): error CS0006: { string.Format(CSharpResources.ERR_NoMetadataFile, "C.dll")  }
«Gray»
> ReferencePaths.Add(@""{dir.Path}"")
> #r ""C.dll""
> new C()
C {{ }}
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"(1,1): error CS0006: { string.Format(CSharpResources.ERR_NoMetadataFile, "C.dll") }",
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

            var csi = CreateRunner(new[] { "b.csx" }, responseFile: rsp.Path);
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
            var runner = CreateRunner(new[] { "/i", init.Path }, input:
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
            var runner = CreateRunner(new[] { $@"/r:""{reference.Path}""", "/i", init.Path }, input:
@"new C()");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
«Red»
{init.Path}(2,3): error CS1002: { CSharpResources.ERR_SemicolonExpected }
«Gray»
> new C()
C {{ }}
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"{init.Path}(2,3): error CS1002: { CSharpResources.ERR_SemicolonExpected }",
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
{ ScriptingResources.HelpText }
> ", runner.Console.Out.ToString());
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
", new[] { TestReferences.NetFx.v4_0_30319.mscorlib }, libBaseName);

            var libBase2 = TestCompilationFactory.CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 2;
}
", new[] { TestReferences.NetFx.v4_0_30319.mscorlib }, libBaseName);

            var lib1 = TestCompilationFactory.CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = TestCompilationFactory.CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib, libBase1.ToMetadataReference() }, lib2Name);

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
{ string.Format(ScriptingResources.AssemblyAlreadyLoaded, libBaseName, "0.0.0.0", fileBase1.Path, fileBase2.Path) }
«Gray»
> ", runner.Console.Out.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        [WorkItem(6580, "https://github.com/dotnet/roslyn/issues/6580")]
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
(1,58): warning CS0162: { CSharpResources.WRN_UnreachableCode }
«Red»
System.Exception: Bang!
«Gray»
> i + j + k
120
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"(1,58): warning CS0162: { CSharpResources.WRN_UnreachableCode }
System.Exception: Bang!",
                runner.Console.Error.ToString());
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        [WorkItem(21327, "https://github.com/dotnet/roslyn/issues/21327")]
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
        [WorkItem(21327, "https://github.com/dotnet/roslyn/issues/21327")]
        public void InferredTupleNames()
        {
            var runner = CreateRunner(input:
@"var a = 1;
var t = (a, 2);
Print(t.a);
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{ string.Format(CSharpScriptingResources.LogoLine1, s_compilerVersion) }
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
