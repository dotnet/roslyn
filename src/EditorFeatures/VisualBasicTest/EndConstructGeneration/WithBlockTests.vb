' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class WithBlockTests
        <WpfFact>
        Public Sub ApplyAfterWithStatement()
            VerifyStatementEndConstructApplied(
                before:="Class c1
Sub goo()
With variable
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub goo()
With variable

End With
End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForMatchedWith()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Sub goo()
With variable
End With
End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyNestedWith()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        With K
            With K
        End With
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub S
        With K
            With K

            End With
        End With
    End Sub
End Class",
                afterCaret:={4, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyWithFollowsCode()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        With K
        Dim x = 5
    End Sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    Sub S
        With K

        End With
        Dim x = 5
    End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidWithSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub S
        With using
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidWithLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    With True
End Class",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
