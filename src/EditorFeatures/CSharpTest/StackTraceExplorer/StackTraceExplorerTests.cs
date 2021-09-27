// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.StackTraceExplorer;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StackTraceExplorer
{
    [UseExportProvider]
    public class StackTraceExplorerTests
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
        public static void ThrowGeneric<T>(T value) => throw new Exception();
    }

    static class GenericClass<T, U> 
    {
        public static void Throw<T>(T t) => throw new Exception();
        public static void Throw<T, U>(T t, U u) => throw new Exception();
        public static void Throw<T, U, V>(T t, U u, V v) => throw new Exception();
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
            var members = type!.GetMembers();
            var symbolsAndDisplayNames = members.Select(m => (m, m.ToDisplayString()));
            var symbolsAndMethodNames = symbolsAndDisplayNames.Select(p => (symbol: p.Item1, name: p.Item2.Split('.').Last()));
            var method = symbolsAndMethodNames.Single(p => p.name == methodName).symbol;
            return method;
        }

        private static Task<ISymbol?> GetSymbolAsync(ParsedFrame parsedFrame, Solution solution, CancellationToken cancellationToken)
        {
            var stackFrame = parsedFrame as ParsedStackFrame;
            Assert.NotNull(stackFrame);

            return stackFrame!.ResolveSymbolAsync(solution, cancellationToken);
        }

        private static TestWorkspace CreateWorkspace()
        {
            return TestWorkspace.CreateCSharp(BaseCode);
        }

        [Theory]
        [InlineData("ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#", "ConsoleApp4.MyClass.ThrowAtOne()")]
        [InlineData(@"   at ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26", "ConsoleApp4.MyClass.ThrowAtOne()")]
        [InlineData(@"at ConsoleApp4.MyClass.ThrowAtOne()", "ConsoleApp4.MyClass.ThrowAtOne()")]
        [InlineData(@"at ConsoleApp4.MyOtherClass.ThrowGeneric<string>(string.Empty)", "ConsoleApp4.MyOtherClass.ThrowGeneric<T>(T)")]
        [InlineData(@"at ConsoleApp4.GenericClass<T, U>.Throw<T>()", "ConsoleApp4.GenericClass`2.Throw<T>(T)")]
        [InlineData(@"at ConsoleApp4.GenericClass<T, U>.Throw<T, U>()", "ConsoleApp4.GenericClass`2.Throw<T, U>(T, U)")]
        [InlineData(@"at ConsoleApp4.GenericClass<T, U>.Throw<T, U, V>(V v)", "ConsoleApp4.GenericClass`2.Throw<T, U, V>(T, U, V)")]
        public async Task TestSymbolFound(string inputLine, string symbolText)
        {
            var workspace = CreateWorkspace();
            var result = await StackTraceAnalyzer.AnalyzeAsync(inputLine, CancellationToken.None);
            Assert.Single(result.ParsedFrames);

            var symbol = await GetSymbolAsync(result.ParsedFrames[0], workspace.CurrentSolution, CancellationToken.None);
            var method = await GetSymbolAsync(symbolText, workspace);
            Assert.Equal(method, symbol);
        }

        [Fact]
        public async Task TestDebugWindowStack()
        {
            var workspace = CreateWorkspace();

            // Callstack from VS debugger callstack window
            var callstack = @">	ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowAtOne() Line 19	C#
 	ConsoleApp4.dll!ConsoleApp4.MyClass.ThrowReferenceOne() Line 24	C#
 	ConsoleApp4.dll!ConsoleApp4.MyClass.ToString() Line 29	C#
 	ConsoleApp4.dll!ConsoleApp4.MyOtherClass.ThrowForNewMyClass() Line 39	C#
 	ConsoleApp4.dll!ConsoleApp4.Program.Main(string[] args) Line 10	C#
";

            var result = await StackTraceAnalyzer.AnalyzeAsync(callstack, CancellationToken.None);
            Assert.Equal(5, result.ParsedFrames.Length);

            var symbol = await GetSymbolAsync(result.ParsedFrames[0], workspace.CurrentSolution, CancellationToken.None);
            var method = await GetSymbolAsync("ConsoleApp4.MyClass.ThrowAtOne", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(result.ParsedFrames[1], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.MyClass.ThrowReferenceOne", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(result.ParsedFrames[2], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.MyClass.ToString", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(result.ParsedFrames[3], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.MyOtherClass.ThrowForNewMyClass", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(result.ParsedFrames[4], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.Program.Main", workspace);
            Assert.Equal(method, symbol);
        }

        [Fact]
        public async Task TestExceptionStack()
        {
            var workspace = CreateWorkspace();

            // Callstack from VS debugger callstack window
            var callstack = @"   at ConsoleApp4.MyClass.ThrowAtOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26
   at ConsoleApp4.MyClass.ThrowReferenceOne() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 31
   at ConsoleApp4.MyClass.ToString() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 36
   at ConsoleApp4.MyOtherClass.ThrowForNewMyClass() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 46
   at ConsoleApp4.Program.Main(String[] args) in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 12
";

            var result = await StackTraceAnalyzer.AnalyzeAsync(callstack, CancellationToken.None);
            Assert.Equal(5, result.ParsedFrames.Length);

            var fileLineResults = result.ParsedFrames.OfType<ParsedFrameWithFile>().ToImmutableArray();
            AssertEx.SetEqual(result.ParsedFrames, fileLineResults);

            var symbol = await GetSymbolAsync(fileLineResults[0], workspace.CurrentSolution, CancellationToken.None);
            var method = await GetSymbolAsync("ConsoleApp4.MyClass.ThrowAtOne", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(fileLineResults[1], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.MyClass.ThrowReferenceOne", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(fileLineResults[2], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.MyClass.ToString", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(fileLineResults[3], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.MyOtherClass.ThrowForNewMyClass", workspace);
            Assert.Equal(method, symbol);

            symbol = await GetSymbolAsync(fileLineResults[4], workspace.CurrentSolution, CancellationToken.None);
            method = await GetSymbolAsync("ConsoleApp4.Program.Main", workspace);
            Assert.Equal(method, symbol);
        }

        [Theory]
        [InlineData("alkjsdflkjasdlkfjasd")]
        [InlineData("at alksjdlfjasdlkfj")]
        [InlineData("line 26")]
        [InlineData("alksdjflkjsadf.cs:line 26")]
        public async Task TestFailureCases(string line)
        {
            var result = await StackTraceAnalyzer.AnalyzeAsync(line, CancellationToken.None);
            Assert.Equal(1, result.ParsedFrames.Length);

            var ignoredFrames = result.ParsedFrames.OfType<IgnoredFrame>();
            AssertEx.SetEqual(result.ParsedFrames, ignoredFrames);
        }

        [Fact]
        public async Task TestLineAndFileParsing()
        {
            var stackLine = @" at ConsoleApp4.MyClass.ThrowAtOne() in test1.cs:line 26";
            var result = await StackTraceAnalyzer.AnalyzeAsync(stackLine, CancellationToken.None);

            var parsedFrameWithFile = result.ParsedFrames[0] as ParsedFrameWithFile;
            AssertEx.NotNull(parsedFrameWithFile);

            var workspace = CreateWorkspace();
            var (document, lineNumber) = parsedFrameWithFile.GetDocumentAndLine(workspace.CurrentSolution);
            AssertEx.NotNull(document);
            Assert.Equal(26, lineNumber);
        }

        [Fact]
        public async Task TestActivityLog()
        {
            var activityLogException = @"Exception occurred while loading solution options: System.Runtime.InteropServices.COMException (0x8000FFFF): Catastrophic failure (Exception from HRESULT: 0x8000FFFF (E_UNEXPECTED))&#x000D;&#x000A;   at System.Runtime.InteropServices.Marshal.ThrowExceptionForHRInternal(Int32 errorCode, IntPtr errorInfo)&#x000D;&#x000A;   at Microsoft.VisualStudio.Shell.Package.Initialize()&#x000D;&#x000A;--- End of stack trace from previous location where exception was thrown ---&#x000D;&#x000A;   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw&lt;string&gt;()&#x000D;&#x000A;   at Microsoft.VisualStudio.Telemetry.WindowsErrorReporting.WatsonReport.GetClrWatsonExceptionInfo(Exception exceptionObject)";

            var result = await StackTraceAnalyzer.AnalyzeAsync(activityLogException, CancellationToken.None);
            Assert.Equal(6, result.ParsedFrames.Length);

            var ignoredFrame1 = result.ParsedFrames[0] as IgnoredFrame;
            AssertEx.NotNull(ignoredFrame1);
            Assert.Equal(@"Exception occurred while loading solution options: System.Runtime.InteropServices.COMException (0x8000FFFF): Catastrophic failure (Exception from HRESULT: 0x8000FFFF (E_UNEXPECTED))", ignoredFrame1.OriginalText);

            var parsedFrame2 = result.ParsedFrames[1] as ParsedStackFrame;
            AssertEx.NotNull(parsedFrame2);
            Assert.Equal(@"at System.Runtime.InteropServices.Marshal.ThrowExceptionForHRInternal(Int32 errorCode, IntPtr errorInfo)", parsedFrame2.OriginalText);

            var parsedFrame3 = result.ParsedFrames[2] as ParsedStackFrame;
            AssertEx.NotNull(parsedFrame3);
            Assert.Equal(@"at Microsoft.VisualStudio.Shell.Package.Initialize()", parsedFrame3.OriginalText);

            var ignoredFrame4 = result.ParsedFrames[3] as IgnoredFrame;
            AssertEx.NotNull(ignoredFrame4);
            Assert.Equal(@"--- End of stack trace from previous location where exception was thrown ---", ignoredFrame4.OriginalText);

            var parsedFrame5 = result.ParsedFrames[4] as ParsedStackFrame;
            AssertEx.NotNull(parsedFrame5);
            Assert.Equal(@"at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw<string>()", parsedFrame5.OriginalText);

            var parsedFrame6 = result.ParsedFrames[5] as ParsedStackFrame;
            AssertEx.NotNull(parsedFrame6);
            Assert.Equal(@"at Microsoft.VisualStudio.Telemetry.WindowsErrorReporting.WatsonReport.GetClrWatsonExceptionInfo(Exception exceptionObject)", parsedFrame6.OriginalText);
        }
    }
}
