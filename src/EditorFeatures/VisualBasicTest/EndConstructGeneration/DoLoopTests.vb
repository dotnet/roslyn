' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class DoLoopTests
        <WpfFact>
        Public Async Function TestApplyAfterUnmatchedDo() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function TestVerifyNestedDo() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function DoNotApplyFromPairedDo() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Do
Loop
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyFromInsideDo() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Do
End Class",
                caret:={1, 1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyFromDoOutsideMethod() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Do
End Class",
                caret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function TestVerifyDoWhile() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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

        End Function

        <WpfFact>
        Public Async Function TestVerifyNestedDoWhile() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function TestVerifyDoUntil() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function TestVerifyNestedDoUntil() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function TestVerifyDoWhileInBrokenSub() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact>
        Public Async Function VerifyDoUntilInvalidLocation01() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s
    End Sub
    do Until True
End Class",
                caret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyDoUntilInvalidLocation02() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Do",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyDoUntilInvalidLocation03() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub s
    End Sub
    do Until
End Class",
                caret:={3, -1})
        End Function
    End Class
End Namespace
