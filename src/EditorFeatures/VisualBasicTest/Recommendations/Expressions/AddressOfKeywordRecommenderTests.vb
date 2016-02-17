' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class AddressOfKeywordRecommenderTests

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfNotInStatementTest() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>|</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(|</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar, |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar + |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(Not |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfInAddHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>AddHandler foo, |</MethodBody>, "AddressOf")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfInRemoveHandlerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>RemoveHandler foo, |</MethodBody>, "AddressOf")
        End Function

        <WorkItem(543270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543270")>
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfInDelegateCreationTest() As Task
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
            Await VerifyRecommendationsContainAsync(code, "AddressOf")
        End Function

        <Fact>
        <WorkItem(545206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545206")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfNotAfterAddressOfTest() As Task
            Dim code = <ModuleDeclaration><![CDATA[
Sub Main(args As String())
    Dim d As Func(Of Boolean) = AddressOf |
End Sub
Function Foo() As Boolean
    Return True
End Function
]]></ModuleDeclaration>
            Await VerifyRecommendationsMissingAsync(code, "AddressOf")
        End Function

        <Fact>
        <WorkItem(545206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545206")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfNotAfterAddressOfInDelegateCreationTest() As Task
            Dim code = <ModuleDeclaration><![CDATA[
Sub Main(args As String())
    Dim d As New Foo(AddressOf |
End Sub
Delegate Sub Foo()
]]></ModuleDeclaration>
            Await VerifyRecommendationsMissingAsync(code, "AddressOf")
        End Function

        <Fact>
        <WorkItem(545206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545206")>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function AddressOfNestedInsideAddressOfExpressionTest() As Task
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
            Await VerifyRecommendationsContainAsync(code, "AddressOf")
        End Function
    End Class
End Namespace
