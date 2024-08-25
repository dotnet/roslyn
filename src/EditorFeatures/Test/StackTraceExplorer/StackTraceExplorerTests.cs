// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.StackTraceExplorer;

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

        // Test that ToString() and reparsing keeps the same outcome
        var reparsedResult = await StackTraceAnalyzer.AnalyzeAsync(stackFrame.ToString(), CancellationToken.None);
        Assert.Single(reparsedResult.ParsedFrames);

        var reparsedFrame = reparsedResult.ParsedFrames[0] as ParsedStackFrame;
        AssertEx.NotNull(reparsedFrame);
        StackFrameUtils.AssertEqual(stackFrame.Root, reparsedFrame.Root);

        // Get the definition for the parsed frame
        var service = workspace.Services.GetRequiredService<IStackTraceExplorerService>();
        var definition = await service.TryFindDefinitionAsync(workspace.CurrentSolution, stackFrame, StackFrameSymbolPart.Method, CancellationToken.None);
        AssertEx.NotNull(definition);

        // Get the symbol that was indicated in the source code by cursor position
        var cursorDoc = workspace.Documents.Single();
        var selectedSpan = cursorDoc.SelectedSpans.Single();
        var doc = workspace.CurrentSolution.GetRequiredDocument(cursorDoc.Id);
        var root = await doc.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var node = root.FindNode(selectedSpan);
        var semanticModel = await doc.GetRequiredSemanticModelAsync(CancellationToken.None);

        var expectedSymbol = semanticModel.GetDeclaredSymbol(node);
        AssertEx.NotNull(expectedSymbol);

        // Compare the definition found to the definition for the test symbol
        var expectedDefinition = expectedSymbol.ToNonClassifiedDefinitionItem(workspace.CurrentSolution, includeHiddenLocations: true);

        Assert.Equal(expectedDefinition.IsExternal, definition.IsExternal);
        AssertEx.SetEqual(expectedDefinition.NameDisplayParts, definition.NameDisplayParts);
        AssertEx.SetEqual(expectedDefinition.Properties, definition.Properties);
        AssertEx.SetEqual(expectedDefinition.SourceSpans, definition.SourceSpans);
        AssertEx.SetEqual(expectedDefinition.Tags, definition.Tags);
    }

    private static void AssertContents(ImmutableArray<ParsedFrame> frames, params string[] contents)
    {
        Assert.Equal(contents.Length, frames.Length);
        for (var i = 0; i < contents.Length; i++)
        {
            Assert.Equal(contents[i], frames[i].ToString());
        }
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

    [Theory]
    [InlineData("object", "Object")]
    [InlineData("bool", "Boolean")]
    [InlineData("sbyte", "SByte")]
    [InlineData("byte", "Byte")]
    [InlineData("decimal", "Decimal")]
    [InlineData("float", "Single")]
    [InlineData("double", "Double")]
    [InlineData("short", "Int16")]
    [InlineData("int", "Int32")]
    [InlineData("long", "Int64")]
    [InlineData("string", "String")]
    [InlineData("ushort", "UInt16")]
    [InlineData("uint", "UInt32")]
    [InlineData("ulong", "UInt64")]
    public Task TestSpecialTypes(string type, string typeName)
    {
        return TestSymbolFoundAsync(
            $"at ConsoleApp.MyClass.M({typeName} value)",
            @$"using System;

namespace ConsoleApp
{{
    class MyClass
    {{
        void [|M|]({type} value) {{}}
    }}
}}");
    }

    [Fact]
    public Task TestSymbolFound_DebuggerLine_SingleSimpleClassParam()
    {
        return TestSymbolFoundAsync(
            "ConsoleApp4.dll!ConsoleApp4.MyClass.M(String s)",
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
            "at ConsoleApp4.MyClass.M(String s)",
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
    public Task TestSymbolFound_GenericType()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp.MyClass`1.M(String s)",
            @"using System;
namespace ConsoleApp
{
    class MyClass<T> 
    {
        void [|M|](string s) { }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_GenericType2()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp.MyClass`2.M(String s)",
            @"using System;
namespace ConsoleApp
{
    class MyClass<T, U> 
    {
        void [|M|](string s) { }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_GenericType_GenericArg()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp.MyClass`1.M(T s)",
            @"using System;
namespace ConsoleApp
{
    class MyClass<T>
    {
        void [|M|](T s) { }
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

    [Fact]
    public Task TestSymbolFound_ParameterSpacing()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M( String   s    )",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string s)
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_OverloadsWithSameName()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M(String value)",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string value)
        {
        }

        void M(int value)
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_ArrayParameter()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M(String[] s)",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string[] s)
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_MultidimensionArrayParameter()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M(String[,] s)",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string[,] s)
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_MultidimensionArrayParameter_WithSpaces()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M(String[ , ] s)",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string[,] s)
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_MultidimensionArrayParameter_WithSpaces2()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M(String[,] s)",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string[ , ] s)
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_MultidimensionArrayParameter2()
    {
        return TestSymbolFoundAsync(
            "at ConsoleApp.MyClass.M(String[,][] s)",
            @"
namespace ConsoleApp
{
    class MyClass
    {
        void [|M|](string[,][] s)
        {
        }
    }
}");
    }

    [Fact(Skip = "Symbol search for nested types does not work")]
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
            public void [|M|]<T>(T t) 
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

    [Fact]
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

    [Fact]
    public Task TestSymbolFound_ExceptionLine_PropertyGet()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp4.MyClass.get_I()",
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

            void [|LocalFunction|]()
            {
                throw new Exception();
            }
        }

        public void LocalFunction()
        {
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_ExceptionLine_MultipleLocalFunctions()
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

            void [|LocalFunction|]()
            {
                throw new Exception();
            }
        }

        public void M2()
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

    [Fact]
    public Task TestSymbolFound_ExceptionLine_MultipleLocalFunctions2()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp4.MyClass.<M2>g__LocalFunction|0_0()",
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

        public void M2()
        {
            LocalFunction();

            void [|LocalFunction()|]
            {
                throw new Exception();
            }
        }
    }
}");
    }

    [Fact]
    public Task TestSymbolFound_ExceptionLine_MemberFunctionSameNameAsFunction()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp4.MyClass.LocalFunction()",
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

        public void [|LocalFunction|]()
        {
        }
    }
}");
    }

    /// <summary>
    /// Behavior for this test needs some explanation. Note that if there are multiple
    /// local functions within a container, they will be uniquely identified by the 
    /// suffix. In this case we have g__Local|0_0 and g__Local|0_1 as the two local functions.
    /// Resolution doesn't try to reverse engineer how these suffixes get produced, which means
    /// that the first applicable symbol with the name "Local" inside the method "M" will be found.
    /// Since local function resolution is done by searching the descendents of the method "M", the top
    /// most local function matching the name will be the first the resolver sees and considers applicable.
    /// This should get the user close to what they want, and hopefully is rare enough that it won't
    /// be frequently encountered. 
    /// </summary>
    [Fact]
    public Task TestSymbolFound_ExceptionLine_NestedLocalFunctions()
    {
        return TestSymbolFoundAsync(
            @"at C.<M>g__Local|0_1()",
            @"using System;

class C 
{
    public void M()
    {
        Local();
        
        void [|Local|]()
        {
            Local();
            
            void Local()
            {
                throw new Exception();
            }
        }
    }
}");
    }

    [Fact(Skip = "Top level local functions are not supported")]
    public Task TestSymbolFound_ExceptionLine_LocalInTopLevelStatement()
    {
        return TestSymbolFoundAsync(
            @"at ConsoleApp4.Program.<Main$>g__LocalInTopLevelStatement|0_0()",
            @"using System;

LocalInTopLevelStatement();

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
        var service = workspace.Services.GetRequiredService<IStackTraceExplorerService>();
        var definition = await service.TryFindDefinitionAsync(workspace.CurrentSolution, parsedFame, StackFrameSymbolPart.Method, CancellationToken.None);
        Assert.Null(definition);
    }

    [Fact]
    public async Task TestActivityLogParsing()
    {
        var activityLogException = @"Exception occurred while loading solution options: System.Runtime.InteropServices.COMException (0x8000FFFF): Catastrophic failure (Exception from HRESULT: 0x8000FFFF (E_UNEXPECTED))&#x000D;&#x000A;   at System.Runtime.InteropServices.Marshal.ThrowExceptionForHRInternal(Int32 errorCode, IntPtr errorInfo)&#x000D;&#x000A;   at Microsoft.VisualStudio.Shell.Package.Initialize()&#x000D;&#x000A;--- End of stack trace from previous location where exception was thrown ---&#x000D;&#x000A;   at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw&lt;string&gt;()&#x000D;&#x000A;   at Microsoft.VisualStudio.Telemetry.WindowsErrorReporting.WatsonReport.GetClrWatsonExceptionInfo(Exception exceptionObject)";

        var result = await StackTraceAnalyzer.AnalyzeAsync(activityLogException, CancellationToken.None);
        AssertContents(result.ParsedFrames,
            @"Exception occurred while loading solution options: System.Runtime.InteropServices.COMException (0x8000FFFF): Catastrophic failure (Exception from HRESULT: 0x8000FFFF (E_UNEXPECTED))",
            @"at System.Runtime.InteropServices.Marshal.ThrowExceptionForHRInternal(Int32 errorCode, IntPtr errorInfo)",
            @"at Microsoft.VisualStudio.Shell.Package.Initialize()",
            @"--- End of stack trace from previous location where exception was thrown ---",
            @"at System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw<string>()",
            @"at Microsoft.VisualStudio.Telemetry.WindowsErrorReporting.WatsonReport.GetClrWatsonExceptionInfo(Exception exceptionObject)");
    }

    [Fact]
    public async Task TestMetadataSymbol()
    {
        var code = @"class C{}";
        using var workspace = TestWorkspace.CreateCSharp(code);

        var result = await StackTraceAnalyzer.AnalyzeAsync("at System.String.ToLower()", CancellationToken.None);
        Assert.Single(result.ParsedFrames);

        var frame = result.ParsedFrames[0] as ParsedStackFrame;
        AssertEx.NotNull(frame);

        var service = workspace.Services.GetRequiredService<IStackTraceExplorerService>();
        var definition = await service.TryFindDefinitionAsync(workspace.CurrentSolution, frame, StackFrameSymbolPart.Method, CancellationToken.None);

        AssertEx.NotNull(definition);
        Assert.Equal("String.ToLower", definition.NameDisplayParts.ToVisibleDisplayString(includeLeftToRightMarker: false));
    }
}
