' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class EventDeclarationOutlinerTests
        Inherits AbstractOutlinerTests(Of EventStatementSyntax)

        Friend Overrides Function GetRegions(eventDeclaration As EventStatementSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New EventDeclarationOutliner
            Return outliner.GetOutliningSpans(eventDeclaration, CancellationToken.None).WhereNotNull()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEvent()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Event AnEvent(ByVal EventNumber As Integer)",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventDeclaration = typeBlock.DigToFirstNodeOfType(Of EventStatementSyntax)()
            Assert.NotNull(eventDeclaration)

            Dim actualRegions = GetRegions(eventDeclaration).ToList()
            Assert.Equal(0, actualRegions.Count)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEventWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  'My",
                                  "  'Event",
                                  "  Event AnEvent(ByVal EventNumber As Integer)",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventDeclaration = typeBlock.DigToFirstNodeOfType(Of EventStatementSyntax)()
            Assert.NotNull(eventDeclaration)

            Dim actualRegions = GetRegions(eventDeclaration).ToList()
            Assert.Equal(1, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                      TextSpan:=TextSpan.FromBounds(12, 25),
                                      bannerText:="' My ...",
                                      hintSpan:=TextSpan.FromBounds(12, 25),
                                      autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEvent()
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
            Dim eventDeclaration = eventBlock.EventStatement
            Assert.NotNull(eventDeclaration)

            Dim actualRegions = GetRegions(eventDeclaration).ToList()
            Assert.Equal(1, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                      TextSpan:=TextSpan.FromBounds(12, 281),
                                      bannerText:="Custom Event eventName As EventHandler ...",
                                      hintSpan:=TextSpan.FromBounds(12, 281),
                                      autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPrivateCustomEvent()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  Private Custom Event eventName As EventHandler",
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
            Dim eventDeclaration = eventBlock.EventStatement
            Assert.NotNull(eventDeclaration)

            Dim actualRegions = GetRegions(eventDeclaration).ToList()
            Assert.Equal(1, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                      textSpan:=TextSpan.FromBounds(12, 289),
                                      bannerText:="Private Custom Event eventName As EventHandler ...",
                                      hintSpan:=TextSpan.FromBounds(12, 289),
                                      autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventWithComments()
            Dim syntaxTree = ParseLines("Class C1",
                                  "  'My",
                                  "  'Event",
                                  "  Custom Event eventName As EventHandler",
                                  "    AddHandler(ByVal value As EventHandler)",
                                  "    End AddHandler",
                                  "    RemoveHandler(ByVal value As EventHandler)",
                                  "    End RemoveHandler",
                                  "    RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)",
                                  "    End RaiseEvent",
                                  "    'End",
                                  "    'Event",
                                  "  End Event",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim eventBlock = typeBlock.DigToFirstNodeOfType(Of EventBlockSyntax)()
            Dim eventDeclaration = eventBlock.EventStatement
            Assert.NotNull(eventDeclaration)

            Dim actualRegions = GetRegions(eventDeclaration).ToList()
            Assert.Equal(3, actualRegions.Count)

            Dim expectedRegion1 = New OutliningSpan(
                                      textSpan:=TextSpan.FromBounds(12, 25),
                                      bannerText:="' My ...",
                                      hintSpan:=TextSpan.FromBounds(12, 25),
                                      autoCollapse:=True)
            AssertRegion(expectedRegion1, actualRegions(0))

            Dim expectedRegion2 = New OutliningSpan(
                                      textSpan:=TextSpan.FromBounds(29, 320),
                                      bannerText:="Custom Event eventName As EventHandler ...",
                                      hintSpan:=TextSpan.FromBounds(29, 320),
                                      autoCollapse:=True)
            AssertRegion(expectedRegion2, actualRegions(1))

            Dim expectedRegion3 = New OutliningSpan(
                                      textSpan:=TextSpan.FromBounds(291, 307),
                                      bannerText:="' End ...",
                                      hintSpan:=TextSpan.FromBounds(291, 307),
                                      autoCollapse:=True)
            AssertRegion(expectedRegion3, actualRegions(2))
        End Sub

    End Class
End Namespace
