' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class IsKeywordRecommenderTests

        <Fact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInCaseClause()
            VerifyRecommendationsContain(
                <MethodBody>        
                    Select Case 5
                         Case |
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoIsKeywordAfterCaseAfterCaseElse()
            VerifyRecommendationsMissing(
                <MethodBody>
                    Select Case 5
                        Case Else
                            Dim i = 3
                        Case |
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInMiddleCaseClause()
            VerifyRecommendationsContain(
                <MethodBody>
                    Select Case 5
                        Case 4, |, Is > 7
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInFinalCaseClause()
            VerifyRecommendationsContain(
                <MethodBody>
                    Select Case 5
                        Case 4, Is > 5, |
                    End Select
                </MethodBody>, "Is")
        End Sub

        <Fact>
        <WorkItem(543384)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsInExistingIsClause()
            VerifyRecommendationsContain(
                <MethodBody>
                    Select Case 5
                        Case |Is > 5
                    End Select
                </MethodBody>, "Is")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterEol()
            VerifyRecommendationsMissing(
<MethodBody>
    Select Case 5
        Case 
|
    End Select
</MethodBody>, "Is")
        End Sub

        <WorkItem(530953)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterExplicitLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>
    Select Case 5
        Case _
|
    End Select
</MethodBody>, "Is")
        End Sub
    End Class
End Namespace
