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
    Public Class AutomaticParenthesesCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact>
        Public Sub TestCreation()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestInvalidLocation_TopLevel()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestInvalidLocation_TopLevel2()
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
        Public Sub TestRightAfterStringLiteral()
            Dim code = <code>Class C
    Sub Method()
        Dim a = ""$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestTypeParameterListSyntax()
            Dim code = <code>Class C$$
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestParameterListSyntax()
            Dim code = <code>Class C
    Sub Method$$
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestArrayRankSpecifierSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a as String$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestParenthesizedExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = $$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetTypeExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = GetType$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestGetXmlNamespaceExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = GetXmlNamespace$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestCTypeExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = CType$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDirectCastExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = DirectCast$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestTryCastExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = TryCast$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestPredefinedCastExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = CInt$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestBinaryConditionalExpressionSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim a = If$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestArgumentListSyntax()
            Dim code = <code>Class C
    Sub Method()
        Method$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestFunctionAggregationSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim max = Aggregate o In metaData.Order Into m = Max$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestTypeArgumentListSyntax()
            Dim code = <code>Class C
    Sub Method()
        Dim d = new List$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestExternalSourceDirectiveSyntax()
            Dim code = <code>Imports System

Public Class ExternalSourceClass
    Sub TestExternalSource()
#ExternalSource $$
#End ExternalSource
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestExternalChecksumDirectiveSyntax()
            Dim code = "#ExternalChecksum$$"
            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5607")>
        Public Sub TestOverTypeAfterIntegerLiteral()
            Dim code = <code>Imports System.Collections.Generic
Class C
    Sub Method()
        Dim lines As New List(Of String)
        lines.RemoveAt$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                Type(session.Session, "0")
                CheckOverType(session.Session)
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/5607")>
        Public Sub TestOverTypeAfterDateLiteral()
            Dim code = <code>Class C
    Sub Method()
        Test(#1AM#)$$
    End Sub
    Sub Test(d As Date)
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckOverType(session.Session)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestOverTypeAfterStringLiteral()
            Dim code = <code>Class C
    Sub Method()
        Console.Write$$
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                Type(session.Session, """a""")
                CheckOverType(session.Session)
            End Using
        End Sub

        Friend Overloads Shared Function CreateSession(code As XElement) As Holder
            Return CreateSession(code.NormalizedValue())
        End Function

        Friend Overloads Shared Function CreateSession(code As String) As Holder
            Return CreateSession(
                EditorTestWorkspace.CreateVisualBasic(code),
                Parenthesis.OpenCharacter, Parenthesis.CloseCharacter)
        End Function
    End Class
End Namespace
