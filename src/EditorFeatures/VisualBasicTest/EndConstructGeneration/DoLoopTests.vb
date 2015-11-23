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
    Public Class DoLoopTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestApplyAfterUnmatchedDo() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyNestedDo() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DoNotApplyFromPairedDo() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Do",
                       "Loop",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DoNotApplyFromInsideDo() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Do",
                       "End Class"},
                caret:={1, 1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DoNotApplyFromDoOutsideMethod() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Do",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyDoWhile() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyNestedDoWhile() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyDoUntil() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyNestedDoUntil() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function TestVerifyDoWhileInBrokenSub() As Task
            Await VerifyStatementEndConstructAppliedAsync(
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyDoUntilInvalidLocation01() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub s",
                       "    End Sub",
                       "    do Until True",
                       "End Class"},
                caret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyDoUntilInvalidLocation02() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Do"},
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyDoUntilInvalidLocation03() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub s",
                       "    End Sub",
                       "    do Until",
                       "End Class"},
                caret:={3, -1})
        End Function
    End Class
End Namespace