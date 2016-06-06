' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class IfKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>Return |</MethodBody>, "If",
$"{String.Format(VBFeaturesResources.Function1, "If")} (+1 {FeaturesResources.Overload})
{IfConditionReturnsResults}
If({Condition} As Boolean, {ExpressionIfTrue}, {ExpressionIfFalse}) As {Result}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(|</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar, |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar + |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(Not |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, "If")
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
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


            Await VerifyRecommendationsMissingAsync(code, "If")
        End Function
    End Class
End Namespace
