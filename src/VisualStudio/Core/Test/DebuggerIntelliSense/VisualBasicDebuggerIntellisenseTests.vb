' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.DebuggerIntelliSense
    Public Class VisualBasicDebuggerIntellisenseTests
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub QueryVariables()
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
                VerifyCompletionAndDotAfter("x", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub EnteringMethod()
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
                VerifyCompletionAndDotAfter("args", state)
                VerifyCompletionAndDotAfter("z", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub ExitingMethod()
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
                VerifyCompletionAndDotAfter("args", state)
                VerifyCompletionAndDotAfter("z", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SingleLineLambda()
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
                VerifyCompletionAndDotAfter("x", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub MultiLineLambda()
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
                VerifyCompletionAndDotAfter("x", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub LocalVariables()
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
                VerifyCompletionAndDotAfter("bar", state)
                VerifyCompletionAndDotAfter("y", state)
                VerifyCompletionAndDotAfter("z", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionAfterReturn()
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
                VerifyCompletionAndDotAfter("bar", state)
                VerifyCompletionAndDotAfter("y", state)
                VerifyCompletionAndDotAfter("z", state)
                state.SendReturn()
                VerifyCompletionAndDotAfter("y", state)
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub TypeALineTenTimes()
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
                    state.AssertSelectedCompletionItem("z")
                    state.SendTab()
                    state.SendTypeChars(".")
                    state.AssertCompletionSession()
                    state.SendReturn()
                Next
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SignatureHelpInParameterizedConstructor()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SignatureHelpInMethodCall()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub SignatureHelpInGenericMethod()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionInExpression()
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
                state.AssertCompletionSession()
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionShowTypesFromProjectReference()
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
                state.AssertSelectedCompletionItem("AClass")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub CompletionForGenericType()
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
                state.AssertCompletionSession()
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub LocalsInForBlock()
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
                VerifyCompletionAndDotAfter("q", state)
                VerifyCompletionAndDotAfter("xx", state)
                VerifyCompletionAndDotAfter("z", state)
            End Using
        End Sub

        Private Sub VerifyCompletionAndDotAfter(item As String, state As TestState)
            If state.IsImmediateWindow Then
                state.SendTypeChars("?")
            End If
            state.SendTypeChars(item)
            state.AssertSelectedCompletionItem(item)
            state.SendTab()
            state.SendTypeChars(".")
            state.AssertCompletionSession()
            For i As Integer = 0 To item.Length
                state.SendBackspace()
            Next
        End Sub

        <WorkItem(1044441)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub StoppedOnEndSub()
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
                VerifyCompletionAndDotAfter("o", state)
            End Using
        End Sub

        <WorkItem(1044441)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.DebuggingIntelliSense)>
        Public Sub StoppedOnEndProperty()
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
                VerifyCompletionAndDotAfter("value", state)
            End Using
        End Sub
    End Class
End Namespace
