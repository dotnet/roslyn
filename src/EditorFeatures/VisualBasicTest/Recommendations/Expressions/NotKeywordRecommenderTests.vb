' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NotKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Not")
        End Sub

        <Fact>
        Public Sub NotNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Not")
        End Sub

        <Fact>
        Public Sub NotAfterWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "Not")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        Public Sub NoNotInDelegateCreationTest()
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

            VerifyRecommendationsMissing(code, "Not")
        End Sub
    End Class
End Namespace
