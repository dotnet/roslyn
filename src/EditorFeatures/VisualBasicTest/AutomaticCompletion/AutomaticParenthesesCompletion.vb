' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticParenthesesCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub Creation()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub InvalidLocation_TopLevel()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub InvalidLocation_TopLevel2()
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
        Public Sub RightAfterStringLiteral()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TypeParameterListSyntax()
            Dim code = <code>Class C$$
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub ParameterListSyntax()
            Dim code = <code>Class C
    Sub Method$$
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub ArrayRankSpecifierSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub ParenthesizedExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub GetTypeExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub GetXmlNamespaceExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub CTypeExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub DirectCastExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TryCastExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub PredefinedCastExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub BinaryConditionalExpressionSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub ArgumentListSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub FunctionAggregationSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TypeArgumentListSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub ExternalSourceDirectiveSyntax()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub ExternalChecksumDirectiveSyntax()
            Dim code = "#ExternalChecksum$$"
            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        Friend Overloads Function CreateSession(code As XElement) As Holder
            Return CreateSession(code.NormalizedValue())
        End Function

        Friend Overloads Function CreateSession(code As String) As Holder
            Return CreateSession(
                VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(code),
                BraceCompletionSessionProvider.Parenthesis.OpenCharacter, BraceCompletionSessionProvider.Parenthesis.CloseCharacter)
        End Function
    End Class
End Namespace
