' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class AccessorDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of AccessorStatementSyntax)

        Friend Overrides Function GetRegions(accessorDeclaration As AccessorStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New AccessorDeclarationOutliner
            Return outliner.GetOutliningSpans(accessorDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestReadOnlyPropertyGet()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  ReadOnly Property P1 As Integer",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(49, 81),
                                     "Get ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestReadOnlyPropertyGetWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  ReadOnly Property P1 As Integer",
                                  "    'My",
                                  "    'Getter",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(49, 65),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(71, 103),
                                     "Get ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertyGet()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Property P1 As Integer",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "    Set(value As Integer)",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(40, 72),
                                     "Get ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertyGetWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Property P1 As Integer",
                                  "    'My",
                                  "    'Getter",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "    Set(value As Integer)",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(40, 56),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(62, 94),
                                     "Get ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestWriteOnlyPropertySet()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  WriteOnly Property P1 As Integer",
                                  "    Set(ByVal value As Integer)",
                                  "      Return 0",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(50, 106),
                                     "Set(value As Integer) ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestWriteOnlyPropertySetWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  WriteOnly Property P1 As Integer",
                                  "    'My",
                                  "    'Setter",
                                  "    Set(ByVal value As Integer)",
                                  "      Return 0",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(50, 66),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(72, 128),
                                     "Set(value As Integer) ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertySet()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Property P1 As Integer",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "    Set(value As Integer)",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToLastNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(78, 112),
                                     "Set(value As Integer) ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertySetWithPrivateModifier()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Property P1 As Integer",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "    Private Set(value As Integer)",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToLastNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(78, 120),
                                     "Private Set(value As Integer) ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertySetWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Property P1 As Integer",
                                  "    Get",
                                  "      Return 0",
                                  "    End Get",
                                  "    'My",
                                  "    'Setter",
                                  "    Set(value As Integer)",
                                  "    End Set",
                                  "  End Property",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim propertyBlock = typeBlock.DigToFirstNodeOfType(Of PropertyBlockSyntax)()
            Dim methodBlock = propertyBlock.DigToLastNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(78, 94),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(100, 134),
                                     "Set(value As Integer) ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventAddHandler()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Custom Event eventName As EventHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim methodBlock = eventBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(56, 115),
                                     "AddHandler(value As EventHandler) ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventAddHandlerWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Custom Event eventName As EventHandler",
                                  "    'My",
                                  "    'AddHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim methodBlock = eventBlock.DigToFirstNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(56, 76),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(82, 141),
                                     "AddHandler(value As EventHandler) ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRemoveHandler()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Custom Event eventName As EventHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim methodBlock = eventBlock.DigToNthNodeOfType(Of AccessorBlockSyntax)(1)
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(121, 186),
                                     "RemoveHandler(value As EventHandler) ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRemoveHandlerWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Custom Event eventName As EventHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    'My",
                                  "    'RemoveHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim methodBlock = eventBlock.DigToNthNodeOfType(Of AccessorBlockSyntax)(1)
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(121, 144),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(150, 215),
                                     "RemoveHandler(value As EventHandler) ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRaiseHandler()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Custom Event eventName As EventHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim methodBlock = eventBlock.DigToLastNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegion = GetRegion(accessor)
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(192, 268),
                                     "RaiseEvent(sender As Object, e As EventArgs) ...",
                                     autoCollapse:=True)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRaiseHandlerWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Custom Event eventName As EventHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    'My",
                                  "    'RaiseEvent",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim methodBlock = eventBlock.DigToLastNodeOfType(Of AccessorBlockSyntax)()
            Dim accessor = methodBlock.DigToFirstNodeOfType(Of AccessorStatementSyntax)()

            Dim actualRegions = GetRegions(accessor).ToList()

            Dim expectedRegion1 = New OutliningSpan(
                                     TextSpan.FromBounds(192, 212),
                                     "' My ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                     TextSpan.FromBounds(218, 294),
                                     "RaiseEvent(sender As Object, e As EventArgs) ...",
                                     autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))
        End Sub
    End Class
End Namespace
