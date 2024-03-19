' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class WhileLoopTests
        <WpfFact>
        Public Sub ApplyAfterWithStatement()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForMatchedWith()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Sub goo()
While variable
End While
End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyNestedWhileBlock()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub VerifyRecommitWhileBlock()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        While [while] = [while]
        End While           
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidWhileSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub S
        While asdf asdf asd
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidWhileLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    While True
End Class",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
