' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class WithBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function ApplyAfterWithStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
Sub foo()
With variable
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub foo()
With variable

End With
End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedWith() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Sub foo()
With variable
End With
End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyNestedWith() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyWithFollowsCode() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidWithSyntax() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Sub S
        With using
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidWithLocation() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    With True
End Class",
                caret:={1, -1})
        End Function
    End Class
End Namespace