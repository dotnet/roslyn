' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.GoToAdjacentMember

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GoToAdjacentMember
    <Trait(Traits.Feature, Traits.Features.GoToAdjacentMember)>
    Public Class VisualBasicGoToAdjacentMemberTests
        Inherits AbstractGoToAdjacentMemberTests

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
        Protected Overrides ReadOnly Property DefaultParseOptions As ParseOptions = VisualBasicParseOptions.Default

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function EmptyFile() As Task
            Assert.Null(Await GetTargetPositionAsync("$$", next:=True))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function ClassWithNoMembers() As Task
            Dim code = "Class C
$$
End Class"
            Assert.Null(Await GetTargetPositionAsync(code, next:=True))
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function BeforeClassWithMember() As Task
            Dim code = "$$
Class C
    [||]Sub M()
    End Sub
End Class"

            Await AssertNavigatedAsync(code, next:=True)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function AfterClassWithMember() As Task
            Dim code = "
Class C
    [||]Sub M()
    End Sub
End Class

$$"

            Await AssertNavigatedAsync(code, next:=True)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        Public Shared Operator +(left As VisualBasicGoToAdjacentMemberTests, right As VisualBasicGoToAdjacentMemberTests) As VisualBasicGoToAdjacentMemberTests
            Throw New System.NotImplementedException()
        End Operator

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function StopsAtField() As Task
            Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Dim f as Integer
End Class"
            Await AssertNavigatedAsync(code, next:=True)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function StopsAtFieldlikeEvent() As Task
            Dim code = "
Class C
    $$Sub M1()
    End Sub

    [||]Event E As System.EventHandler
End Class"
            Await AssertNavigatedAsync(code, next:=True)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function StopsAtAutoProperty() As Task
            Dim code = "
Class C
    $$Sub M1()
    End Sub
    [||]Property P As Integer
End Class"
            Await AssertNavigatedAsync(code, next:=True)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/10588")>
        Public Async Function PreviousFromInsideCurrent() As Task
            Dim code = "
class C
    [||]Sub M1()
        Console.WriteLine($$)
    End Sub

    Sub M2()
    End Sub
End Class"

            Await AssertNavigatedAsync(code, next:=False)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function NextInScript() As Task
            Dim code = "
$$Sub M1()
End Sub

[||]Sub M2()
End Sub"

            Await AssertNavigatedAsync(code, next:=True, sourceCodeKind:=SourceCodeKind.Script)
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4311")>
        Public Async Function PrevInScript() As Task
            Dim code = "
[||]Sub M1()
End Sub

$$Sub M2()
End Sub"

            Await AssertNavigatedAsync(code, next:=False, sourceCodeKind:=SourceCodeKind.Script)
        End Function
    End Class
End Namespace
