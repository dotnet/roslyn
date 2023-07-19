' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
    Public Class VisualBasicDebuggerIntellisenseTests
        <WpfFact>
        Public Async Function QueryVariables() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.VerifyCompletionAndDotAfter("x")
            End Using
        End Function

        <WpfFact>
        Public Async Function EnteringMethod() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    [|Sub Main(args As String())|]
        Dim z = 4
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await state.VerifyCompletionAndDotAfter("args")
                Await state.VerifyCompletionAndDotAfter("z")
            End Using
        End Function

        <WpfFact>
        Public Async Function ExitingMethod() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    Sub Main(args As String())
        Dim z = 4
    [|End Sub|]
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await state.VerifyCompletionAndDotAfter("args")
                Await state.VerifyCompletionAndDotAfter("z")
            End Using
        End Function

        <WpfFact>
        Public Async Function SingleLineLambda() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    Sub Main(args As String())
        Dim z = [|Function(x) x + 5|]
        z(3)
    End Sub
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await state.VerifyCompletionAndDotAfter("x")
            End Using
        End Function

        <WpfFact>
        Public Async Function MultiLineLambda() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.VerifyCompletionAndDotAfter("x")
            End Using
        End Function

        <WpfFact>
        Public Async Function LocalVariables() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.VerifyCompletionAndDotAfter("bar")
                Await state.VerifyCompletionAndDotAfter("y")
                Await state.VerifyCompletionAndDotAfter("z")
            End Using
        End Function

        <WpfFact>
        Public Async Function CompletionAfterReturn() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.VerifyCompletionAndDotAfter("bar")
                Await state.VerifyCompletionAndDotAfter("y")
                Await state.VerifyCompletionAndDotAfter("z")
                state.SendReturn()
                Await state.VerifyCompletionAndDotAfter("y")
            End Using
        End Function

        <WpfFact>
        Public Async Function TypeALineTenTimes() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("z")
                    state.SendTab()
                    state.SendTypeChars(".")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertCompletionSession()
                    state.SendReturn()
                    Await state.AssertNoCompletionSession()
                    state.SendReturn()
                    Assert.DoesNotContain("z", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Next
            End Using
        End Function

        <WpfFact>
        Public Async Function SignatureHelpInParameterizedConstructor() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function SignatureHelpInMethodCall() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function SignatureHelpInGenericMethod() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    Sub Self(Of T)(goo as T)
        Return goo
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function CompletionInExpression() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    Sub Self(Of T)(goo as T)
        Return goo
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function CompletionShowTypesFromProjectReference() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <ProjectReference>ReferencedProject</ProjectReference>
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("AClass")
            End Using
        End Function

        <WpfFact>
        Public Async Function CompletionForGenericType() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    Sub Self(Of T)(goo as T)
        Return goo
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
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function LocalsInForBlock() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.VerifyCompletionAndDotAfter("q")
                Await state.VerifyCompletionAndDotAfter("xx")
                Await state.VerifyCompletionAndDotAfter("z")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1044441")>
        <WpfFact>
        Public Async Function StoppedOnEndSub() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>Module Program
    Sub Main(o as Integer)
    [|End Sub|]
End Module</Document>
                           </Project>
                       </Workspace>
            Using state = TestState.CreateVisualBasicTestState(text, False)
                Await state.VerifyCompletionAndDotAfter("o")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1044441")>
        <WpfFact>
        Public Async Function StoppedOnEndProperty() As Task
            Dim text = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
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
                Await state.VerifyCompletionAndDotAfter("value")
            End Using
        End Function
    End Class
End Namespace
