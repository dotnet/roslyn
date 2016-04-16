' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Declarations
    Public Class DelegateSubFunctionKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubAndFunctionAfterDelegateTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(<ClassDeclaration>Delegate |</ClassDeclaration>, "Sub", "Function")
        End Function

        <WorkItem(530953, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AfterExplicitLineContinuationTest() As Task
            Await VerifyRecommendationsAreExactlyAsync(
<ClassDeclaration>Delegate _
|</ClassDeclaration>, "Sub", "Function")
        End Function
    End Class
End Namespace
