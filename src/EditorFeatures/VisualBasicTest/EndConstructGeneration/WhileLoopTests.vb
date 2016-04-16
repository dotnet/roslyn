' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class WhileLoopTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function ApplyAfterWithStatement() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
Sub foo()
While variable
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub foo()
While variable

End While
End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedWith() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Sub foo()
While variable
End While
End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyNestedWhileBlock() As Threading.Tasks.Task
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyRecommitWhileBlock() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        While [while] = [while]
        End While           
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidWhileSyntax() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Sub S
        While asdf asdf asd
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidWhileLocation() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    While True
End Class",
                caret:={1, -1})
        End Function
    End Class
End Namespace