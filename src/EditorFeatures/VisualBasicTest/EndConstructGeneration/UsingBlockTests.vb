' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class UsingBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForMatchedUsing()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Sub goo()
Using variable
End Using
End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyUsingAtInvalidSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub S
        Using x asf asdf
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyUsingAtInvalidLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Using x
End Class",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
