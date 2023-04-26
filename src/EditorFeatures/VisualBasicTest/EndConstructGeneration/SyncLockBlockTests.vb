' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class SyncLockBlockTests
        <WpfFact>
        Public Sub ApplyAfterSyncLockStatement()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub DoNotApplyForMatchedUsing()
            VerifyStatementEndConstructNotApplied(
                text:="Class c1
Sub goo()
SyncLock variable
End SyncLock
End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyNestedSyncBlock()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidSyntax()
            VerifyStatementEndConstructNotApplied(
                text:="Class C
    Sub S
        Using (SyncLock 1) 
    End Sub
End Class",
                caret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub VerifyInvalidLocation()
            VerifyStatementEndConstructNotApplied(
                text:="Class EC
    Synclock 1
End Class",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
