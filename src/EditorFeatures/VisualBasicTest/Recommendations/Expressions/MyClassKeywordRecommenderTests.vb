' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class MyClassKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassInStatement()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassNotInModule()
            VerifyRecommendationsMissing(<File>
Module Foo
Sub Foo()
|
End Sub()
End Module</File>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassNotInSharedMethod()
            VerifyRecommendationsMissing(<File>
Class Foo
Shared Sub Foo()
|
End Sub()
End Class</File>, "MyClass")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassInStructure()
            VerifyRecommendationsMissing(<File>
Module Foo
Sub Foo()
|
End Sub()
End Module</File>, "MyClass")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassAfterHandlesInClassWithEvents()
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsContain(text, "MyClass")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassNotAfterHandlesInClassWithNoEvents()
            Dim text = <ClassDeclaration>
        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsMissing(text, "MyClass")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassForDerivedEvent()
            Dim text = <File>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
    Sub Foo() Handles |
    End Sub
End Class|</File>

            VerifyRecommendationsContain(text, "MyClass")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassInNameOf1()
            VerifyRecommendationsContain(<MethodBody>Dim s = NameOf(|</MethodBody>, "MyClass")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyClassInNameOf2()
            VerifyRecommendationsMissing(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "MyClass")
        End Sub
    End Class
End Namespace
