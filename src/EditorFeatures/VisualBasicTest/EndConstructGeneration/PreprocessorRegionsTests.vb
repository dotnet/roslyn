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
        Public Async Function ApplyAfterHashRegion() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="#Region ""Foo""",
                beforeCaret:={0, -1},
                after:="#Region ""Foo""

#End Region",
                afterCaret:={1, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function ApplyAfterHashRegion1() As Threading.Tasks.Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="#Region ""Foo""
#Region ""Bar""
#End Region",
                beforeCaret:={1, -1},
                after:="#Region ""Foo""
#Region ""Bar""

#End Region
#End Region",
                afterCaret:={2, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterHashRegionWithoutStringConstant() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region",
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterHashRegionWhenEndRegionExists1() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region ""Foo""
#End Region",
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterHashRegionWhenEndRegionExists2() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region ""Foo""
#Region ""Bar""
#End Region
#End Region",
                caret:={0, -1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Async Function DontApplyAfterHashRegionWhenEndRegionExists3() As Threading.Tasks.Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region ""Foo""
#Region ""Bar""
#End Region
#End Region",
                caret:={1, -1})
        End Function
    End Class
End Namespace
