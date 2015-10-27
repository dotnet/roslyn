Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Public Class VisualBasicGoToNextAndPreviousMemberTests

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub EmptyFile()
        Assert.Null(GetTargetPosition("$$", next:=True))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub ClassWithNoMembers()
        Dim code = "Class C
$$
End Class"
        Assert.Null(GetTargetPosition(code, next:=True))
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub BeforeClassWithMember()
        Dim code = "$$
Class C
    [||]Sub M()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub AfterClassWithMember()
        Dim code = "
Class C
    [||]Sub M()
    End Sub
End Class

$$"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub BetweenClasses()
        Dim code = "
Class C1
    Sub M()
    End Sub
End Class

$$

Class C2
    [||]Sub M()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub BetweenClassesPrevious()
        Dim code = "
Class C1
    [||]Sub M()
    End Sub
End Class

$$

Class C2
    Sub M()
    End Sub
End Class"

        AssertNavigated(code, next:=False)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub FromFirstMemberToSecond()
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub FromSecondToFirst()
        Dim code = "
Class C
    [||]Sub M1()
    End Sub
    $$Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=False)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub NextWraps()
        Dim code = "
Class C
    [||]Sub M1()
    End Sub
    $$Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub PreviousWraps()
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=False)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub DescendsIntoNestedType()
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    Class N
        [||]Sub M2()
        End Sub
    End Class
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtConstructor()
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Public Sub New()
    End Sub
End Class"
        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtOperator()
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Shared Operator +(left As VisualBasicGoToNextAndPreviousMemberTests, right As VisualBasicGoToNextAndPreviousMemberTests) As VisualBasicGoToNextAndPreviousMemberTests
        Throw New System.NotImplementedException()
    End Operator
End Class"
        AssertNavigated(code, next:=True)
    End Sub

    Shared Operator +(left As VisualBasicGoToNextAndPreviousMemberTests, right As VisualBasicGoToNextAndPreviousMemberTests) As VisualBasicGoToNextAndPreviousMemberTests
        Throw New System.NotImplementedException()
    End Operator


    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtField()
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Dim f as Integer
End Class"
        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtFieldlikeEvent()
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Event E As System.EventHandler
End Class"
        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtAutoProperty()
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Property P As Integer
End Class"
        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtPropertyWithAccessors()
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Property P As Integer
        Get
            Return 42
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub SkipsPropertyAccessors()
        Dim code = "
Class C
    Sub M1()
    End Sub

    $$Property P As Integer
        Get
            Return 42
        End Get
        Set(value As Integer)
        End Set
    End Property

    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub FromInsidePropertyAccessor()
        Dim code = "
Class C
    Sub M1()
    End Sub

    Property P As Integer
        Get
            Return $$42
        End Get
        Set(value As Integer)
        End Set
    End Property

    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub StopsAtEventWithAddRemove()
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Custom Event E As EventHandler
        AddHandler(value As EventHandler)

        End AddHandler
        RemoveHandler(value As EventHandler)

        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)

        End RaiseEvent
    End Event
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub SkipsEventAddRemove()
        Dim code = "
Class C
    Sub M1()
    End Sub

    $$Custom Event E As EventHandler
        AddHandler(value As EventHandler)

        End AddHandler
        RemoveHandler(value As EventHandler)

        End RemoveHandler
        RaiseEvent(sender As Object, e As EventArgs)

        End RaiseEvent
    End Event

    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub FromInsideMethod()
        Dim code = "
Class C
    Sub M1()
        $$System.Console.WriteLine()
    End Sub

    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub NextFromBetweenMethods()
        Dim code = "
Class C
    Sub M1()
    End Sub

    $$

    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub PreviousFromBetweenMethods()
        Dim code = "
Class C
    [||]Sub M1()
    End Sub

    $$

    Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=False)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub NextFromBetweenMethodsInTrailingTrivia()
        Dim code = "
Class C
    Sub M1()
    End Sub $$

    [||]Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=True)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub PreviousFromBetweenMethodsInTrailingTrivia()
        Dim code = "
Class C
    [||]Sub M1()
    End Sub $$

    Sub M2()
    End Sub
End Class"

        AssertNavigated(code, next:=False)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub NextInScript()
        Dim code = "
$$Sub M1()
End Sub

[||]Sub M2()
End Sub"

        AssertNavigated(code, next:=True, kind:=SourceCodeKind.Script)
    End Sub

    <Fact, Trait(Traits.Feature, Traits.Features.GoToNextAndPreviousMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Sub PrevInScript()
        Dim code = "
[||]Sub M1()
End Sub

$$Sub M2()
End Sub"

        AssertNavigated(code, next:=False, kind:=SourceCodeKind.Script)
    End Sub

    Private Sub AssertNavigated(code As String, [next] As Boolean, Optional kind As SourceCodeKind? = Nothing)

        Dim kinds = If(kind IsNot Nothing,
                SpecializedCollections.SingletonEnumerable(kind.Value),
                 {SourceCodeKind.Regular, SourceCodeKind.Script})
        For Each currentKind In kinds
            Using workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.VisualBasic,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                VisualBasicParseOptions.Default.WithKind(currentKind),
                code)
                Dim hostDocument = workspace.DocumentWithCursor
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Assert.Empty(document.GetSyntaxTreeAsync().Result.GetDiagnostics())
                Dim targetPosition = GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                        document,
                        hostDocument.CursorPosition.Value,
                        [next],
                        CancellationToken.None)

                Assert.NotNull(targetPosition)
                Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value)
            End Using
        Next
    End Sub

    Private Function GetTargetPosition(code As String, [next] As Boolean) As Integer?
        Using workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                LanguageNames.VisualBasic,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                VisualBasicParseOptions.Default,
                code)
            Dim hostDocument = workspace.DocumentWithCursor
            Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Assert.Empty(document.GetSyntaxTreeAsync().Result.GetDiagnostics())
            Return GoToNextAndPreviousMethodCommandHandler.GetTargetPosition(
                    document,
                    hostDocument.CursorPosition.Value,
                    [next],
                    CancellationToken.None)
        End Using
    End Function

End Class
