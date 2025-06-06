' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NothingKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Nothing")
        End Sub

        <Fact>
        Public Sub NothingKeywordAfterWhileTest()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "Nothing")
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

            VerifyRecommendationsMissing(code, "Nothing")
        End Sub
    End Class
End Namespace
