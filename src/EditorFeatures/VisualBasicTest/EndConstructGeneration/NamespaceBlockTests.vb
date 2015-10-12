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
    Public Class NamespaceBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterNamespace()
            VerifyStatementEndConstructApplied(
                before:={"Namespace foo"},
                beforeCaret:={0, -1},
                after:={"Namespace foo",
                        "",
                        "End Namespace"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub TestApplyAfterNestedNamespace()
            VerifyStatementEndConstructApplied(
                before:={"Namespace foo",
                         "Namespace bar",
                         "End Namespace"},
                beforeCaret:={1, -1},
                after:={"Namespace foo",
                        "Namespace bar",
                        "",
                        "End Namespace",
                        "End Namespace"},
                afterCaret:={2, -1})
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyRecommit()
            VerifyStatementEndConstructNotApplied(
                text:={"NameSpace Bar",
                       "End Namespace"},
                caret:={0, -1})
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidNSInMethod()
            VerifyStatementEndConstructNotApplied(
                text:={"Class C",
                       "    Sub S",
                       "        NameSpace T",
                       "    End Sub",
                       "End Class"},
                caret:={2, -1})
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub VerifyInvalidNSInModule()
            VerifyStatementEndConstructNotApplied(
                text:={"Module M",
                       "    Namespace n",
                       "End Module"},
                caret:={1, -1})
        End Sub

    End Class
End Namespace
