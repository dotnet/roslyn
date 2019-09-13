' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    <[UseExportProvider]>
    Public Class CSharpDebuggerIntellisenseTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals1()
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
                Await state.AssertCompletionItemsContainAll({"variable1", "variable2"})
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals2()
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
                Await state.AssertCompletionItemsContainAll({"variable1", "variable2"})
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals3()
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
                Await state.AssertCompletionItemsDoNotContainAny({"variable1"})
                Await state.AssertCompletionItemsContainAll({"variable2"})
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals4()
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
                Await state.AssertCompletionItemsDoNotContainAny({"variable1"})
                Await state.AssertCompletionItemsContainAll({"variable2"})
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals5()
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
                Await state.AssertCompletionItemsDoNotContainAny({"variable1"})
                Await state.AssertCompletionItemsContainAll({"variable2"})
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals6()
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
                Await state.AssertCompletionItemsDoNotContainAny({"variable1"})
                Await state.AssertCompletionItemsDoNotContainAny({"variable2"})
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInParameterizedConstructor()
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInMethodCall()
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInGenericMethodCall()
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub ClassDesigner1()
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
                Await state.AssertCompletionItemsDoNotContainAny({"STATICINT"})
            End Using
        End Sub

        <WorkItem(531167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531167")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub ClassDesigner2()
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
        End Sub

        <WorkItem(1124544, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1124544")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub CompletionUsesContextBufferPositions()
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub CompletionOnTypeCharacterInLinkedFileContext()
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

            Using state = TestState.CreateCSharpTestState(text, True, <Document>123123123123123123123123123 + $$</Document>)
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("123123123123123123123123123 + arg", state.GetCurrentViewLineText())
                state.SendTab()
                Assert.Contains("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
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
    End Class
End Namespace
