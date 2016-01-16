' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticEndConstructCorrection
    Public Class AutomaticEndConstructCorrectorTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestStructureToInterface() As Task
            Dim code = <code>[|Structure|] A
End [|Structure|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestEnumToInterface() As Task
            Dim code = <code>[|Enum|] A
End [|Enum|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestInterfaceToEnum() As Task
            Dim code = <code>[|Interface|] A
End [|Interface|]</code>.Value

            Await VerifyAsync(code, "Enum")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestClassToInterface() As Task
            Dim code = <code>[|Class|] A
End [|Class|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestModuleToInterface() As Task
            Dim code = <code>[|Module|] A
End [|Module|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestNamespaceToInterface() As Task
            Dim code = <code>[|Namespace|] A
End [|Namespace|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestSubToFunction() As Task
            Dim code = <code>Class A
    [|Sub|] Test()
    End [|Sub|]
End Class</code>.Value

            Await VerifyAsync(code, "Function")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestFunctionToSub() As Task
            Dim code = <code>Class A
    [|Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyAsync(code, "Sub")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestModuleToInterface1() As Task
            Dim code = <code>[|Module|] A : End [|Module|] : Module B : End Module</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestModuleToInterface2() As Task
            Dim code = <code>Module A : End Module : [|Module|] B : End [|Module|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestModuleToInterface3() As Task
            Dim code = <code>Module A : End Module:[|Module|] B : End [|Module|]</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestModuleToInterface4() As Task
            Dim code = <code>[|Module|] A : End [|Module|]:Module B : End Module</code>.Value

            Await VerifyAsync(code, "Interface")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestErrorCaseMissingEndFunction() As Task
            Dim code = <code>Class A
    [|Function|] Test() As Integer
    End [|Sub|]
End Class</code>.Value.Replace(vbLf, vbCrLf)

            Await VerifyBeginAsync(code, "Interface", "Sub")
            Await VerifyEndAsync(code, "Interface", "Function")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestContinuousEditsOnFunctionToInterface() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "Interface", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestContinuousEditsOnFunctionToInterfaceWithLeadingSpaces() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "     Interface", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestContinuousEditsOnFunctionToInterfaceWithTrailingSpaces() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "Interface              ", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestContinuousEditsOnFunctionToInterfaceWithLeadingAndTrailingSpaces() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "             Interface              ", Function(s) If(s.Trim() = "Interface", "Interface", "Function"), removeOriginalContent:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestAddSharedModifierToFunction() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, " Shared ", Function(s) "Function", removeOriginalContent:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestAddSharedModifierToFunction1() As Task
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "Shared   ", Function(s) "Function", removeOriginalContent:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestAddTrailingSpaceToFunction() As Task
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "           ", Function(s) "Function", removeOriginalContent:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestAddLeadingSpaceToFunction() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "           ", Function(s) "Function", removeOriginalContent:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestAddSharedModifierToFunction2() As Task
            Dim code = <code>Class A
    [|Function$$|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "Shared", Function(s) "Function", removeOriginalContent:=False, split:="Function")
        End Function

        <WorkItem(539362)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestMultiLineLambdaSubToFunction() As Task
            Dim code = <code>Class A
    Public Sub F()
        Dim nums() As Integer = {1, 2, 3, 4, 5}
        Array.ForEach(nums, [|Sub|](n)
                                Console.Write("Number: ")
                                Console.WriteLine(n)
                            End [|Sub|])
    End Sub
End Class</code>.Value

            Await VerifyAsync(code, "Function")
        End Function

        <WorkItem(539362)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestMultiLineLambdaFunctionToSub() As Task
            Dim code = <code>Class A
    Public Sub F()
        Dim nums() As Integer = {1, 2, 3, 4, 5}
        Dim numDelegate = [|Function|](n As Integer)

                          End [|Function|]
    End Sub
End Class</code>.Value

            Await VerifyAsync(code, "Sub")
        End Function

        <WorkItem(539365)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function BugFix5290() As Task
            Dim code = <code>Public Class Class1
    Sub M()
        [|Class|]
    End Sub
End [|Class|]</code>.Value

            Await VerifyBeginAsync(code, "Structure", "Class")
            Await VerifyEndAsync(code, "Structure", "Class")
        End Function

        <WorkItem(539357)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestBugFix5276() As Task
            Dim code = <code>Class A
    [|Func$$tion|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "  ", Function(s) "Function", removeOriginalContent:=False)
        End Function

        <WorkItem(539360)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestBugFix5283() As Task
            Dim code = <code>Class A
    [|$$Function|] Test() As Integer
    End [|Function|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "Shared Sub", Function(s) If(s.Trim() = "Shared Sub", "Sub", "Function"), removeOriginalContent:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        <WorkItem(539498)>
        Public Async Function TestDontThrowDueToSingleLineDeletion() As Task
            Dim code = <code>Class A
    [|$$Sub M() : End Sub|]
End Class</code>.Value

            Await VerifyContinuousEditsAsync(code, "", Function() "", removeOriginalContent:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestPropertySet() As Task
            Dim code = <code>Class A
    Property Test
        [|Get|]
        End [|Get|]
        Set(value)
        End Set
    End Property
End Class</code>.Value

            Await VerifyAsync(code, "Set")
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Async Function TestPropertyGet() As Task
            Dim code = <code>Class A
    Property Test
        Get
        End Get
        [|Set|](value)
        End [|Set|]
    End Property
End Class</code>.Value

            Await VerifyAsync(code, "Get")
        End Function

        Private Async Function VerifyContinuousEditsAsync(codeWithMarker As String,
                                          type As String,
                                          expectedStringGetter As Func(Of String, String),
                                          removeOriginalContent As Boolean,
                                          Optional split As String = Nothing) As Task
            ' do this since xml value put only vbLf
            codeWithMarker = codeWithMarker.Replace(vbLf, vbCrLf)

            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(codeWithMarker)
                Dim document = workspace.Documents.Single()

                Dim buffer = document.TextBuffer
                Dim snapshot = buffer.CurrentSnapshot

                Dim caretPosition = snapshot.CreateTrackingPoint(document.CursorPosition.Value,
                                                                 PointTrackingMode.Positive,
                                                                 TrackingFidelityMode.Backward)
                Dim corrector = New AutomaticEndConstructCorrector(buffer, New TestWaitIndicator())

                corrector.Connect()

                If removeOriginalContent Then
                    Dim spanToRemove = document.SelectedSpans.Single(Function(s) s.Contains(caretPosition.GetPosition(snapshot)))
                    buffer.Replace(spanToRemove.ToSpan(), "")
                End If

                For i = 0 To type.Length - 1
                    Dim charToInsert = type(i)
                    buffer.Insert(caretPosition.GetPosition(buffer.CurrentSnapshot), charToInsert)

                    Dim insertedString = type.Substring(0, i + 1)
                    For Each span In document.SelectedSpans.Skip(1)
                        Dim trackingSpan = New LetterOnlyTrackingSpan(span.ToSnapshotSpan(document.InitialTextSnapshot))
                        Assert.Equal(expectedStringGetter(insertedString), trackingSpan.GetText(document.TextBuffer.CurrentSnapshot))
                    Next
                Next

                If split IsNot Nothing Then
                    Dim beginSpan = document.SelectedSpans.First()
                    Dim trackingSpan = New LetterOnlyTrackingSpan(beginSpan.ToSnapshotSpan(document.InitialTextSnapshot))

                    buffer.Insert(trackingSpan.GetEndPoint(buffer.CurrentSnapshot).Position - type.Trim().Length, " ")

                    Assert.Equal(split, trackingSpan.GetText(buffer.CurrentSnapshot))
                End If

                corrector.Disconnect()
            End Using
        End Function

        Private Async Function VerifyAsync(codeWithMarker As String, keyword As String) As Task
            ' do this since xml value put only vbLf
            codeWithMarker = codeWithMarker.Replace(vbLf, vbCrLf)

            Await VerifyBeginAsync(codeWithMarker, keyword)
            Await VerifyEndAsync(codeWithMarker, keyword)
        End Function

        Private Async Function VerifyBeginAsync(code As String, keyword As String, Optional expected As String = Nothing) As Task
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code)
                Dim document = workspace.Documents.Single()

                Dim selectedSpans = document.SelectedSpans

                Dim spanToReplace = selectedSpans.First()
                Dim spanToVerify = selectedSpans.Skip(1).Single()

                Verify(workspace, document, keyword, expected, spanToReplace, spanToVerify)
            End Using
        End Function

        Private Async Function VerifyEndAsync(code As String, keyword As String, Optional expected As String = Nothing) As Task
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code)
                Dim document = workspace.Documents.Single()

                Dim selectedSpans = document.SelectedSpans

                Dim spanToReplace = selectedSpans.Skip(1).Single()
                Dim spanToVerify = selectedSpans.First()

                Verify(workspace, document, keyword, expected, spanToReplace, spanToVerify)
            End Using
        End Function

        Private Sub Verify(workspace As TestWorkspace, document As TestHostDocument, keyword As String, expected As String, spanToReplace As TextSpan, spanToVerify As TextSpan)
            Dim buffer = document.TextBuffer
            Dim corrector = New AutomaticEndConstructCorrector(buffer, New TestWaitIndicator())

            corrector.Connect()
            buffer.Replace(spanToReplace.ToSpan(), keyword)
            corrector.Disconnect()

            expected = If(expected Is Nothing, keyword, expected)

            Dim correspondingSpan = document.InitialTextSnapshot.CreateTrackingSpan(spanToVerify.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Assert.Equal(expected, correspondingSpan.GetText(buffer.CurrentSnapshot))
        End Sub
    End Class
End Namespace
