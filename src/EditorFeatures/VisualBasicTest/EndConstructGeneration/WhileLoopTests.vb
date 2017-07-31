﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    Public Class WhileLoopTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterWithStatement()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForMatchedWith()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Sub foo()
While variable
End While
End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidWhileSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Sub S
        While asdf asdf asd
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidWhileLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    While True
End Class",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
