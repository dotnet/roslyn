' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class UsingBlockTests
        <WpfFact>
        Public Async Function ApplyAfterUsingStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForMatchedUsing() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Sub goo()
Using variable
End Using
End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyNestedUsing() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function VerifyUsingWithDelegate() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function VerifyUsingAtInvalidSyntax() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Sub S
        Using x asf asdf
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyUsingAtInvalidLocation() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Using x
End Class",
                caret:={1, -1})
        End Function
    End Class
End Namespace
