' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense
    Public Class VisualBasicDebuggerIntellisenseTests
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function QueryVariables() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim bar = From x In "asdf"
                  Where [|x = "d"|]
                  Select x
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("x", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function EnteringMethod() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    [|Sub Main(args As String())|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("args", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("z", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function ExitingMethod() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim z = 4
    [|End Sub|]
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("args", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("z", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function SingleLineLambda() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim z = [|Function(x) x + 5|]
        z(3)
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("x", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function MultiLineLambda() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim z = [|Function(x)|]
                    Return x + 5
                End Function
        z(3)
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("x", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function LocalVariables() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim bar as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("bar", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("y", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("z", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Function CompletionAfterReturn() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim bar as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, True)
                Await VerifyCompletionAndDotAfter("bar", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("y", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("z", state).ConfigureAwait(True)
                state.SendReturn()
                Await VerifyCompletionAndDotAfter("y", state).ConfigureAwait(True)
            End Using
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub TypeALineTenTimes()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, True)
                For xx = 0 To 10
                    state.SendTypeChars("z")
                    Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                    Await state.AssertSelectedCompletionItem("z").ConfigureAwait(True)
                    state.SendTab()
                    state.SendTypeChars(".")
                    Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                    Await state.AssertCompletionSession().ConfigureAwait(True)
                    state.SendReturn()
                Next
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInParameterizedConstructor()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                state.SendTypeChars("new String(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInMethodCall()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                state.SendTypeChars("Main(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub SignatureHelpInGenericMethod()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Self(Of T)(foo as T)
        Return foo
    End Sub

    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                state.SendTypeChars("Self(Of Integer)(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub CompletionInExpression()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Self(Of T)(foo as T)
        Return foo
    End Sub

    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                state.SendTypeChars("new List(Of String) From { a")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub CompletionShowTypesFromProjectReference()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <ProjectReference>ReferencedProject</ProjectReference>
                               <Document>$$</Document>

                               <Document>Module Program

    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                           <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ReferencedProject">
                               <Document>
Public Class AClass
    Sub New()
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                state.SendTypeChars("new AClass")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("AClass").ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub CompletionForGenericType()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Self(Of T)(foo as T)
        Return foo
    End Sub

    Sub Main(args As String())
        Dim xx as String = "boo"
        [|Dim y = 3|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                state.SendTypeChars("Self(Of ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub LocalsInForBlock()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>

                               <Document>Module Program
    Sub Main(args As String())
        Dim xx as String = "boo"
        Dim y = 3
        Dim z = 4
        For xx As Integer = 1 To 10
            [|Dim q = xx + 2|]
        Next

    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("q", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("xx", state).ConfigureAwait(True)
                Await VerifyCompletionAndDotAfter("z", state).ConfigureAwait(True)
            End Using
        End Sub

        Private Async Function VerifyCompletionAndDotAfter(item As String, state As TestState) As Task
            If state.IsImmediateWindow Then
                state.SendTypeChars("?")
            End If
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

        <WorkItem(1044441)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub StoppedOnEndSub()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>Module Program
    Sub Main(o as Integer)
    [|End Sub|]
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("o", state).ConfigureAwait(True)
            End Using
        End Sub

        <WorkItem(1044441)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Async Sub StoppedOnEndProperty()
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>$$</Document>
                               <Document>Class C
    Public Property x As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        [|End Set|]
    End Property
End Class</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await VerifyCompletionAndDotAfter("value", state).ConfigureAwait(True)
            End Using
        End Sub
    End Class
End Namespace
