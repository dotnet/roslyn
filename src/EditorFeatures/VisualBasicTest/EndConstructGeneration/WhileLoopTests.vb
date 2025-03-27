' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class WhileLoopTests
        <WpfFact>
        Public Async Function ApplyAfterWithStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
Sub goo()
While variable
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub goo()
While variable

End While
End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForMatchedWith() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Sub goo()
While variable
End While
End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyNestedWhileBlock() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub S
        While True
            While True
                Dim x = 5
        End While
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub S
        While True
            While True

            End While
                Dim x = 5
        End While
    End Sub
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyRecommitWhileBlock() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        While [while] = [while]
        End While           
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidWhileSyntax() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Sub S
        While asdf asdf asd
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidWhileLocation() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    While True
End Class",
                caret:={1, -1})
        End Function
    End Class
End Namespace
