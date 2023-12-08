' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class UsingBlockTests
        <WpfFact>
        Public Sub ApplyAfterUsingStatement()
            VerifyStatementEndConstructApplied(
                before:="Class c1
Sub goo()
Using variable
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub goo()
Using variable

End Using
End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForMatchedUsing()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Sub goo()
Using variable
End Using
End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyNestedUsing()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        Using y
            Using z
        End Using
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub S
        Using y
            Using z

            End Using
        End Using
    End Sub
End Class",
                afterCaret:={4, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyUsingWithDelegate()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub S
        Using Func(of String, String)
    End Sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    Sub S
        Using Func(of String, String)

        End Using
    End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyUsingAtInvalidSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub S
        Using x asf asdf
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyUsingAtInvalidLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Using x
End Class",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
