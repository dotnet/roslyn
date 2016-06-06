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
        Public Async Function NumericTypesAfterEnumAs() As Threading.Tasks.Task
            Await VerifyRecommendationsAreExactlyAsync(<File>Enum Foo As |</File>, "Byte",
                                                                        "SByte",
                                                                        "Short",
                                                                        "UShort",
                                                                        "Integer",
                                                                        "UInteger",
                                                                        "Long",
                                                                        "ULong")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AllTypesAfterMethodBody() As Threading.Tasks.Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim foo As |</MethodBody>, _keywordList)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoTypesAreInTypeConstraint() As Threading.Tasks.Task
            Await VerifyRecommendationsMissingAsync(<File>Class Foo(Of String As |</File>, _keywordList)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoTypesAfterImports() As Threading.Tasks.Task
            Await VerifyRecommendationsMissingAsync(<File>Imports |</File>, _keywordList)
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoTypesInDelegateCreation() As Task
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

            Await VerifyRecommendationsMissingAsync(code, _keywordList)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoTypesInInheritsStatement() As Task
            Dim code =
<File>
Class C
    Inherits |
End Class
</File>

            Await VerifyRecommendationsMissingAsync(code, _keywordList)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoTypesInImplementsStatement() As Task
            Dim code =
<File>
Class C
    Implements |
End Class
</File>

            Await VerifyRecommendationsMissingAsync(code, _keywordList)
        End Function

    End Class
End Namespace
