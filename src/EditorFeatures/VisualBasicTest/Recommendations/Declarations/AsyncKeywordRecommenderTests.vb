' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class AsyncKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function KeywordsAfterAsyncTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Async |</ClassDeclaration>,
                                            "Friend", "Function", "Private", "Protected", "Protected Friend", "Public", "Sub")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInMethodStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Async")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InMethodExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim z = |</MethodBody>, "Async")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FunctionDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>|</ClassDeclaration>, "Async")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AlreadyAsyncFunctionDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>| Async</ClassDeclaration>, "Async")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubDeclarationTest() As Task
            Await VerifyRecommendationsContainAsync(<ClassDeclaration>| Sub bar()</ClassDeclaration>, "Async")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function FunctionDeclarationInInterfaceTest() As Task
            Await VerifyRecommendationsContainAsync(<InterfaceDeclaration>|</InterfaceDeclaration>, "Async")
        End Function

        <WorkItem(547254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterAsyncTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Async |</ClassDeclaration>, "Async")
        End Function

        <WorkItem(645060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterConstInClassTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>Const |</ClassDeclaration>, "Async")
        End Function

        <WorkItem(645060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterConstInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>Const |</ModuleDeclaration>, "Async")
        End Function

        <WorkItem(645060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterWithEventsInClassTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>WithEvents |</ClassDeclaration>, "Async")
        End Function

        <WorkItem(645060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterWithEventsInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<ModuleDeclaration>WithEvents |</ModuleDeclaration>, "Async")
        End Function

        <WorkItem(674791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotAfterHashTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Async")
        End Function
    End Class
End Namespace
