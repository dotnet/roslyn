' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticEndConstructCorrection
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
    Public Class AutomaticEndConstructCorrectorTests
        <WpfFact>
        Public Sub TestStructureToInterface()
            Dim code = <code>[|Structure|] A
End [|Structure|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestEnumToInterface()
            Dim code = <code>[|Enum|] A
End [|Enum|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestInterfaceToEnum()
            Dim code = <code>[|Interface|] A
End [|Interface|]</code>.Value

            Verify(code, "Enum")
        End Sub

        <WpfFact>
        Public Sub TestClassToInterface()
            Dim code = <code>[|Class|] A
End [|Class|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestModuleToInterface()
            Dim code = <code>[|Module|] A
End [|Module|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestNamespaceToInterface()
            Dim code = <code>[|Namespace|] A
End [|Namespace|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestSubToFunction()
            Dim code = <code>Class A
    [|Sub|] Test()
    End [|Sub|]
End Class</code>.Value

            Verify(code, "Function")
        End Sub

        <WpfFact>
        Public Sub TestFunctionToSub()
            Dim code = <code>Class A
    [|Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Verify(code, "Sub")
        End Sub

        <WpfFact>
        Public Sub TestModuleToInterface1()
            Dim code = <code>[|Module|] A : End [|Module|] : Module B : End Module</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestModuleToInterface2()
            Dim code = <code>Module A : End Module : [|Module|] B : End [|Module|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestModuleToInterface3()
            Dim code = <code>Module A : End Module:[|Module|] B : End [|Module|]</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestModuleToInterface4()
            Dim code = <code>[|Module|] A : End [|Module|]:Module B : End Module</code>.Value

            Verify(code, "Interface")
        End Sub

        <WpfFact>
        Public Sub TestErrorCaseMissingEndFunction()
            Dim code = <code>Class A
    [|Function|] Test() As Integer
    End [|Sub|]
End Class</code>.Value.Replace(vbLf, vbCrLf)

            VerifyBegin(code, "Interface", "Sub")
            VerifyEnd(code, "Interface", "Function")
        End Sub

        <WpfFact>
        Public Sub TestContinuousEditsOnFunctionToInterface()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Interface", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        Public Sub TestContinuousEditsOnFunctionToInterfaceWithLeadingSpaces()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "     Interface", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        Public Sub TestContinuousEditsOnFunctionToInterfaceWithTrailingSpaces()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Interface              ", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        Public Sub TestContinuousEditsOnFunctionToInterfaceWithLeadingAndTrailingSpaces()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "             Interface              ", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact>
        Public Sub TestAddSharedModifierToFunction()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, " Shared ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        Public Sub TestAddSharedModifierToFunction1()
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Shared   ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        Public Sub TestAddTrailingSpaceToFunction()
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "           ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        Public Sub TestAddLeadingSpaceToFunction()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "           ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact>
        Public Sub TestAddSharedModifierToFunction2()
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Shared", Function(s) "Function", removeOriginalContent:=False, split:="Function")
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539362")>
        Public Sub TestMultiLineLambdaSubToFunction()
            Dim code = <code>Class A
    Public Sub F()
        Dim nums() As Integer = {1, 2, 3, 4, 5}
        Array.ForEach(nums, [|Sub|](n)
                                Console.Write("Number: ")
                                Console.WriteLine(n)
                            End [|Sub|])
    End Sub
End Class</code>.Value

            Verify(code, "Function")
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539362")>
        Public Sub TestMultiLineLambdaFunctionToSub()
            Dim code = <code>Class A
    Public Sub F()
        Dim nums() As Integer = {1, 2, 3, 4, 5}
        Dim numDelegate = [|Function|](n As Integer)

                          End [|Function|]
    End Sub
End Class</code>.Value

            Verify(code, "Sub")
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539365")>
        Public Sub BugFix5290()
            Dim code = <code>Public Class Class1
    Sub M()
        [|Class|]
    End Sub
End [|Class|]</code>.Value

            VerifyBegin(code, "Structure", "Class")
            VerifyEnd(code, "Structure", "Class")
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539357")>
        Public Sub TestBugFix5276()
            Dim code = <code>Class A
    [|Func$$tion|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "  ", Function(s) "Function", removeOriginalContent:=False)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539360")>
        Public Sub TestBugFix5283()
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            VerifyContinuousEdits(code, "Shared Sub", Function(s) If(s.Trim() = "Shared Sub", "Sub", "Function"), removeOriginalContent:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539498")>
        Public Sub TestDoNotThrowDueToSingleLineDeletion()
            Dim code = <code>Class A
    [|$$Sub M() : End Sub|]
End Class</code>.Value

            VerifyContinuousEdits(code, "", Function() "", removeOriginalContent:=True)
        End Sub

        <WpfFact>
        Public Sub TestPropertySet()
            Dim code = <code>Class A
    Property Test
        [|Get|]
        End [|Get|]
        Set(value)
        End Set
    End Property
End Class</code>.Value

            Verify(code, "Set")
        End Sub

        <WpfFact>
        Public Sub TestPropertyGet()
            Dim code = <code>Class A
    Property Test
        Get
        End Get
        [|Set|](value)
        End [|Set|]
    End Property
End Class</code>.Value

            Verify(code, "Get")
        End Sub

        Private Shared Sub VerifyContinuousEdits(codeWithMarker As String,
                                          type As String,
                                          expectedStringGetter As Func(Of String, String),
                                          removeOriginalContent As Boolean,
                                          Optional split As String = Nothing)
            ' do this since xml value put only vbLf
            codeWithMarker = codeWithMarker.Replace(vbLf, vbCrLf)

            Using workspace = EditorTestWorkspace.CreateVisualBasic(codeWithMarker)
                Dim document = workspace.Documents.Single()

                Dim buffer = document.GetTextBuffer()
                Dim initialTextSnapshot = buffer.CurrentSnapshot

                Dim caretPosition = initialTextSnapshot.CreateTrackingPoint(document.CursorPosition.Value,
                                                                            PointTrackingMode.Positive,
                                                                            TrackingFidelityMode.Backward)
                Dim corrector = New AutomaticEndConstructCorrector(buffer, workspace.GetService(Of IUIThreadOperationExecutor))

                corrector.Connect()

                If removeOriginalContent Then
                    Dim spanToRemove = document.SelectedSpans.Single(Function(s) s.Contains(caretPosition.GetPosition(initialTextSnapshot)))
                    buffer.Replace(spanToRemove.ToSpan(), "")
                End If

                For i = 0 To type.Length - 1
                    Dim charToInsert = type(i)
                    buffer.Insert(caretPosition.GetPosition(buffer.CurrentSnapshot), charToInsert)

                    Dim insertedString = type.Substring(0, i + 1)
                    For Each span In document.SelectedSpans.Skip(1)
                        Dim trackingSpan = New LetterOnlyTrackingSpan(span.ToSnapshotSpan(initialTextSnapshot))
                        Assert.Equal(expectedStringGetter(insertedString), trackingSpan.GetText(document.GetTextBuffer().CurrentSnapshot))
                    Next
                Next

                If split IsNot Nothing Then
                    Dim beginSpan = document.SelectedSpans.First()
                    Dim trackingSpan = New LetterOnlyTrackingSpan(beginSpan.ToSnapshotSpan(initialTextSnapshot))

                    buffer.Insert(trackingSpan.GetEndPoint(buffer.CurrentSnapshot).Position - type.Trim().Length, " ")

                    Assert.Equal(split, trackingSpan.GetText(buffer.CurrentSnapshot))
                End If

                corrector.Disconnect()
            End Using
        End Sub

        Private Shared Sub Verify(codeWithMarker As String, keyword As String)
            ' do this since xml value put only vbLf
            codeWithMarker = codeWithMarker.Replace(vbLf, vbCrLf)

            VerifyBegin(codeWithMarker, keyword)
            VerifyEnd(codeWithMarker, keyword)
        End Sub

        Private Shared Sub VerifyBegin(code As String, keyword As String, Optional expected As String = Nothing)
            Using workspace = EditorTestWorkspace.CreateVisualBasic(code)
                Dim document = workspace.Documents.Single()

                Dim selectedSpans = document.SelectedSpans

                Dim spanToReplace = selectedSpans.First()
                Dim spanToVerify = selectedSpans.Skip(1).Single()

                Verify(document, keyword, expected, spanToReplace, spanToVerify, workspace)
            End Using
        End Sub

        Private Shared Sub VerifyEnd(code As String, keyword As String, Optional expected As String = Nothing)
            Using workspace = EditorTestWorkspace.CreateVisualBasic(code)
                Dim document = workspace.Documents.Single()

                Dim selectedSpans = document.SelectedSpans

                Dim spanToReplace = selectedSpans.Skip(1).Single()
                Dim spanToVerify = selectedSpans.First()

                Verify(document, keyword, expected, spanToReplace, spanToVerify, workspace)
            End Using
        End Sub

        Private Shared Sub Verify(document As EditorTestHostDocument, keyword As String, expected As String, spanToReplace As TextSpan, spanToVerify As TextSpan, workspace As EditorTestWorkspace)
            Dim buffer = document.GetTextBuffer()
            Dim uiThreadOperationExecutor = workspace.GetService(Of IUIThreadOperationExecutor)
            Dim corrector = New AutomaticEndConstructCorrector(buffer, uiThreadOperationExecutor)

            corrector.Connect()
            buffer.Replace(spanToReplace.ToSpan(), keyword)
            corrector.Disconnect()

            expected = If(expected Is Nothing, keyword, expected)

            Dim correspondingSpan = document.InitialTextSnapshot.CreateTrackingSpan(spanToVerify.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Assert.Equal(expected, correspondingSpan.GetText(buffer.CurrentSnapshot))
        End Sub
    End Class
End Namespace
