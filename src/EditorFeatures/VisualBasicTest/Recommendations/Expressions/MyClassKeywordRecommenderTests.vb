' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class MyClassKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassInStatementTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassNotInModuleTest()
            VerifyRecommendationsMissing(<File>
Module Goo
Sub Goo()
|
End Sub()
End Module</File>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassNotInSharedMethodTest()
            VerifyRecommendationsMissing(<File>
Class Goo
Shared Sub Goo()
|
End Sub()
End Class</File>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassInStructureTest()
            VerifyRecommendationsMissing(<File>
Module Goo
Sub Goo()
|
End Sub()
End Module</File>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassAfterHandlesInClassWithEventsTest()
            Dim text = <ClassDeclaration>
        Public Event Ev_Event()

        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsContain(text, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassNotAfterHandlesInClassWithNoEventsTest()
            Dim text = <ClassDeclaration>
        Sub Handler() Handles |</ClassDeclaration>

            VerifyRecommendationsMissing(text, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassForDerivedEventTest()
            Dim text = <File>Public Class Base
    Public Event Click()
End Class
Public Class Derived
    Inherits Base
    Sub Goo() Handles |
    End Sub
End Class|</File>

            VerifyRecommendationsContain(text, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassInNameOf1Test()
            VerifyRecommendationsContain(<MethodBody>Dim s = NameOf(|</MethodBody>, "MyClass")
        End Sub

        <Fact>
        Public Sub MyClassInNameOf2Test()
            VerifyRecommendationsMissing(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "MyClass")
        End Sub
    End Class
End Namespace
