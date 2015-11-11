' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Public Class VisualBasicGoToAdjacentMemberTests

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function EmptyFile() As Task
        Assert.Null(Await GetTargetPositionAsync("$$", next:=True))
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function ClassWithNoMembers() As Task
        Dim code = "Class C
$$
End Class"
        Assert.Null(Await GetTargetPositionAsync(code, next:=True))
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function BeforeClassWithMember() As Task
        Dim code = "$$
Class C
    [||]Sub M()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function AfterClassWithMember() As Task
        Dim code = "
Class C
    [||]Sub M()
    End Sub
End Class

$$"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function BetweenClasses() As Task
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

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function BetweenClassesPrevious() As Task
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

        Await AssertNavigatedAsync(code, next:=False)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function FromFirstMemberToSecond() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function FromSecondToFirst() As Task
        Dim code = "
Class C
    [||]Sub M1()
    End Sub
    $$Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=False)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function NextWraps() As Task
        Dim code = "
Class C
    [||]Sub M1()
    End Sub
    $$Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function PreviousWraps() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=False)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function DescendsIntoNestedType() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    Class N
        [||]Sub M2()
        End Sub
    End Class
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtConstructor() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Public Sub New()
    End Sub
End Class"
        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtOperator() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Shared Operator +(left As C, right As C) As C
        Throw New System.NotImplementedException()
    End Operator
End Class"
        Await AssertNavigatedAsync(code, next:=True)
    End Function

    Shared Operator +(left As VisualBasicGoToAdjacentMemberTests, right As VisualBasicGoToAdjacentMemberTests) As VisualBasicGoToAdjacentMemberTests
        Throw New System.NotImplementedException()
    End Operator


    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtField() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Dim f as Integer
End Class"
        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtFieldlikeEvent() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Event E As System.EventHandler
End Class"
        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtAutoProperty() As Task
        Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Property P As Integer
End Class"
        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtPropertyWithAccessors() As Task
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

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function SkipsPropertyAccessors() As Task
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

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function FromInsidePropertyAccessor() As Task
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

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function StopsAtEventWithAddRemove() As Task
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

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function SkipsEventAddRemove() As Task
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

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function FromInsideMethod() As Task
        Dim code = "
Class C
    Sub M1()
        $$System.Console.WriteLine()
    End Sub

    [||]Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function NextFromBetweenMethods() As Task
        Dim code = "
Class C
    Sub M1()
    End Sub

    $$

    [||]Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function PreviousFromBetweenMethods() As Task
        Dim code = "
Class C
    [||]Sub M1()
    End Sub

    $$

    Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=False)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function NextFromBetweenMethodsInTrailingTrivia() As Task
        Dim code = "
Class C
    Sub M1()
    End Sub $$

    [||]Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=True)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function PreviousFromBetweenMethodsInTrailingTrivia() As Task
        Dim code = "
Class C
    [||]Sub M1()
    End Sub $$

    Sub M2()
    End Sub
End Class"

        Await AssertNavigatedAsync(code, next:=False)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function NextInScript() As Task
        Dim code = "
$$Sub M1()
End Sub

[||]Sub M2()
End Sub"

        Await AssertNavigatedAsync(code, next:=True, kind:=SourceCodeKind.Script)
    End Function

    <Fact, Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    <WorkItem(4311, "https://github.com/dotnet/roslyn/issues/4311")>
    Public Async Function PrevInScript() As Task
        Dim code = "
[||]Sub M1()
End Sub

$$Sub M2()
End Sub"

        Await AssertNavigatedAsync(code, next:=False, kind:=SourceCodeKind.Script)
    End Function

    Private Async Function AssertNavigatedAsync(code As String, [next] As Boolean, Optional kind As SourceCodeKind? = Nothing) As Task

        Dim kinds = If(kind IsNot Nothing,
                SpecializedCollections.SingletonEnumerable(kind.Value),
                 {SourceCodeKind.Regular, SourceCodeKind.Script})
        For Each currentKind In kinds
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceFromLinesAsync(
                LanguageNames.VisualBasic,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                VisualBasicParseOptions.Default.WithKind(currentKind),
                code)
                Dim hostDocument = workspace.DocumentWithCursor
                Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Assert.Empty(document.GetSyntaxTreeAsync().Result.GetDiagnostics())
                Dim targetPosition = Await GoToAdjacentMemberCommandHandler.GetTargetPositionAsync(
                        document,
                        hostDocument.CursorPosition.Value,
                        [next],
                        CancellationToken.None)

                Assert.NotNull(targetPosition)
                Assert.Equal(hostDocument.SelectedSpans.Single().Start, targetPosition.Value)
            End Using
        Next
    End Function

    Private Async Function GetTargetPositionAsync(code As String, [next] As Boolean) As Task(Of Integer?)
        Using workspace = Await TestWorkspaceFactory.CreateWorkspaceFromLinesAsync(
                LanguageNames.VisualBasic,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                VisualBasicParseOptions.Default,
                code)
            Dim hostDocument = workspace.DocumentWithCursor
            Dim document As Document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Assert.Empty(document.GetSyntaxTreeAsync().Result.GetDiagnostics())
            Return Await GoToAdjacentMemberCommandHandler.GetTargetPositionAsync(
                    document,
                    hostDocument.CursorPosition.Value,
                    [next],
                    CancellationToken.None)
        End Using
    End Function

End Class
