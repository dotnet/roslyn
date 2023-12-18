' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class IfKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub IfHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "If",
$"{String.Format(VBFeaturesResources._0_function, "If")} (+1 {FeaturesResources.overload})
{VBWorkspaceResources.If_condition_returns_True_the_function_calculates_and_returns_expressionIfTrue_Otherwise_it_returns_expressionIfFalse}
If({VBWorkspaceResources.condition} As Boolean, {VBWorkspaceResources.expressionIfTrue}, {VBWorkspaceResources.expressionIfFalse}) As {VBWorkspaceResources.result}")
        End Sub

        <Fact>
        Public Sub IfAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "If")
        End Sub

        <Fact>
        Public Sub IfAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "If")
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

            VerifyRecommendationsMissing(code, "If")
        End Sub
    End Class
End Namespace
