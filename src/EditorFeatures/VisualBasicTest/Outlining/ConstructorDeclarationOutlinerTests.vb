' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class ConstructorDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of SubNewStatementSyntax)

        Friend Overrides Function GetRegions(constructorDeclaration As SubNewStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New ConstructorDeclarationOutliner
            Return outliner.GetOutliningSpans(constructorDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestConstructor1()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub New()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim constructorDeclaration = TryCast(methodBlock.BlockStatement, SubNewStatementSyntax)
            Assert.NotNull(constructorDeclaration)

            Dim actualRegion = GetRegion(constructorDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(12, 32),
                                     "Sub New() ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestConstructor2()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub New()",
                                  "  End Sub                     ",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim constructorDeclaration = TryCast(methodBlock.BlockStatement, SubNewStatementSyntax)
            Assert.NotNull(constructorDeclaration)

            Dim actualRegion = GetRegion(constructorDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(12, 32),
                                     "Sub New() ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestConstructor3()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub New()",
                                  "  End Sub ' .ctor",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim constructorDeclaration = TryCast(methodBlock.BlockStatement, SubNewStatementSyntax)
            Assert.NotNull(constructorDeclaration)

            Dim actualRegion = GetRegion(constructorDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(12, 32),
                                     "Sub New() ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPrivateConstructor()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Private Sub New()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim constructorDeclaration = TryCast(methodBlock.BlockStatement, SubNewStatementSyntax)
            Assert.NotNull(constructorDeclaration)

            Dim actualRegion = GetRegion(constructorDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(12, 40),
                                     "Private Sub New() ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestConstructorWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  'My",
                                  "  'Constructor",
                                  "  Sub New()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim constructorDeclaration = TryCast(methodBlock.BlockStatement, SubNewStatementSyntax)
            Assert.NotNull(constructorDeclaration)

            Dim actualRegions = GetRegions(constructorDeclaration).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(12, 31),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(35, 55),
                                     "Sub New() ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

    End Class
End Namespace
