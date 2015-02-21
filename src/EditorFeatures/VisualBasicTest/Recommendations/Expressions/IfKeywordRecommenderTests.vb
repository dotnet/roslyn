' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class IfKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "If",
$"{String.Format(VBFeaturesResources.Function1, "If")} (+1 {FeaturesResources.Overload})
{IfConditionReturnsResults}
If({Condition} As Boolean, {ExpressionIfTrue}, {ExpressionIfFalse}) As {Result}")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "If")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IfAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "If")
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInDelegateCreation()
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


            VerifyRecommendationsMissing(code, "If")
        End Sub

    End Class
End Namespace
