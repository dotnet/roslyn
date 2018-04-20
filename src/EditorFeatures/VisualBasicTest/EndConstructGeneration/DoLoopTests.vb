' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class DoLoopTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterUnmatchedDo()
            VerifyStatementEndConstructApplied(
                before:="Class c1
  Sub goo()
    Do
  End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
  Sub goo()
    Do

    Loop
  End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestVerifyNestedDo()
            VerifyStatementEndConstructApplied(
                before:="Class c1
  Sub goo()
    Do
      Do
    Loop
  End Sub
End Class",
                beforeCaret:={3, -1},
                after:="Class c1
  Sub goo()
    Do
      Do

      Loop
    Loop
  End Sub
End Class",
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyFromPairedDo()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Do
Loop
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyFromInsideDo()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Do
End Class",
                caret:={1, 1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyFromDoOutsideMethod()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Do
End Class",
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestVerifyDoWhile()
            VerifyStatementEndConstructApplied(
                before:="Class C
Sub s
Do While True
End Sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
Sub s
Do While True

Loop
End Sub
End Class",
                afterCaret:={3, -1})

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestVerifyNestedDoWhile()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub s
        do While True
            do While a
        Loop
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub s
        do While True
            do While a

            Loop
        Loop
    End Sub
End Class",
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestVerifyDoUntil()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub s
        do Until true
    End Sub
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    Sub s
        do Until true

        Loop
    End Sub
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestVerifyNestedDoUntil()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub s
        do Until True
            Do Until True
        Loop
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub s
        do Until True
            Do Until True

            Loop
        Loop
    End Sub
End Class",
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestVerifyDoWhileInBrokenSub()
            VerifyStatementEndConstructApplied(
                before:="Class C
    Sub s
        Do While True
End Class",
                beforeCaret:={2, -1},
                 after:="Class C
    Sub s
        Do While True

        Loop
End Class",
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntilInvalidLocation01()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s
    End Sub
    do Until True
End Class",
                caret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntilInvalidLocation02()
            VerifyStatementEndConstructNotApplied(
                text:="Do",
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntilInvalidLocation03()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub s
    End Sub
    do Until
End Class",
                caret:={3, -1})
        End Sub
    End Class
End Namespace
