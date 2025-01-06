' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class SyncLockBlockTests
        <WpfFact>
        Public Async Function ApplyAfterSyncLockStatement() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class c1
Sub goo()
SyncLock variable
End Sub
End Class",
                beforeCaret:={2, -1},
                after:="Class c1
Sub goo()
SyncLock variable

End SyncLock
End Sub
End Class",
                afterCaret:={3, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyForMatchedUsing() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class c1
Sub goo()
SyncLock variable
End SyncLock
End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyNestedSyncBlock() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="Class C
    Sub S
        SyncLock x
            SyncLock y
        End SyncLock
    End Sub
End Class",
                beforeCaret:={3, -1},
                 after:="Class C
    Sub S
        SyncLock x
            SyncLock y

            End SyncLock
        End SyncLock
    End Sub
End Class",
                afterCaret:={4, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidSyntax() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class C
    Sub S
        Using (SyncLock 1) 
    End Sub
End Class",
                caret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function VerifyInvalidLocation() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="Class EC
    Synclock 1
End Class",
                caret:={1, -1})
        End Function
    End Class
End Namespace
