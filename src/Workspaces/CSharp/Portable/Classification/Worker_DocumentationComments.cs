// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal partial class Worker
    {
        private void ClassifyDocumentationComment(DocumentationCommentTriviaSyntax documentationComment)
        {
            if (!_textSpan.OverlapsWith(documentationComment.Span))
            {
                return;
            }

            foreach (var xmlNode in documentationComment.Content)
            {
                var childFullSpan = xmlNode.FullSpan;
                if (childFullSpan.Start > _textSpan.End)
                {
                    return;
                }
                else if (childFullSpan.End < _textSpan.Start)
                {
                    continue;
                }

                ClassifyXmlNode(xmlNode);
            }

            // NOTE: the "EndOfComment" token is a special, zero width token.  However, if it's a multi-line xml doc comment
            // the final '*/" will be leading exterior trivia on it.
            ClassifyXmlTrivia(documentationComment.EndOfComment.LeadingTrivia);
        }

        private void ClassifyXmlNode(XmlNodeSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.XmlElement:
                    ClassifyXmlElement((XmlElementSyntax)node);
                    break;
                case SyntaxKind.XmlEmptyElement:
                    ClassifyXmlEmptyElement((XmlEmptyElementSyntax)node);
                    break;
                case SyntaxKind.XmlText:
                    ClassifyXmlText((XmlTextSyntax)node);
                    break;
                case SyntaxKind.XmlComment:
                    ClassifyXmlComment((XmlCommentSyntax)node);
                    break;
                case SyntaxKind.XmlCDataSection:
                    ClassifyXmlCDataSection((XmlCDataSectionSyntax)node);
                    break;
                case SyntaxKind.XmlProcessingInstruction:
                    ClassifyXmlProcessingInstruction((XmlProcessingInstructionSyntax)node);
                    break;
            }
        }

        private void ClassifyXmlTrivia(SyntaxTriviaList triviaList, ClassificationTypeKind? whitespaceClassificationType = null)
        {
            foreach (var t in triviaList)
            {
                switch (t.Kind())
                {
                    case SyntaxKind.DocumentationCommentExteriorTrivia:
                        ClassifyExteriorTrivia(t);
                        break;

                    case SyntaxKind.WhitespaceTrivia:
                        if (whitespaceClassificationType != null)
                        {
                            AddClassification(t, whitespaceClassificationType);
                        }

                        break;
                }
            }
        }

        private void ClassifyExteriorTrivia(SyntaxTrivia trivia)
        {
            // Note: The exterior trivia can contain whitespace (usually leading) and we want to avoid classifying it.
            // However, meaningful exterior trivia can also have an undetermined length in the case of
            // multiline doc comments.

            // For example:
            //
            //     /**<summary>
            //      ********* Foo
            //      ******* </summary>*/

            // PERFORMANCE:
            // While the call to SyntaxTrivia.ToString() looks like an allocation, it isn't.
            // The SyntaxTrivia green node holds the string text of the trivia in a field and ToString()
            // just returns a reference to that.
            var text = trivia.ToString();

            int? spanStart = null;

            for (var index = 0; index < text.Length; index++)
            {
                var ch = text[index];

                if (spanStart != null && char.IsWhiteSpace(ch))
                {
                    var span = TextSpan.FromBounds(spanStart.Value, spanStart.Value + index);
                    AddClassification(span, ClassificationTypeKind.XmlDocCommentDelimiter);

                    spanStart = null;
                }
                else if (spanStart == null && !char.IsWhiteSpace(ch))
                {
                    spanStart = trivia.Span.Start + index;
                }
            }

            // Add a final classification if we hadn't encountered anymore whitespace at the end.
            if (spanStart != null)
            {
                var span = TextSpan.FromBounds(spanStart.Value, trivia.Span.End);
                AddClassification(span, ClassificationTypeKind.XmlDocCommentDelimiter);
            }
        }

        private void AddXmlClassification(SyntaxToken token, ClassificationTypeKind classificationType)
        {
            if (token.HasLeadingTrivia)
            {
                ClassifyXmlTrivia(token.LeadingTrivia, classificationType);
            }

            AddClassification(token, classificationType);

            if (token.HasTrailingTrivia)
            {
                ClassifyXmlTrivia(token.TrailingTrivia, classificationType);
            }
        }

        private void ClassifyXmlTextTokens(SyntaxTokenList textTokens)
        {
            foreach (var token in textTokens)
            {
                if (token.HasLeadingTrivia)
                {
                    ClassifyXmlTrivia(token.LeadingTrivia, whitespaceClassificationType: ClassificationTypeKind.XmlDocCommentText);
                }

                ClassifyXmlTextToken(token);

                if (token.HasTrailingTrivia)
                {
                    ClassifyXmlTrivia(token.TrailingTrivia, whitespaceClassificationType: ClassificationTypeKind.XmlDocCommentText);
                }
            }
        }

        private void ClassifyXmlTextToken(SyntaxToken token)
        {
            if (token.Kind() == SyntaxKind.XmlEntityLiteralToken)
            {
                AddClassification(token, ClassificationTypeKind.XmlDocCommentEntityReference);
            }
            else if (token.Kind() != SyntaxKind.XmlTextLiteralNewLineToken)
            {
                switch (token.Parent.Kind())
                {
                    case SyntaxKind.XmlText:
                        AddClassification(token, ClassificationTypeKind.XmlDocCommentText);
                        break;
                    case SyntaxKind.XmlTextAttribute:
                        AddClassification(token, ClassificationTypeKind.XmlDocCommentAttributeValue);
                        break;
                    case SyntaxKind.XmlComment:
                        AddClassification(token, ClassificationTypeKind.XmlDocCommentComment);
                        break;
                    case SyntaxKind.XmlCDataSection:
                        AddClassification(token, ClassificationTypeKind.XmlDocCommentCDataSection);
                        break;
                    case SyntaxKind.XmlProcessingInstruction:
                        AddClassification(token, ClassificationTypeKind.XmlDocCommentProcessingInstruction);
                        break;
                }
            }
        }

        private void ClassifyXmlName(XmlNameSyntax node)
        {
            ClassificationTypeKind classificationType;
            if (node.Parent is XmlAttributeSyntax)
            {
                classificationType = ClassificationTypeKind.XmlDocCommentAttributeName;
            }
            else if (node.Parent is XmlProcessingInstructionSyntax)
            {
                classificationType = ClassificationTypeKind.XmlDocCommentProcessingInstruction;
            }
            else
            {
                classificationType = ClassificationTypeKind.XmlDocCommentName;
            }

            var prefix = node.Prefix;
            if (prefix != null)
            {
                AddXmlClassification(prefix.Prefix, classificationType);
                AddXmlClassification(prefix.ColonToken, classificationType);
            }

            AddXmlClassification(node.LocalName, classificationType);
        }

        private void ClassifyXmlElement(XmlElementSyntax node)
        {
            ClassifyXmlElementStartTag(node.StartTag);

            foreach (var xmlNode in node.Content)
            {
                ClassifyXmlNode(xmlNode);
            }

            ClassifyXmlElementEndTag(node.EndTag);
        }

        private void ClassifyXmlElementStartTag(XmlElementStartTagSyntax node)
        {
            AddXmlClassification(node.LessThanToken, ClassificationTypeKind.XmlDocCommentDelimiter);
            ClassifyXmlName(node.Name);

            foreach (var attribute in node.Attributes)
            {
                ClassifyXmlAttribute(attribute);
            }

            AddXmlClassification(node.GreaterThanToken, ClassificationTypeKind.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlElementEndTag(XmlElementEndTagSyntax node)
        {
            AddXmlClassification(node.LessThanSlashToken, ClassificationTypeKind.XmlDocCommentDelimiter);
            ClassifyXmlName(node.Name);
            AddXmlClassification(node.GreaterThanToken, ClassificationTypeKind.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlEmptyElement(XmlEmptyElementSyntax node)
        {
            AddXmlClassification(node.LessThanToken, ClassificationTypeKind.XmlDocCommentDelimiter);
            ClassifyXmlName(node.Name);

            foreach (var attribute in node.Attributes)
            {
                ClassifyXmlAttribute(attribute);
            }

            AddXmlClassification(node.SlashGreaterThanToken, ClassificationTypeKind.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlAttribute(XmlAttributeSyntax attribute)
        {
            ClassifyXmlName(attribute.Name);
            AddXmlClassification(attribute.EqualsToken, ClassificationTypeKind.XmlDocCommentDelimiter);
            AddXmlClassification(attribute.StartQuoteToken, ClassificationTypeKind.XmlDocCommentAttributeQuotes);

            switch (attribute.Kind())
            {
                case SyntaxKind.XmlTextAttribute:
                    ClassifyXmlTextTokens(((XmlTextAttributeSyntax)attribute).TextTokens);
                    break;
                case SyntaxKind.XmlCrefAttribute:
                    ClassifyNode(((XmlCrefAttributeSyntax)attribute).Cref);
                    break;
                case SyntaxKind.XmlNameAttribute:
                    ClassifyNode(((XmlNameAttributeSyntax)attribute).Identifier);
                    break;
            }

            AddXmlClassification(attribute.EndQuoteToken, ClassificationTypeKind.XmlDocCommentAttributeQuotes);
        }

        private void ClassifyXmlText(XmlTextSyntax node)
        {
            ClassifyXmlTextTokens(node.TextTokens);
        }

        private void ClassifyXmlComment(XmlCommentSyntax node)
        {
            AddXmlClassification(node.LessThanExclamationMinusMinusToken, ClassificationTypeKind.XmlDocCommentDelimiter);
            ClassifyXmlTextTokens(node.TextTokens);
            AddXmlClassification(node.MinusMinusGreaterThanToken, ClassificationTypeKind.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlCDataSection(XmlCDataSectionSyntax node)
        {
            AddXmlClassification(node.StartCDataToken, ClassificationTypeKind.XmlDocCommentDelimiter);
            ClassifyXmlTextTokens(node.TextTokens);
            AddXmlClassification(node.EndCDataToken, ClassificationTypeKind.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
        {
            AddXmlClassification(node.StartProcessingInstructionToken, ClassificationTypeKind.XmlDocCommentProcessingInstruction);
            ClassifyXmlName(node.Name);
            ClassifyXmlTextTokens(node.TextTokens);
            AddXmlClassification(node.EndProcessingInstructionToken, ClassificationTypeKind.XmlDocCommentProcessingInstruction);
        }
    }
}
