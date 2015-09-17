// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Hosting.CSharp;
using Microsoft.CodeAnalysis.Scripting.Test;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.CSharp.UnitTests
{
    public class CommandLineRunnerTests : TestBase
    {
        private static readonly string CompilerVersion =
            typeof(CSharpInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

        private static CommandLineRunner CreateRunner(string[] args = null, string input = "", string responseFile = null)
        {
            var io = new TestConsoleIO(input);

            var compiler = new CSharpInteractiveCompiler(
                responseFile,
                AppContext.BaseDirectory,
                args ?? Array.Empty<string>(),
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

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

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
        public void Args()
        {
            var runner = CreateRunner(
                args: new[] { "--", "arg1", "arg2", "arg3" },
                input: "foreach (var arg in Args) Print(arg);");

            Assert.Equal(0, runner.RunInteractive());

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

> foreach (var arg in Args) Print(arg);
""arg1""
""arg2""
""arg3""
> ", runner.Console.Out.ToString());
        }

        [Fact]
        public void Script_ExitCode()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("ExitCode = 42;");

            var runner = CreateRunner(new[] { script.Path });

            Assert.Equal(42, runner.RunInteractive());
            Assert.Equal("", runner.Console.Out.ToString());
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

            Assert.Equal(
$@"Microsoft (R) Visual C# Interactive Compiler version {CompilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.

Usage: csi [options] [script-file.csx] [-- script-arguments]

If script-file is specified executes the script, otherwise launches an interactive REPL (Read Eval Print Loop).

Options:
  /help                          Display this usage message (Short form: /?)
  /reference:<alias>=<file>      Reference metadata from the specified assembly file using the given alias (Short form: /r)
  /reference:<file list>         Reference metadata from the specified assembly files (Short form: /r)
  /referencePath:<path list>     List of paths where to look for metadata references specified as unrooted paths. (Short form: /rp)
  /using:<namespace>             Define global namespace using (Short form: /u)
  /define:<symbol list>          Define conditional compilation symbol(s) (Short form: /d)
  @<file>                        Read response file for more options
", runner.Console.Out.ToString());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/5277")]
        public void Script_BadUsings()
        {
            var script = Temp.CreateFile(extension: ".csx").WriteAllText("WriteLine(42);");

            var runner = CreateRunner(new[] { script.Path, "/u:System.Console;Foo.Bar" });

            Assert.Equal(1, runner.RunInteractive());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
error CS0246: The type or namespace name 'Foo' could not be found (are you missing a using directive or an assembly reference?)
", runner.Console.Out.ToString());

            Assert.Equal("", runner.Console.Error.ToString());
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
    }
}
