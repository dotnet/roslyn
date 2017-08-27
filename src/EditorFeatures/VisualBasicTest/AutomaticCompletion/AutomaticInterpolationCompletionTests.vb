' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticCompletion
    Public Class AutomaticInterpolationCompletionTests
        Inherits AbstractAutomaticBraceCompletionTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestCreation()
            Using session = CreateSession("$$")
                Assert.NotNull(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestInvalidLocation_String()
            Dim code = <code>Class C
    Dim s As String = "$$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestInvalidLocation_Comment()
            Dim code = <code>Class C
    ' $$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestInvalidLocation_DocComment()
            Dim code = <code>Class C
    ''' $$
End Class</code>

            Using session = CreateSession(code)
                Assert.Null(session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestStartOfInterpolatedStringExpression()
            Dim code = <code>Class C
    Sub M()
        Dim s = $"$$"
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestAfterInterpolatedStringExpressionText()
            Dim code = <code>Class C
    Sub M()
        Dim s = $"Jenny I got your number: $$"
    End Sub
End Class</code>

            Using session = CreateSession(code)
                Assert.NotNull(session)
                CheckStart(session.Session)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)>
        Public Sub TestAfterInterpolation()
            Dim code = <code>Class C
    Sub M()
        Dim s = $"Result = {prefix}$$"
    End Sub
End Class</code>

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
TestWorkspace.CreateVisualBasic(code),
BraceCompletionSessionProvider.CurlyBrace.OpenCharacter, BraceCompletionSessionProvider.CurlyBrace.CloseCharacter)
        End Function
    End Class
End Namespace
