' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class CastOperatorsKeywordRecommenderTests
        Inherits RecommenderTests

        Private Shared ReadOnly Property AllTypeConversionOperatorKeywords As String()
            Get
                Dim keywords As New List(Of String) From {"CType", "DirectCast", "TryCast"}

                For Each k In CastOperatorsKeywordRecommender.PredefinedKeywordList
                    keywords.Add(SyntaxFacts.GetText(k))
                Next

                Return keywords.ToArray()
            End Get
        End Property

        <Fact>
        Public Sub DirectCastHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "DirectCast",
$"{VBFeaturesResources.DirectCast_function}
{VBWorkspaceResources.Introduces_a_type_conversion_operation_similar_to_CType_The_difference_is_that_CType_succeeds_as_long_as_there_is_a_valid_conversion_whereas_DirectCast_requires_that_one_type_inherit_from_or_implement_the_other_type}
DirectCast({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}")
        End Sub

        <Fact>
        Public Sub TryCastHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "TryCast",
$"{VBFeaturesResources.TryCast_function}
{VBWorkspaceResources.Introduces_a_type_conversion_operation_that_does_not_throw_an_exception_If_an_attempted_conversion_fails_TryCast_returns_Nothing_which_your_program_can_test_for}
TryCast({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}")
        End Sub

        <Fact>
        Public Sub CTypeHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "CType",
$"{VBFeaturesResources.CType_function}
{VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type}
CType({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}")
        End Sub

        <Fact>
        Public Sub CBoolHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "CBool",
$"{String.Format(VBFeaturesResources._0_function, "CBool")}
{String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Boolean")}
CBool({VBWorkspaceResources.expression}) As Boolean")
        End Sub

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllInStatementTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact>
        Public Sub AllAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        Public Sub NoneInDelegateCreationTest()
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

            VerifyRecommendationsMissing(code, AllTypeConversionOperatorKeywords)
        End Sub
    End Class
End Namespace
