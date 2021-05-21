﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    <[UseExportProvider]>
    Public Class CSharpDebuggerIntellisenseTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionOnTypeCharacter() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]

    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("arg", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession()
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionOnTypeCharacterInImmediateWindow() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]

    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("arg", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession()
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function LocalsInBlockAfterInstructionPointer() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]
        int x = 3;
        string bar = "goo";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("x")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("x")
                state.SendBackspace()
                state.SendTypeChars("bar")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("bar")
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionAfterReturn() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]
        int x = 3;
        string bar = "goo";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("bar")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("bar")
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
                state.SendReturn()
                Assert.Equal("", state.GetCurrentViewLineText())
                state.SendTypeChars("bar")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("bar")
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function ExecutedUnexecutedLocals() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    {
        string goo = "green";
        [|string bar = "goo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("goo")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("goo")
                state.SendTab()
                state.SendTypeChars(".ToS")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("ToString")
                For i As Integer = 0 To 7
                    state.SendBackspace()
                Next
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("green")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("green")
                state.SendTab()
                state.SendTypeChars(".ToS")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("ToString")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function Locals1() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main()
    {
        {
            [|int variable1 = 0;|]
        }
        Console.Write(0);
        int variable2 = 0;
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("variable")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsContainAll("variable1", "variable2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function Locals2() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main()
    {
        {
            int variable1 = 0;
        [|}|]
        Console.Write(0);
        int variable2 = 0;
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("variable")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsContainAll("variable1", "variable2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function Locals3() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main()
    {
        {
            int variable1 = 0;
        }
        [|Console.Write(0);|]
        int variable2 = 0;
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("variable")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsDoNotContainAny("variable1")
                Await state.AssertCompletionItemsContainAll("variable2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function Locals4() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main()
    {
        {
            int variable1 = 0;
        }
        Console.Write(0);
        [|int variable2 = 0;|]
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("variable")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsDoNotContainAny("variable1")
                Await state.AssertCompletionItemsContainAll("variable2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function Locals5() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main()
    {
        {
            int variable1 = 0;
        }
        Console.Write(0);
        int variable2 = 0;
    [|}|]
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("variable")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsDoNotContainAny("variable1")
                Await state.AssertCompletionItemsContainAll("variable2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function Locals6() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main()
    {
        {
            int variable1 = 0;
        }
        Console.Write(0);
        int variable2 = 0;
    }
[|}|]</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("variable")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsDoNotContainAny("variable1")
                Await state.AssertCompletionItemsDoNotContainAny("variable2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function SignatureHelpInParameterizedConstructor() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    {
        string goo = "green";
        [|string bar = "goo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("new string(")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function SignatureHelpInMethodCall() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void something(string z, int b)
    {
    }

    static void Main(string[] args)
    {
        string goo = "green";
        [|string bar = "goo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("something(")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function SignatureHelpInGenericMethodCall() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void something&lt;T&gt;(&lt;T&gt; z, int b)
    {
        return z
    }

    static void Main(string[] args)
    {
        string goo = "green";
        [|string bar = "goo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("something<int>(")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function InstructionPointerInForeach() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    {
        int OOO = 3;
        foreach (var z in "goo")
        {
            [|var q = 1;|]
        }
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                Await state.VerifyCompletionAndDotAfter("q")
                Await state.VerifyCompletionAndDotAfter("OOO")
            End Using
        End Function

        <WorkItem(531165, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531165")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function ClassDesigner1() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static int STATICINT;
    static void Main(string[] args)
    {
    }

[| |]   public void M1()
    {
        throw new System.NotImplementedException();
    }
}
</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("STATICI")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionItemsDoNotContainAny("STATICINT")
            End Using
        End Function

        <WorkItem(531167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531167")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function ClassDesigner2() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    {
    }
[| |]   void M1()
    {
    }
}
</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("1")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(1124544, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1124544")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionUsesContextBufferPositions() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
                                   e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.
(1,3): error CS1001: Identifier expected
e.
(1,3): error CS1001: Identifier expected
e.
(1,3): error CS1001: Identifier expected
e.
(1,3): error CS1001: Identifier expected
e.
(1,3): error CS1001: Identifier expected
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
e.InnerException
{"Exception of type 'System.Exception' was thrown."}
    Data: {System.Collections.ListDictionaryInternal}
    HResult: -2146233088
    HelpLink: null
    InnerException: null
    Message: "Exception of type 'System.Exception' was thrown."
    Source: null
    StackTrace: null
    TargetSite: null
$$</Document>
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]

    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("arg", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession()
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionOnTypeCharacterInLinkedFileContext() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                           </Project>
                           <Project Language="C#" CommonReferences="true" AssemblyName="CSProj">
                               <Document FilePath="C.cs">
{
    static void Main(string[] args)
    [|{|]

    }
}
                              </Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.TextView.TextBuffer.Insert(0, "123123123123123123123123123 + ")
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("123123123123123123123123123 + arg", state.GetCurrentViewLineText())
                state.SendTab()
                Assert.Contains("args", state.GetCurrentViewLineText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function TypeNumberAtStartOfViewDoesNotCrash() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
	{
		static void Main(string[] args)
		[|{|]

		}
	}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("4")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function BuilderSettingRetainedBetweenComputations_Watch() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]

    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, isImmediateWindow:=False)
                state.SendTypeChars("args")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("args", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession()
                Assert.True(state.HasSuggestedItem())
                state.SendToggleCompletionMode()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.HasSuggestedItem())
                state.SendTypeChars(".")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.HasSuggestedItem())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function BuilderSettingRetainedBetweenComputations_Watch_Immediate() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]

    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, isImmediateWindow:=True)
                state.SendTypeChars("args")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("args", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession()
                Assert.True(state.HasSuggestedItem())
                state.SendToggleCompletionMode()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.HasSuggestedItem())
                state.SendTypeChars(".")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.HasSuggestedItem())
            End Using
        End Function

        <WorkItem(1163608, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1163608")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function TestItemDescription() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("args")
                Dim description = Await state.GetSelectedItemDescriptionAsync()
                Assert.Contains("args", description.Text)
                state.SendTab()
                Assert.Contains("args", state.GetCurrentViewLineText())
            End Using
        End Function
    End Class
End Namespace
