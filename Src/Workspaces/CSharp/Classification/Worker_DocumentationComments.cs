// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal partial class Worker
    {
        private void ClassifyDocumentationComment(DocumentationCommentTriviaSyntax documentationComment)
        {
            if (!textSpan.OverlapsWith(documentationComment.Span))
            {
                return;
            }

            foreach (var xmlNode in documentationComment.Content)
            {
                var childFullSpan = xmlNode.FullSpan;
                if (childFullSpan.Start > textSpan.End)
                {
                    return;
                }
                else if (childFullSpan.End < textSpan.Start)
                {
                    continue;
                }

                ClassifyXmlNode(xmlNode);
            }

            // NOTE: the "EndOfComment" token is a special, zero width token.  However, if it's a multi-line xml doc comment
            // the final '*/" will be leading exterior trivia on it.
            ClassifyExteriorTrivia(documentationComment.EndOfComment.LeadingTrivia);
        }

        private void ClassifyXmlNode(XmlNodeSyntax node)
        {
            switch (node.CSharpKind())
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

        private void ClassifyExteriorTrivia(SyntaxTriviaList triviaList)
        {
            foreach (var t in triviaList)
            {
                if (t.CSharpKind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                {
                    AddClassification(t, ClassificationTypeNames.XmlDocCommentDelimiter);
                }
            }
        }

        private void AddXmlClassification(SyntaxToken token, string classificationType)
        {
            if (token.HasLeadingTrivia)
            {
                ClassifyExteriorTrivia(token.LeadingTrivia);
            }

            AddClassification(token, classificationType);

            if (token.HasTrailingTrivia)
            {
                ClassifyExteriorTrivia(token.TrailingTrivia);
            }
        }

        private void ClassifyXmlTextTokens(SyntaxTokenList textTokens)
        {
            foreach (var token in textTokens)
            {
                if (token.HasLeadingTrivia)
                {
                    ClassifyExteriorTrivia(token.LeadingTrivia);
                }

                ClassifyXmlTextToken(token);

                if (token.HasTrailingTrivia)
                {
                    ClassifyExteriorTrivia(token.TrailingTrivia);
                }
            }
        }

        private void ClassifyXmlTextToken(SyntaxToken token)
        {
            if (token.CSharpKind() == SyntaxKind.XmlEntityLiteralToken)
            {
                AddClassification(token, ClassificationTypeNames.XmlDocCommentEntityReference);
            }
            else if (!string.IsNullOrWhiteSpace(token.ToString()))
            {
                switch (token.Parent.CSharpKind())
                {
                    case SyntaxKind.XmlText:
                        AddClassification(token, ClassificationTypeNames.XmlDocCommentText);
                        break;
                    case SyntaxKind.XmlTextAttribute:
                        AddClassification(token, ClassificationTypeNames.XmlDocCommentAttributeValue);
                        break;
                    case SyntaxKind.XmlComment:
                        AddClassification(token, ClassificationTypeNames.XmlDocCommentComment);
                        break;
                    case SyntaxKind.XmlCDataSection:
                        AddClassification(token, ClassificationTypeNames.XmlDocCommentCDataSection);
                        break;
                    case SyntaxKind.XmlProcessingInstruction:
                        AddClassification(token, ClassificationTypeNames.XmlDocCommentProcessingInstruction);
                        break;
                }
            }
        }

        private void ClassifyXmlName(XmlNameSyntax node)
        {
            string classificationType;
            if (node.Parent is XmlAttributeSyntax)
            {
                classificationType = ClassificationTypeNames.XmlDocCommentAttributeName;
            }
            else if (node.Parent is XmlProcessingInstructionSyntax)
            {
                classificationType = ClassificationTypeNames.XmlDocCommentProcessingInstruction;
            }
            else
            {
                classificationType = ClassificationTypeNames.XmlDocCommentName;
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
            AddXmlClassification(node.LessThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
            ClassifyXmlName(node.Name);

            foreach (var attribute in node.Attributes)
            {
                ClassifyXmlAttribute(attribute);
            }

            AddXmlClassification(node.GreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlElementEndTag(XmlElementEndTagSyntax node)
        {
            AddXmlClassification(node.LessThanSlashToken, ClassificationTypeNames.XmlDocCommentDelimiter);
            ClassifyXmlName(node.Name);
            AddXmlClassification(node.GreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlEmptyElement(XmlEmptyElementSyntax node)
        {
            AddXmlClassification(node.LessThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
            ClassifyXmlName(node.Name);

            foreach (var attribute in node.Attributes)
            {
                ClassifyXmlAttribute(attribute);
            }

            AddXmlClassification(node.SlashGreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlAttribute(XmlAttributeSyntax attribute)
        {
            ClassifyXmlName(attribute.Name);
            AddXmlClassification(attribute.EqualsToken, ClassificationTypeNames.XmlDocCommentDelimiter);
            AddXmlClassification(attribute.StartQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes);

            switch (attribute.CSharpKind())
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

            AddXmlClassification(attribute.EndQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes);
        }

        private void ClassifyXmlText(XmlTextSyntax node)
        {
            ClassifyXmlTextTokens(node.TextTokens);
        }

        private void ClassifyXmlComment(XmlCommentSyntax node)
        {
            AddXmlClassification(node.LessThanExclamationMinusMinusToken, ClassificationTypeNames.XmlDocCommentDelimiter);
            ClassifyXmlTextTokens(node.TextTokens);
            AddXmlClassification(node.MinusMinusGreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlCDataSection(XmlCDataSectionSyntax node)
        {
            AddXmlClassification(node.StartCDataToken, ClassificationTypeNames.XmlDocCommentDelimiter);
            ClassifyXmlTextTokens(node.TextTokens);
            AddXmlClassification(node.EndCDataToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        }

        private void ClassifyXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
        {
            AddXmlClassification(node.StartProcessingInstructionToken, ClassificationTypeNames.XmlDocCommentProcessingInstruction);
            ClassifyXmlName(node.Name);
            ClassifyXmlTextTokens(node.TextTokens);
            AddXmlClassification(node.EndProcessingInstructionToken, ClassificationTypeNames.XmlDocCommentProcessingInstruction);
        }
    }
}
