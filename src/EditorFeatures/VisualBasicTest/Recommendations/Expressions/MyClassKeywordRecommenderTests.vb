' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class MyClassKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function NoneInClassDeclarationTest() As Task
            Await VerifyRecommendationsMissingAsync(<ClassDeclaration>|</ClassDeclaration>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassInStatementTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>|</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterReturnTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Return |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterArgument1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(|</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterArgument2Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar, |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterBinaryExpressionTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(bar + |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterNotTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Foo(Not |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterTypeOfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If TypeOf |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterDoWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do While |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterDoUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Do Until |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterLoopWhileTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop While |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterLoopUntilTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>
Do
Loop Until |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>If |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterElseIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>ElseIf |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterElseSpaceIfTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Else If |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterErrorTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Error |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterThrowTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Throw |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterInitializerTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterArrayInitializerSquiggleTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {|</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterArrayInitializerCommaTest() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim x = {0, |</MethodBody>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassNotInModuleTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Module Foo
Sub Foo()
|
End Sub()
End Module</File>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassNotInSharedMethodTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Class Foo
Shared Sub Foo()
|
End Sub()
End Class</File>, "MyClass")
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassInStructureTest() As Task
            Await VerifyRecommendationsMissingAsync(<File>
Module Foo
Sub Foo()
|
End Sub()
End Module</File>, "MyClass")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassAfterHandlesInClassWithEventsTest() As Task
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles |</ClassDeclaration>

            Await VerifyRecommendationsContainAsync(text, "MyClass")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassNotAfterHandlesInClassWithNoEventsTest() As Task
            Dim text = <ClassDeclaration>
        Sub Handler() Handles |</ClassDeclaration>

            Await VerifyRecommendationsMissingAsync(text, "MyClass")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassForDerivedEventTest() As Task
            Dim text = <File>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
    Sub Foo() Handles |
    End Sub
End Class|</File>

            Await VerifyRecommendationsContainAsync(text, "MyClass")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassInNameOf1Test() As Task
            Await VerifyRecommendationsContainAsync(<MethodBody>Dim s = NameOf(|</MethodBody>, "MyClass")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function MyClassInNameOf2Test() As Task
            Await VerifyRecommendationsMissingAsync(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "MyClass")
        End Function
    End Class
End Namespace
