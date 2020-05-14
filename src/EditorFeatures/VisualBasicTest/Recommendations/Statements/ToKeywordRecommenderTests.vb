' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ToKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ToInCaseStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
                                             Select Case 5
                                                 Case 6 |
                                         </MethodBody>, "To")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ToInForLoopTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>For i = 1 |</MethodBody>, "To")
        End Function
    End Class
End Namespace
