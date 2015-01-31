' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class EqualsKeywordRecommenderTests
        <WorkItem(543136)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EqualsAfterJoinInOnIdentifier()
            Dim method = <MethodBody>
                             Dim arr = New Integer() {4, 5}
                             Dim q2 = From num In arr Join n1 In arr On num |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Equals")
        End Sub

        <WorkItem(543136)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EqualsAfterJoinInOnBinaryExpression()
            Dim method = <MethodBody>
                             Dim arr = New Integer() {4, 5}
                             Dim q2 = From num In arr Join n1 In arr On num + 5L |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Equals")
        End Sub
    End Class
End Namespace
