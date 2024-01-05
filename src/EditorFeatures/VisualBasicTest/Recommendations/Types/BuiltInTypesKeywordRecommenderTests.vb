' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Types
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class BuiltInTypesKeywordRecommenderTests
        Inherits RecommenderTests

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

        <Fact>
        Public Sub NumericTypesAndGlobalAfterEnumAs()
            VerifyRecommendationsAreExactly(<File>Enum Goo As |</File>, "Global",
                                                                        "Byte",
                                                                        "SByte",
                                                                        "Short",
                                                                        "UShort",
                                                                        "Integer",
                                                                        "UInteger",
                                                                        "Long",
                                                                        "ULong")
        End Sub

        <Fact>
        Public Sub AllTypesAfterMethodBody()
            VerifyRecommendationsContain(<MethodBody>Dim goo As |</MethodBody>, _keywordList)
        End Sub

        <Fact>
        Public Sub NoTypesAreInTypeConstraint()
            VerifyRecommendationsMissing(<File>Class Goo(Of String As |</File>, _keywordList)
        End Sub

        <Fact>
        Public Sub NoTypesAfterImports()
            VerifyRecommendationsMissing(<File>Imports |</File>, _keywordList)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        Public Sub NoTypesInDelegateCreation()
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

            VerifyRecommendationsMissing(code, _keywordList)
        End Sub

        <Fact>
        Public Sub NoTypesInInheritsStatement()
            Dim code =
<File>
Class C
    Inherits |
End Class
</File>

            VerifyRecommendationsMissing(code, _keywordList)
        End Sub

        <Fact>
        Public Sub NoTypesInImplementsStatement()
            Dim code =
<File>
Class C
    Implements |
End Class
</File>

            VerifyRecommendationsMissing(code, _keywordList)
        End Sub

        <Fact>
        Public Sub Preselection()
            Dim code =
<File>
Class Program
    Sub Main(args As String())
        Goo(|)
    End Sub

    Sub Goo(x As Integer)

    End Sub
End Class
</File>

            VerifyRecommendationsWithPriority(code, SymbolMatchPriority.Keyword, "Integer")
        End Sub
    End Class
End Namespace
