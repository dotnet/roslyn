' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Types
    Public Class BuiltInTypesKeywordRecommenderTests
        Private ReadOnly _keywordList As String() = {
            "Boolean",
            "Byte",
            "Char",
            "Date",
            "Decimal",
            "Double",
            "Integer",
            "Long",
            "Object",
            "SByte",
            "Short",
            "Single",
            "String",
            "UInteger",
            "ULong",
            "UShort"
        }

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NumericTypesAfterEnumAs()
            VerifyRecommendationsAreExactly(<File>Enum Foo As |</File>, "Byte",
                                                                        "SByte",
                                                                        "Short",
                                                                        "UShort",
                                                                        "Integer",
                                                                        "UInteger",
                                                                        "Long",
                                                                        "ULong")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AllTypesAfterMethodBody()
            VerifyRecommendationsContain(<MethodBody>Dim foo As |</MethodBody>, _keywordList)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoTypesAreInTypeConstraint()
            VerifyRecommendationsMissing(<File>Class Foo(Of String As |</File>, _keywordList)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoTypesAfterImports()
            VerifyRecommendationsMissing(<File>Imports |</File>, _keywordList)
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoTypesInDelegateCreation()
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

            VerifyRecommendationsMissing(code, _keywordList)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoTypesInInheritsStatement()
            Dim code =
<File>
Class C
    Inherits |
End Class
</File>

            VerifyRecommendationsMissing(code, _keywordList)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoTypesInImplementsStatement()
            Dim code =
<File>
Class C
    Implements |
End Class
</File>

            VerifyRecommendationsMissing(code, _keywordList)
        End Sub

    End Class
End Namespace
