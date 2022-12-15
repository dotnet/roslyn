' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class MyBaseKeywordRecommenderTests
        Inherits RecommenderTests

        <Fact>
        Public Sub NoneInClassDeclarationTest()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "MyBase")
        End Sub

        <Fact>
        Public Sub NoneInModuleDeclarationTest()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "MyBase")
        End Sub

        <Fact>
        Public Sub NoneInModuleMethodBodyTest()
            VerifyRecommendationsMissing(<ModuleMethodBody>|</ModuleMethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub NoneInStructureDeclarationTest()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "MyBase")
        End Sub

        <Fact>
        Public Sub NoneInStructureMethodBodyTest()
            VerifyRecommendationsMissing(<StructureMethodBody>|</StructureMethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseInStatementTest()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterReturnTest()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterArgument1Test()
            VerifyRecommendationsContain(<MethodBody>Goo(|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterArgument2Test()
            VerifyRecommendationsContain(<MethodBody>Goo(bar, |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterBinaryExpressionTest()
            VerifyRecommendationsContain(<MethodBody>Goo(bar + |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterNotTest()
            VerifyRecommendationsContain(<MethodBody>Goo(Not |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterTypeOfTest()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterDoWhileTest()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterDoUntilTest()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterLoopWhileTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterLoopUntilTest()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterIfTest()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterElseIfTest()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterElseSpaceIfTest()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterErrorTest()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterThrowTest()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterInitializerTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterArrayInitializerSquiggleTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseAfterArrayInitializerCommaTest()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseNotInModuleTest()
            VerifyRecommendationsMissing(<File>
Module Goo
Sub Goo()
|
End Sub()
End Module</File>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseInStructureTest()
            VerifyRecommendationsMissing(<File>
Module Goo
Sub Goo()
|
End Sub()
End Module</File>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseToHandleInheritedMemberTest()
            Dim text = <File>Public Class BaseClass
    Protected Event Event1()
End Class
                           
Public Class Class1
    Inherits BaseClass

        Sub Handler() Handles |
    End Class</File>

            VerifyRecommendationsContain(text, "MyBase")
        End Sub

        <Fact>
        Public Sub NoMyBaseToHandleInheritedMemberIfThereIsNotOneTest()
            Dim text = <File>Public Class BaseClass
End Class
                           
Public Class Class1
    Inherits BaseClass

        Sub Handler() Handles |
    End Class |</File>

            VerifyRecommendationsMissing(text, "MyBase")
        End Sub

        <Fact>
        Public Sub NoMyBaseToHandleInaccessibleInheritedMemberTest()
            Dim text = <File>Public Class Base
    Private Event Click()
    Sub a() Handles MyClass.Click
    End Sub
End Class
Public Class Derived
    Inherits Base
    Sub b() Handles |
End Class
</File>

            VerifyRecommendationsMissing(text, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseInNameOf1Test()
            VerifyRecommendationsContain(<MethodBody>Dim s = NameOf(|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        Public Sub MyBaseInNameOf2Test()
            VerifyRecommendationsMissing(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "MyBase")
        End Sub
    End Class
End Namespace
