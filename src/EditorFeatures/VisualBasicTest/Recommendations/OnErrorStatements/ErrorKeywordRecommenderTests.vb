' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.OnErrorStatements
    Public Class ErrorKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ErrorOptionsAfterOnTest() As Task
            ' We can always exit a Sub/Function, so it should be there
            Await VerifyRecommendationsAreExactlyAsync(<MethodBody>On |</MethodBody>, "Error Resume Next", "Error GoTo")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ErrorStatementInMethodBodyTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "Error")
        End Function

        <WorkItem(899057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899057")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ErrorStatementInLambdaTest() As Task
            Dim code = <File>
Public Class Z
    Public Sub Main()
        Dim c = Sub() |
    End Sub
End Class</File>

            Await VerifyRecommendationsContainAsync(code, "Error")
        End Function
    End Class
End Namespace
