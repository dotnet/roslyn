' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticStringLiteralCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Creation()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub String_TopLevel()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub String_TopLevel2()
            Using session = CreateSession("Imports System$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub InvalidLocation_String()
            Dim code = <code>Class C
    Dim s As String = "$$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub InvalidLocation_Comment()
            Dim code = <code>Class C
    ' $$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub InvalidLocation_DocComment()
            Dim code = <code>Class C
    ''' $$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub String_Tab()
            Dim code = <code>Class C
    Sub Method()
        Dim a = $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                CheckTab(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub String_Quotation()
            Dim code = <code>Class C
    Sub Method()
        Dim a = $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                CheckOverType(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub String_Backspace()
            Dim code = <code>Class C
    Sub Method()
        Dim a = $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                CheckBackspace(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Proprocessor_Backspace()
            Dim code = <code>Class C
    Sub Method()
        #Region $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                CheckBackspace(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Proprocessor_Tab()
            Dim code = <code>Class C
    Sub Method()
        #Region $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                CheckTab(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Proprocessor_EndPoint()
            Dim code = <code>Class C
    Sub Method()
        #Region $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                CheckOverType(session.Session)
            End Using
        End Sub

        Friend Overloads Function CreateSession(code As XElement) As Holder
            Return CreateSession(code.NormalizedValue())
        End Function

        Friend Overloads Function CreateSession(code As String) As Holder
            Return CreateSession(
                VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(code),
                BraceCompletionSessionProvider.DoubleQuote.OpenCharacter, BraceCompletionSessionProvider.DoubleQuote.CloseCharacter)
        End Function
    End Class
End Namespace
