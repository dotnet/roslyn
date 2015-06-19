' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class GetXmlNamespaceKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceHelpText()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "GetXmlNamespace",
$"{VBFeaturesResources.GetxmlnamespaceFunction}
{ReturnsXNamespaceObject}
GetXmlNamespace([{XmlNamespacePrefix}]) As System.Xml.Linq.XNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceNotInStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub GetXmlNamespaceAfterWhileLoop()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "GetXmlNamespace")
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoGetXmlNamespaceInDelegateCreation()
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

            VerifyRecommendationsMissing(code, "GetXmlNamespace")
        End Sub

    End Class
End Namespace
