' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class AddressOfKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNotInStatement()
            VerifyRecommendationsMissing(<MethodBody>|</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfInAddHandler()
            VerifyRecommendationsContain(<MethodBody>AddHandler foo, |</MethodBody>, "AddressOf")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfInRemoveHandler()
            VerifyRecommendationsContain(<MethodBody>RemoveHandler foo, |</MethodBody>, "AddressOf")
        End Sub

        <WorkItem(543270)>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfInDelegateCreation()
            Dim code = <ModuleDeclaration><![CDATA[
Module Program
    Sub Main(args As String())
        Dim f1 As New Foo2( |
    End Sub
 
    Delegate Sub Foo2()
 
    Function Bar2() As Object
        Return Nothing
    End Function
End Module
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "AddressOf")
        End Sub

        <Fact>
        <WorkItem(545206)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNotAfterAddressOf()
            Dim code = <ModuleDeclaration><![CDATA[
Sub Main(args As String())
    Dim d As Func(Of Boolean) = AddressOf |
End Sub
Function Foo() As Boolean
    Return True
End Function
]]></ModuleDeclaration>
            VerifyRecommendationsMissing(code, "AddressOf")
        End Sub

        <Fact>
        <WorkItem(545206)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNotAfterAddressOfInDelegateCreation()
            Dim code = <ModuleDeclaration><![CDATA[
Sub Main(args As String())
    Dim d As New Foo(AddressOf |
End Sub
Delegate Sub Foo()
]]></ModuleDeclaration>
            VerifyRecommendationsMissing(code, "AddressOf")
        End Sub

        <Fact>
        <WorkItem(545206)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub AddressOfNestedInsideAddressOfExpression()
            Dim code = <ModuleDeclaration><![CDATA[
Class C
    Sub M(args As String())
        Dim x As Action = AddressOf Foo2(|
    End Sub

    Sub Foo()
    End Sub

    Function Foo2(a As Action) As C
        Return New C()
    End Function
End Class
]]></ModuleDeclaration>
            VerifyRecommendationsContain(code, "AddressOf")
        End Sub
    End Class
End Namespace
