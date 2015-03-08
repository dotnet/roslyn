' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class MidKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MidHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>|</MethodBody>, "Mid",
$"{VBFeaturesResources.MidStatement}
{ReplacesChars}
Mid({StringName}, {StartIndex}, [{Length}]) = {StringExpression}")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MidInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Mid")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MidAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "Mid")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MidMissingInClassBlock()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Mid")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MidInSingleLineLambda()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "Mid")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MidNotInSingleLineFunctionLambda()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "Mid")
        End Sub
    End Class
End Namespace
