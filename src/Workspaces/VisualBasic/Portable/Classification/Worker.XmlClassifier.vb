' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Partial Friend Class Worker
        Private Class XmlClassifier
            Private ReadOnly _worker As Worker

            Public Sub New(worker As Worker)
                _worker = worker
            End Sub

            Private Sub AddTokenClassification(token As SyntaxToken, classificationType As String)
                _worker.AddClassification(token, classificationType)
                _worker.ClassifyTrivia(token)
            End Sub

            Private Sub ClassifyToken(token As SyntaxToken)
                _worker.ClassifyToken(token)
            End Sub

            Friend Sub ClassifyNode(node As SyntaxNode)
                ' Note: Use FullSpan in case we need to classify trivia around the span of the node.
                If Not _worker._textSpan.OverlapsWith(node.FullSpan) Then
                    Return
                End If

                If TypeOf node Is XmlNodeSyntax Then
                    ClassifyXmlNode(DirectCast(node, XmlNodeSyntax))
                Else
                    Select Case node.Kind
                        Case SyntaxKind.XmlDeclaration
                            ClassifyDeclaration(DirectCast(node, XmlDeclarationSyntax))
                        Case SyntaxKind.XmlDeclarationOption
                            ClassifyDeclarationOption(DirectCast(node, XmlDeclarationOptionSyntax))
                        Case SyntaxKind.XmlNamespaceImportsClause
                            ClassifyXmlNamespaceImportsClause(DirectCast(node, XmlNamespaceImportsClauseSyntax))
                        Case SyntaxKind.XmlAttributeAccessExpression,
                             SyntaxKind.XmlDescendantAccessExpression,
                             SyntaxKind.XmlElementAccessExpression
                            ClassifyXmlMemberAccessExpression(DirectCast(node, XmlMemberAccessExpressionSyntax))
                        Case SyntaxKind.GetXmlNamespaceExpression
                            ClassifyGetXmlNamespaceExpression(DirectCast(node, GetXmlNamespaceExpressionSyntax))
                    End Select
                End If
            End Sub

            Private Sub ClassifyXmlNode(node As XmlNodeSyntax)
                Select Case node.Kind
                    Case SyntaxKind.XmlDocument
                        ClassifyXmlDocument(DirectCast(node, XmlDocumentSyntax))
                    Case SyntaxKind.XmlElement
                        ClassifyXmlElement(DirectCast(node, XmlElementSyntax))
                    Case SyntaxKind.XmlElementStartTag
                        ClassifyXmlStartElement(DirectCast(node, XmlElementStartTagSyntax))
                    Case SyntaxKind.XmlElementEndTag
                        ClassifyXmlEndElement(DirectCast(node, XmlElementEndTagSyntax))
                    Case SyntaxKind.XmlEmptyElement
                        ClassifyXmlEmptyElement(DirectCast(node, XmlEmptyElementSyntax))
                    Case SyntaxKind.XmlAttribute
                        ClassifyXmlAttribute(DirectCast(node, XmlAttributeSyntax))
                    Case SyntaxKind.XmlString
                        ClassifyXmlString(DirectCast(node, XmlStringSyntax))
                    Case SyntaxKind.XmlProcessingInstruction
                        ClassifyXmlProcessingInstruction(DirectCast(node, XmlProcessingInstructionSyntax))
                    Case SyntaxKind.XmlName
                        ClassifyXmlName(DirectCast(node, XmlNameSyntax))
                    Case SyntaxKind.XmlComment
                        ClassifyXmlComment(DirectCast(node, XmlCommentSyntax))
                    Case SyntaxKind.XmlCDataSection
                        ClassifyXmlCData(DirectCast(node, XmlCDataSectionSyntax))
                    Case SyntaxKind.XmlText
                        ClassifyXmlText(DirectCast(node, XmlTextSyntax))
                    Case SyntaxKind.XmlEmbeddedExpression
                        ClassifyXmlEmbeddedExpression(DirectCast(node, XmlEmbeddedExpressionSyntax))
                End Select
            End Sub

            Private Sub ClassifyXmlDocument(node As XmlDocumentSyntax)
                For Each xmlNode In node.PrecedingMisc
                    ClassifyXmlNode(xmlNode)
                Next

                ClassifyDeclaration(node.Declaration)

                For Each xmlNode In node.FollowingMisc
                    ClassifyXmlNode(xmlNode)
                Next

                ClassifyXmlNode(node.Root)
            End Sub

            Private Sub ClassifyXmlStartElement(node As XmlElementStartTagSyntax)
                AddTokenClassification(node.GreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
                ClassifyXmlNode(node.Name)

                For Each attribute In node.Attributes
                    ClassifyXmlNode(attribute)
                Next

                AddTokenClassification(node.LessThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlEndElement(node As XmlElementEndTagSyntax)
                AddTokenClassification(node.GreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)

                If node.Name IsNot Nothing Then
                    ClassifyXmlName(node.Name)
                End If

                AddTokenClassification(node.LessThanSlashToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlElement(node As XmlElementSyntax)
                If node.StartTag IsNot Nothing Then
                    ClassifyXmlNode(node.StartTag)
                End If

                For Each content In node.Content
                    ClassifyXmlNode(content)
                Next

                If node.EndTag IsNot Nothing Then
                    ClassifyXmlNode(node.EndTag)
                End If
            End Sub

            Private Sub ClassifyXmlEmptyElement(node As XmlEmptyElementSyntax)
                AddTokenClassification(node.LessThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
                ClassifyXmlNode(node.Name)

                For Each attribute In node.Attributes
                    ClassifyXmlNode(attribute)
                Next

                AddTokenClassification(node.SlashGreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlAttribute(node As XmlAttributeSyntax)
                ClassifyXmlNode(node.Name)
                AddTokenClassification(node.EqualsToken, ClassificationTypeNames.XmlLiteralDelimiter)
                ClassifyXmlNode(node.Value)
            End Sub

            Private Sub ClassifyXmlString(node As XmlStringSyntax)
                AddTokenClassification(node.StartQuoteToken, ClassificationTypeNames.XmlLiteralAttributeQuotes)

                For Each textToken In node.TextTokens
                    ClassifyToken(textToken)
                Next

                AddTokenClassification(node.EndQuoteToken, ClassificationTypeNames.XmlLiteralAttributeQuotes)
            End Sub
            Private Sub ClassifyXmlProcessingInstruction(node As XmlProcessingInstructionSyntax)
                AddTokenClassification(node.LessThanQuestionToken, ClassificationTypeNames.XmlLiteralDelimiter)
                AddTokenClassification(node.Name, ClassificationTypeNames.XmlLiteralName)

                For Each textToken In node.TextTokens
                    ClassifyToken(textToken)
                Next

                AddTokenClassification(node.QuestionGreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlEmbeddedExpression(node As XmlEmbeddedExpressionSyntax)
                AddTokenClassification(node.LessThanPercentEqualsToken, ClassificationTypeNames.XmlLiteralEmbeddedExpression)
                _worker.ClassifyNode(node.Expression)
                AddTokenClassification(node.PercentGreaterThanToken, ClassificationTypeNames.XmlLiteralEmbeddedExpression)
            End Sub

            Private Sub ClassifyXmlComment(node As XmlCommentSyntax)
                AddTokenClassification(node.LessThanExclamationMinusMinusToken, ClassificationTypeNames.XmlLiteralDelimiter)

                For Each textToken In node.TextTokens
                    ClassifyToken(textToken)
                Next

                AddTokenClassification(node.MinusMinusGreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlCData(node As XmlCDataSectionSyntax)
                AddTokenClassification(node.BeginCDataToken, ClassificationTypeNames.XmlLiteralDelimiter)

                For Each textToken In node.TextTokens
                    ClassifyToken(textToken)
                Next

                AddTokenClassification(node.EndCDataToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlText(node As XmlTextSyntax)
                For Each textToken In node.TextTokens
                    ClassifyToken(textToken)
                Next
            End Sub

            Private Sub ClassifyDeclaration(node As XmlDeclarationSyntax)
                AddTokenClassification(node.LessThanQuestionToken, ClassificationTypeNames.XmlLiteralDelimiter)
                AddTokenClassification(node.XmlKeyword, ClassificationTypeNames.XmlLiteralName)

                If node.Encoding IsNot Nothing Then
                    ClassifyDeclarationOption(node.Encoding)
                End If

                If node.Standalone IsNot Nothing Then
                    ClassifyDeclarationOption(node.Standalone)
                End If

                ClassifyDeclarationOption(node.Version)

                AddTokenClassification(node.QuestionGreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyDeclarationOption(node As XmlDeclarationOptionSyntax)
                AddTokenClassification(node.Name, ClassificationTypeNames.XmlLiteralAttributeName)
                AddTokenClassification(node.Equals, ClassificationTypeNames.XmlLiteralDelimiter)
                ClassifyXmlNode(node.Value)
            End Sub

            Private Sub ClassifyXmlNamespaceImportsClause(node As XmlNamespaceImportsClauseSyntax)
                AddTokenClassification(node.LessThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
                ClassifyXmlNode(node.XmlNamespace)
                AddTokenClassification(node.GreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlName(element As XmlNameSyntax)
                ' First we need to determine the context of the name so we know the type
                Dim type As String = Nothing
                If TypeOf element.Parent Is XmlAttributeSyntax AndAlso
                    DirectCast(element.Parent, XmlAttributeSyntax).Name Is element Then
                    type = ClassificationTypeNames.XmlLiteralAttributeName
                ElseIf TypeOf element.Parent Is XmlMemberAccessExpressionSyntax AndAlso
                    element.Parent.Kind = SyntaxKind.XmlAttributeAccessExpression AndAlso
                    DirectCast(element.Parent, XmlMemberAccessExpressionSyntax).Name Is element Then
                    type = ClassificationTypeNames.XmlLiteralAttributeName
                ElseIf IsElementName(element) Then
                    type = ClassificationTypeNames.XmlLiteralName
                End If

                If type IsNot Nothing Then
                    If element.Prefix IsNot Nothing Then
                        Dim prefix = element.Prefix
                        AddTokenClassification(prefix.ColonToken, type)
                        AddTokenClassification(prefix.Name, type)
                    End If

                    If Not element.LocalName.IsMissing Then
                        AddTokenClassification(element.LocalName, type)
                    End If
                End If
            End Sub

            Friend Shared Function IsElementName(name As XmlNameSyntax) As Boolean
                Dim parent = name.Parent
                Dim startParent = TryCast(parent, XmlElementStartTagSyntax)
                If startParent IsNot Nothing AndAlso startParent.Name Is name Then
                    Return True
                End If

                Dim endParent = TryCast(parent, XmlElementEndTagSyntax)
                If endParent IsNot Nothing AndAlso endParent.Name Is name Then
                    Return True
                End If

                Dim emptyParent = TryCast(parent, XmlEmptyElementSyntax)
                If emptyParent IsNot Nothing AndAlso emptyParent.Name Is name Then
                    Return True
                End If

                Dim bracketedNameParent = TryCast(parent, XmlBracketedNameSyntax)
                If bracketedNameParent IsNot Nothing AndAlso bracketedNameParent.Name Is name Then
                    Return True
                End If

                Return False
            End Function

            Private Sub ClassifyXmlBracketedName(bracketedName As XmlBracketedNameSyntax)
                AddTokenClassification(bracketedName.LessThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
                ClassifyXmlName(bracketedName.Name)
                AddTokenClassification(bracketedName.GreaterThanToken, ClassificationTypeNames.XmlLiteralDelimiter)
            End Sub

            Private Sub ClassifyXmlMemberAccessExpression(syntax As XmlMemberAccessExpressionSyntax)
                If syntax.Base IsNot Nothing Then
                    _worker.ClassifyNode(syntax.Base)
                End If

                ' XML member access operators are split into several tokens:
                '     1. separator -- the initial '.'
                '     2. optional '.' or '@' -- required for descendant access '...' or attribute access '.@'
                '     3. final optional '.' -- required for descendant access '...'
                ' After the operator, is a name -- which might need to be classified as an element or an attribute name

                ' Classify initial '.'
                AddTokenClassification(syntax.Token1, ClassificationTypeNames.XmlLiteralDelimiter)

                ' Classify access modifier (e.g. '..' or '@')
                If Not syntax.Token2.IsMissing Then
                    AddTokenClassification(syntax.Token2, ClassificationTypeNames.XmlLiteralDelimiter)
                End If

                If Not syntax.Token3.IsMissing Then
                    AddTokenClassification(syntax.Token3, ClassificationTypeNames.XmlLiteralDelimiter)
                End If

                ' Classify name -- which should be the last child of the expression (e.g.
                ' x.<goo>, x...<goo> or x.@goo). Note that the name can be an XmlName in the
                ' case of an attribute, or an XmlBracketName, in which case, the brackets need
                ' to be classified as well
                Dim childNodesAndTokens = syntax.ChildNodesAndTokens()
                Dim lastChild = If(childNodesAndTokens.IsEmpty, Nothing, childNodesAndTokens(childNodesAndTokens.Count - 1))
                If lastChild.Kind() <> SyntaxKind.None Then
                    Select Case lastChild.Kind()
                        Case SyntaxKind.XmlName
                            ClassifyXmlName(DirectCast(lastChild.AsNode(), XmlNameSyntax))
                        Case SyntaxKind.XmlBracketedName
                            ClassifyXmlBracketedName(DirectCast(lastChild.AsNode(), XmlBracketedNameSyntax))
                    End Select
                End If
            End Sub

            Private Sub ClassifyGetXmlNamespaceExpression(node As GetXmlNamespaceExpressionSyntax)
                AddTokenClassification(node.GetXmlNamespaceKeyword, ClassificationTypeNames.Keyword)
                AddTokenClassification(node.OpenParenToken, ClassificationTypeNames.Punctuation)

                If node.Name IsNot Nothing Then
                    AddTokenClassification(node.Name.Name, ClassificationTypeNames.XmlLiteralName)
                End If

                AddTokenClassification(node.CloseParenToken, ClassificationTypeNames.Punctuation)
            End Sub
        End Class
    End Class
End Namespace
