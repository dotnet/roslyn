' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class TrueFalseKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "True", "False")
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

            VerifyRecommendationsMissing(code, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective1()
            VerifyRecommendationsContain(<File>#if |</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective2()
            VerifyRecommendationsContain(<File>#if not |</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective3()
            VerifyRecommendationsContain(<File>#if (|</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective4()
            VerifyRecommendationsContain(<File>#if true andalso |</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective5()
            VerifyRecommendationsContain(<File>#if true and |</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective6()
            VerifyRecommendationsContain(<File>#if true orelse |</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective7()
            VerifyRecommendationsContain(<File>#if true or |</File>, "True", "False")
        End Sub

        <Fact>
        Public Sub TrueFalseInDirective()
            VerifyRecommendationsContain(
<File>
#if true
#elseif |
</File>, "True", "False")
        End Sub
    End Class
End Namespace
