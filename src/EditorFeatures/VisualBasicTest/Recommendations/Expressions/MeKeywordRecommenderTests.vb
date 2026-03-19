' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class MeKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "Me")
        End Sub

        <Fact>
        Public Sub NoneInModuleDeclarationTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "Me")
        End Sub

        <Fact>
        Public Sub NoneInModuleMethodBodyTest()
            VerifyRecommendationsMissing(<ModuleMethodBody>|</ModuleMethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub NoneInSharedMethodBodyTest()
            VerifyRecommendationsMissing(<SharedMethodBody>|</SharedMethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub NoneInSharedPropertyGetterTest()
            VerifyRecommendationsMissing(<SharedPropertyGetter>|</SharedPropertyGetter>, "Me")
        End Sub

        <Fact>
        Public Sub NoneInSharedEventAddHandlerTest()
            VerifyRecommendationsMissing(<SharedEventAddHandler>|</SharedEventAddHandler>, "Me")
        End Sub

        <Fact>
        Public Sub MeInStatementTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeInStatementInPropertyGetterTest()
            VerifyRecommendationsContain(<PropertyGetter>|</PropertyGetter>, "Me")
        End Sub

        <Fact>
        Public Sub MeInStatementInEventAddHandlerTest()
            VerifyRecommendationsContain(<EventAddHandler>|</EventAddHandler>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeInFieldInitializerTest()
            VerifyRecommendationsContain(<ClassDeclaration>Dim x = |</ClassDeclaration>, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterHandlesInClassWithEventsTest()
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsContain(text, "Me")
        End Sub

        <Fact>
        Public Sub MeAfterHandlesInClassWithEventsAfterCommaTest()
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles Ev_event,  |</ClassDeclaration>

            VerifyRecommendationsContain(text, "Me")
        End Sub

        <Fact>
        Public Sub MeNotAfterHandlesInClassWithNoEventsTest()
            Dim text = <ClassDeclaration>
        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsMissing(text, "Me")
        End Sub

        <Fact>
        Public Sub MeForDerivedEventTest()
            Dim text = <File>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
    Sub Goo() Handles |
    End Sub
End Class|</File>

            VerifyRecommendationsContain(text, "Me")
        End Sub

        <Fact>
        Public Sub MeInNameOf1Test()
            VerifyRecommendationsContain(<MethodBody>Dim s = NameOf(|</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub MeInNameOf2Test()
            VerifyRecommendationsMissing(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "Me")
        End Sub

        <Fact>
        Public Sub Preselection()
            Dim code =
<File>
Class Program
    Sub Main(args As String())
        Goo(|)
    End Sub

    Sub Goo(x As Program)

    End Sub
End Class
</File>

            VerifyRecommendationsWithPriority(code, SymbolMatchPriority.Keyword, "Me")
        End Sub
    End Class
End Namespace
