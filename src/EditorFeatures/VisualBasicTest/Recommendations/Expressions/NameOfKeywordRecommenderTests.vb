' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class NameOfKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NotInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "NameOf")
        End Sub

        <Fact>
        Public Sub NotAtStartOfStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub InArgumentList_Position1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub InArgumentList_Position2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub InBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub InVariableInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub InArrayInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub InArrayInitializerAfterCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterWhileTest()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "NameOf")
        End Sub

        <Fact>
        Public Sub AfterCallTest()
            VerifyRecommendationsContain(<MethodBody>Call |</MethodBody>, "NameOf")
        End Sub
    End Class
End Namespace
