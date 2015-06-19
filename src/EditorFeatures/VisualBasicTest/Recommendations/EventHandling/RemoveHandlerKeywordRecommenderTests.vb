' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.EventHandling
    Public Class RemoveHandlerKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>|</MethodBody>, "RemoveHandler",
$"{VBFeaturesResources.RemovehandlerStatement}
{RemovesEventAssociation}
RemoveHandler {Event1}, {Handler}")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x 
|</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerMissingInClassBlock()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "RemoveHandler")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerInSingleLineLambda()
            VerifyRecommendationsContain(<MethodBody>Dim x = Sub() |</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerInSingleLineFunctionLambda()
            VerifyRecommendationsMissing(<MethodBody>Dim x = Function() |</MethodBody>, "RemoveHandler")
        End Sub

        <Fact>
        <WorkItem(808406)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub RemoveHandlerInCustomEvent()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
       |
    End Event
End Class</File>

            VerifyRecommendationsContain(code, "RemoveHandler")
        End Sub

        <Fact>
        <WorkItem(808406)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotRemoveHandlerInCustomEventWithRemoveHandler()
            Dim code = <File>
Public Class Z
    Public Custom Event E As Action
        RemoveHandler(z as Action)
        End RemoveHandler
       |
    End Event
End Class</File>

            VerifyRecommendationsMissing(code, "RemoveHandler")
        End Sub
    End Class
End Namespace
