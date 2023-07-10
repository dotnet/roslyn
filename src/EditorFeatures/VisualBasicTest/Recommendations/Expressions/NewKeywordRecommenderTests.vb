' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NewKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NewNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterDimAsTest()
            VerifyRecommendationsContain(<MethodBody>Dim x As |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterAsInPropertyDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public Property goo As |</ClassDeclaration>, "New")
        End Sub

        <Fact>
        Public Sub NewAfterAsInReadOnlyPropertyDeclarationTest()
            VerifyRecommendationsContain(<ClassDeclaration>Public ReadOnly Property goo As |</ClassDeclaration>, "New")
        End Sub

        <Fact>
        Public Sub NewNotAfterAsInWriteOnlyPropertyDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>Public WriteOnly Property goo As |</ClassDeclaration>, "New")
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

            VerifyRecommendationsMissing(code, "New")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub NotAfterEolTest()
            VerifyRecommendationsMissing(
<MethodBody>Dim x As 
|</MethodBody>, "New")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530953")>
        Public Sub AfterExplicitLineContinuationTest()
            VerifyRecommendationsContain(
<MethodBody>Dim x As _
|</MethodBody>, "New")
        End Sub

        <Fact>
        Public Sub AfterExplicitLineContinuationTestCommentsAfterLineContinuation()
            VerifyRecommendationsContain(
<MethodBody>Dim x As _ ' Test
|</MethodBody>, "New")
        End Sub
    End Class
End Namespace
