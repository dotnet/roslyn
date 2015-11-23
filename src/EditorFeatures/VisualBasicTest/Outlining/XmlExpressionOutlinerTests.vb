' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class XmlDocumentOutlinerTests
        Inherits AbstractOutlinerTests(Of XmlNodeSyntax)

        Friend Overrides Function GetRegions(xmlExpression As XmlNodeSyntax) As IEnumerable(Of OutliningSpan)
            Dim outliner As New XmlExpressionOutliner
            Return outliner.GetOutliningSpans(xmlExpression, CancellationToken.None).WhereNotNull()
        End Function

        Private Function GetXmlExpressionRegion(xmlTextLines As String(), digger As Func(Of SyntaxTree, XmlNodeSyntax, TreeNodePair(Of XmlNodeSyntax))) As OutliningSpan
            Dim xmlExpression = ParseAsXmlExpression(Of XmlNodeSyntax)(xmlTextLines, digger)

            Return GetRegion(xmlExpression.Node)
        End Function

        Friend Function ParseAsXmlExpression(Of T As XmlNodeSyntax)(xmlTextLines As String(), digger As Func(Of SyntaxTree, XmlNodeSyntax, TreeNodePair(Of T))) As TreeNodePair(Of T)
            Dim codeBuilder As New StringBuilder
            codeBuilder.AppendLine("Class C")
            codeBuilder.AppendLine("    Sub Foo()")
            codeBuilder.AppendLine("        Dim xml = " & xmlTextLines(0))
            For i = 1 To xmlTextLines.Length - 1
                codeBuilder.AppendLine("                  " & xmlTextLines(i))
            Next
            codeBuilder.AppendLine("    End Sub")
            codeBuilder.AppendLine("End Class")

            Dim syntaxTree = ParseCode(codeBuilder.ToString())

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim methodBlock = typeBlock.DigToFirstNodeOfType(Of MethodBlockBaseSyntax)()
            Dim variableMemberDecl = methodBlock.DigToFirstNodeOfType(Of LocalDeclarationStatementSyntax)()
            Dim initializer = variableMemberDecl.Declarators(0).Initializer
            Return digger(syntaxTree, DirectCast(initializer.Value, XmlNodeSyntax))
        End Function

        Private Function GetXmlExpressionRegion(xmlTextLines As String()) As OutliningSpan
            Return GetXmlExpressionRegion(xmlTextLines, Function(syntaxTree, expr) TreeNodePair.Create(syntaxTree, expr))
        End Function

        Private Function GetXmlExpressionDigger(digger As Func(Of XmlNodeSyntax, XmlNodeSyntax)) As Func(Of SyntaxTree, XmlNodeSyntax, TreeNodePair(Of XmlNodeSyntax))
            Return Function(syntaxTree, expr) TreeNodePair.Create(syntaxTree, digger(expr))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlDocument1()
            Dim actualRegion = GetXmlExpressionRegion({"<?xml version=""1.0""?>",
                                                      "<foo>",
                                                      "</foo>"})
            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(42, 114),
                                     "<?xml version=""1.0""?> ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlDocument2()
            Dim actualRegion = GetXmlExpressionRegion({"<?xml version=""1.0""?><foo>",
                                                      "</foo>"})

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(42, 94),
                                     "<?xml version=""1.0""?><foo> ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlLiteral()
            Dim actualRegion = GetXmlExpressionRegion({"<foo>",
                                                      "</foo>"})

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(42, 73),
                                     "<foo> ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestNestedXmlLiteral()
            Dim digger = GetXmlExpressionDigger(Function(expr)
                                                    Dim xmlElement = DirectCast(expr, XmlElementSyntax)
                                                    Return DirectCast(xmlElement.Content(0), XmlElementSyntax)
                                                End Function)

            Dim actualRegion = GetXmlExpressionRegion({"<foo>",
                                                       "    <bar>",
                                                       "    </bar>",
                                                       "</foo>"},
                                                      digger)

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(71, 106),
                                     "<bar> ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlProcessingInstruction()
            Dim actualRegion = GetXmlExpressionRegion({"<?foo",
                                                       "bar=""baz""?>"})

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(42, 78),
                                     "<?foo ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlComment()
            Dim actualRegion = GetXmlExpressionRegion({"<!-- Foo",
                                                       "Bar -->"})

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(42, 77),
                                     "<!-- Foo ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlCData()
            Dim actualRegion = GetXmlExpressionRegion({"<![CDATA[",
                                                       "Foo]]>"})

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(42, 77),
                                     "<![CDATA[ ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestXmlEmbeddedExpression()
            Dim digger = GetXmlExpressionDigger(Function(expr As XmlNodeSyntax) As XmlNodeSyntax
                                                    Dim xmlElement = DirectCast(expr, XmlElementSyntax)
                                                    Return DirectCast(xmlElement.Content(0), XmlEmbeddedExpressionSyntax)
                                                End Function)


            Dim actualRegion = GetXmlExpressionRegion({"<foo>",
                                                       "    <%=",
                                                       "        From c in ""abc""",
                                                       "    %>",
                                                       "</foo>"},
                                                      digger)

            Dim expectedRegion = New OutliningSpan(
                                     TextSpan.FromBounds(71, 143),
                                     "<%= ...",
                                     autoCollapse:=False)

            AssertRegion(expectedRegion, actualRegion)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestDocumentationCommentIsNotOutlined()
            Dim syntaxTree = ParseLines("''' <summary>",
                                  "''' Foo",
                                  "''' </summary>",
                                  "Class C",
                                  "End Class")

            Dim typeBlock = syntaxTree.DigToFirstTypeBlock()
            Dim trivia = typeBlock.GetLeadingTrivia()
            Assert.Equal(1, trivia.Count)

            Dim docComment = DirectCast(trivia.ElementAt(0).GetStructure(), DocumentationCommentTriviaSyntax)
            Dim xmlElement = docComment.DigToFirstNodeOfType(Of XmlElementSyntax)()

            Dim actualRegions = GetRegions(xmlElement).ToList()
            Assert.Equal(0, actualRegions.Count)
        End Sub
    End Class
End Namespace
