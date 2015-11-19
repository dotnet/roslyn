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
    Public Class SelectBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterSelectKeyword()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "Sub foo()",
                         "Select foo",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "Sub foo()",
                        "Select foo",
                        "    Case ",
                        "End Select",
                        "End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterSelectCaseKeyword()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "Sub foo()",
                         "Select Case foo",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "Sub foo()",
                        "Select Case foo",
                        "    Case ",
                        "End Select",
                        "End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedDo()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        Select Case 1",
                         "            Case 1",
                         "                Select 1",
                         "        End Select",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={4, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        Select Case 1",
                         "            Case 1",
                         "                Select 1",
                         "                    Case ",
                         "                End Select",
                         "        End Select",
                         "    End Sub",
                         "End Class"},
                afterCaret:={5, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSelectBlock() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub S",
                       "        dim x = Select 1",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSelectBlock01() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Sub T",
                       "        Select 1",
                       "            Case 1",
                       "    End Sub",
                       "End Class"},
                caret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSelectBlock02() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Select 1",
                       "End Class"},
                caret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyReCommitSelectBlock() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub S",
                       "        Select Case 1",
                       "            Case 1",
                       "        End Select",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function
    End Class
End Namespace
