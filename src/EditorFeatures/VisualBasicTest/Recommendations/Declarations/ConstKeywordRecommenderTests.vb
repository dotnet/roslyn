' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ConstKeywordRecommenderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstInMethodBody()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Const")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstInLambda()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Const")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterStatement()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "Const")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstNotInsideSingleLineLambda()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Const")
        End Sub

        <WorkItem(544912)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterDimInClass()
            VerifyRecommendationsContain(<ClassDeclaration>Dim |</ClassDeclaration>, "Const")
        End Sub

        <WorkItem(644881)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterFriendInClass()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Const")
        End Sub

        <WorkItem(644881)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterFriendInModule()
            VerifyRecommendationsContain(<ModuleDeclaration>Friend |</ModuleDeclaration>, "Const")
        End Sub

        <WorkItem(674791)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHash()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Const")
        End Sub

    End Class
End Namespace
