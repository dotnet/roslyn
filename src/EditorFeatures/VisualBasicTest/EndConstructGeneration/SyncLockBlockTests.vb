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
    Public Class SyncLockBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function ApplyAfterSyncLockStatement() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class c1",
                         "Sub foo()",
                         "SyncLock variable",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "Sub foo()",
                        "SyncLock variable",
                        "",
                        "End SyncLock",
                        "End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedUsing() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Sub foo()",
                       "SyncLock variable",
                       "End SyncLock",
                       "End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyNestedSyncBlock() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:={"Class C",
                         "    Sub S",
                         "        SyncLock x",
                         "            SyncLock y",
                         "        End SyncLock",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={3, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        SyncLock x",
                         "            SyncLock y",
                         "",
                         "            End SyncLock",
                         "        End SyncLock",
                         "    End Sub",
                         "End Class"},
                afterCaret:={4, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSyntax() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub S",
                       "        Using (SyncLock 1) ",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidLocation() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Synclock 1",
                       "End Class"},
                caret:={1, -1})
        End Function
    End Class
End Namespace
