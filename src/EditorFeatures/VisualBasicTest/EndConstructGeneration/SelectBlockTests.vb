' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class SelectBlockTests
        <WpfFact>
        Public Async Function TestApplyAfterSelectKeyword() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
Sub goo()
Select goo
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub goo()
Select goo
    Case 
End Select
End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function TestApplyAfterSelectCaseKeyword() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
Sub goo()
Select Case goo
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub goo()
Select Case goo
    Case 
End Select
End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyNestedDo() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub S
        Select Case 1
            Case 1
                Select 1
        End Select
    End Sub
End Class",
                beforeCaret:={4, -1},
                 after:="Class C
    Sub S
        Select Case 1
            Case 1
                Select 1
                    Case 
                End Select
        End Select
    End Sub
End Class",
                afterCaret:={5, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSelectBlock() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        dim x = Select 1
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSelectBlock01() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Sub T
        Select 1
            Case 1
    End Sub
End Class",
                caret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSelectBlock02() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Select 1
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyReCommitSelectBlock() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        Select Case 1
            Case 1
        End Select
    End Sub
End Class",
                caret:={2, -1})
        End Function
    End Class
End Namespace
