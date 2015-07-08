' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Expressions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Recommendations.Expressions
    Public Class MyBaseKeywordRecommenderTests
        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInClassDeclaration()
            VerifyRecommendationsMissing(<ClassDeclaration>|</ClassDeclaration>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInModuleDeclaration()
            VerifyRecommendationsMissing(<ModuleDeclaration>|</ModuleDeclaration>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInModuleMethodBody()
            VerifyRecommendationsMissing(<ModuleMethodBody>|</ModuleMethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInStructureDeclaration()
            VerifyRecommendationsMissing(<StructureDeclaration>|</StructureDeclaration>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoneInStructureMethodBody()
            VerifyRecommendationsMissing(<StructureMethodBody>|</StructureMethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseInStatement()
            VerifyRecommendationsContain(<MethodBody>|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterReturn()
            VerifyRecommendationsContain(<MethodBody>Return |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterArgument1()
            VerifyRecommendationsContain(<MethodBody>Foo(|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterArgument2()
            VerifyRecommendationsContain(<MethodBody>Foo(bar, |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterBinaryExpression()
            VerifyRecommendationsContain(<MethodBody>Foo(bar + |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterNot()
            VerifyRecommendationsContain(<MethodBody>Foo(Not |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterTypeOf()
            VerifyRecommendationsContain(<MethodBody>If TypeOf |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterDoWhile()
            VerifyRecommendationsContain(<MethodBody>Do While |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterDoUntil()
            VerifyRecommendationsContain(<MethodBody>Do Until |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterLoopWhile()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop While |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterLoopUntil()
            VerifyRecommendationsContain(<MethodBody>
Do
Loop Until |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterIf()
            VerifyRecommendationsContain(<MethodBody>If |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterElseIf()
            VerifyRecommendationsContain(<MethodBody>ElseIf |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterElseSpaceIf()
            VerifyRecommendationsContain(<MethodBody>Else If |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterError()
            VerifyRecommendationsContain(<MethodBody>Error |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterThrow()
            VerifyRecommendationsContain(<MethodBody>Throw |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterInitializer()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterArrayInitializerSquiggle()
            VerifyRecommendationsContain(<MethodBody>Dim x = {|</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseAfterArrayInitializerComma()
            VerifyRecommendationsContain(<MethodBody>Dim x = {0, |</MethodBody>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseNotInModule()
            VerifyRecommendationsMissing(<File>
Module Foo
Sub Foo()
|
End Sub()
End Module</File>, "MyBase")
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseInStructure()
            VerifyRecommendationsMissing(<File>
Module Foo
Sub Foo()
|
End Sub()
End Module</File>, "MyBase")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseToHandleInheritedMember()
            Dim text = <File>Public Class BaseClass
    Protected Event Event1()
End Class
                           
Public Class Class1
    Inherits BaseClass

        Sub Handler() Handles |
    End Class</File>

            VerifyRecommendationsContain(text, "MyBase")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoMyBaseToHandleInheritedMemberIfThereIsNotOne()
            Dim text = <File>Public Class BaseClass
End Class
                           
Public Class Class1
    Inherits BaseClass

        Sub Handler() Handles |
    End Class |</File>

            VerifyRecommendationsMissing(text, "MyBase")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NoMyBaseToHandleInaccessibleInheritedMember()
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

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseInNameOf1()
            VerifyRecommendationsContain(<MethodBody>Dim s = NameOf(|</MethodBody>, "MyBase")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub MyBaseInNameOf2()
            VerifyRecommendationsMissing(<MethodBody>Dim s = NameOf(System.|</MethodBody>, "MyBase")
        End Sub
    End Class
End Namespace
