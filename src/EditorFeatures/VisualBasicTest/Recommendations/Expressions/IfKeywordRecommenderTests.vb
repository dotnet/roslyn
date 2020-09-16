' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class IfKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>Return |</MethodBody>, "If",
$"{String.Format(VBFeaturesResources._0_function, "If")} (+1 {FeaturesResources.overload})
{VBWorkspaceResources.If_condition_returns_True_the_function_calculates_and_returns_expressionIfTrue_Otherwise_it_returns_expressionIfFalse}
If({VBWorkspaceResources.condition} As Boolean, {VBWorkspaceResources.expressionIfTrue}, {VBWorkspaceResources.expressionIfFalse}) As {VBWorkspaceResources.result}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(|</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar, |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar + |</MethodBody>, "If")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IfAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(Not |</MethodBody>, "If")
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
        Dim f1 As New Goo2( |
    End Sub

    Delegate Sub Goo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</File>

            Await VerifyRecommendationsMissingAsync(code, "If")
        End Function
    End Class
End Namespace
