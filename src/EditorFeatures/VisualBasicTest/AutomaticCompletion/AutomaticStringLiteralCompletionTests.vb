' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.AutomaticCompletion
Imports Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    <Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
    Public Class AutomaticStringLiteralCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact>
        Public Sub TestCreation()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestString_TopLevel()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestString_TopLevel2()
            Using session = CreateSession("Imports System$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestInvalidLocation_String()
            Dim code = <code>Class C
    Dim s As String = "$$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestInvalidLocation_Comment()
            Dim code = <code>Class C
    ' $$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestInvalidLocation_DocComment()
            Dim code = <code>Class C
    ''' $$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestString_Tab()
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

        <WpfFact>
        Public Sub TestString_Quotation()
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

        <WpfFact>
        Public Sub TestString_Backspace()
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

        <WpfFact>
        Public Sub TestProprocessor_Backspace()
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

        <WpfFact>
        Public Sub TestProprocessor_Tab()
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

        <WpfFact>
        Public Sub TestProprocessor_EndPoint()
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

        Friend Overloads Shared Function CreateSession(code As XElement) As Holder
            Return CreateSession(code.NormalizedValue())
        End Function

        Friend Overloads Shared Function CreateSession(code As String) As Holder
            Return AbstractAutomaticBraceCompletionTests.CreateSession(
                EditorTestWorkspace.CreateVisualBasic(code),
                DoubleQuote.OpenCharacter, DoubleQuote.CloseCharacter)
        End Function
    End Class
End Namespace
