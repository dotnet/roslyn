// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CallstackExplorer
{
    [UseExportProvider]
    public class CallstackExplorerTests
    {
        private const string BaseCode = @"using System;

namespace ConsoleApp4
{
    class Program
    {
        static void Main(string[] args)
        {
            // Choose trigger for exception here to get stack
            MyOtherClass.ThrowForNewMyClass();
        }
    }


    class MyClass
    {
        public void ThrowAtOne()
        {
            throw new Exception();
        }

        public void ThrowReferenceOne()
        {
            ThrowAtOne();
        }

        public override string ToString()
        {
            ThrowReferenceOne();
            return base.ToString();
        }
    }

    static class MyOtherClass
    {
        public static void ThrowForNewMyClass()
        {
            var c = new MyClass();
            c.ToString();
        }

        public static void Overload(int i) => throw new Exception();
        public static void Overload(string s) => throw new Exception();
        public static void Overload(MyClass myClass) => myClass.ThrowAtOne();
    }
}
";

        private static async Task<ISymbol> GetSymbolAsync(string fqn, TestWorkspace workspace)
        {
            var fqnSpltit = fqn.LastIndexOf(".");
            var methodName = fqn.Substring(fqnSpltit + 1);
            var className = fqn.Substring(0, fqnSpltit);

            var project = workspace.CurrentSolution.Projects.Single();
            var compilation = await project.GetCompilationAsync();
            Assert.NotNull(compilation);

            var type = compilation!.GetTypeByMetadataName(className);
            Assert.NotNull(type);
            var method = type!.GetMembers().Single(n => n.Name == methodName);
            return method;
        }

        private static TestWorkspace CreateWorkspace()
        {
            return TestWorkspace.CreateCSharp(BaseCode);
        }

        [Fact]
        public async Task TestOne()
        {
            var fqn = "ConsoleApp4.MyClass.ThrowAtOne";
            var workspace = CreateWorkspace();
            var method = await GetSymbolAsync(fqn, workspace);

            // Callstack from VS debugger callstack window
            var callstack = @">	ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
 	ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowReferenceOne() Line 24	C#
 	ConsoleApp4.dll!ConsoleApp4.MyClass.ToString() Line 29	C#
 	ConsoleApp4.dll!ConsoleApp4.MyOtherClass.ThrowForNewMyClass() Line 39	C#
 	ConsoleApp4.dll!ConsoleApp4.Program.Main(string[] args) Line 10	C#
";

            var result = await CallstackAnalyzer.AnalyzeAsync(workspace.CurrentSolution, callstack, CancellationToken.None);
            Assert.Equal(5, result.ParsedLines.Length);

            var debugLineResults = result.ParsedLines.OfType<DebugWindowResult>();
            AssertEx.SetEqual(result.ParsedLines, debugLineResults);

            var symbol = await debugLineResults.First().ResolveSymbolAsync(result.Solution, CancellationToken.None);
            Assert.Equal(method, symbol);
        }

        [Fact]
        public async Task TestExceptionStack()
        {
            var fqn = "ConsoleApp4.MyClass.ThrowAtOne";
            var workspace = CreateWorkspace();
            var method = await GetSymbolAsync(fqn, workspace);

            // Callstack from VS debugger callstack window
            var callstack = @"   at ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
   at ConsoleApp4.MyClass.ThrowReferenceOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 31
   at ConsoleApp4.MyClass.ToString() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 36
   at ConsoleApp4.MyOtherClass.ThrowForNewMyClass() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 46
   at ConsoleApp4.Program.Main(String[] args) in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 12
";

            var result = await CallstackAnalyzer.AnalyzeAsync(workspace.CurrentSolution, callstack, CancellationToken.None);
            Assert.Equal(5, result.ParsedLines.Length);

            var fileLineResults = result.ParsedLines.OfType<FileLineResult>();
            AssertEx.SetEqual(result.ParsedLines, fileLineResults);

            var symbol = await fileLineResults.First().ResolveSymbolAsync(result.Solution, CancellationToken.None);
            Assert.Equal(method, symbol);
        }
    }
}
