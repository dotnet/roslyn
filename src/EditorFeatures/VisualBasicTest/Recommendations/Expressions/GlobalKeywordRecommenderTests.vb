' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GlobalKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalInStatementTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalNotAfterItselfTest()
            VerifyRecommendationsMissing(<MethodBody>Global.|</MethodBody>, "Global")
        End Sub

        <Fact>
        Public Sub GlobalNotAfterImportsTest()
            VerifyRecommendationsMissing(<File>Imports |</File>, "Global")
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

            VerifyRecommendationsMissing(code, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterInheritsTest()
            Dim code =
<File>
Class C
    Inherits |
End Class
</File>

            VerifyRecommendationsContain(code, "Global")
        End Sub

        <Fact>
        Public Sub GlobalAfterImplementsTest()
            Dim code =
<File>
Class C
    Implements |
End Class
</File>

            VerifyRecommendationsContain(code, "Global")
        End Sub
    End Class
End Namespace
