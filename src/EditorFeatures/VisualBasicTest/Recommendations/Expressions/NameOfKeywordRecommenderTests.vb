' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class NameOfKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotAtStartOfStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InArgumentList_Position1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InArgumentList_Position2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InVariableInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InArrayInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InArrayInitializerAfterComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterWhile()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "NameOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AfterCall()
            VerifyRecommendationsContain(<MethodBody>Call |</MethodBody>, "NameOf")
        End Sub

    End Class
End Namespace
