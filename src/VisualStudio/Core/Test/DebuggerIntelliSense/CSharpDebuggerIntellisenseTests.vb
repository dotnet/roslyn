' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense

    Public Class CSharpDebuggerIntellisenseTests

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionOnTypeCharacter()
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
                Assert.Equal("arg", state.GetCurrentViewLineText())
                state.AssertCompletionSession()
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionOnTypeCharacterInImmediateWindow()
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
                Assert.Equal("arg", state.GetCurrentViewLineText())
                state.AssertCompletionSession()
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub LocalsInBlockAfterInstructionPointer()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("x")
                state.SendBackspace()
                state.SendTypeChars("bar")
                state.AssertSelectedCompletionItem("bar")
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionAfterReturn()
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
                state.AssertSelectedCompletionItem("bar")
                state.SendTab()
                Assert.Equal("bar", state.GetCurrentViewLineText())
                state.SendReturn()
                Assert.Equal("", state.GetCurrentViewLineText())
                state.SendTypeChars("bar")
                state.AssertSelectedCompletionItem("bar")
                state.SendTab()
                Assert.Equal("    bar", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub ExecutedUnexecutedLocals()
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
                state.AssertSelectedCompletionItem("foo")
                state.SendTab()
                state.SendTypeChars(".ToS")
                state.AssertSelectedCompletionItem("ToString")
                For i As Integer = 0 To 7
                    state.SendBackspace()
                Next

                state.SendTypeChars("green")
                state.AssertSelectedCompletionItem("green")
                state.SendTab()
                state.SendTypeChars(".ToS")
                state.AssertSelectedCompletionItem("ToString")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub Locals1()
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
                state.AssertCompletionItemsContainAll("variable1", "variable2")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub Locals2()
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
                state.AssertCompletionItemsContainAll("variable1", "variable2")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub Locals3()
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
                state.AssertCompletionItemsContainNone("variable1")
                state.AssertCompletionItemsContainAll("variable2")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub Locals4()
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
                state.AssertCompletionItemsContainNone("variable1")
                state.AssertCompletionItemsContainAll("variable2")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub Locals5()
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
                state.AssertCompletionItemsContainNone("variable1")
                state.AssertCompletionItemsContainAll("variable2")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub Locals6()
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
                state.AssertCompletionItemsContainNone("variable1")
                state.AssertCompletionItemsContainNone("variable2")
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SignatureHelpInParameterizedConstructor()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SignatureHelpInMethodCall()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SignatureHelpInGenericMethodCall()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub InstructionPointerInForeach()
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
                VerifyCompletionAndDotAfter("q", state)
                VerifyCompletionAndDotAfter("OOO", state)
            End Using
        End Sub

        <WorkItem(531165)>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub ClassDesigner1()
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
                state.AssertCompletionItemsContainNone("STATICINT")
            End Using
        End Sub

        <WorkItem(531167)>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub ClassDesigner2()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(1124544)>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionUsesContextBufferPositions()
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
                Assert.Equal("arg", state.GetCurrentViewLineText())
                state.AssertCompletionSession()
                state.SendTab()
                Assert.Equal("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionOnTypeCharacterInLinkedFileContext()
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
                Assert.Equal("123123123123123123123123123 + arg", state.GetCurrentViewLineText())
                state.AssertCompletionSession()
                state.SendTab()
                Assert.Contains("args", state.GetCurrentViewLineText())
            End Using
        End Sub

        Private Sub VerifyCompletionAndDotAfter(item As String, state As TestState)
            state.SendTypeChars(item)
            state.AssertSelectedCompletionItem(item)
            state.SendTab()
            state.SendTypeChars(".")
            state.AssertCompletionSession()
            For i As Integer = 0 To item.Length
                state.SendBackspace()
            Next
        End Sub

    End Class
End Namespace
