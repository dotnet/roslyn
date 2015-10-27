' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    Public Class CSharpDebuggerIntellisenseTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionOnTypeCharacter() As Threading.Tasks.Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal("arg", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionOnTypeCharacterInImmediateWindow() As Threading.Tasks.Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal("arg", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function LocalsInBlockAfterInstructionPointer() As Threading.Tasks.Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]
        int x = 3;
        string bar = "foo";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("x")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("x").ConfigureAwait(True)
                state.SendBackspace()
                state.SendTypeChars("bar")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("bar").ConfigureAwait(True)
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionAfterReturn() As Threading.Tasks.Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void Main(string[] args)
    [|{|]
        int x = 3;
        string bar = "foo";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, True)
                state.SendTypeChars("bar")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("bar").ConfigureAwait(True)
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
                state.SendReturn()
                Assert.Equal("", state.GetCurrentViewLineText())
                state.SendTypeChars("bar")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("bar").ConfigureAwait(True)
                state.SendTab()
                Assert.Equal("    bar", state.GetCurrentViewLineText())
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function ExecutedUnexecutedLocals() As Threading.Tasks.Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void Main(string[] args)
    {
        string foo = "green";
        [|string bar = "foo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("foo")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("foo").ConfigureAwait(True)
                state.SendTab()
                state.SendTypeChars(".ToS")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("ToString").ConfigureAwait(True)
                For i As Integer = 0 To 7
                    state.SendBackspace()
                Next

                state.SendTypeChars("green")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("green").ConfigureAwait(True)
                state.SendTab()
                state.SendTypeChars(".ToS")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("ToString").ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals1()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainAll("variable1", "variable2").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals2()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainAll("variable1", "variable2").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals3()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainNone("variable1").ConfigureAwait(True)
                Await state.AssertCompletionItemsContainAll("variable2").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals4()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainNone("variable1").ConfigureAwait(True)
                Await state.AssertCompletionItemsContainAll("variable2").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals5()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainNone("variable1").ConfigureAwait(True)
                Await state.AssertCompletionItemsContainAll("variable2").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub Locals6()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainNone("variable1").ConfigureAwait(True)
                Await state.AssertCompletionItemsContainNone("variable2").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInParameterizedConstructor()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void Main(string[] args)
    {
        string foo = "green";
        [|string bar = "foo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("new string(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInMethodCall()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void something(string z, int b)
    {
    }

    static void Main(string[] args)
    {
        string foo = "green";
        [|string bar = "foo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("something(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInGenericMethodCall()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void something&lt;T&gt;(&lt;T&gt; z, int b)
    {
        return z
    }

    static void Main(string[] args)
    {
        string foo = "green";
        [|string bar = "foo";|]
        string green = "yellow";
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                state.SendTypeChars("something<int>(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function InstructionPointerInForeach() As Task
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>class Program
{
    static void Main(string[] args)
    {
        int OOO = 3;
        foreach (var z in "foo")
        {
            [|var q = 1;|]
        }
    }
}</Document>
                           </Project>
                       </Workspace>

            Using state = TestState.CreateCSharpTestState(text, False)
                Await VerifyCompletionAndDotAfter("q", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("OOO", state).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(531165)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub ClassDesigner1()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionItemsContainNone("STATICINT").ConfigureAwait(True)
            End Using
        End Sub

        <WorkItem(531167)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub ClassDesigner2()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>$$</Document>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Sub

        <WorkItem(1124544)>
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal("arg", state.GetCurrentViewLineText())
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub CompletionOnTypeCharacterInLinkedFileContext()
            Dim text = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document>
123123123123123123123123123 + $$</Document>
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
                state.SendTypeChars("arg")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal("123123123123123123123123123 + arg", state.GetCurrentViewLineText())
                state.SendTab()
                Assert.Contains("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        Private Async Function VerifyCompletionAndDotAfter(item As String, state As TestState) As Threading.Tasks.Task
            state.SendTypeChars(item)
            Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
            Await state.AssertSelectedCompletionItem(item).ConfigureAwait(True)
            state.SendTab()
            state.SendTypeChars(".")
            Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
            Await state.AssertCompletionSession().ConfigureAwait(True)
            For i As Integer = 0 To item.Length
                state.SendBackspace()
            Next
        End Function

    End Class
End Namespace
