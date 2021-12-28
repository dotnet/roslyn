' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ConstKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Const")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstInLambdaTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Const")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterStatementTest()
            VerifyRecommendationsContain(<MethodBody>
Dim x
|</MethodBody>, "Const")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstNotInsideSingleLineLambdaTest()
            VerifyRecommendationsMissing(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Const")
        End Sub

        <WorkItem(544912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544912")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterDimInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim |</ClassDeclaration>, "Const")
        End Sub

        <WorkItem(644881, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/644881")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterFriendInClassTest()
            VerifyRecommendationsContain(<ClassDeclaration>Friend |</ClassDeclaration>, "Const")
        End Sub

        <WorkItem(644881, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/644881")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ConstAfterFriendInModuleTest()
            VerifyRecommendationsContain(<ModuleDeclaration>Friend |</ModuleDeclaration>, "Const")
        End Sub

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Const")
        End Sub
    End Class
End Namespace
