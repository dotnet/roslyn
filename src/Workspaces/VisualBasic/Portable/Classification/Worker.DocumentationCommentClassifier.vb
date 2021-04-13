' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Partial Friend Class Worker
        Private Class DocumentationCommentClassifier
            Private ReadOnly _worker As Worker

            Public Sub New(worker As Worker)
                _worker = worker
            End Sub

            Friend Sub Classify(documentationComment As DocumentationCommentTriviaSyntax)
                If Not _worker._textSpan.OverlapsWith(documentationComment.Span) Then
                    Return
                End If

                For Each xmlNode In documentationComment.Content
                    Dim childFullSpan = xmlNode.FullSpan
                    If childFullSpan.Start > _worker._textSpan.End Then
                        Return
                    ElseIf childFullSpan.End < _worker._textSpan.Start Then
                        Continue For
                    End If

                    ClassifyXmlNode(xmlNode)
                Next
            End Sub

            Private Sub ClassifyXmlNode(node As XmlNodeSyntax)
                If node Is Nothing Then
                    Return
                End If

                Select Case node.Kind
                    Case SyntaxKind.XmlText
                        ClassifyXmlText(DirectCast(node, XmlTextSyntax))
                    Case SyntaxKind.XmlElement
                        ClassifyElement(DirectCast(node, XmlElementSyntax))
                    Case SyntaxKind.XmlEmptyElement
                        ClassifyEmptyElement(DirectCast(node, XmlEmptyElementSyntax))
                    Case SyntaxKind.XmlName
                        ClassifyXmlName(DirectCast(node, XmlNameSyntax))
                    Case SyntaxKind.XmlString
                        ClassifyString(DirectCast(node, XmlStringSyntax))
                    Case SyntaxKind.XmlComment
                        ClassifyComment(DirectCast(node, XmlCommentSyntax))
                    Case SyntaxKind.XmlCDataSection
                        ClassifyCData(DirectCast(node, XmlCDataSectionSyntax))
                    Case SyntaxKind.XmlProcessingInstruction
                        ClassifyProcessingInstruction(DirectCast(node, XmlProcessingInstructionSyntax))
                End Select
            End Sub

            Private Sub ClassifyXmlTrivia(trivialList As SyntaxTriviaList, Optional whitespaceClassificationType As String = Nothing)
                For Each t In trivialList
                    Select Case t.Kind()
                        Case SyntaxKind.DocumentationCommentExteriorTrivia
                            ClassifyExteriorTrivia(t)
                        Case SyntaxKind.WhitespaceTrivia
                            If whitespaceClassificationType IsNot Nothing Then
                                _worker.AddClassification(t, whitespaceClassificationType)
                            End If
                    End Select
                Next
            End Sub

            Private Sub ClassifyExteriorTrivia(trivia As SyntaxTrivia)
                ' Note: The exterior trivia can contain whitespace (usually leading) and we want to avoid classifying it.

                ' PERFORMANCE:
                ' While the call to SyntaxTrivia.ToString() looks Like an allocation, it isn't.
                ' The SyntaxTrivia green node holds the string text of the trivia in a field And ToString()
                ' just returns a reference to that.
                Dim text = trivia.ToString()

                Dim spanStart As Integer? = Nothing

                For index = 0 To text.Length - 1
                    Dim ch = text(index)

                    If spanStart IsNot Nothing AndAlso Char.IsWhiteSpace(ch) Then
                        Dim span = TextSpan.FromBounds(spanStart.Value, spanStart.Value + index)
                        _worker.AddClassification(span, ClassificationTypeNames.XmlDocCommentDelimiter)
                        spanStart = Nothing
                    ElseIf spanStart Is Nothing AndAlso Not Char.IsWhiteSpace(ch) Then
                        spanStart = trivia.Span.Start + index
                    End If
                Next

                ' Add a final classification if we hadn't encountered anymore whitespace at the end
                If spanStart IsNot Nothing Then
                    Dim span = TextSpan.FromBounds(spanStart.Value, trivia.Span.End)
                    _worker.AddClassification(span, ClassificationTypeNames.XmlDocCommentDelimiter)
                End If
            End Sub

            Private Sub AddXmlClassification(token As SyntaxToken, classificationType As String)
                If token.HasLeadingTrivia Then
                    ClassifyXmlTrivia(token.LeadingTrivia, classificationType)
                End If

                _worker.AddClassification(token, classificationType)

                If token.HasTrailingTrivia Then
                    ClassifyXmlTrivia(token.TrailingTrivia, classificationType)
                End If
            End Sub

            Private Sub ClassifyXmlTextTokens(textTokens As SyntaxTokenList)
                For Each token In textTokens
                    If token.HasLeadingTrivia Then
                        ClassifyXmlTrivia(token.LeadingTrivia, whitespaceClassificationType:=ClassificationTypeNames.XmlDocCommentText)
                    End If

                    ClassifyXmlTextToken(token)

                    If token.HasTrailingTrivia Then
                        ClassifyXmlTrivia(token.TrailingTrivia, whitespaceClassificationType:=ClassificationTypeNames.XmlDocCommentText)
                    End If
                Next token
            End Sub

            Private Sub ClassifyXmlTextToken(token As SyntaxToken)
                If token.Kind = SyntaxKind.XmlEntityLiteralToken Then
                    _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentEntityReference)
                ElseIf token.Kind() <> SyntaxKind.DocumentationCommentLineBreakToken Then
                    Select Case token.Parent.Kind
                        Case SyntaxKind.XmlText
                            _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentText)
                        Case SyntaxKind.XmlString
                            _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentAttributeValue)
                        Case SyntaxKind.XmlComment
                            _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentComment)
                        Case SyntaxKind.XmlCDataSection
                            _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentCDataSection)
                        Case SyntaxKind.XmlProcessingInstruction
                            _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentProcessingInstruction)
                    End Select
                End If
            End Sub

            Private Sub ClassifyXmlName(node As XmlNameSyntax)
                Dim classificationType As String
                If TypeOf node.Parent Is BaseXmlAttributeSyntax Then
                    classificationType = ClassificationTypeNames.XmlDocCommentAttributeName
                ElseIf TypeOf node.Parent Is XmlProcessingInstructionSyntax Then
                    classificationType = ClassificationTypeNames.XmlDocCommentProcessingInstruction
                Else
                    classificationType = ClassificationTypeNames.XmlDocCommentName
                End If

                Dim prefix = node.Prefix
                If prefix IsNot Nothing Then
                    AddXmlClassification(prefix.Name, classificationType)
                    AddXmlClassification(prefix.ColonToken, classificationType)
                End If

                AddXmlClassification(node.LocalName, classificationType)
            End Sub

            Private Sub ClassifyElement(xmlElementSyntax As XmlElementSyntax)
                ClassifyElementStart(xmlElementSyntax.StartTag)

                For Each xmlNode In xmlElementSyntax.Content
                    ClassifyXmlNode(xmlNode)
                Next

                ClassifyElementEnd(xmlElementSyntax.EndTag)
            End Sub

            Private Sub ClassifyElementStart(node As XmlElementStartTagSyntax)
                AddXmlClassification(node.LessThanToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                ClassifyXmlNode(node.Name)

                ' Note: In xml doc comments, attributes can only _be_ attributes.
                ' there are no expression holes here.
                For Each attribute In node.Attributes
                    ClassifyBaseXmlAttribute(TryCast(attribute, BaseXmlAttributeSyntax))
                Next

                AddXmlClassification(node.GreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter)
            End Sub

            Private Sub ClassifyElementEnd(node As XmlElementEndTagSyntax)
                AddXmlClassification(node.LessThanSlashToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                ClassifyXmlNode(node.Name)
                AddXmlClassification(node.GreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter)
            End Sub

            Private Sub ClassifyEmptyElement(node As XmlEmptyElementSyntax)
                AddXmlClassification(node.LessThanToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                ClassifyXmlNode(node.Name)

                ' Note: In xml doc comments, attributes can only _be_ attributes.
                ' there are no expression holes here.
                For Each attribute In node.Attributes
                    ClassifyBaseXmlAttribute(TryCast(attribute, BaseXmlAttributeSyntax))
                Next

                AddXmlClassification(node.SlashGreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter)
            End Sub

            Private Sub ClassifyBaseXmlAttribute(attribute As BaseXmlAttributeSyntax)
                If attribute IsNot Nothing Then
                    Select Case attribute.Kind
                        Case SyntaxKind.XmlAttribute
                            ClassifyAttribute(DirectCast(attribute, XmlAttributeSyntax))

                        Case SyntaxKind.XmlCrefAttribute
                            ClassifyCrefAttribute(DirectCast(attribute, XmlCrefAttributeSyntax))

                        Case SyntaxKind.XmlNameAttribute
                            ClassifyNameAttribute(DirectCast(attribute, XmlNameAttributeSyntax))
                    End Select
                End If
            End Sub

            Private Sub ClassifyAttribute(attribute As XmlAttributeSyntax)
                ClassifyXmlNode(attribute.Name)
                AddXmlClassification(attribute.EqualsToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                ClassifyXmlNode(attribute.Value)
            End Sub

            Private Sub ClassifyCrefAttribute(attribute As XmlCrefAttributeSyntax)
                ClassifyXmlNode(attribute.Name)
                AddXmlClassification(attribute.EqualsToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                AddXmlClassification(attribute.StartQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes)
                _worker.ClassifyNode(attribute.Reference)
                AddXmlClassification(attribute.EndQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes)
            End Sub

            Private Sub ClassifyNameAttribute(attribute As XmlNameAttributeSyntax)
                ClassifyXmlNode(attribute.Name)
                AddXmlClassification(attribute.EqualsToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                AddXmlClassification(attribute.StartQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes)
                _worker.ClassifyNode(attribute.Reference)
                AddXmlClassification(attribute.EndQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes)
            End Sub

            Private Sub ClassifyXmlText(xmlTextSyntax As XmlTextSyntax)
                ClassifyXmlTextTokens(xmlTextSyntax.TextTokens)
            End Sub

            Private Sub ClassifyString(node As XmlStringSyntax)
                AddXmlClassification(node.StartQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes)
                ClassifyXmlTextTokens(node.TextTokens)
                AddXmlClassification(node.EndQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes)
            End Sub

            Private Sub ClassifyComment(node As XmlCommentSyntax)
                AddXmlClassification(node.LessThanExclamationMinusMinusToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                ClassifyXmlTextTokens(node.TextTokens)
                AddXmlClassification(node.MinusMinusGreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter)
            End Sub

            Private Sub ClassifyCData(node As XmlCDataSectionSyntax)
                AddXmlClassification(node.BeginCDataToken, ClassificationTypeNames.XmlDocCommentDelimiter)
                ClassifyXmlTextTokens(node.TextTokens)
                AddXmlClassification(node.EndCDataToken, ClassificationTypeNames.XmlDocCommentDelimiter)
            End Sub

            Private Sub ClassifyProcessingInstruction(node As XmlProcessingInstructionSyntax)
                AddXmlClassification(node.LessThanQuestionToken, ClassificationTypeNames.XmlDocCommentProcessingInstruction)
                AddXmlClassification(node.Name, ClassificationTypeNames.XmlDocCommentProcessingInstruction)
                ClassifyXmlTextTokens(node.TextTokens)
                AddXmlClassification(node.QuestionGreaterThanToken, ClassificationTypeNames.XmlDocCommentProcessingInstruction)
            End Sub

        End Class
    End Class
End Namespace
