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
    Public Class WithBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterWithStatement()
            VerifyStatementEndConstructApplied(
                before:={"Class c1",
                         "Sub foo()",
                         "With variable",
                         "End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                after:={"Class c1",
                        "Sub foo()",
                        "With variable",
                        "",
                        "End With",
                        "End Sub",
                        "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyForMatchedWith()
            VerifyStatementEndConstructNotApplied(
                text:={"Class c1",
                       "Sub foo()",
                       "With variable",
                       "End With",
                       "End Sub",
                       "End Class"},
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyNestedWith()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        With K",
                         "            With K",
                         "        End With",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={3, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        With K",
                         "            With K",
                         "",
                         "            End With",
                         "        End With",
                         "    End Sub",
                         "End Class"},
                afterCaret:={4, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyWithFollowsCode()
            VerifyStatementEndConstructApplied(
                before:={"Class C",
                         "    Sub S",
                         "        With K",
                         "        Dim x = 5",
                         "    End Sub",
                         "End Class"},
                beforeCaret:={2, -1},
                 after:={"Class C",
                         "    Sub S",
                         "        With K",
                         "",
                         "        End With",
                         "        Dim x = 5",
                         "    End Sub",
                         "End Class"},
                afterCaret:={3, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidWithSyntax()
            VerifyStatementEndConstructNotApplied(
                text:={"Class EC",
                       "    Sub S",
                       "        With using",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidWithLocation()
            VerifyStatementEndConstructNotApplied(
                text:={"Class EC",
                       "    With True",
                       "End Class"},
                caret:={1, -1})
        End Sub

    End Class
End Namespace
