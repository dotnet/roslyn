// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests
{
    public class CommandLineRunnerTests : TestBase
    {
        private static readonly string CompilerVersion =
            typeof(CSharpInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

        // default csi.rsp
        private static readonly string[] DefaultArgs = new[]
        {
            "/r:System;System.Core;Microsoft.CSharp",
            "/u:System;System.IO;System.Collections.Generic;System.Diagnostics;System.Dynamic;System.Linq;System.Linq.Expressions;System.Text;System.Threading.Tasks",
        };

        private static CommandLineRunner CreateRunner(
            string[] args = null,
            string input = "", 
            string responseFile = null,
            string workingDirectory = null)
        {
            var io = new TestConsoleIO(input);

            var compiler = new CSharpInteractiveCompiler(
                responseFile,
                workingDirectory ?? AppContext.BaseDirectory,
                null,
                args ?? DefaultArgs,
                new NotImplementedAnalyzerLoader());

            return new CommandLineRunner(io, compiler, CSharpScriptCompiler.Instance, CSharpObjectFormatter.Instance);
        }

        private static Compilation CreateLibrary(string assemblyName, string source)
        {
            return CSharpCompilation.Create(
                assemblyName,
                new[] { SyntaxFactory.ParseSyntaxTree(source) },
                new[] { MscorlibRef },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
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
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> Print(1);
1
> Print(2)
2
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
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> int div(int a, int b) => a/b;
> div(10, 2)
5
> div(10, 0)
«Red»
Attempted to divide by zero.
«DarkRed»
  + Submission#0.div(Int32 a, Int32 b)
«Gray»
> ", runner.Console.Out.ToString());
        }

        [Fact]
        public void Args_Interactive1()
        {
            var runner = CreateRunner(
                args: new[] { "-i" },
                input: "1+1");

            runner.RunInteractive();

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
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

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"error CS2001: Source file '{Path.Combine(AppContext.BaseDirectory, "@arg1")}' could not be found.", 
                runner.Console.Out.ToString());
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

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
error CS2001: Source file '{Path.Combine(AppContext.BaseDirectory, "a + b")}' could not be found.
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Help()
        {
            var runner = CreateRunner(new[] { "/help" });

            Assert.Equal(0, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Usage: csi [option] ... [script-file.csx] [script-argument] ...

Executes script-file.csx if specified, otherwise launches an interactive REPL (Read Eval Print Loop).

Options:
  /help                          Display this usage message (alternative form: /?)
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
        public void Script_BadUsings()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("WriteLine(42);");

            var runner = CreateRunner(new[] { "/u:System.Console;Foo.Bar", script.Path });

            Assert.Equal(1, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
error CS0246: The type or namespace name 'Foo' could not be found (are you missing a using directive or an assembly reference?)
", runner.Console.Out.ToString());
        }

        [Fact]
        public void Script_NoHostNamespaces()
        {
            var runner = CreateRunner(input: "nameof(Microsoft.CodeAnalysis)");

            runner.RunInteractive();

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> nameof(Microsoft.CodeAnalysis)
«Red»
(1,8): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
«Gray»
> ", runner.Console.Out.ToString());
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
            dir1.CreateFile("1.dll").WriteAllBytes(CreateLibrary("1", "public class C1 {}").EmitToArray());
            
            var dir2 = Temp.CreateDirectory();
            dir2.CreateFile("2.dll").WriteAllBytes(CreateLibrary("2", "public class C2 {}").EmitToArray());

            var dir3 = Temp.CreateDirectory();
            dir3.CreateFile("3.dll").WriteAllBytes(CreateLibrary("3", "public class C3 {}").EmitToArray());

            var dir4 = Temp.CreateDirectory();
            dir4.CreateFile("4.dll").WriteAllBytes(CreateLibrary("4", "public class C4 {}").EmitToArray());

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
        public void HelpCommand()
        {
            var runner = CreateRunner(input:
@"#help
");
            runner.RunInteractive();

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> #help
Keyboard shortcuts:
  Enter         If the current submission appears to be complete, evaluate it.  Otherwise, insert a new line.
  Escape        Clear the current submission.
  UpArrow       Replace the current submission with a previous submission.
  DownArrow     Replace the current submission with a subsequent submission (after having previously navigated backwards).
REPL commands:
  #help         Display help on available commands and key bindings.
> ", runner.Console.Out.ToString());
        }
    }
}
