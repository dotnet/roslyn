' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticBracketCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestCreation() As Task
            Using session = CreateSession("$$")
                Assert.NotNull(session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestBracket() As Task
            Using session = CreateSession("$$")
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestBracket2() As Task
            Using session = CreateSession("Imports System$$")
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_Bracket() As Task
            Dim code = <code>Class C
    Dim s As String = "$$
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.Null(session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_Comment() As Task
            Dim code = <code>Class C
    ' $$
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.Null(session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_DocComment() As Task
            Dim code = <code>Class C
    ''' $$
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.Null(session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_Comment_CloseBracket() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim $$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                Type(session.Session, "'")
                CheckOverType(session.Session, allowOverType:=False)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_Comment_Tab() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim $$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                Type(session.Session, "'")
                CheckTab(session.Session)
            End Using
        End Function

        Friend Overloads Function CreateSessionAsync(code As XElement) As Threading.Tasks.Task(Of Holder)
            Return CreateSession(code.NormalizedValue())
        End Function

        Friend Overloads Function CreateSession(code As String) As Holder
            Return CreateSession(
TestWorkspace.CreateVisualBasic(code),
BraceCompletionSessionProvider.Bracket.OpenCharacter, BraceCompletionSessionProvider.Bracket.CloseCharacter)
        End Function
    End Class
End Namespace
