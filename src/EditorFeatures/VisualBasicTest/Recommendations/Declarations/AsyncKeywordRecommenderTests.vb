' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AsyncKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub KeywordsAfterAsyncTest()
            VerifyRecommendationsAreExactly(<ClassDeclaration>Async |</ClassDeclaration>,
                                            "Friend", "Function", "Private", "Protected", "Protected Friend", "Public", "Sub")
        End Sub

        <Fact>
        Public Sub NotInMethodStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Async")
        End Sub

        <Fact>
        Public Sub InMethodExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Dim z = |</MethodBody>, "Async")
        End Sub

        <Fact>
        Public Sub FunctionDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>|</ClassDeclaration>, "Async")
        End Sub

        <Fact>
        Public Sub AlreadyAsyncFunctionDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>| Async</ClassDeclaration>, "Async")
        End Sub

        <Fact>
        Public Sub SubDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>| Sub bar()</ClassDeclaration>, "Async")
        End Sub

        <Fact>
        Public Sub FunctionDeclarationInInterfaceTest()
            VerifyRecommendationsContain(<InterfaceDeclaration>|</InterfaceDeclaration>, "Async")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547254")>
        Public Sub NotAfterAsyncTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Async |</ClassDeclaration>, "Async")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        Public Sub NotAfterConstInClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Const |</ClassDeclaration>, "Async")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        Public Sub NotAfterConstInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>Const |</ModuleDeclaration>, "Async")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        Public Sub NotAfterWithEventsInClassTest()
            VerifyRecommendationsMissing(<ClassDeclaration>WithEvents |</ClassDeclaration>, "Async")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645060")>
        Public Sub NotAfterWithEventsInModuleTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>WithEvents |</ModuleDeclaration>, "Async")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674791")>
        Public Sub NotAfterHashTest()
            VerifyRecommendationsMissing(<File>
Imports System

#|
 
Module Module1
 
End Module

</File>, "Async")
        End Sub
    End Class
End Namespace
