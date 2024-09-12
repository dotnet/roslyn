' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
    Public Class PreprocessorRegionTests
        <WpfFact>
        Public Sub ApplyAfterHashRegion()
            VerifyStatementEndConstructApplied(
                before:="#Region ""Goo""",
                beforeCaret:={0, -1},
                after:="#Region ""Goo""

#End Region",
                afterCaret:={1, -1})
        End Sub

        <WpfFact>
        Public Sub ApplyAfterHashRegion1()
            VerifyStatementEndConstructApplied(
                before:="#Region ""Goo""
#Region ""Bar""
#End Region",
                beforeCaret:={1, -1},
                after:="#Region ""Goo""
#Region ""Bar""

#End Region
#End Region",
                afterCaret:={2, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyAfterHashRegionWithoutStringConstant()
            VerifyStatementEndConstructNotApplied(
                text:="#Region",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyAfterHashRegionWhenEndRegionExists1()
            VerifyStatementEndConstructNotApplied(
                text:="#Region ""Goo""
#End Region",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyAfterHashRegionWhenEndRegionExists2()
            VerifyStatementEndConstructNotApplied(
                text:="#Region ""Goo""
#Region ""Bar""
#End Region
#End Region",
                caret:={0, -1})
        End Sub

        <WpfFact>
        Public Sub DoNotApplyAfterHashRegionWhenEndRegionExists3()
            VerifyStatementEndConstructNotApplied(
                text:="#Region ""Goo""
#Region ""Bar""
#End Region
#End Region",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
