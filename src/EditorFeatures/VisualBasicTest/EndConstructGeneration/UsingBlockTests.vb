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
    Public Class UsingBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterUsingStatement()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "Sub foo()",
                         "Using variable",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "Sub foo()",
                        "Using variable",
                        "",
                        "End Using",
                        "End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyForMatchedUsing() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class c1",
                       "Sub foo()",
                       "Using variable",
                       "End Using",
                       "End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedUsing()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        Using y",
                         "            Using z",
                         "        End Using",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={3, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        Using y",
                         "            Using z",
                         "",
                         "            End Using",
                         "        End Using",
                         "    End Sub",
                         "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyUsingWithDelegate()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        Using Func(of String, String)",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        Using Func(of String, String)",
                         "",
                         "        End Using",
                         "    End Sub",
                         "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyUsingAtInvalidSyntax() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Sub S",
                       "        Using x asf asdf",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function VerifyUsingAtInvalidLocation() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:={"Class EC",
                       "    Using x",
                       "End Class"},
                caret:={1, -1})
        End Function
    End Class
End Namespace
