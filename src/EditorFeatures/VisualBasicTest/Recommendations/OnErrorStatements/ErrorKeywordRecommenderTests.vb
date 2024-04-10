' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class ErrorKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub ErrorOptionsAfterOnTest()
            ' We can always exit a Sub/Function, so it should be there
            VerifyRecommendationsAreExactly(<MethodBody>On |</MethodBody>, "Error Resume Next", "Error GoTo")
        End Sub

        <Fact>
        Public Sub ErrorStatementInMethodBodyTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Error")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899057")>
        Public Sub ErrorStatementInLambdaTest()
            Dim code = <File>
Public Class Z
    Public Sub Main()
        Dim c = Sub() |
    End Sub
End Class</File>

            VerifyRecommendationsContain(code, "Error")
        End Sub
    End Class
End Namespace
