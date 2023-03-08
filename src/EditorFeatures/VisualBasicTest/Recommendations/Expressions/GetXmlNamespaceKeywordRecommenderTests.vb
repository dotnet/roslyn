' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class GetXmlNamespaceKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub GetXmlNamespaceHelpTextTest()
            VerifyRecommendationDescriptionTextIs(<MethodBody>Return |</MethodBody>, "GetXmlNamespace",
$"{VBFeaturesResources.GetXmlNamespace_function}
{VBWorkspaceResources.Returns_the_System_Xml_Linq_XNamespace_object_corresponding_to_the_specified_XML_namespace_prefix}
GetXmlNamespace([{VBWorkspaceResources.xmlNamespacePrefix}]) As System.Xml.Linq.XNamespace")
        End Sub

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact>
        Public Sub GetXmlNamespaceAfterWhileLoopTest()
            VerifyRecommendationsContain(<MethodBody>While |</MethodBody>, "GetXmlNamespace")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        Public Sub NoGetXmlNamespaceInDelegateCreationTest()
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

            VerifyRecommendationsMissing(code, "GetXmlNamespace")
        End Sub
    End Class
End Namespace
