' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class MeKeywordRecommenderTests
        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInModuleDeclaration()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInModuleMethodBody()
            VerifyRecommendationsMissing(<ModuleMethodBody>|</ModuleMethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInSharedMethodBody()
            VerifyRecommendationsMissing(<SharedMethodBody>|</SharedMethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInSharedPropertyGetter()
            VerifyRecommendationsMissing(<SharedPropertyGetter>|</SharedPropertyGetter>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInSharedEventAddHandler()
            VerifyRecommendationsMissing(<SharedEventAddHandler>|</SharedEventAddHandler>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeInStatement()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeInStatementInPropertyGetter()
            VerifyRecommendationsContain(<PropertyGetter>|</PropertyGetter>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeInStatementInEventAddHandler()
            VerifyRecommendationsContain(<EventAddHandler>|</EventAddHandler>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeInFieldInitializer()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = |</ClassDeclaration>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterHandlesInClassWithEvents()
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsContain(text, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeAfterHandlesInClassWithEventsAfterComma()
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles Ev_event,  |</ClassDeclaration>

            VerifyRecommendationsContain(text, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeNotAfterHandlesInClassWithNoEvents()
            Dim text = <ClassDeclaration>
        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsMissing(text, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeForDerivedEvent()
            Dim text = <File>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
    Sub Foo() Handles |
    End Sub
End Class|</File>

            VerifyRecommendationsContain(text, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeInNameOf1()
            VerifyRecommendationsContain(<MethodBody>Dim s = NameOf(|</MethodBody>, "Me")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MeInNameOf2()
            VerifyRecommendationsMissing(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "Me")
        End Sub
    End Class
End Namespace
