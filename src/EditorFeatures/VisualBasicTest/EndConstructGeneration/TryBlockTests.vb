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
    Public Class TryBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterTryStatement()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "Sub foo()",
                         "Try",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "Sub foo()",
                        "Try",
                        "",
                        "Catch ex As Exception",
                        "",
                        "End Try",
                        "End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedTryWithCatch() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Sub foo()",
                       "Try",
                       "Catch ex As Exception",
                       "End Try",
                       "End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedTryWithoutCatch() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Sub foo()",
                       "Try",
                       "End Try",
                       "End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedTryBlock()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        Try",
                         "        Catch ex As Exception",
                         "        Finally",
                         "            Try",
                         "        End Try",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={5, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        Try",
                         "        Catch ex As Exception",
                         "        Finally",
                         "            Try",
                         "",
                         "            Catch ex As Exception",
                         "",
                         "            End Try",
                         "        End Try",
                         "    End Sub",
                         "End Class"},
                afterCaret:={6, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedTryBlockWithCode()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        Try",
                         "        Dim x = 1",
                         "        Dim y = 2",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        Try",
                         "",
                         "        Catch ex As Exception",
                         "",
                         "        End Try",
                         "        Dim x = 1",
                         "        Dim y = 2",
                         "    End Sub",
                         "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyMissingCatchInTryBlock() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class C",
                       "    Sub S",
                       "        dim x = function(x)",
                       "                    try",
                       "                    End Try",
                       "                    x += 1",
                       "                End function",
                       "    End Sub",
                       "End Class"},
                caret:={3, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidSyntax() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Sub S",
                       "        Dim x = try",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyInvalidLocation() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Sub Try",
                       "End Class"},
                caret:={1, -1})
        End Function

    End Class
End Namespace
