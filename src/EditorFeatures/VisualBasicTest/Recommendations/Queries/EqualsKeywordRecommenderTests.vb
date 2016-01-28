' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class EqualsKeywordRecommenderTests
        <WorkItem(543136)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EqualsAfterJoinInOnIdentifierTest() As Task
            Dim method = <MethodBody>
                             Dim arr = New Integer() {4, 5}
                             Dim q2 = From num In arr Join n1 In arr On num |
                         </MethodBody>

            Await VerifyRecommendationsAreExactlyAsync(method, "Equals")
        End Function

        <WorkItem(543136)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function EqualsAfterJoinInOnBinaryExpressionTest() As Task
            Dim method = <MethodBody>
                             Dim arr = New Integer() {4, 5}
                             Dim q2 = From num In arr Join n1 In arr On num + 5L |
                         </MethodBody>

            Await VerifyRecommendationsAreExactlyAsync(method, "Equals")
        End Function
    End Class
End Namespace
