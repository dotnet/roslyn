' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class GlobalKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalInStatement()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Global")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalNotAfterItself()
            VerifyRecommendationsMissing(<MethodBody>Global.|</MethodBody>, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalNotAfterImports()
            VerifyRecommendationsMissing(<File>Imports |</File>, "Global")
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInDelegateCreation()
            Dim code =
<File>
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo2( |
    End Sub

    Delegate Sub Foo2()

    Function Bar2() As Object
        Return Nothing
    End Function
End Module
</File>


            VerifyRecommendationsMissing(code, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterInherits()
            Dim code =
<File>
Class C
    Inherits |
End Class
</File>

            VerifyRecommendationsContain(code, "Global")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GlobalAfterImplements()
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
