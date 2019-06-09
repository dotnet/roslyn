// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    // TODO: The Xml parser recognizes most commonplace XML, according to the XML spec.
    // It does not recognize the following:
    //
    //  * Document Type Definition
    //      <!DOCTYPE ... >
    //  * Element Type Declaration, Attribute-List Declarations, Entity Declarations, Notation Declarations
    //      <!ELEMENT ... >
    //      <!ATTLIST ... >
    //      <!ENTITY ... >
    //      <!NOTATION ... >
    //  * Conditional Sections
    //      <![INCLUDE[ ... ]]>
    //      <![IGNORE[ ... ]]>
    // 
    // This probably does not matter. However, if it becomes necessary to recognize any
    // of these bits of XML, the most sensible thing to do is probably to scan them without
    // trying to understand them, e.g. like comments or CDATA, so that they are available
    // to whoever processes these comments and do not produce an error. 

    internal class DocumentationCommentParser : SyntaxParser
    {
        private readonly SyntaxListPool _pool = new SyntaxListPool();
        private bool _isDelimited;

        internal DocumentationCommentParser(Lexer lexer, LexerMode modeflags)
            : base(lexer, LexerMode.XmlDocComment | LexerMode.XmlDocCommentLocationStart | modeflags, null, null, true)
        {
            _isDelimited = (modeflags & LexerMode.XmlDocCommentStyleDelimited) != 0;
        }

        internal void ReInitialize(LexerMode modeflags)
        {
            base.ReInitialize();
            this.Mode = LexerMode.XmlDocComment | LexerMode.XmlDocCommentLocationStart | modeflags;
            _isDelimited = (modeflags & LexerMode.XmlDocCommentStyleDelimited) != 0;
        }

        private LexerMode SetMode(LexerMode mode)
        {
            var tmp = this.Mode;
            this.Mode = mode | (tmp & (LexerMode.MaskXmlDocCommentLocation | LexerMode.MaskXmlDocCommentStyle));
            return tmp;
        }

        private void ResetMode(LexerMode mode)
        {
            this.Mode = mode;
        }

        public DocumentationCommentTriviaSyntax ParseDocumentationComment(out bool isTerminated)
        {
            var nodes = _pool.Allocate<XmlNodeSyntax>();
            try
            {
                this.ParseXmlNodes(nodes);

                // It's possible that we finish parsing the xml, and we are still left in the middle
                // of an Xml comment. For example,
                //
                //     /// <goo></goo></uhoh>
                //                    ^
                // In this case, we stop at the caret. We need to ensure that we consume the remainder
                // of the doc comment here, since otherwise we will return the lexer to the state
                // where it recognizes C# tokens, which means that C# parser will get the </uhoh>,
                // which is not at all what we want.

                if (this.CurrentToken.Kind != SyntaxKind.EndOfDocumentationCommentToken)
                {
                    this.ParseRemainder(nodes);
                }

                var eoc = this.EatToken(SyntaxKind.EndOfDocumentationCommentToken);

                isTerminated = !_isDelimited || (eoc.LeadingTrivia.Count > 0 && eoc.LeadingTrivia[eoc.LeadingTrivia.Count - 1].ToString() == "*/");
                SyntaxKind kind = _isDelimited ? SyntaxKind.MultiLineDocumentationCommentTrivia : SyntaxKind.SingleLineDocumentationCommentTrivia;

                return SyntaxFactory.DocumentationCommentTrivia(kind, nodes.ToList(), eoc);
            }
            finally
            {
                _pool.Free(nodes);
            }
        }

        public void ParseRemainder(SyntaxListBuilder<XmlNodeSyntax> nodes)
        {
            bool endTag = this.CurrentToken.Kind == SyntaxKind.LessThanSlashToken;

            var saveMode = this.SetMode(LexerMode.XmlCDataSectionText);

            var textTokens = _pool.Allocate();
            try
            {
                while (this.CurrentToken.Kind != SyntaxKind.EndOfDocumentationCommentToken)
                {
                    var token = this.EatToken();

                    // TODO: It is possible that a non-literal gets in here. ]]>, specifically. Is that ok?
                    textTokens.Add(token);
                }

                var allRemainderText = SyntaxFactory.XmlText(textTokens.ToList());

                XmlParseErrorCode code = endTag ? XmlParseErrorCode.XML_EndTagNotExpected : XmlParseErrorCode.XML_ExpectedEndOfXml;
                allRemainderText = WithAdditionalDiagnostics(allRemainderText, new XmlSyntaxDiagnosticInfo(0, 1, code));

                nodes.Add(allRemainderText);
            }
            finally
            {
                _pool.Free(textTokens);
            }

            this.ResetMode(saveMode);
        }

        private void ParseXmlNodes(SyntaxListBuilder<XmlNodeSyntax> nodes)
        {
            while (true)
            {
                var node = this.ParseXmlNode();
                if (node == null)
                {
                    return;
                }

                nodes.Add(node);
            }
        }

        private XmlNodeSyntax ParseXmlNode()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.XmlTextLiteralToken:
                case SyntaxKind.XmlTextLiteralNewLineToken:
                case SyntaxKind.XmlEntityLiteralToken:
                    return this.ParseXmlText();
                case SyntaxKind.LessThanToken:
                    return this.ParseXmlElement();
                case SyntaxKind.XmlCommentStartToken:
                    return this.ParseXmlComment();
                case SyntaxKind.XmlCDataStartToken:
                    return this.ParseXmlCDataSection();
                case SyntaxKind.XmlProcessingInstructionStartToken:
                    return this.ParseXmlProcessingInstruction();
                case SyntaxKind.EndOfDocumentationCommentToken:
                    return null;
                default:
                    // This means we have some unrecognized token. We probably need to give an error.
                    return null;
            }
        }

        private bool IsXmlNodeStartOrStop()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanSlashToken:
                case SyntaxKind.XmlCommentStartToken:
                case SyntaxKind.XmlCDataStartToken:
                case SyntaxKind.XmlProcessingInstructionStartToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.SlashGreaterThanToken:
                case SyntaxKind.EndOfDocumentationCommentToken:
                    return true;
                default:
                    return false;
            }
        }

        private XmlNodeSyntax ParseXmlText()
        {
            var textTokens = _pool.Allocate();
            while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
                || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken
                || this.CurrentToken.Kind == SyntaxKind.XmlEntityLiteralToken)
            {
                textTokens.Add(this.EatToken());
            }

            var list = textTokens.ToList();
            _pool.Free(textTokens);
            return SyntaxFactory.XmlText(list);
        }

        private XmlNodeSyntax ParseXmlElement()
        {
            var lessThan = this.EatToken(SyntaxKind.LessThanToken); // guaranteed
            var saveMode = this.SetMode(LexerMode.XmlElementTag);
            var name = this.ParseXmlName();
            if (lessThan.GetTrailingTriviaWidth() > 0 || name.GetLeadingTriviaWidth() > 0)
            {
                // The Xml spec disallows whitespace here: STag ::= '<' Name (S Attribute)* S? '>' 
                name = this.WithXmlParseError(name, XmlParseErrorCode.XML_InvalidWhitespace);
            }

            var attrs = _pool.Allocate<XmlAttributeSyntax>();
            try
            {
                this.ParseXmlAttributes(ref name, attrs);

                if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                {
                    var startTag = SyntaxFactory.XmlElementStartTag(lessThan, name, attrs, this.EatToken());
                    this.SetMode(LexerMode.XmlDocComment);
                    var nodes = _pool.Allocate<XmlNodeSyntax>();
                    try
                    {
                        this.ParseXmlNodes(nodes);

                        XmlNameSyntax endName;
                        SyntaxToken greaterThan;

                        // end tag
                        var lessThanSlash = this.EatToken(SyntaxKind.LessThanSlashToken, reportError: false);

                        // If we didn't see "</", then we can't really be confident that this is actually an end tag,
                        // so just insert a missing one.
                        if (lessThanSlash.IsMissing)
                        {
                            this.ResetMode(saveMode);
                            lessThanSlash = this.WithXmlParseError(lessThanSlash, XmlParseErrorCode.XML_EndTagExpected, name.ToString());
                            endName = SyntaxFactory.XmlName(prefix: null, localName: SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken));
                            greaterThan = SyntaxFactory.MissingToken(SyntaxKind.GreaterThanToken);
                        }
                        else
                        {
                            this.SetMode(LexerMode.XmlElementTag);
                            endName = this.ParseXmlName();
                            if (lessThanSlash.GetTrailingTriviaWidth() > 0 || endName.GetLeadingTriviaWidth() > 0)
                            {
                                // The Xml spec disallows whitespace here: STag ::= '<' Name (S Attribute)* S? '>' 
                                endName = this.WithXmlParseError(endName, XmlParseErrorCode.XML_InvalidWhitespace);
                            }

                            if (!endName.IsMissing && !MatchingXmlNames(name, endName))
                            {
                                endName = this.WithXmlParseError(endName, XmlParseErrorCode.XML_ElementTypeMatch, endName.ToString(), name.ToString());
                            }

                            // if we don't see the greater than token then skip the badness until we do or abort
                            if (this.CurrentToken.Kind != SyntaxKind.GreaterThanToken)
                            {
                                this.SkipBadTokens(ref endName, null,
                                    p => p.CurrentToken.Kind != SyntaxKind.GreaterThanToken,
                                    p => p.IsXmlNodeStartOrStop(),
                                    XmlParseErrorCode.XML_InvalidToken
                                    );
                            }

                            greaterThan = this.EatToken(SyntaxKind.GreaterThanToken);
                        }

                        var endTag = SyntaxFactory.XmlElementEndTag(lessThanSlash, endName, greaterThan);
                        this.ResetMode(saveMode);
                        return SyntaxFactory.XmlElement(startTag, nodes.ToList(), endTag);
                    }
                    finally
                    {
                        _pool.Free(nodes);
                    }
                }
                else
                {
                    var slashGreater = this.EatToken(SyntaxKind.SlashGreaterThanToken, false);
                    if (slashGreater.IsMissing && !name.IsMissing)
                    {
                        slashGreater = this.WithXmlParseError(slashGreater, XmlParseErrorCode.XML_ExpectedEndOfTag, name.ToString());
                    }

                    this.ResetMode(saveMode);
                    return SyntaxFactory.XmlEmptyElement(lessThan, name, attrs, slashGreater);
                }
            }
            finally
            {
                _pool.Free(attrs);
            }
        }

        private static bool MatchingXmlNames(XmlNameSyntax name, XmlNameSyntax endName)
        {
            // PERF: because of deduplication we often get the same name for name and endName,
            //       so we will check for such case first before materializing text for entire nodes 
            //       and comparing that.
            if (name == endName)
            {
                return true;
            }

            // before doing ToString, check if 
            // all nodes contributing to ToString are recursively the same
            // NOTE: leading and trailing trivia do not contribute to ToString
            if (!name.HasLeadingTrivia &&
                !endName.HasTrailingTrivia &&
                name.IsEquivalentTo(endName))
            {
                return true;
            }

            return name.ToString() == endName.ToString();
        }

        // assuming this is not used concurrently
        private readonly HashSet<string> _attributesSeen = new HashSet<string>();

        private void ParseXmlAttributes(ref XmlNameSyntax elementName, SyntaxListBuilder<XmlAttributeSyntax> attrs)
        {
            _attributesSeen.Clear();
            while (true)
            {
                if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    var attr = this.ParseXmlAttribute(elementName);
                    string attrName = attr.Name.ToString();
                    if (_attributesSeen.Contains(attrName))
                    {
                        attr = this.WithXmlParseError(attr, XmlParseErrorCode.XML_DuplicateAttribute, attrName);
                    }
                    else
                    {
                        _attributesSeen.Add(attrName);
                    }

                    attrs.Add(attr);
                }
                else
                {
                    var skip = this.SkipBadTokens(ref elementName, attrs,

                    // not expected condition
                        p => p.CurrentToken.Kind != SyntaxKind.IdentifierName,

                    // abort condition (looks like something we might understand later)
                        p => p.CurrentToken.Kind == SyntaxKind.GreaterThanToken
                            || p.CurrentToken.Kind == SyntaxKind.SlashGreaterThanToken
                            || p.CurrentToken.Kind == SyntaxKind.LessThanToken
                            || p.CurrentToken.Kind == SyntaxKind.LessThanSlashToken
                            || p.CurrentToken.Kind == SyntaxKind.EndOfDocumentationCommentToken
                            || p.CurrentToken.Kind == SyntaxKind.EndOfFileToken,

                        XmlParseErrorCode.XML_InvalidToken
                        );

                    if (skip == SkipResult.Abort)
                    {
                        break;
                    }
                }
            }
        }

        private enum SkipResult
        {
            Continue,
            Abort
        }

        private SkipResult SkipBadTokens<T>(
            ref T startNode,
            SyntaxListBuilder list,
            Func<DocumentationCommentParser, bool> isNotExpectedFunction,
            Func<DocumentationCommentParser, bool> abortFunction,
            XmlParseErrorCode error
            ) where T : CSharpSyntaxNode
        {
            var badTokens = default(SyntaxListBuilder<SyntaxToken>);
            bool hasError = false;

            try
            {
                SkipResult result = SkipResult.Continue;

                while (isNotExpectedFunction(this))
                {
                    if (abortFunction(this))
                    {
                        result = SkipResult.Abort;
                        break;
                    }

                    if (badTokens.IsNull)
                    {
                        badTokens = _pool.Allocate<SyntaxToken>();
                    }

                    var token = this.EatToken();
                    if (!hasError)
                    {
                        token = this.WithXmlParseError(token, error, token.ToString());
                        hasError = true;
                    }

                    badTokens.Add(token);
                }

                if (!badTokens.IsNull && badTokens.Count > 0)
                {
                    // use skipped text since cannot nest structured trivia under structured trivia
                    if (list == null || list.Count == 0)
                    {
                        startNode = AddTrailingSkippedSyntax(startNode, badTokens.ToListNode());
                    }
                    else
                    {
                        list[list.Count - 1] = AddTrailingSkippedSyntax((CSharpSyntaxNode)list[list.Count - 1], badTokens.ToListNode());
                    }

                    return result;
                }
                else
                {
                    // somehow we did not consume anything, so tell caller to abort parse rule
                    return SkipResult.Abort;
                }
            }
            finally
            {
                if (!badTokens.IsNull)
                {
                    _pool.Free(badTokens);
                }
            }
        }

        private XmlAttributeSyntax ParseXmlAttribute(XmlNameSyntax elementName)
        {
            var attrName = this.ParseXmlName();
            if (attrName.GetLeadingTriviaWidth() == 0)
            {
                // The Xml spec requires whitespace here: STag ::= '<' Name (S Attribute)* S? '>' 
                attrName = this.WithXmlParseError(attrName, XmlParseErrorCode.XML_WhitespaceMissing);
            }

            var equals = this.EatToken(SyntaxKind.EqualsToken, false);
            if (equals.IsMissing)
            {
                equals = this.WithXmlParseError(equals, XmlParseErrorCode.XML_MissingEqualsAttribute);

                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.SingleQuoteToken:
                    case SyntaxKind.DoubleQuoteToken:
                        // There could be a value coming up, let's keep parsing.
                        break;
                    default:
                        // This is probably not a complete attribute.
                        return SyntaxFactory.XmlTextAttribute(
                            attrName,
                            equals,
                            SyntaxFactory.MissingToken(SyntaxKind.DoubleQuoteToken),
                            default(SyntaxList<SyntaxToken>),
                            SyntaxFactory.MissingToken(SyntaxKind.DoubleQuoteToken));
                }
            }

            SyntaxToken startQuote;
            SyntaxToken endQuote;
            string attrNameText = attrName.LocalName.ValueText;
            bool hasNoPrefix = attrName.Prefix == null;
            if (hasNoPrefix && DocumentationCommentXmlNames.AttributeEquals(attrNameText, DocumentationCommentXmlNames.CrefAttributeName) &&
                !IsVerbatimCref())
            {
                CrefSyntax cref;
                this.ParseCrefAttribute(out startQuote, out cref, out endQuote);
                return SyntaxFactory.XmlCrefAttribute(attrName, equals, startQuote, cref, endQuote);
            }
            else if (hasNoPrefix && DocumentationCommentXmlNames.AttributeEquals(attrNameText, DocumentationCommentXmlNames.NameAttributeName) &&
                XmlElementSupportsNameAttribute(elementName))
            {
                IdentifierNameSyntax identifier;
                this.ParseNameAttribute(out startQuote, out identifier, out endQuote);
                return SyntaxFactory.XmlNameAttribute(attrName, equals, startQuote, identifier, endQuote);
            }
            else
            {
                var textTokens = _pool.Allocate<SyntaxToken>();
                try
                {
                    this.ParseXmlAttributeText(out startQuote, textTokens, out endQuote);
                    return SyntaxFactory.XmlTextAttribute(attrName, equals, startQuote, textTokens, endQuote);
                }
                finally
                {
                    _pool.Free(textTokens);
                }
            }
        }

        private static bool XmlElementSupportsNameAttribute(XmlNameSyntax elementName)
        {
            if (elementName.Prefix != null)
            {
                return false;
            }

            string localName = elementName.LocalName.ValueText;
            return
                DocumentationCommentXmlNames.ElementEquals(localName, DocumentationCommentXmlNames.ParameterElementName) ||
                DocumentationCommentXmlNames.ElementEquals(localName, DocumentationCommentXmlNames.ParameterReferenceElementName) ||
                DocumentationCommentXmlNames.ElementEquals(localName, DocumentationCommentXmlNames.TypeParameterElementName) ||
                DocumentationCommentXmlNames.ElementEquals(localName, DocumentationCommentXmlNames.TypeParameterReferenceElementName);
        }

        private bool IsVerbatimCref()
        {
            // As in XMLDocWriter::ReplaceReferences, if the first character of the value is not colon and the second character
            // is, then don't process the cref - just emit it as-is.
            bool isVerbatim = false;

            var resetPoint = this.GetResetPoint();

            SyntaxToken openQuote = EatToken(this.CurrentToken.Kind == SyntaxKind.SingleQuoteToken
                ? SyntaxKind.SingleQuoteToken
                : SyntaxKind.DoubleQuoteToken);

            // NOTE: Don't need to save mode, since we're already using a reset point.
            this.SetMode(LexerMode.XmlCharacter);

            SyntaxToken current = this.CurrentToken;
            if ((current.Kind == SyntaxKind.XmlTextLiteralToken || current.Kind == SyntaxKind.XmlEntityLiteralToken) &&
                current.ValueText != SyntaxFacts.GetText(openQuote.Kind) &&
                current.ValueText != ":")
            {
                EatToken();

                current = this.CurrentToken;
                if ((current.Kind == SyntaxKind.XmlTextLiteralToken || current.Kind == SyntaxKind.XmlEntityLiteralToken) &&
                    current.ValueText == ":")
                {
                    isVerbatim = true;
                }
            }

            this.Reset(ref resetPoint);
            this.Release(ref resetPoint);

            return isVerbatim;
        }

        private void ParseCrefAttribute(out SyntaxToken startQuote, out CrefSyntax cref, out SyntaxToken endQuote)
        {
            startQuote = ParseXmlAttributeStartQuote();
            SyntaxKind quoteKind = startQuote.Kind;

            {
                var saveMode = this.SetMode(quoteKind == SyntaxKind.SingleQuoteToken
                    ? LexerMode.XmlCrefQuote
                    : LexerMode.XmlCrefDoubleQuote);

                cref = this.ParseCrefAttributeValue();

                this.ResetMode(saveMode);
            }

            endQuote = ParseXmlAttributeEndQuote(quoteKind);
        }

        private void ParseNameAttribute(out SyntaxToken startQuote, out IdentifierNameSyntax identifier, out SyntaxToken endQuote)
        {
            startQuote = ParseXmlAttributeStartQuote();
            SyntaxKind quoteKind = startQuote.Kind;

            {
                var saveMode = this.SetMode(quoteKind == SyntaxKind.SingleQuoteToken
                    ? LexerMode.XmlNameQuote
                    : LexerMode.XmlNameDoubleQuote);

                identifier = this.ParseNameAttributeValue();

                this.ResetMode(saveMode);
            }

            endQuote = ParseXmlAttributeEndQuote(quoteKind);
        }

        private void ParseXmlAttributeText(out SyntaxToken startQuote, SyntaxListBuilder<SyntaxToken> textTokens, out SyntaxToken endQuote)
        {
            startQuote = ParseXmlAttributeStartQuote();
            SyntaxKind quoteKind = startQuote.Kind;

            // NOTE: Being a bit sneaky here - if the width isn't 0, we consumed something else in
            // place of the quote and we should continue parsing the attribute.
            if (startQuote.IsMissing && startQuote.FullWidth == 0)
            {
                endQuote = SyntaxFactory.MissingToken(quoteKind);
            }
            else
            {
                var saveMode = this.SetMode(quoteKind == SyntaxKind.SingleQuoteToken
                    ? LexerMode.XmlAttributeTextQuote
                    : LexerMode.XmlAttributeTextDoubleQuote);

                while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
                    || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken
                    || this.CurrentToken.Kind == SyntaxKind.XmlEntityLiteralToken
                    || this.CurrentToken.Kind == SyntaxKind.LessThanToken)
                {
                    var token = this.EatToken();
                    if (token.Kind == SyntaxKind.LessThanToken)
                    {
                        // TODO: It is possible that a non-literal gets in here. <, specifically. Is that ok?
                        token = this.WithXmlParseError(token, XmlParseErrorCode.XML_LessThanInAttributeValue);
                    }

                    textTokens.Add(token);
                }

                this.ResetMode(saveMode);

                // NOTE: This will never consume a non-ascii quote, since non-ascii quotes
                // are legal in the attribute value and are consumed by the preceding loop.
                endQuote = ParseXmlAttributeEndQuote(quoteKind);
            }
        }

        private SyntaxToken ParseXmlAttributeStartQuote()
        {
            if (IsNonAsciiQuotationMark(this.CurrentToken))
            {
                return SkipNonAsciiQuotationMark();
            }

            var quoteKind = this.CurrentToken.Kind == SyntaxKind.SingleQuoteToken
                ? SyntaxKind.SingleQuoteToken
                : SyntaxKind.DoubleQuoteToken;

            var startQuote = this.EatToken(quoteKind, reportError: false);
            if (startQuote.IsMissing)
            {
                startQuote = this.WithXmlParseError(startQuote, XmlParseErrorCode.XML_StringLiteralNoStartQuote);
            }
            return startQuote;
        }

        private SyntaxToken ParseXmlAttributeEndQuote(SyntaxKind quoteKind)
        {
            if (IsNonAsciiQuotationMark(this.CurrentToken))
            {
                return SkipNonAsciiQuotationMark();
            }

            var endQuote = this.EatToken(quoteKind, reportError: false);
            if (endQuote.IsMissing)
            {
                endQuote = this.WithXmlParseError(endQuote, XmlParseErrorCode.XML_StringLiteralNoEndQuote);
            }
            return endQuote;
        }

        private SyntaxToken SkipNonAsciiQuotationMark()
        {
            var quote = SyntaxFactory.MissingToken(SyntaxKind.DoubleQuoteToken);
            quote = AddTrailingSkippedSyntax(quote, EatToken());
            quote = this.WithXmlParseError(quote, XmlParseErrorCode.XML_StringLiteralNonAsciiQuote);
            return quote;
        }

        /// <summary>
        /// These aren't acceptable in place of ASCII quotation marks in XML, 
        /// but we want to consume them (and produce an appropriate error) if
        /// they occur in a place where a quotation mark is legal.
        /// </summary>
        private static bool IsNonAsciiQuotationMark(SyntaxToken token)
        {
            return token.Text.Length == 1 && SyntaxFacts.IsNonAsciiQuotationMark(token.Text[0]);
        }

        private XmlNameSyntax ParseXmlName()
        {
            var id = this.EatToken(SyntaxKind.IdentifierToken);
            XmlPrefixSyntax prefix = null;
            if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                var colon = this.EatToken();

                int prefixTrailingWidth = id.GetTrailingTriviaWidth();
                int colonLeadingWidth = colon.GetLeadingTriviaWidth();

                if (prefixTrailingWidth > 0 || colonLeadingWidth > 0)
                {
                    // NOTE: offset is relative to full-span start of colon (i.e. before leading trivia).
                    int offset = -prefixTrailingWidth;
                    int width = prefixTrailingWidth + colonLeadingWidth;
                    colon = WithAdditionalDiagnostics(colon, new XmlSyntaxDiagnosticInfo(offset, width, XmlParseErrorCode.XML_InvalidWhitespace));
                }

                prefix = SyntaxFactory.XmlPrefix(id, colon);
                id = this.EatToken(SyntaxKind.IdentifierToken);

                int colonTrailingWidth = colon.GetTrailingTriviaWidth();
                int localNameLeadingWidth = id.GetLeadingTriviaWidth();
                if (colonTrailingWidth > 0 || localNameLeadingWidth > 0)
                {
                    // NOTE: offset is relative to full-span start of identifier (i.e. before leading trivia).
                    int offset = -colonTrailingWidth;
                    int width = colonTrailingWidth + localNameLeadingWidth;
                    id = WithAdditionalDiagnostics(id, new XmlSyntaxDiagnosticInfo(offset, width, XmlParseErrorCode.XML_InvalidWhitespace));

                    // CONSIDER: Another interpretation would be that the local part of this name is a missing identifier and the identifier
                    // we've just consumed is actually part of something else (e.g. an attribute name).
                }
            }

            return SyntaxFactory.XmlName(prefix, id);
        }

        private XmlCommentSyntax ParseXmlComment()
        {
            var lessThanExclamationMinusMinusToken = this.EatToken(SyntaxKind.XmlCommentStartToken);
            var saveMode = this.SetMode(LexerMode.XmlCommentText);
            var textTokens = _pool.Allocate<SyntaxToken>();
            while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
                || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken
                || this.CurrentToken.Kind == SyntaxKind.MinusMinusToken)
            {
                var token = this.EatToken();
                if (token.Kind == SyntaxKind.MinusMinusToken)
                {
                    // TODO: It is possible that a non-literal gets in here. --, specifically. Is that ok?
                    token = this.WithXmlParseError(token, XmlParseErrorCode.XML_IncorrectComment);
                }

                textTokens.Add(token);
            }

            var list = textTokens.ToList();
            _pool.Free(textTokens);

            var minusMinusGreaterThanToken = this.EatToken(SyntaxKind.XmlCommentEndToken);
            this.ResetMode(saveMode);
            return SyntaxFactory.XmlComment(lessThanExclamationMinusMinusToken, list, minusMinusGreaterThanToken);
        }

        private XmlCDataSectionSyntax ParseXmlCDataSection()
        {
            var startCDataToken = this.EatToken(SyntaxKind.XmlCDataStartToken);
            var saveMode = this.SetMode(LexerMode.XmlCDataSectionText);
            var textTokens = new SyntaxListBuilder<SyntaxToken>(10);
            while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
               || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken)
            {
                textTokens.Add(this.EatToken());
            }

            var endCDataToken = this.EatToken(SyntaxKind.XmlCDataEndToken);
            this.ResetMode(saveMode);
            return SyntaxFactory.XmlCDataSection(startCDataToken, textTokens, endCDataToken);
        }

        private XmlProcessingInstructionSyntax ParseXmlProcessingInstruction()
        {
            var startProcessingInstructionToken = this.EatToken(SyntaxKind.XmlProcessingInstructionStartToken);
            var saveMode = this.SetMode(LexerMode.XmlElementTag); //this mode accepts names
            var name = this.ParseXmlName();


            // NOTE: The XML spec says that name cannot be "xml" (case-insensitive comparison), 
            // but Dev10 does not enforce this.

            this.SetMode(LexerMode.XmlProcessingInstructionText); //this mode consumes text
            var textTokens = new SyntaxListBuilder<SyntaxToken>(10);
            while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
               || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken)
            {
                var textToken = this.EatToken();

                // NOTE: The XML spec says that the each text token must begin with a whitespace
                // character, but Dev10 does not enforce this.

                textTokens.Add(textToken);
            }

            var endProcessingInstructionToken = this.EatToken(SyntaxKind.XmlProcessingInstructionEndToken);
            this.ResetMode(saveMode);
            return SyntaxFactory.XmlProcessingInstruction(startProcessingInstructionToken, name, textTokens, endProcessingInstructionToken);
        }

        protected override SyntaxDiagnosticInfo GetExpectedTokenError(SyntaxKind expected, SyntaxKind actual, int offset, int length)
        {
            // NOTE: There are no errors in crefs - only warnings.  We accomplish this by wrapping every diagnostic in ErrorCode.WRN_ErrorOverride.
            if (InCref)
            {
                SyntaxDiagnosticInfo rawInfo = base.GetExpectedTokenError(expected, actual, offset, length);
                SyntaxDiagnosticInfo crefInfo = new SyntaxDiagnosticInfo(rawInfo.Offset, rawInfo.Width, ErrorCode.WRN_ErrorOverride, rawInfo, rawInfo.Code);
                return crefInfo;
            }

            switch (expected)
            {
                case SyntaxKind.IdentifierToken:
                    return new XmlSyntaxDiagnosticInfo(offset, length, XmlParseErrorCode.XML_ExpectedIdentifier);

                default:
                    return new XmlSyntaxDiagnosticInfo(offset, length, XmlParseErrorCode.XML_InvalidToken, SyntaxFacts.GetText(actual));
            }
        }

        protected override SyntaxDiagnosticInfo GetExpectedTokenError(SyntaxKind expected, SyntaxKind actual)
        {
            // NOTE: There are no errors in crefs - only warnings.  We accomplish this by wrapping every diagnostic in ErrorCode.WRN_ErrorOverride.
            if (InCref)
            {
                int offset, width;
                this.GetDiagnosticSpanForMissingToken(out offset, out width);

                return GetExpectedTokenError(expected, actual, offset, width);
            }

            switch (expected)
            {
                case SyntaxKind.IdentifierToken:
                    return new XmlSyntaxDiagnosticInfo(XmlParseErrorCode.XML_ExpectedIdentifier);

                default:
                    return new XmlSyntaxDiagnosticInfo(XmlParseErrorCode.XML_InvalidToken, SyntaxFacts.GetText(actual));
            }
        }

        private TNode WithXmlParseError<TNode>(TNode node, XmlParseErrorCode code) where TNode : CSharpSyntaxNode
        {
            return WithAdditionalDiagnostics(node, new XmlSyntaxDiagnosticInfo(0, node.Width, code));
        }

        private TNode WithXmlParseError<TNode>(TNode node, XmlParseErrorCode code, params string[] args) where TNode : CSharpSyntaxNode
        {
            return WithAdditionalDiagnostics(node, new XmlSyntaxDiagnosticInfo(0, node.Width, code, args));
        }

        private SyntaxToken WithXmlParseError(SyntaxToken node, XmlParseErrorCode code, params string[] args)
        {
            return WithAdditionalDiagnostics(node, new XmlSyntaxDiagnosticInfo(0, node.Width, code, args));
        }

        protected override TNode WithAdditionalDiagnostics<TNode>(TNode node, params DiagnosticInfo[] diagnostics)
        {
            // Don't attach any diagnostics to syntax nodes within a documentation comment if the DocumentationMode
            // is not at least Diagnose.
            return Options.DocumentationMode >= DocumentationMode.Diagnose
                ? base.WithAdditionalDiagnostics<TNode>(node, diagnostics)
                : node;
        }

        #region Cref

        /// <summary>
        /// ACASEY: This grammar is derived from the behavior and sources of the native compiler.
        /// Tokens start with underscores (I've cheated for _PredefinedTypeToken, which is not actually a
        /// SyntaxKind), "*" indicates "0 or more", "?" indicates "0 or 1", and parentheses are for grouping.
        /// 
        /// Cref	 			= CrefType _DotToken CrefMember
        /// 					| CrefType
        /// 					| CrefMember
        ///                     | CrefFirstType _OpenParenToken CrefParameterList? _CloseParenToken
        /// CrefName			= _IdentifierToken (_LessThanToken _IdentifierToken (_CommaToken _IdentifierToken)* _GreaterThanToken)?
        /// CrefFirstType 		= ((_IdentifierToken _ColonColonToken)? CrefName) 
        ///                     | _PredefinedTypeToken
        /// CrefType 			= CrefFirstType (_DotToken CrefName)*
        /// CrefMember 			= CrefName (_OpenParenToken CrefParameterList? _CloseParenToken)?
        /// 					| _ThisKeyword (_OpenBracketToken CrefParameterList _CloseBracketToken)?
        /// 					| _OperatorKeyword _OperatorToken (_OpenParenToken CrefParameterList? _CloseParenToken)?
        /// 					| (_ImplicitKeyword | _ExplicitKeyword) _OperatorKeyword CrefParameterType (_OpenParenToken CrefParameterList? _CloseParenToken)?
        /// CrefParameterList	= CrefParameter (_CommaToken CrefParameter)*
        /// CrefParameter		= (_RefKeyword | _OutKeyword)? CrefParameterType
        /// CrefParameterType	= CrefParameterType2 _QuestionToken? _AsteriskToken* (_OpenBracketToken _CommaToken* _CloseBracketToken)*
        /// CrefParameterType2 	= (((_IdentifierToken _ColonColonToken)? CrefParameterType3) | _PredefinedTypeToken) (_DotToken CrefParameterType3)*
        /// CrefParameterType3 	= _IdentifierToken (_LessThanToken CrefParameterType (_CommaToken CrefParameterType)* _GreaterThanToken)?
        ///
        /// NOTE: type parameters, not type arguments
        /// NOTE: the first production of Cref is preferred to the other two
        /// NOTE: pointer, array, and nullable types only work in parameters
        /// NOTE: CrefParameterType2 and CrefParameterType3 correspond to CrefType and CrefName, respectively.
        /// Since the only difference is that they accept non-identifier type arguments, this is accomplished
        /// using parameters on the parsing methods (rather than whole new methods).
        /// </summary>
        private CrefSyntax ParseCrefAttributeValue()
        {
            CrefSyntax result;

            TypeSyntax type = ParseCrefType(typeArgumentsMustBeIdentifiers: true, checkForMember: true);
            if (type == null)
            {
                result = ParseMemberCref();
            }
            else if (IsEndOfCrefAttribute)
            {
                result = SyntaxFactory.TypeCref(type);
            }
            else if (type.Kind != SyntaxKind.QualifiedName && this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                // Special case for crefs like "string()" and "A::B()".
                CrefParameterListSyntax parameters = ParseCrefParameterList();
                result = SyntaxFactory.NameMemberCref(type, parameters);
            }
            else
            {
                SyntaxToken dot = EatToken(SyntaxKind.DotToken);
                MemberCrefSyntax member = ParseMemberCref();
                result = SyntaxFactory.QualifiedCref(type, dot, member);
            }

            bool needOverallError = !IsEndOfCrefAttribute || result.ContainsDiagnostics;

            if (!IsEndOfCrefAttribute)
            {
                var badTokens = _pool.Allocate<SyntaxToken>();
                while (!IsEndOfCrefAttribute)
                {
                    badTokens.Add(this.EatToken());
                }
                result = AddTrailingSkippedSyntax(result, badTokens.ToListNode());
                _pool.Free(badTokens);
            }

            if (needOverallError)
            {
                result = this.AddError(result, ErrorCode.WRN_BadXMLRefSyntax, result.ToFullString());
            }

            return result;
        }

        /// <summary>
        /// Parse the custom cref syntax for a named member (method, property, etc),
        /// an indexer, an overloadable operator, or a user-defined conversion.
        /// </summary>
        private MemberCrefSyntax ParseMemberCref()
        {
            switch (CurrentToken.Kind)
            {
                case SyntaxKind.ThisKeyword:
                    return ParseIndexerMemberCref();
                case SyntaxKind.OperatorKeyword:
                    return ParseOperatorMemberCref();
                case SyntaxKind.ExplicitKeyword:
                case SyntaxKind.ImplicitKeyword:
                    return ParseConversionOperatorMemberCref();
                default:
                    return ParseNameMemberCref();
            }
        }

        /// <summary>
        /// Parse a named member (method, property, etc), with optional type
        /// parameters and regular parameters.
        /// </summary>
        private NameMemberCrefSyntax ParseNameMemberCref()
        {
            SimpleNameSyntax name = ParseCrefName(typeArgumentsMustBeIdentifiers: true);
            CrefParameterListSyntax parameters = ParseCrefParameterList();

            return SyntaxFactory.NameMemberCref(name, parameters);
        }

        /// <summary>
        /// Parse an indexer member, with optional parameters.
        /// </summary>
        private IndexerMemberCrefSyntax ParseIndexerMemberCref()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.ThisKeyword);
            SyntaxToken thisKeyword = EatToken();
            CrefBracketedParameterListSyntax parameters = ParseBracketedCrefParameterList();

            return SyntaxFactory.IndexerMemberCref(thisKeyword, parameters);
        }

        /// <summary>
        /// Parse an overloadable operator, with optional parameters.
        /// </summary>
        private OperatorMemberCrefSyntax ParseOperatorMemberCref()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.OperatorKeyword);
            SyntaxToken operatorKeyword = EatToken();

            SyntaxToken operatorToken;

            if (SyntaxFacts.IsAnyOverloadableOperator(CurrentToken.Kind))
            {
                operatorToken = EatToken();
            }
            else
            {
                operatorToken = SyntaxFactory.MissingToken(SyntaxKind.PlusToken);

                // Grab the offset and width before we consume the invalid keyword and change our position.
                int offset;
                int width;
                GetDiagnosticSpanForMissingToken(out offset, out width);

                if (SyntaxFacts.IsUnaryOperatorDeclarationToken(CurrentToken.Kind) || SyntaxFacts.IsBinaryExpressionOperatorToken(CurrentToken.Kind))
                {
                    operatorToken = AddTrailingSkippedSyntax(operatorToken, EatToken());
                }

                SyntaxDiagnosticInfo rawInfo = new SyntaxDiagnosticInfo(offset, width, ErrorCode.ERR_OvlOperatorExpected);
                SyntaxDiagnosticInfo crefInfo = new SyntaxDiagnosticInfo(offset, width, ErrorCode.WRN_ErrorOverride, rawInfo, rawInfo.Code);

                operatorToken = WithAdditionalDiagnostics(operatorToken, crefInfo);
            }

            // Have to fake >> because it looks like the closing of nested type parameter lists (e.g. A<A<T>>).
            // Have to fake >= so the lexer doesn't mishandle >>=.
            if (operatorToken.Kind == SyntaxKind.GreaterThanToken && operatorToken.GetTrailingTriviaWidth() == 0 && CurrentToken.GetLeadingTriviaWidth() == 0)
            {
                if (CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                {
                    var operatorToken2 = this.EatToken();
                    operatorToken = SyntaxFactory.Token(
                        operatorToken.GetLeadingTrivia(),
                        SyntaxKind.GreaterThanGreaterThanToken,
                        operatorToken.Text + operatorToken2.Text,
                        operatorToken.ValueText + operatorToken2.ValueText,
                        operatorToken2.GetTrailingTrivia());
                }
                else if (CurrentToken.Kind == SyntaxKind.EqualsToken)
                {
                    var operatorToken2 = this.EatToken();
                    operatorToken = SyntaxFactory.Token(
                        operatorToken.GetLeadingTrivia(),
                        SyntaxKind.GreaterThanEqualsToken,
                        operatorToken.Text + operatorToken2.Text,
                        operatorToken.ValueText + operatorToken2.ValueText,
                        operatorToken2.GetTrailingTrivia());
                }
                else if (CurrentToken.Kind == SyntaxKind.GreaterThanEqualsToken)
                {
                    var operatorToken2 = this.EatToken();
                    var nonOverloadableOperator = SyntaxFactory.Token(
                        operatorToken.GetLeadingTrivia(),
                        SyntaxKind.GreaterThanGreaterThanEqualsToken,
                        operatorToken.Text + operatorToken2.Text,
                        operatorToken.ValueText + operatorToken2.ValueText,
                        operatorToken2.GetTrailingTrivia());

                    operatorToken = SyntaxFactory.MissingToken(SyntaxKind.PlusToken);

                    // Add non-overloadable operator as skipped token.
                    operatorToken = AddTrailingSkippedSyntax(operatorToken, nonOverloadableOperator);

                    // Add an appropriate diagnostic.
                    const int offset = 0;
                    int width = nonOverloadableOperator.Width;
                    SyntaxDiagnosticInfo rawInfo = new SyntaxDiagnosticInfo(offset, width, ErrorCode.ERR_OvlOperatorExpected);
                    SyntaxDiagnosticInfo crefInfo = new SyntaxDiagnosticInfo(offset, width, ErrorCode.WRN_ErrorOverride, rawInfo, rawInfo.Code);
                    operatorToken = WithAdditionalDiagnostics(operatorToken, crefInfo);
                }
            }

            Debug.Assert(SyntaxFacts.IsAnyOverloadableOperator(operatorToken.Kind));

            CrefParameterListSyntax parameters = ParseCrefParameterList();

            return SyntaxFactory.OperatorMemberCref(operatorKeyword, operatorToken, parameters);
        }

        /// <summary>
        /// Parse a user-defined conversion, with optional parameters.
        /// </summary>
        private ConversionOperatorMemberCrefSyntax ParseConversionOperatorMemberCref()
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.ExplicitKeyword ||
                CurrentToken.Kind == SyntaxKind.ImplicitKeyword);
            SyntaxToken implicitOrExplicit = EatToken();

            SyntaxToken operatorKeyword = EatToken(SyntaxKind.OperatorKeyword);

            TypeSyntax type = ParseCrefType(typeArgumentsMustBeIdentifiers: false);

            CrefParameterListSyntax parameters = ParseCrefParameterList();

            return SyntaxFactory.ConversionOperatorMemberCref(implicitOrExplicit, operatorKeyword, type, parameters);
        }

        /// <summary>
        /// Parse a parenthesized parameter list.
        /// </summary>
        private CrefParameterListSyntax ParseCrefParameterList()
        {
            return (CrefParameterListSyntax)ParseBaseCrefParameterList(useSquareBrackets: false);
        }

        /// <summary>
        /// Parse a bracketed parameter list.
        /// </summary>
        private CrefBracketedParameterListSyntax ParseBracketedCrefParameterList()
        {
            return (CrefBracketedParameterListSyntax)ParseBaseCrefParameterList(useSquareBrackets: true);
        }

        /// <summary>
        /// Parse the parameter list (if any) of a cref member (name, indexer, operator, or conversion).
        /// </summary>
        private BaseCrefParameterListSyntax ParseBaseCrefParameterList(bool useSquareBrackets)
        {
            SyntaxKind openKind = useSquareBrackets ? SyntaxKind.OpenBracketToken : SyntaxKind.OpenParenToken;
            SyntaxKind closeKind = useSquareBrackets ? SyntaxKind.CloseBracketToken : SyntaxKind.CloseParenToken;

            if (CurrentToken.Kind != openKind)
            {
                return null;
            }

            SyntaxToken open = EatToken(openKind);

            var list = _pool.AllocateSeparated<CrefParameterSyntax>();
            try
            {
                while (CurrentToken.Kind == SyntaxKind.CommaToken || IsPossibleCrefParameter())
                {
                    list.Add(ParseCrefParameter());

                    if (CurrentToken.Kind != closeKind)
                    {
                        SyntaxToken comma = EatToken(SyntaxKind.CommaToken);
                        if (!comma.IsMissing || IsPossibleCrefParameter())
                        {
                            // Only do this if it won't be last in the list.
                            list.AddSeparator(comma);
                        }
                        else
                        {
                            // How could this scenario arise?  If it does, just expand the if-condition.
                            Debug.Assert(CurrentToken.Kind != SyntaxKind.CommaToken);
                        }
                    }
                }

                // NOTE: nothing follows a cref parameter list, so there's no reason to recover here.
                // Just let the cref-level recovery code handle any remaining tokens.

                SyntaxToken close = EatToken(closeKind);

                return useSquareBrackets
                    ? (BaseCrefParameterListSyntax)SyntaxFactory.CrefBracketedParameterList(open, list, close)
                    : SyntaxFactory.CrefParameterList(open, list, close);
            }
            finally
            {
                _pool.Free(list);
            }
        }

        /// <summary>
        /// True if the current token could be the beginning of a cref parameter.
        /// </summary>
        private bool IsPossibleCrefParameter()
        {
            SyntaxKind kind = this.CurrentToken.Kind;
            switch (kind)
            {
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.IdentifierToken:
                    return true;
                default:
                    return SyntaxFacts.IsPredefinedType(kind);
            }
        }

        /// <summary>
        /// Parse an element of a cref parameter list.
        /// </summary>
        /// <remarks>
        /// "ref" and "out" work, but "params", "this", and "__arglist" don't.
        /// </remarks>
        private CrefParameterSyntax ParseCrefParameter()
        {
            SyntaxToken refKindOpt = null;
            switch (CurrentToken.Kind)
            {
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                    refKindOpt = EatToken();
                    break;
            }

            TypeSyntax type = ParseCrefType(typeArgumentsMustBeIdentifiers: false);
            return SyntaxFactory.CrefParameter(refKindOpt, type);
        }

        /// <summary>
        /// Parse an identifier, optionally followed by an angle-bracketed list of type parameters.
        /// </summary>
        /// <param name="typeArgumentsMustBeIdentifiers">True to give an error when a non-identifier
        /// type argument is seen, false to accept.  No change in the shape of the tree.</param>
        private SimpleNameSyntax ParseCrefName(bool typeArgumentsMustBeIdentifiers)
        {
            SyntaxToken identifierToken = EatToken(SyntaxKind.IdentifierToken);

            if (CurrentToken.Kind != SyntaxKind.LessThanToken)
            {
                return SyntaxFactory.IdentifierName(identifierToken);
            }

            var open = EatToken();

            var list = _pool.AllocateSeparated<TypeSyntax>();
            try
            {
                while (true)
                {
                    TypeSyntax typeSyntax = ParseCrefType(typeArgumentsMustBeIdentifiers);

                    if (typeArgumentsMustBeIdentifiers && typeSyntax.Kind != SyntaxKind.IdentifierName)
                    {
                        typeSyntax = this.AddError(typeSyntax, ErrorCode.WRN_ErrorOverride,
                            new SyntaxDiagnosticInfo(ErrorCode.ERR_TypeParamMustBeIdentifier), $"{(int)ErrorCode.ERR_TypeParamMustBeIdentifier:d4}");
                    }

                    list.Add(typeSyntax);

                    var currentKind = CurrentToken.Kind;
                    if (currentKind == SyntaxKind.CommaToken || currentKind == SyntaxKind.IdentifierToken ||
                        SyntaxFacts.IsPredefinedType(CurrentToken.Kind))
                    {
                        // NOTE: if the current token is an identifier or predefined type, then we're
                        // actually inserting a missing commas.
                        list.AddSeparator(EatToken(SyntaxKind.CommaToken));
                    }
                    else
                    {
                        break;
                    }
                }

                SyntaxToken close = EatToken(SyntaxKind.GreaterThanToken);

                open = CheckFeatureAvailability(open, MessageID.IDS_FeatureGenerics, forceWarning: true);

                return SyntaxFactory.GenericName(identifierToken, SyntaxFactory.TypeArgumentList(open, list, close));
            }
            finally
            {
                _pool.Free(list);
            }
        }

        /// <summary>
        /// Parse a type.  May include an alias, a predefined type, and/or a qualified name.
        /// </summary>
        /// <remarks>
        /// Pointer, nullable, or array types are only allowed if <paramref name="typeArgumentsMustBeIdentifiers"/> is false.
        /// Leaves a dot and a name unconsumed if the name is not followed by another dot
        /// and checkForMember is true.
        /// </remarks>
        /// <param name="typeArgumentsMustBeIdentifiers">True to give an error when a non-identifier
        /// type argument is seen, false to accept.  No change in the shape of the tree.</param>
        /// <param name="checkForMember">True means that the last name should not be consumed
        /// if it is followed by a parameter list.</param>
        private TypeSyntax ParseCrefType(bool typeArgumentsMustBeIdentifiers, bool checkForMember = false)
        {
            TypeSyntax typeWithoutSuffix = ParseCrefTypeHelper(typeArgumentsMustBeIdentifiers, checkForMember);
            return typeArgumentsMustBeIdentifiers
                ? typeWithoutSuffix
                : ParseCrefTypeSuffix(typeWithoutSuffix);
        }

        /// <summary>
        /// Parse a type.  May include an alias, a predefined type, and/or a qualified name.
        /// </summary>
        /// <remarks>
        /// No pointer, nullable, or array types.
        /// Leaves a dot and a name unconsumed if the name is not followed by another dot
        /// and checkForMember is true.
        /// </remarks>
        /// <param name="typeArgumentsMustBeIdentifiers">True to give an error when a non-identifier
        /// type argument is seen, false to accept.  No change in the shape of the tree.</param>
        /// <param name="checkForMember">True means that the last name should not be consumed
        /// if it is followed by a parameter list.</param>
        private TypeSyntax ParseCrefTypeHelper(bool typeArgumentsMustBeIdentifiers, bool checkForMember = false)
        {
            NameSyntax leftName;

            if (SyntaxFacts.IsPredefinedType(CurrentToken.Kind))
            {
                // e.g. "int"
                // NOTE: a predefined type will not fit into a NameSyntax, so we'll return
                // immediately.  The upshot is that you can only dot into a predefined type
                // once (e.g. not "int.A.B"), which is fine because we know that none of them
                // have nested types.
                return SyntaxFactory.PredefinedType(EatToken());
            }
            else if (CurrentToken.Kind == SyntaxKind.IdentifierToken && PeekToken(1).Kind == SyntaxKind.ColonColonToken)
            {
                // e.g. "A::B"
                SyntaxToken alias = EatToken();
                if (alias.ContextualKind == SyntaxKind.GlobalKeyword)
                {
                    alias = ConvertToKeyword(alias);
                }

                alias = CheckFeatureAvailability(alias, MessageID.IDS_FeatureGlobalNamespace, forceWarning: true);

                SyntaxToken colonColon = EatToken();
                SimpleNameSyntax name = ParseCrefName(typeArgumentsMustBeIdentifiers);
                leftName = SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(alias), colonColon, name);
            }
            else
            {
                // e.g. "A"
                ResetPoint resetPoint = GetResetPoint();
                leftName = ParseCrefName(typeArgumentsMustBeIdentifiers);
                if (checkForMember && (leftName.IsMissing || CurrentToken.Kind != SyntaxKind.DotToken))
                {
                    // If this isn't the first part of a dotted name, then we prefer to represent it
                    // as a MemberCrefSyntax.
                    this.Reset(ref resetPoint);
                    this.Release(ref resetPoint);

                    return null;
                }
                this.Release(ref resetPoint);
            }

            while (CurrentToken.Kind == SyntaxKind.DotToken)
            {
                // NOTE: we make a lot of these, but we'll reset, at most, one time.
                ResetPoint resetPoint = GetResetPoint();

                SyntaxToken dot = EatToken();

                SimpleNameSyntax rightName = ParseCrefName(typeArgumentsMustBeIdentifiers);

                if (checkForMember && (rightName.IsMissing || CurrentToken.Kind != SyntaxKind.DotToken))
                {
                    this.Reset(ref resetPoint); // Go back to before the dot - it must have been the trailing dot.
                    this.Release(ref resetPoint);

                    return leftName;
                }

                this.Release(ref resetPoint);

                leftName = SyntaxFactory.QualifiedName(leftName, dot, rightName);
            }

            return leftName;
        }

        /// <summary>
        /// Once the name part of a type (including type parameter/argument lists) is parsed,
        /// we need to consume ?, *, and rank specifiers.
        /// </summary>
        private TypeSyntax ParseCrefTypeSuffix(TypeSyntax type)
        {
            if (CurrentToken.Kind == SyntaxKind.QuestionToken)
            {
                type = SyntaxFactory.NullableType(type, EatToken());
            }

            while (CurrentToken.Kind == SyntaxKind.AsteriskToken)
            {
                type = SyntaxFactory.PointerType(type, EatToken());
            }

            if (CurrentToken.Kind == SyntaxKind.OpenBracketToken)
            {
                var omittedArraySizeExpressionInstance = SyntaxFactory.OmittedArraySizeExpression(SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken));
                var rankList = _pool.Allocate<ArrayRankSpecifierSyntax>();
                try
                {
                    while (CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                    {
                        SyntaxToken open = EatToken();
                        var dimensionList = _pool.AllocateSeparated<ExpressionSyntax>();
                        try
                        {
                            while (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
                            {
                                if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                                {
                                    // NOTE: trivia will be attached to comma, not omitted array size
                                    dimensionList.Add(omittedArraySizeExpressionInstance);
                                    dimensionList.AddSeparator(this.EatToken());
                                }
                                else
                                {
                                    // CONSIDER: if we expect people to try to put expressions in between
                                    // the commas, then it might be more reasonable to recover by skipping
                                    // tokens until we hit a CloseBracketToken (or some other terminator).
                                    break;
                                }
                            }

                            // Don't end on a comma.
                            // If the omitted size would be the only element, then skip it unless sizes were expected.
                            if ((dimensionList.Count & 1) == 0)
                            {
                                dimensionList.Add(omittedArraySizeExpressionInstance);
                            }

                            // Eat the close brace and we're done.
                            var close = this.EatToken(SyntaxKind.CloseBracketToken);

                            rankList.Add(SyntaxFactory.ArrayRankSpecifier(open, dimensionList, close));
                        }
                        finally
                        {
                            _pool.Free(dimensionList);
                        }
                    }

                    type = SyntaxFactory.ArrayType(type, rankList);
                }
                finally
                {
                    _pool.Free(rankList);
                }
            }
            return type;
        }

        /// <summary>
        /// Ends at appropriate quotation mark, EOF, or EndOfDocumentationComment.
        /// </summary>
        private bool IsEndOfCrefAttribute
        {
            get
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.SingleQuoteToken:
                        return (this.Mode & LexerMode.XmlCrefQuote) == LexerMode.XmlCrefQuote;
                    case SyntaxKind.DoubleQuoteToken:
                        return (this.Mode & LexerMode.XmlCrefDoubleQuote) == LexerMode.XmlCrefDoubleQuote;
                    case SyntaxKind.EndOfFileToken:
                    case SyntaxKind.EndOfDocumentationCommentToken:
                        return true;
                    case SyntaxKind.BadToken:
                        // If it's a real '<' (not &lt;, etc), then we assume it's the beginning
                        // of the next XML element.
                        return CurrentToken.Text == SyntaxFacts.GetText(SyntaxKind.LessThanToken) ||
                            IsNonAsciiQuotationMark(CurrentToken);
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Convenience method for checking the mode.
        /// </summary>
        private bool InCref
        {
            get
            {
                switch (this.Mode & (LexerMode.XmlCrefDoubleQuote | LexerMode.XmlCrefQuote))
                {
                    case LexerMode.XmlCrefQuote:
                    case LexerMode.XmlCrefDoubleQuote:
                        return true;
                    default:
                        return false;
                }
            }
        }

        #endregion Cref

        #region Name attribute values

        private IdentifierNameSyntax ParseNameAttributeValue()
        {
            // Never report a parse error - just fail to bind the name later on.
            SyntaxToken identifierToken = this.EatToken(SyntaxKind.IdentifierToken, reportError: false);

            if (!IsEndOfNameAttribute)
            {
                var badTokens = _pool.Allocate<SyntaxToken>();
                while (!IsEndOfNameAttribute)
                {
                    badTokens.Add(this.EatToken());
                }
                identifierToken = AddTrailingSkippedSyntax(identifierToken, badTokens.ToListNode());
                _pool.Free(badTokens);
            }

            return SyntaxFactory.IdentifierName(identifierToken);
        }

        /// <summary>
        /// Ends at appropriate quotation mark, EOF, or EndOfDocumentationComment.
        /// </summary>
        private bool IsEndOfNameAttribute
        {
            get
            {
                switch (CurrentToken.Kind)
                {
                    case SyntaxKind.SingleQuoteToken:
                        return (this.Mode & LexerMode.XmlNameQuote) == LexerMode.XmlNameQuote;
                    case SyntaxKind.DoubleQuoteToken:
                        return (this.Mode & LexerMode.XmlNameDoubleQuote) == LexerMode.XmlNameDoubleQuote;
                    case SyntaxKind.EndOfFileToken:
                    case SyntaxKind.EndOfDocumentationCommentToken:
                        return true;
                    case SyntaxKind.BadToken:
                        // If it's a real '<' (not &lt;, etc), then we assume it's the beginning
                        // of the next XML element.
                        return CurrentToken.Text == SyntaxFacts.GetText(SyntaxKind.LessThanToken) ||
                            IsNonAsciiQuotationMark(CurrentToken);
                    default:
                        return false;
                }
            }
        }

        #endregion Name attribute values
    }
}
