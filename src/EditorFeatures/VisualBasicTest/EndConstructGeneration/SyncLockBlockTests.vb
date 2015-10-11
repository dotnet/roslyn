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
        Public Sub ApplyAfterSyncLockStatement()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForMatchedUsing()
            VerifyStatementEndConstructNotApplied(
                text:={"Class c1",
                       "Sub foo()",
                       "SyncLock variable",
                       "End SyncLock",
                       "End Sub",
                       "End Class"},
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedSyncBlock()
            VerifyStatementEndConstructApplied(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidSyntax()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                       "    Sub S",
                       "        Using (SyncLock 1) ",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidLocation()
            VerifyStatementEndConstructNotApplied(
                text:={"Class EC",
                       "    Synclock 1",
                       "End Class"},
                caret:={1, -1})
        End Sub
    End Class
End Namespace
