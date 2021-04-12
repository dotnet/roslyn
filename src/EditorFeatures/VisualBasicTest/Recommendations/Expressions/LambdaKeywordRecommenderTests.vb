' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class LambdaKeywordRecommenderTests
        ' TODO: potentially restrict this to smarter cases where you'd need a parenthesis around the lambda to actually
        ' call it

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SubFunctionAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyFunctionAfterIteratorTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Iterator |</MethodBody>, "Function")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub OnlyFunctionAndSubAfterAsyncTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = Async |</MethodBody>, "Function", "Sub")
        End Sub
    End Class
End Namespace
