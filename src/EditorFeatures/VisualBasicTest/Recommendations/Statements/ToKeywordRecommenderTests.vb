' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Statements
    Public Class ToKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ToInCaseStatementTest()
            VerifyRecommendationsContain(<MethodBody>
                                             Select Case 5
                                                 Case 6 |
                                         </MethodBody>, "To")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ToInForLoopTest()
            VerifyRecommendationsContain(<MethodBody>For i = 1 |</MethodBody>, "To")
        End Sub
    End Class
End Namespace
