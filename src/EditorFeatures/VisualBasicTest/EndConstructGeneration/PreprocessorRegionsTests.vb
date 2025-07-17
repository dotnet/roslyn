' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class PreprocessorRegionTests
        <WpfFact>
        Public Async Function ApplyAfterHashRegion() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="#Region ""Goo""",
                beforeCaret:={0, -1},
                after:="#Region ""Goo""

#End Region",
                afterCaret:={1, -1})
        End Function

        <WpfFact>
        Public Async Function ApplyAfterHashRegion1() As Task
            Await VerifyStatementEndConstructAppliedAsync(
                before:="#Region ""Goo""
#Region ""Bar""
#End Region",
                beforeCaret:={1, -1},
                after:="#Region ""Goo""
#Region ""Bar""

#End Region
#End Region",
                afterCaret:={2, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterHashRegionWithoutStringConstant() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterHashRegionWhenEndRegionExists1() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region ""Goo""
#End Region",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterHashRegionWhenEndRegionExists2() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region ""Goo""
#Region ""Bar""
#End Region
#End Region",
                caret:={0, -1})
        End Function

        <WpfFact>
        Public Async Function DoNotApplyAfterHashRegionWhenEndRegionExists3() As Task
            Await VerifyStatementEndConstructNotAppliedAsync(
                text:="#Region ""Goo""
#Region ""Bar""
#End Region
#End Region",
                caret:={1, -1})
        End Function
    End Class
End Namespace
