' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

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

        <WorkItem(544912)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterDimInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Dim |</ClassDeclaration>, "Const")
        End Function

        <WorkItem(644881)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterFriendInClassTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>Friend |</ClassDeclaration>, "Const")
        End Function

        <WorkItem(644881)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ConstAfterFriendInModuleTest() As Task
            Await VerifyRecommendationsContainAsync(<ModuleDeclaration>Friend |</ModuleDeclaration>, "Const")
        End Function

        <WorkItem(674791)>
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
