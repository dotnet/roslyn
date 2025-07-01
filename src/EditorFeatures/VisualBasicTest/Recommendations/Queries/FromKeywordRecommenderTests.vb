' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Queries
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class FromKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub FromNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "From")
        End Sub

        <Fact>
        Public Sub FromAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "From")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543173")>
        Public Sub FromAfterMultiLineFunctionLambdaExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By Function()
                                             Return 5
                                         End Function |</MethodBody>, "From")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543174")>
        Public Sub FromAfterAnonymousObjectCreationExprTest()
            VerifyRecommendationsContain(<MethodBody>Dim q2 = From i1 In arr Order By New With {.Key = 10} |</MethodBody>, "From")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543219")>
        Public Sub FromAfterIntoClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = From i1 In arr Group By i1 Into Count |</MethodBody>, "From")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543232")>
        Public Sub FromAfterNestedAggregateFromClauseTest()
            VerifyRecommendationsContain(<MethodBody>Dim q1 = Aggregate i1 In arr From i4 In arr |</MethodBody>, "From")
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

            VerifyRecommendationsMissing(code, "From")
        End Sub
    End Class
End Namespace
