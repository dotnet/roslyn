' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EndConstructGeneration
    <[UseExportProvider]>
    Public Class PreprocessorRegionTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub ApplyAfterHashRegion()
            VerifyStatementEndConstructApplied(
                before:="#Region ""Goo""",
                beforeCaret:={0, -1},
                after:="#Region ""Goo""

#End Region",
                afterCaret:={1, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWithoutStringConstant()
            VerifyStatementEndConstructNotApplied(
                text:="#Region",
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWhenEndRegionExists1()
            VerifyStatementEndConstructNotApplied(
                text:="#Region ""Goo""
#End Region",
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWhenEndRegionExists2()
            VerifyStatementEndConstructNotApplied(
                text:="#Region ""Goo""
#Region ""Bar""
#End Region
#End Region",
                caret:={0, -1})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EndConstructGeneration)>
        Public Sub DontApplyAfterHashRegionWhenEndRegionExists3()
            VerifyStatementEndConstructNotApplied(
                text:="#Region ""Goo""
#Region ""Bar""
#End Region
#End Region",
                caret:={1, -1})
        End Sub
    End Class
End Namespace
