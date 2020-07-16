' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class CastOperatorsKeywordRecommenderTests
        Private Shared ReadOnly Property AllTypeConversionOperatorKeywords As String()
            Get
                Dim keywords As New List(Of String) From {"CType", "DirectCast", "TryCast"}

                For Each k In CastOperatorsKeywordRecommender.PredefinedKeywordList
                    keywords.Add(SyntaxFacts.GetText(k))
                Next

                Return keywords.ToArray()
            End Get
        End Property

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function DirectCastHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>Return |</MethodBody>, "DirectCast",
$"{VBFeaturesResources.DirectCast_function}
{VBWorkspaceResources.Introduces_a_type_conversion_operation_similar_to_CType_The_difference_is_that_CType_succeeds_as_long_as_there_is_a_valid_conversion_whereas_DirectCast_requires_that_one_type_inherit_from_or_implement_the_other_type}
DirectCast({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TryCastHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>Return |</MethodBody>, "TryCast",
$"{VBFeaturesResources.TryCast_function}
{VBWorkspaceResources.Introduces_a_type_conversion_operation_that_does_not_throw_an_exception_If_an_attempted_conversion_fails_TryCast_returns_Nothing_which_your_program_can_test_for}
TryCast({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CTypeHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>Return |</MethodBody>, "CType",
$"{VBFeaturesResources.CType_function}
{VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type}
CType({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function CBoolHelpTextTest() As Task
            Await VerifyRecommendationDescriptionTextIsAsync(<MethodBody>Return |</MethodBody>, "CBool",
$"{String.Format(VBFeaturesResources._0_function, "CBool")}
{String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Boolean")}
CBool({VBWorkspaceResources.expression}) As Boolean")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllInStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar, |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(bar + |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Goo(Not |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInDelegateCreationTest() As Task
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

            Await VerifyRecommendationsMissingAsync(code, AllTypeConversionOperatorKeywords)
        End Function
    End Class
End Namespace
