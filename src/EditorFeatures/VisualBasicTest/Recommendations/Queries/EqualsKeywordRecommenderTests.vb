﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class EqualsKeywordRecommenderTests
        <WorkItem(543136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543136")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EqualsAfterJoinInOnIdentifierTest()
            Dim method = <MethodBody>
                             Dim arr = New Integer() {4, 5}
                             Dim q2 = From num In arr Join n1 In arr On num |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Equals")
        End Sub

        <WorkItem(543136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543136")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub EqualsAfterJoinInOnBinaryExpressionTest()
            Dim method = <MethodBody>
                             Dim arr = New Integer() {4, 5}
                             Dim q2 = From num In arr Join n1 In arr On num + 5L |
                         </MethodBody>

            VerifyRecommendationsAreExactly(method, "Equals")
        End Sub
    End Class
End Namespace
