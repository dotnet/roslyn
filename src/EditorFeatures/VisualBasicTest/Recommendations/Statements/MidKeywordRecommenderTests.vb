' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class MidKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub MidHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>|</MethodBody>, "Mid",
$"{VBFeaturesResources.Mid_statement}
{VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string}
Mid({VBWorkspaceResources.stringName}, {VBWorkspaceResources.startIndex}, [{VBWorkspaceResources.length}]) = {VBWorkspaceResources.stringExpression}")
        End Sub

        <Fact>
        Public Sub MidInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Mid")
        End Sub

        <Fact>
        Public Sub MidAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "Mid")
        End Sub

        <Fact>
        Public Sub MidMissingInClassBlockTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Mid")
        End Sub

        <Fact>
        Public Sub MidInSingleLineLambdaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "Mid")
        End Sub

        <Fact>
        Public Sub MidNotInSingleLineFunctionLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "Mid")
        End Sub
    End Class
End Namespace
