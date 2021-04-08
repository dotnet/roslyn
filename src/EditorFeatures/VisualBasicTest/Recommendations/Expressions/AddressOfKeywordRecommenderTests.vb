﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <[UseExportProvider]>
    Public Class AddressOfKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNotInStatementTest()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfInAddHandlerTest()
            VerifyRecommendationsContain(<MethodBody>AddHandler goo, |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfInRemoveHandlerTest()
            VerifyRecommendationsContain(<MethodBody>RemoveHandler goo, |</MethodBody>, "AddressOf")
        End Sub

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfInDelegateCreationTest()
            Dim code = <ModuleDeclaration><![CDATA[
Module Program
    Sub Main(args As String())
        Dim f1 As New Goo2( |
    End Sub
 
    Delegate Sub Goo2()
 
    Function Bar2() As Object
        Return Nothing
    End Function
End Module
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "AddressOf")
        End Sub

        <Fact>
        <WorkItem(545206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545206")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNotAfterAddressOfTest()
            Dim code = <ModuleDeclaration><![CDATA[
Sub Main(args As String())
    Dim d As Func(Of Boolean) = AddressOf |
End Sub
Function Goo() As Boolean
    Return True
End Function
]]></ModuleDeclaration>
            VerifyRecommendationsMissing(code, "AddressOf")
        End Sub

        <Fact>
        <WorkItem(545206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545206")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNotAfterAddressOfInDelegateCreationTest()
            Dim code = <ModuleDeclaration><![CDATA[
Sub Main(args As String())
    Dim d As New Goo(AddressOf |
End Sub
Delegate Sub Goo()
]]></ModuleDeclaration>
            VerifyRecommendationsMissing(code, "AddressOf")
        End Sub

        <Fact>
        <WorkItem(545206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545206")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNestedInsideAddressOfExpressionTest()
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Sub M(args As String())
        Dim x As Action = AddressOf Goo2(|
    End Sub

    Sub Goo()
    End Sub

    Function Goo2(a As Action) As C
        Return New C()
    End Function
End Class
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "AddressOf")
        End Sub
    End Class
End Namespace
