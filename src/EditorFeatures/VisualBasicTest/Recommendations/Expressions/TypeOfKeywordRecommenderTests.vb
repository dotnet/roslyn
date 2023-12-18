' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class TypeOfKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "TypeOf")
        End Sub

        <Fact>
        Public Sub TypeOfAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "TypeOf")
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

            VerifyRecommendationsMissing(code, "TypeOf")
        End Sub
    End Class
End Namespace
