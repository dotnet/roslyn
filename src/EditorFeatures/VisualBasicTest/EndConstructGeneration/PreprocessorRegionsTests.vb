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
    Public Class PreprocessorRegionTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterHashRegion()
            VerifyStatementEndConstructApplied(
                before:={"#Region ""Foo"""},
                beforeCaret:={0, -1},
                after:={"#Region ""Foo""",
                        "",
                        "#End Region"},
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterHashRegion1()
            VerifyStatementEndConstructApplied(
                before:={"#Region ""Foo""", "#Region ""Bar""", "#End Region"},
                beforeCaret:={1, -1},
                after:={"#Region ""Foo""",
                        "#Region ""Bar""",
                        "",
                        "#End Region",
                        "#End Region"},
                afterCaret:={2, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWithoutStringConstant()
            VerifyStatementEndConstructNotApplied(
                text:={"#Region"},
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWhenEndRegionExists1()
            VerifyStatementEndConstructNotApplied(
                text:={"#Region ""Foo""",
                       "#End Region"},
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWhenEndRegionExists2()
            VerifyStatementEndConstructNotApplied(
                text:={"#Region ""Foo""",
                       "#Region ""Bar""",
                       "#End Region",
                       "#End Region"},
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWhenEndRegionExists3()
            VerifyStatementEndConstructNotApplied(
                text:={"#Region ""Foo""",
                       "#Region ""Bar""",
                       "#End Region",
                       "#End Region"},
                caret:={1, -1})
        End Sub
    End Class
End Namespace
