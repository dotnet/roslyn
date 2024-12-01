' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class AggregateKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub AggregateNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub AggregateAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub SpecExample1Test()
            VerifyRecommendationsContain(
<MethodBody>
Dim orderTotals = _
    From cust In Customers _
    Where cust.State = "WA" _
    |
</MethodBody>, "Aggregate")
        End Sub

        <Fact>
        Public Sub SpecExample2Test()
            VerifyRecommendationsContain(
<MethodBody>
Dim ordersTotal = _
    |
</MethodBody>, "Aggregate")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Sub AggregateAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Aggregate")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub AggregateAfterAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Aggregate")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        Public Sub AggregateAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Aggregate")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        Public Sub AggregateAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Aggregate")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        Public Sub NotInDelegateCreationTest()
            Dim code =
<File>
Module Program
    Sub Main(args As String())
        Dim f1 As New Goo2( |
    End Sub

    Delegate Sub Goo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</File>

            VerifyRecommendationsMissing(code, "Aggregate")
        End Sub
    End Class
End Namespace
