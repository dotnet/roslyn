// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UnitTests.StackTraceExplorer
{
    [UseExportProvider]
    public class StackTraceExplorerTests
    {
        private static async Task TestSymbolFoundAsync(string inputLine, string code)
        {
            using var workspace = TestWorkspace.CreateCSharp(code);
            var result = await StackTraceAnalyzer.AnalyzeAsync(inputLine, CancellationToken.None);
            Assert.Single(result.ParsedFrames);

            var stackFrame = result.ParsedFrames[0] as ParsedStackFrame;
            AssertEx.NotNull(stackFrame);

            var symbol = await stackFrame.ResolveSymbolAsync(workspace.CurrentSolution, CancellationToken.None);

            var cursorDoc = workspace.Documents.Single();
            var selectedSpan = cursorDoc.SelectedSpans.Single();
            var doc = workspace.CurrentSolution.GetRequiredDocument(cursorDoc.Id);
            var root = await doc.GetRequiredSyntaxRootAsync(CancellationToken.None);
            var node = root.FindNode(selectedSpan);
            var semanticModel = await doc.GetRequiredSemanticModelAsync(CancellationToken.None);

            var expectedSymbol = semanticModel.GetDeclaredSymbol(node);
            AssertEx.NotNull(expectedSymbol);

            Assert.Equal(expectedSymbol, symbol);
        }

        [Fact]
        public Task TestSymbolFound_DebuggerLine()
        {
            return TestSymbolFoundAsync(
                "ConsoleApp4.dll!ConsoleApp4.MyClass.M()",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|]() {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_DebuggerLine_SingleSimpleClassParam()
        {
            return TestSymbolFoundAsync(
                "ConsoleApp4.dll!ConsoleApp4.MyClass.M(string s)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|](string s) {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_ExceptionLine()
        {
            return TestSymbolFoundAsync(
                "at ConsoleApp4.MyClass.M()",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|]() {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_ExceptionLine_SingleSimpleClassParam()
        {
            return TestSymbolFoundAsync(
                "at ConsoleApp4.MyClass.M(string s)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|](string s) {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_ExceptionLineWithFile()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.M() in C:\repos\ConsoleApp4\ConsoleApp4\Program.cs:line 26",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|]() {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_ExceptionLine_GenericMethod()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.M[T](T t) in C:\repos\Test\MyClass.cs:line 7",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|]<T>(T t) {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_ExceptionLine_GenericMethod_FromActivityLog()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.M&lt;T&gt;(T t)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|]<T>(T t) {}
    }
}");
        }

        [Fact]
        public Task TestSymbolFound_ExceptionLine_MultipleGenerics()
        {
            return TestSymbolFoundAsync(
                "at ConsoleApp4.MyClass.M<T>(T t)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|]<T>(T t) {}
    }
}");
        }

        [Fact(Skip = "The parser does not handle arity on types yet")]
        public Task TestSymbolFound_ExceptionLine_GenericsHierarchy()
        {
            return TestSymbolFoundAsync(
                "at ConsoleApp4.MyClass`1.MyInnerClass`1.M[T](T t)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass<A>
    {
        public class MyInnerClass<B>
        {
            public void M<T>(T t) 
            {
                throw new Exception();
            }
        }
    }
}");
        }

        [Fact(Skip = "ref params do not work yet")]
        public Task TestSymbolFound_ExceptionLine_RefArg()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.M(String& s) in C:\repos\Test\MyClass.cs:line 8",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|](ref string s)
        {
            s = string.Empty;
        }
    }
}");
        }

        [Fact(Skip = "out params do not work yet")]
        public Task TestSymbolFound_ExceptionLine_OutArg()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.M(String& s) in C:\repos\Test\MyClass.cs:line 8",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|](out string s)
        {
            s = string.Empty;
        }
    }
}");
        }

        [Fact(Skip = "in params do not work yet")]
        public Task TestSymbolFound_ExceptionLine_InArg()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.M(Int32& i)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        void [|M|](in int i)
        {
            throw new Exception();
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_AsyncMethod()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.<>c.<DoThingAsync>b__1_0() in C:\repos\Test\MyClass.cs:line 15",
                @"namespace ConsoleApp4
{
    class MyClass
    {
        public async Task M()
        {
            await DoThingAsync();
        }

        async Task DoThingAsync()
        {
            var task = new Task(() => 
            {
                Console.WriteLine(""Doing async work"");
                throw new Exception();
            });

            task.Start();

            await task;
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_PropertySet()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.set_I(Int32 value)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        public int I
        {
            get => throw new Exception();
            [|set|] => throw new Exception();
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_PropertyGet()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.get_I(Int32 value)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        public int I
        {
            [|get|] => throw new Exception();
            set => throw new Exception();
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_IndexerSet()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.set_Item(Int32 i, Int32 value)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        public int this[int i]
        {
            get => throw new Exception();
            [|set|] => throw new Exception();
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_IndexerGet()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.get_Item(Int32 i)",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        public int this[int i]
        {
            [|get|] => throw new Exception();
            set => throw new Exception();
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_LocalFunction()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.<M>g__LocalFunction|0_0()",
                @"using System;

namespace ConsoleApp4
{
    class MyClass
    {
        public void M()
        {
            LocalFunction();

            void LocalFunction()
            {
                throw new Exception();
            }
        }
    }
}");
        }

        [Fact(Skip = "Generated types/methods are not supported")]
        public Task TestSymbolFound_ExceptionLine_LocalInTopLevelStatement()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass.<M>g__LocalFunction|0_0()",
                @"using System;

LoaclInTopLevelStatement();

void [|LocalInTopLevelStatement|]()
{
    throw new Exception();
}");
        }

        [Fact(Skip = "The parser doesn't correctly handle ..ctor() methods yet")]
        public Task TestSymbolFound_ExceptionLine_Constructor()
        {
            return TestSymbolFoundAsync(
                @"at ConsoleApp4.MyClass..ctor()",
                @"namespace ConsoleApp4
{
    class MyClass
    {
        public MyClass()
        {
            throw new Exception();
        }

        ~MyClass()
        {
            throw new Exception();
        }
    }
}");
        }

        [Theory]
        [InlineData("alkjsdflkjasdlkfjasd")]
        [InlineData("at alksjdlfjasdlkfj")]
        [InlineData("line 26")]
        [InlineData("alksdjflkjsadf.cs:line 26")]
        [InlineData("This,that.A,,,,,,,,,b()")]
        [InlineData("ConsoleWriteLine()")]
        [InlineData("at <><>.<><>()")]
        [InlineData("at 897098.70987__ ()")]
        [InlineData("at jlksdjf . kljsldkjf () in aklsjdflkj")]
        public async Task TestFailureToParse(string line)
        {
            var result = await StackTraceAnalyzer.AnalyzeAsync(line, CancellationToken.None);
            Assert.Equal(1, result.ParsedFrames.Length);

            var ignoredFrames = result.ParsedFrames.OfType<IgnoredFrame>();
            AssertEx.SetEqual(result.ParsedFrames, ignoredFrames);
        }

        /// <summary>
        /// Tests cases where the text will technically parse and look like a symbol, but does not point to
        /// a symbol in the solution. 
        /// </summary>
        [Theory]
        [InlineData("at __.__._()")]
        [InlineData("abcd!__.__._()")]
        public async Task TestInvalidSymbol(string line)
        {
            using var workspace = TestWorkspace.CreateCSharp(@"
class C
{
}");

            var result = await StackTraceAnalyzer.AnalyzeAsync(line, CancellationToken.None);
            Assert.Equal(1, result.ParsedFrames.Length);

            var parsedFame = result.ParsedFrames.OfType<ParsedStackFrame>().Single();
            var symbol = await parsedFame.ResolveSymbolAsync(workspace.CurrentSolution, CancellationToken.None);
            Assert.Null(symbol);
        }

        [Fact]
        public async Task TestActivityLogParsing()
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
