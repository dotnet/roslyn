// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification;

internal ref partial struct Worker
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

            ClassifyXmlNode(xmlNode, skipXmlTextTokens: false);
        }

        // NOTE: the "EndOfComment" token is a special, zero width token.  However, if it's a multi-line xml doc comment
        // the final '*/" will be leading exterior trivia on it.
        ClassifyXmlTrivia(documentationComment.EndOfComment.LeadingTrivia);
    }

    private void ClassifyXmlNode(XmlNodeSyntax node, bool skipXmlTextTokens)
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
                ClassifyXmlText((XmlTextSyntax)node, skipXmlTextTokens);
                break;
            case SyntaxKind.XmlComment:
                ClassifyXmlComment((XmlCommentSyntax)node);
                break;
            case SyntaxKind.XmlCDataSection:
                ClassifyXmlCDataSection((XmlCDataSectionSyntax)node, skipXmlTextTokens);
                break;
            case SyntaxKind.XmlProcessingInstruction:
                ClassifyXmlProcessingInstruction((XmlProcessingInstructionSyntax)node);
                break;
        }
    }

    private void ClassifyXmlTrivia(SyntaxTriviaList triviaList)
    {
        foreach (var t in triviaList)
        {
            switch (t.Kind())
            {
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    ClassifyExteriorTrivia(t);
                    break;

                case SyntaxKind.SkippedTokensTrivia:
                    AddClassification(t, ClassificationTypeNames.XmlDocCommentText);
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
        //      ********* Goo
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
                AddClassification(span, ClassificationTypeNames.XmlDocCommentDelimiter);

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
            AddClassification(span, ClassificationTypeNames.XmlDocCommentDelimiter);
        }
    }

    private void AddXmlClassification(SyntaxToken token, string classificationType)
    {
        if (token.HasLeadingTrivia)
            ClassifyXmlTrivia(token.LeadingTrivia);

        AddClassification(token, classificationType);

        if (token.HasTrailingTrivia)
            ClassifyXmlTrivia(token.TrailingTrivia);
    }

    private void ClassifyXmlTextTokens(
        SyntaxTokenList textTokens, bool skipXmlTextTokens)
    {
        foreach (var token in textTokens)
        {
            if (token.HasLeadingTrivia)
                ClassifyXmlTrivia(token.LeadingTrivia);

            ClassifyXmlTextToken(token, skipXmlTextTokens);

            if (token.HasTrailingTrivia)
                ClassifyXmlTrivia(token.TrailingTrivia);
        }
    }

    private readonly void ClassifyXmlTextToken(SyntaxToken token, bool skipXmlTextTokens)
    {
        if (skipXmlTextTokens)
            return;

        if (token.Kind() == SyntaxKind.XmlEntityLiteralToken)
        {
            AddClassification(token, ClassificationTypeNames.XmlDocCommentEntityReference);
        }
        else if (token.Kind() != SyntaxKind.XmlTextLiteralNewLineToken)
        {
            RoslynDebug.Assert(token.Parent is object);
            switch (token.Parent.Kind())
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
        var classificationType = node.Parent switch
        {
            XmlAttributeSyntax => ClassificationTypeNames.XmlDocCommentAttributeName,
            XmlProcessingInstructionSyntax => ClassificationTypeNames.XmlDocCommentProcessingInstruction,
            _ => ClassificationTypeNames.XmlDocCommentName,
        };

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

        // For C# code blocks, still recurse into content but only classify the /// trivia
        var (isCSharp, isCSharpTest) = ClassificationHelpers.IsCodeBlockWithCSharpLang(node);

        var isCSharpCodeBlock = isCSharp || isCSharpTest;

        foreach (var xmlNode in node.Content)
            ClassifyXmlNode(xmlNode, skipXmlTextTokens: isCSharpCodeBlock);

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

        switch (attribute.Kind())
        {
            case SyntaxKind.XmlTextAttribute:
                // Since the langword attribute in `<see langword="..." />` is not parsed into its own
                // SyntaxNode as cref is, we need to handle it specially.
                if (IsLangWordAttribute(attribute))
                    ClassifyLangWordTextTokenList(((XmlTextAttributeSyntax)attribute).TextTokens);
                else
                    ClassifyXmlTextTokens(((XmlTextAttributeSyntax)attribute).TextTokens, skipXmlTextTokens: false);

                break;
            case SyntaxKind.XmlCrefAttribute:
                ClassifyNode(((XmlCrefAttributeSyntax)attribute).Cref);
                break;
            case SyntaxKind.XmlNameAttribute:
                ClassifyNode(((XmlNameAttributeSyntax)attribute).Identifier);
                break;
        }

        AddXmlClassification(attribute.EndQuoteToken, ClassificationTypeNames.XmlDocCommentAttributeQuotes);

        static bool IsLangWordAttribute(XmlAttributeSyntax attribute)
        {
            return attribute.Name.LocalName.Text == DocumentationCommentXmlNames.LangwordAttributeName && IsSeeElement(attribute.Parent);
        }

        static bool IsSeeElement(SyntaxNode? node)
        {
            return node is XmlElementStartTagSyntax { Name: XmlNameSyntax { Prefix: null, LocalName: SyntaxToken { Text: DocumentationCommentXmlNames.SeeElementName } } }
                || node is XmlEmptyElementSyntax { Name: XmlNameSyntax { Prefix: null, LocalName: SyntaxToken { Text: DocumentationCommentXmlNames.SeeElementName } } };
        }
    }

    private void ClassifyLangWordTextTokenList(SyntaxTokenList list)
    {
        foreach (var token in list)
        {
            if (token.HasLeadingTrivia)
                ClassifyXmlTrivia(token.LeadingTrivia);

            ClassifyLangWordTextToken(token);

            if (token.HasTrailingTrivia)
                ClassifyXmlTrivia(token.TrailingTrivia);
        }
    }

    private void ClassifyLangWordTextToken(SyntaxToken token)
    {
        var kind = SyntaxFacts.GetKeywordKind(token.Text);
        if (kind is SyntaxKind.None)
            kind = SyntaxFacts.GetContextualKeywordKind(token.Text);

        if (kind is SyntaxKind.None)
        {
            ClassifyXmlTextToken(token, skipXmlTextTokens: false);
            return;
        }

        var isControlKeyword = ClassificationHelpers.IsControlKeywordKind(kind) || ClassificationHelpers.IsControlStatementKind(kind);
        AddClassification(token, isControlKeyword ? ClassificationTypeNames.ControlKeyword : ClassificationTypeNames.Keyword);
    }

    private void ClassifyXmlText(XmlTextSyntax node, bool skipXmlTextTokens)
        => ClassifyXmlTextTokens(node.TextTokens, skipXmlTextTokens);

    private void ClassifyXmlComment(XmlCommentSyntax node)
    {
        AddXmlClassification(node.LessThanExclamationMinusMinusToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        ClassifyXmlTextTokens(node.TextTokens, skipXmlTextTokens: false);
        AddXmlClassification(node.MinusMinusGreaterThanToken, ClassificationTypeNames.XmlDocCommentDelimiter);
    }

    private void ClassifyXmlCDataSection(XmlCDataSectionSyntax node, bool skipXmlTextTokens)
    {
        AddXmlClassification(node.StartCDataToken, ClassificationTypeNames.XmlDocCommentDelimiter);
        ClassifyXmlTextTokens(node.TextTokens, skipXmlTextTokens);
        AddXmlClassification(node.EndCDataToken, ClassificationTypeNames.XmlDocCommentDelimiter);
    }

    private void ClassifyXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
    {
        AddXmlClassification(node.StartProcessingInstructionToken, ClassificationTypeNames.XmlDocCommentProcessingInstruction);
        ClassifyXmlName(node.Name);
        ClassifyXmlTextTokens(node.TextTokens, skipXmlTextTokens: false);
        AddXmlClassification(node.EndProcessingInstructionToken, ClassificationTypeNames.XmlDocCommentProcessingInstruction);
    }
}
