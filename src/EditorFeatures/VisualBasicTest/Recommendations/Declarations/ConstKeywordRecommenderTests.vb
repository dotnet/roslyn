' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class ConstKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Const")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstInLambdaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x = Sub()
|
        End Sub</MethodBody>, "Const")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Dim x
|</MethodBody>, "Const")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstNotInsideSingleLineLambdaTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>
Dim x = Sub() |
</MethodBody>, "Const")
        End Function

        <WorkItem(544912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544912")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterDimInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim |</ClassDeclaration>, "Const")
        End Function

        <WorkItem(644881, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/644881")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterFriendInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Const")
        End Function

        <WorkItem(644881, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/644881")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterFriendInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>Friend |</ModuleDeclaration>, "Const")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Const")
        End Function
    End Class
End Namespace
