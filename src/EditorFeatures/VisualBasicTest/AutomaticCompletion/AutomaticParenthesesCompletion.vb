' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticParenthesesCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestCreation() As Task
            Using session = Await CreateSessionAsync("$$")
                Assert.NotNull(session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_TopLevel() As Task
            Using session = Await CreateSessionAsync("$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_TopLevel2() As Task
            Using session = Await CreateSessionAsync("Imports System$$")
                Assert.NotNull(session)
                CheckStart(session.Session, expectValidSession:=False)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestInvalidLocation_String() As Task
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
        Public Async Function TestRightAfterStringLiteral() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = ""$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestTypeParameterListSyntax() As Task
            Dim code = <code>Class C$$
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestParameterListSyntax() As Task
            Dim code = <code>Class C
    Sub Method$$
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestArrayRankSpecifierSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a as String$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestParenthesizedExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = $$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestGetTypeExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = GetType$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestGetXmlNamespaceExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = GetXmlNamespace$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestCTypeExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = CType$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestDirectCastExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = DirectCast$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestTryCastExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = TryCast$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestPredefinedCastExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = CInt$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestBinaryConditionalExpressionSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim a = If$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestArgumentListSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Method$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestFunctionAggregationSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim max = Aggregate o In metaData.Order Into m = Max$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestTypeArgumentListSyntax() As Task
            Dim code = <code>Class C
    Sub Method()
        Dim d = new List$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestExternalSourceDirectiveSyntax() As Task
            Dim code = <code>Imports System

Public Class ExternalSourceClass
    Sub TestExternalSource()
#ExternalSource $$
#End ExternalSource
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestExternalChecksumDirectiveSyntax() As Task
            Dim code = "#ExternalChecksum$$"
            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Function

        <WorkItem(5607, "https://github.com/dotnet/roslyn/issues/5607")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestOverTypeAfterIntegerLiteral() As Task
            Dim code = <code>Imports System.Collections.Generic
Class C
    Sub Method()
        Dim lines As New List(Of String)
        lines.RemoveAt$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                Type(session.Session, "0")
                CheckOverType(session.Session)
            End Using
        End Function

        <WorkItem(5607, "https://github.com/dotnet/roslyn/issues/5607")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestOverTypeAfterDateLiteral() As Task
            Dim code = <code>Class C
    Sub Method()
        Test(#1AM#)$$
    End Sub
    Sub Test(d As Date)
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckOverType(session.Session)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Async Function TestOverTypeAfterStringLiteral() As Task
            Dim code = <code>Class C
    Sub Method()
        Console.Write$$
    End Sub
End Class</code>

            Using session = Await CreateSessionAsync(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
                Type(session.Session, """a""")
                CheckOverType(session.Session)
            End Using
        End Function

        Friend Overloads Function CreateSessionAsync(code As XElement) As Threading.Tasks.Task(Of Holder)
            Return CreateSessionAsync(code.NormalizedValue())
        End Function

        Friend Overloads Async Function CreateSessionAsync(code As String) As Threading.Tasks.Task(Of Holder)
            Return CreateSession(
                Await TestWorkspace.CreateVisualBasicAsync(code),
                BraceCompletionSessionProvider.Parenthesis.OpenCharacter, BraceCompletionSessionProvider.Parenthesis.CloseCharacter)
        End Function
    End Class
End Namespace
