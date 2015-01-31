' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class CastOperatorsKeywordRecommenderTests
        Private ReadOnly Property AllTypeConversionOperatorKeywords As String()
            Get
                Dim keywords As New List(Of String) From {"CType", "DirectCast", "TryCast"}

                For Each k In CastOperatorsKeywordRecommender.PredefinedKeywordList
                    keywords.Add(SyntaxFacts.GetText(k))
                Next

                Return keywords.ToArray()
            End Get
        End Property

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub DirectCastHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "DirectCast",
                                                  <Text><![CDATA[
DirectCast function
Introduces a type conversion operation similar to CType. The difference is that CType succeeds as long as there is a valid conversion, whereas DirectCast requires that one type inherit from or implement the other type.
DirectCast(<expression>, <typeName>) As <result>]]></Text>)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub TryCastHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "TryCast",
                                                  <Text><![CDATA[
TryCast function
Introduces a type conversion operation that does not throw an exception. If an attempted conversion fails, TryCast returns Nothing, which your program can test for.
TryCast(<expression>, <typeName>) As <result>]]></Text>)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CTypeHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "CType",
                                                  <Text><![CDATA[
CType function
Returns the result of explicitly converting an expression to a specified data type.
CType(<expression>, <typeName>) As <result>]]></Text>)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub CBoolHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "CBool",
                                                  <Text><![CDATA[
CBool function
Converts an expression to the Boolean data type.
CBool(<expression>) As Boolean]]></Text>)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllInStatement()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, AllTypeConversionOperatorKeywords)
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInDelegateCreation()
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


            VerifyRecommendationsMissing(code, AllTypeConversionOperatorKeywords)
        End Sub

    End Class
End Namespace
