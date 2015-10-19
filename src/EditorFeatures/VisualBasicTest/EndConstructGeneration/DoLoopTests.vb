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
    Public Class DoLoopTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterUnmatchedDo()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "  Sub foo()",
                         "    Do",
                         "  End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "  Sub foo()",
                        "    Do",
                        "",
                        "    Loop",
                        "  End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedDo()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "  Sub foo()",
                         "    Do",
                         "      Do",
                         "    Loop",
                         "  End Sub",
                         "End Class"},
                beforeCaret:={3, -1},
                after:={"Class c1",
                         "  Sub foo()",
                         "    Do",
                         "      Do",
                         "",
                         "      Loop",
                         "    Loop",
                         "  End Sub",
                         "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyFromPairedDo()
            VerifyStatementEndConstructNotApplied(
                text:={"Class c1",
                       "Do",
                       "Loop",
                       "End Class"},
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyFromInsideDo()
            VerifyStatementEndConstructNotApplied(
                text:={"Class c1",
                       "Do",
                       "End Class"},
                caret:={1, 1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DoNotApplyFromDoOutsideMethod()
            VerifyStatementEndConstructNotApplied(
                text:={"Class c1",
                       "Do",
                       "End Class"},
                caret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoWhile()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "Sub s",
                         "Do While True",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                 after:={"Class C",
                         "Sub s",
                         "Do While True",
                         "",
                         "Loop",
                         "End Sub",
                         "End Class"},
                afterCaret:={3, -1})

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedDoWhile()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub s",
                         "        do While True",
                         "            do While a",
                         "        Loop",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={3, -1},
                 after:={"Class C",
                         "    Sub s",
                         "        do While True",
                         "            do While a",
                         "",
                         "            Loop",
                         "        Loop",
                         "    End Sub",
                         "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntil()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub s",
                         "        do Until true",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                 after:={"Class C",
                         "    Sub s",
                         "        do Until true",
                         "",
                         "        Loop",
                         "    End Sub",
                         "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedDoUntil()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub s",
                         "        do Until True",
                         "            Do Until True",
                         "        Loop",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={3, -1},
                 after:={"Class C",
                         "    Sub s",
                         "        do Until True",
                         "            Do Until True",
                         "",
                         "            Loop",
                         "        Loop",
                         "    End Sub",
                         "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoWhileInBrokenSub()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub s",
                         "        Do While True",
                         "End Class"},
                beforeCaret:={2, -1},
                 after:={"Class C",
                         "    Sub s",
                         "        Do While True",
                         "",
                         "        Loop",
                         "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntilInvalidLocation01()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                       "    Sub s",
                       "    End Sub",
                       "    do Until True",
                       "End Class"},
                caret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntilInvalidLocation02()
            VerifyStatementEndConstructNotApplied(
                text:={"Do"},
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyDoUntilInvalidLocation03()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                       "    Sub s",
                       "    End Sub",
                       "    do Until",
                       "End Class"},
                caret:={3, -1})
        End Sub

    End Class
End Namespace
