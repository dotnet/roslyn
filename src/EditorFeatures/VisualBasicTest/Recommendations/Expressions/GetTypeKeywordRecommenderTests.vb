' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GetTypeKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub GetTypeHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "GetType",
$"{VBFeaturesResources.GetType_function}
{VBWorkspaceResources.Returns_a_System_Type_object_for_the_specified_type_name}
GetType({VBWorkspaceResources.typeName}) As Type")
        End Sub

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeInStatementTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "GetType")
        End Sub

        <Fact>
        Public Sub GetTypeAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "GetType")
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

            VerifyRecommendationsMissing(code, "GetType")
        End Sub
    End Class
End Namespace
