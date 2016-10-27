﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
using Xunit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests
{
    using static TestCompilationFactory;

    public class CommandLineRunnerTests : TestBase
    {
        private static readonly string s_compilerVersion =
            typeof(CSharpInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

        // default csi.rsp
        private static readonly string[] s_defaultArgs = new[]
        {
            "/r:System;System.Core;Microsoft.CSharp;System.ValueTuple.dll",
            "/u:System;System.IO;System.Collections.Generic;System.Diagnostics;System.Dynamic;System.Linq;System.Linq.Expressions;System.Text;System.Threading.Tasks",
        };

        private static CommandLineRunner CreateRunner(
            string[] args = null,
            string input = "",
            string responseFile = null,
            string workingDirectory = null)
        {
            var io = new TestConsoleIO(input);
            var buildPaths = new BuildPaths(
                clientDir: AppContext.BaseDirectory,
                workingDir: workingDirectory ?? AppContext.BaseDirectory,
                sdkDir: null,
                tempDir: Path.GetTempPath());

            var compiler = new CSharpInteractiveCompiler(
                responseFile,
                buildPaths,
                args ?? s_defaultArgs,
                new NotImplementedAnalyzerLoader());

            return new CommandLineRunner(io, compiler, CSharpScriptCompiler.Instance, CSharpObjectFormatter.Instance);
        }

        [Fact]
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
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> async Task<int[]> GetStuffAsync()
. {{
.   return new int[] {{ 1, 2, 3, 4, 5 }};
. }}
«Yellow»
(1,19): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
«Gray»
> from x in await GetStuffAsync()
. where x > 2
. select x * x
Enumerable.WhereSelectArrayIterator<int, int> {{ 9, 16, 25 }}
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                @"(1,19): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.",
                runner.Console.Error.ToString());
        }

        [Fact]
        [WorkItem(7133, "http://github.com/dotnet/roslyn/issues/7133")]
        public void TestDisplayResultsWithCurrentUICulture()
        {
            var runner = CreateRunner(input:
@"using static System.Globalization.CultureInfo;
DefaultThreadCurrentUICulture = GetCultureInfo(""en-GB"")
Math.PI
DefaultThreadCurrentUICulture = GetCultureInfo(""de-DE"")
Math.PI
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> using static System.Globalization.CultureInfo;
> DefaultThreadCurrentUICulture = GetCultureInfo(""en-GB"")
[en-GB]
> Math.PI
3.1415926535897931
> DefaultThreadCurrentUICulture = GetCultureInfo(""de-DE"")
[de-DE]
> Math.PI
3,1415926535897931
>", runner.Console.Out.ToString());

            // Tests that DefaultThreadCurrentUICulture is respected and not DefaultThreadCurrentCulture.
            runner = CreateRunner(input:
@"using static System.Globalization.CultureInfo;
DefaultThreadCurrentUICulture = GetCultureInfo(""en-GB"")
DefaultThreadCurrentCulture = GetCultureInfo(""en-GB"")
Math.PI
DefaultThreadCurrentCulture = GetCultureInfo(""de-DE"")
Math.PI
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> using static System.Globalization.CultureInfo;
> DefaultThreadCurrentUICulture = GetCultureInfo(""en-GB"")
[en-GB]
> DefaultThreadCurrentCulture = GetCultureInfo(""en-GB"")
[en-GB]
> Math.PI
3.1415926535897931
> DefaultThreadCurrentCulture = GetCultureInfo(""de-DE"")
[de-DE]
> Math.PI
3.1415926535897931
>", runner.Console.Out.ToString());
        }

        [Fact]
        public void Void()
        {
            var runner = CreateRunner(input:
@"Print(1);
Print(2)
");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> Print(1);
1
> Print(2)
2
> ", runner.Console.Out.ToString());
        }

        [Fact]
        public void Tuples()
        {
            var runner = CreateRunner(input: "(1,2)");
            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> (1,2)
[(1, 2)]
> ", runner.Console.Out.ToString());
        }

        [Fact]
        public void Exception()
        {
            var runner = CreateRunner(input:
@"int div(int a, int b) => a/b;
div(10, 2)
div(10, 0)
");
            Assert.Equal(0, runner.RunInteractive());

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> int div(int a, int b) => a/b;
> div(10, 2)
5
> div(10, 0)
«Red»
{new System.DivideByZeroException().Message}
  + Submission#0.div(int, int)
«Gray»
> ", runner.Console.Out.ToString());

            Assert.Equal(
$@"{new System.DivideByZeroException().Message}
  + Submission#0.div(int, int)
", runner.Console.Error.ToString());
        }

        [Fact]
        public void ExceptionInGeneric()
        {
            var runner = CreateRunner(input:
@"static class C<T> { public static int div<U>(int a, int b) => a/b; }
C<string>.div<bool>(10, 2)
C<string>.div<bool>(10, 0)
");
            Assert.Equal(0, runner.RunInteractive());

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> static class C<T> {{ public static int div<U>(int a, int b) => a/b; }}
> C<string>.div<bool>(10, 2)
5
> C<string>.div<bool>(10, 0)
«Red»
{new System.DivideByZeroException().Message}
  + Submission#0.C<T>.div<U>(int, int)
«Gray»
> ", runner.Console.Out.ToString());

            Assert.Equal(
$@"{new System.DivideByZeroException().Message}
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
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
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

            var error = $@"error CS2001: Source file '{Path.Combine(AppContext.BaseDirectory, "@arg1")}' could not be found.";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [Fact]
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

        [Fact]
        public void Args_Script1()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: new[] { script.Path, "arg1", "arg2", "arg3" });

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""arg1""
""arg2""
""arg3""
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Args_Script2()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var runner = CreateRunner(
                args: new[] { script.Path, "@arg1", "@arg2", "@arg3" });

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
""@arg1""
""@arg2""
""@arg3""
", runner.Console.Out.ToString());
        }

        [Fact]
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

            Assert.Equal(0, runner.RunInteractive());

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

        [Fact]
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

        [Fact]
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

            var error = $@"error CS2001: Source file '{Path.Combine(AppContext.BaseDirectory, "a + b")}' could not be found.";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [Fact]
        public void Help()
        {
            var runner = CreateRunner(new[] { "/help" });

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Usage: csi [option] ... [script-file.csx] [script-argument] ...

Executes script-file.csx if specified, otherwise launches an interactive REPL (Read Eval Print Loop).

Options:
  /help                          Display this usage message (alternative form: /?)
  /version                       Display the version and exit
  /i                             Drop to REPL after executing the specified script.
  /r:<file>                      Reference metadata from the specified assembly file (alternative form: /reference)
  /r:<file list>                 Reference metadata from the specified assembly files (alternative form: /reference)
  /lib:<path list>               List of directories where to look for libraries specified by #r directive. 
                                 (alternative forms: /libPath /libPaths)
  /u:<namespace>                 Define global namespace using (alternative forms: /using, /usings, /import, /imports)
  @<file>                        Read response file for more options
  --                             Indicates that the remaining arguments should not be treated as options.
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Version()
        {
            var runner = CreateRunner(new[] { "/version" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"{s_compilerVersion}", runner.Console.Out.ToString());

            runner = CreateRunner(new[] { "/version", "/help" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"{s_compilerVersion}", runner.Console.Out.ToString());

            runner = CreateRunner(new[] { "/version", "/r:somefile" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"{s_compilerVersion}", runner.Console.Out.ToString());

            runner = CreateRunner(new[] { "/version", "/nologo" });
            Assert.Equal(0, runner.RunInteractive());
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"{s_compilerVersion}", runner.Console.Out.ToString());
        }

        [Fact]
        public void Script_BadUsings()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("WriteLine(42);");

            var runner = CreateRunner(new[] { "/u:System.Console;Foo.Bar", script.Path });

            Assert.Equal(1, runner.RunInteractive());

            const string error = @"error CS0246: The type or namespace name 'Foo' could not be found (are you missing a using directive or an assembly reference?)";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Out.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences(error, runner.Console.Error.ToString());
        }

        [Fact]
        public void Script_NoHostNamespaces()
        {
            var runner = CreateRunner(input: "nameof(Microsoft.CodeAnalysis)");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> nameof(Microsoft.CodeAnalysis)
«Red»
(1,8): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
«Gray»
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "(1,8): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)",
                runner.Console.Error.ToString());
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
            dir1.CreateFile("1.dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib("public class C1 {}", "1").EmitToArray());

            var dir2 = Temp.CreateDirectory();
            dir2.CreateFile("2.dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib("public class C2 {}", "2").EmitToArray());

            var dir3 = Temp.CreateDirectory();
            dir3.CreateFile("3.dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib("public class C3 {}", "3").EmitToArray());

            var dir4 = Temp.CreateDirectory();
            dir4.CreateFile("4.dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib("public class C4 {}", "4").EmitToArray());

            var runner = CreateRunner(new[] { "/r:4.dll", $"/lib:{dir1.Path}", $"/libpath:{dir2.Path}", $"/libpaths:{dir3.Path};{dir4.Path}", main.Path });

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
C1 { }
C2 { }
C3 { }
C4 { }
", runner.Console.Out.ToString());
        }

        [Fact]
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
Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> SourcePaths
SearchPaths {{ }}
> #load ""a.csx""
«Red»
(1,7): error CS1504: Source file 'a.csx' could not be opened -- Could not find file.
«Gray»
> SourcePaths.Add(@""{dir.Path}"")
> #load ""a.csx""
> X
1
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                @"(1,7): error CS1504: Source file 'a.csx' could not be opened -- Could not find file.",
                runner.Console.Error.ToString());
        }

        [Fact]
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
Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> ReferencePaths
SearchPaths {{ }}
> #r ""C.dll""
«Red»
(1,1): error CS0006: Metadata file 'C.dll' could not be found
«Gray»
> ReferencePaths.Add(@""{dir.Path}"")
> #r ""C.dll""
> new C()
C {{ }}
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                @"(1,1): error CS0006: Metadata file 'C.dll' could not be found",
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

        [Fact]
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
{init.Path}(2,3): error CS1002: ; expected
«Gray»
> new C()
C {{ }}
> 
", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"{init.Path}(2,3): error CS1002: ; expected",
                runner.Console.Error.ToString());
        }

        [Fact]
        public void HelpCommand()
        {
            var runner = CreateRunner(input:
@"#help
");
            runner.RunInteractive();

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> #help
Keyboard shortcuts:
  Enter         If the current submission appears to be complete, evaluate it.  Otherwise, insert a new line.
  Escape        Clear the current submission.
  UpArrow       Replace the current submission with a previous submission.
  DownArrow     Replace the current submission with a subsequent submission (after having previously navigated backwards).
  Ctrl-C        Exit the REPL.
REPL commands:
  #help         Display help on available commands and key bindings.
Script directives:
  #r            Add a metadata reference to specified assembly and all its dependencies, e.g. #r ""myLib.dll"".
  #load         Load specified script file and execute it, e.g. #load ""myScript.csx"".
> ", runner.Console.Out.ToString());
        }

        [Fact]
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
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> #r ""{file1.Path}""
> var l1 = new Lib1();
> #r ""{file2.Path}""
> var l2 = new Lib2();
«Red»
Assembly '{libBaseName}, Version=0.0.0.0' has already been loaded from '{fileBase1.Path}'. A different assembly with the same name and version can't be loaded: '{fileBase2.Path}'.
«Gray»
> ", runner.Console.Out.ToString());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/11716")]
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
$@"Microsoft (R) Visual C# Interactive Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> int i = 100;
> int j = 20; throw new System.Exception(""Bang!""); int k = 3;
«Red»
Bang!
«Gray»
> i + j + k
120
> ", runner.Console.Out.ToString());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                @"Bang!",
                runner.Console.Error.ToString());
        }
    }
}
