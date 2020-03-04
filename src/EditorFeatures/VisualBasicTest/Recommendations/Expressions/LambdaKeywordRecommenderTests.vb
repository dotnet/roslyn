﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class LambdaKeywordRecommenderTests
        ' TODO: potentially restrict this to smarter cases where you'd need a parenthesis around the lambda to actually
        ' call it

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(|</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar, |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar + |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(Not |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SubFunctionAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, "Sub", "Function", "Async", "Iterator")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyFunctionAfterIteratorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Iterator |</MethodBody>, "Function")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function OnlyFunctionAndSubAfterAsyncTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = Async |</MethodBody>, "Function", "Sub")
        End Function
    End Class
End Namespace
