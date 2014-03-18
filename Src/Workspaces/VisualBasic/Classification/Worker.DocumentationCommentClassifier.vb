' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Partial Friend Class Worker
        Private Class DocumentationCommentClassifier
            Private _worker As Worker

            Sub New(worker As Worker)
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

                Select Case node.VisualBasicKind
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

            Private Sub ClassifyExteriorTrivia(triviaList As SyntaxTriviaList)
                For Each trivie In triviaList
                    If trivie.VisualBasicKind = SyntaxKind.DocumentationCommentExteriorTrivia Then
                        _worker.AddClassification(trivie, ClassificationTypeNames.XmlDocCommentDelimiter)
                    End If
                Next
            End Sub

            Private Sub AddXmlClassification(token As SyntaxToken, classificationType As String)
                If token.HasLeadingTrivia Then
                    ClassifyExteriorTrivia(token.LeadingTrivia)
                End If

                _worker.AddClassification(token, classificationType)

                If token.HasTrailingTrivia Then
                    ClassifyExteriorTrivia(token.TrailingTrivia)
                End If
            End Sub

            Private Sub ClassifyXmlTextTokens(textTokens As SyntaxTokenList)
                For Each token In textTokens
                    If token.HasLeadingTrivia Then
                        ClassifyExteriorTrivia(token.LeadingTrivia)
                    End If

                    ClassifyXmlTextToken(token)

                    If token.HasTrailingTrivia Then
                        ClassifyExteriorTrivia(token.TrailingTrivia)
                    End If
                Next token
            End Sub

            Private Sub ClassifyXmlTextToken(token As SyntaxToken)
                If token.VisualBasicKind = SyntaxKind.XmlEntityLiteralToken Then
                    _worker.AddClassification(token, ClassificationTypeNames.XmlDocCommentEntityReference)
                ElseIf Not String.IsNullOrWhiteSpace(token.ToString()) Then
                    Select Case token.Parent.VisualBasicKind
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
                    Select Case attribute.VisualBasicKind
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