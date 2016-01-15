' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    Public Class AggregateKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(|</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar, |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar + |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(Not |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SpecExample1Test() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
Dim orderTotals = _
    From cust In Customers _
    Where cust.State = "WA" _
    |
</MethodBody>, "Aggregate")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SpecExample2Test() As Task
            Await VerifyRecommendationsContainAsync(
<MethodBody>
Dim ordersTotal = _
    |
</MethodBody>, "Aggregate")
        End Function

        <WorkItem(543173)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterMultiLineFunctionLambdaExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "Aggregate")
        End Function

        <WorkItem(543174)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterAnonymousObjectCreationExprTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "Aggregate")
        End Function

        <WorkItem(543219)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterIntoClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "Aggregate")
        End Function

        <WorkItem(543232)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AggregateAfterNestedAggregateFromClauseTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "Aggregate")
        End Function

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NotInDelegateCreationTest() As Task
            Dim code =
<File>
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo2( |
    End Sub

    Delegate Sub Foo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</File>


            Await VerifyRecommendationsMissingAsync(code, "Aggregate")
        End Function
    End Class
End Namespace
