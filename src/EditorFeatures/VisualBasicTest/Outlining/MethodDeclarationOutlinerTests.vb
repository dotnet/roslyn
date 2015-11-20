' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class MethodDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxOutlinerTests(Of MethodStatementSyntax)

        Friend Overrides Function GetRegions(methodDeclaration As MethodStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New MethodDeclarationOutliner
            Return outliner.GetOutliningSpans(methodDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSub()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo()",
                                  "  End Sub ' Foo",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 32),
                                     bannerText:="Sub Foo() ...",
                                     hintSpan:=TextSpan.FromBounds(12, 32),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithGenericTypeParameter()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo(Of T)()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 38),
                                     bannerText:="Sub Foo(Of T)() ...",
                                     hintSpan:=TextSpan.FromBounds(12, 38),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithGenericTypeParameterAndSingleConstraint()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo(Of T As Class)()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 47),
                                     bannerText:="Sub Foo(Of T As Class)() ...",
                                     hintSpan:=TextSpan.FromBounds(12, 47),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithGenericTypeParameterAndMultipleConstraint()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo(Of T As {Class, New})()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 54),
                                     bannerText:="Sub Foo(Of T As {Class, New})() ...",
                                     hintSpan:=TextSpan.FromBounds(12, 54),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPrivateSub()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Private Sub Foo()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 40),
                                     bannerText:="Private Sub Foo() ...",
                                     hintSpan:=TextSpan.FromBounds(12, 40),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithByRefParameter()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo(ByRef i As Integer)",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 50),
                                     bannerText:="Sub Foo(ByRef i As Integer) ...",
                                     hintSpan:=TextSpan.FromBounds(12, 50),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithByValParameter()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo(ByVal i As Integer)",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 50),
                                     bannerText:="Sub Foo(i As Integer) ...",
                                     hintSpan:=TextSpan.FromBounds(12, 50),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithOptionalParameter()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo(Optional i As Integer = -1)",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 58),
                                     bannerText:="Sub Foo(Optional i As Integer = -1) ...",
                                     hintSpan:=TextSpan.FromBounds(12, 58),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithHandlesClause()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo() Handles Bar.Baz",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 48),
                                     bannerText:="Sub Foo() Handles Bar.Baz ...",
                                     hintSpan:=TextSpan.FromBounds(12, 48),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithImplementsClause()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Sub Foo() Implements Bar.Baz",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegion = GetRegion(methodDeclaration)
            Dim expectedRegion = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 51),
                                     bannerText:="Sub Foo() Implements Bar.Baz ...",
                                     hintSpan:=TextSpan.FromBounds(12, 51),
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSubWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  'My",
                                  "  'Constructor",
                                  "  Sub Foo()",
                                  "  End Sub",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockSyntax)()
            Dim methodDeclaration = TryCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Assert.NotNull(methodDeclaration)

            Dim actualRegions = GetRegions(methodDeclaration).ToList()
            Assert.Equal(2, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(12, 31),
                                     bannerText:="' My ...",
                                     hintSpan:=TextSpan.FromBounds(12, 31),
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     textSpan:=TextSpan.FromBounds(35, 55),
                                     bannerText:="Sub Foo() ...",
                                     hintSpan:=TextSpan.FromBounds(35, 55),
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

    End Class
End Namespace
