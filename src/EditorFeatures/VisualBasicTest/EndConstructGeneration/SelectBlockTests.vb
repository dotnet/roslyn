' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class SelectBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterSelectKeyword()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterSelectCaseKeyword()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedDo()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidSelectBlock()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        dim x = Select 1
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidSelectBlock01()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub T
        Select 1
            Case 1
    End Sub
End Class",
                caret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidSelectBlock02()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Select 1
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyReCommitSelectBlock()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        Select Case 1
            Case 1
        End Select
    End Sub
End Class",
                caret:={2, -1})
        End Sub
    End Class
End Namespace
