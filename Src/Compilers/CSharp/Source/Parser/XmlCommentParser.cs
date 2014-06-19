// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Roslyn.Compilers.CSharp.InternalSyntax
{
    // TODO: The Xml parser recognizes most commonplace XML, according to the XML spec.
    // It does not recognize the following:
    //
    //  * Prolog XMLDecl and Processing Instructions
    //      <?xml ... ?>
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

    internal class XmlDocCommentParser : SyntaxParser
    {
        private bool isDelimited;
        private SyntaxListPool pool = new SyntaxListPool();

        internal XmlDocCommentParser(Lexer lexer, LexMode modeflags)
            : base(lexer.Options, lexer, LexMode.XmlDocComment | LexMode.XmlDocCommentLocationStart | modeflags, null, null, true)
        {
            isDelimited = (modeflags & LexMode.XmlDocCommentStyleDelimited) != 0;
        }

        internal void ReInitialize(LexMode modeflags)
        {
            base.ReInitialize();
            this.Mode = LexMode.XmlDocComment | LexMode.XmlDocCommentLocationStart | modeflags;
            isDelimited = (modeflags & LexMode.XmlDocCommentStyleDelimited) != 0;
        }

        private LexMode SetMode(LexMode mode)
        {
            var tmp = this.Mode;
            this.Mode = mode | (tmp & (LexMode.MaskXmlDocCommentLocation | LexMode.MaskXmlDocCommentStyle));
            return tmp;
        }

        private void ResetMode(LexMode mode)
        {
            this.Mode = mode;
        }

        public SyntaxNode ParseXmlDocComment(out bool isTerminated)
        {
            var nodes = this.pool.Allocate<XmlNodeSyntax>();
            try
            {
                this.ParseXmlNodes(nodes);

                // It's possible that we finish parsing the xml, and we are still left in the middle
                // of an Xml comment. For example,
                //
                //     /// <foo></foo></uhoh>
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

                isTerminated = !isDelimited || (eoc.LeadingTrivia.Count > 0 && eoc.LeadingTrivia[eoc.LeadingTrivia.Count - 1].GetText() == "*/");

                return Syntax.DocumentationComment(nodes.ToList(), eoc);
            }
            finally
            {
                this.pool.Free(nodes);
            }
        }

        public void ParseRemainder(SyntaxListBuilder<XmlNodeSyntax> nodes)
        {
            bool endTag = this.CurrentToken.Kind == SyntaxKind.LessThanSlashToken;

            var saveMode = this.SetMode(LexMode.XmlCDataSectionText);

            var textTokens = this.pool.Allocate();
            try
            {
                while (this.CurrentToken.Kind != SyntaxKind.EndOfDocumentationCommentToken)
                {
                    var token = this.EatToken();

                    // TODO: It is possible that a non-literal gets in here. ]]>, specifically. Is that ok?
                    textTokens.Add(token);
                }

                var allRemainderText = Syntax.XmlText(textTokens.ToTokenList());
                XmlParseErrorArgument arg = endTag
                    ? new XmlParseErrorArgument(XmlParseErrorCode.XML_EndTagNotExpected)
                    : new XmlParseErrorArgument(XmlParseErrorCode.XML_ExpectedEndOfXml);

                allRemainderText = allRemainderText.WithAdditionalDiagnostics(this.MakeError(0, 1, ErrorCode.WRN_XMLParseError, arg));

                nodes.Add(allRemainderText);
            }
            finally
            {
                this.pool.Free(textTokens);
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
                case SyntaxKind.EndOfDocumentationCommentToken:
                    return null;
                default:
                    // This means we have some unrecognized
                    // token. We probably need to give an
                    // error without screwing up the recursion.
                    return null;
            }
        }

        private XmlNodeSyntax ParseXmlText()
        {
            var textTokens = new SyntaxListBuilder(10);
            while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
                || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken
                || this.CurrentToken.Kind == SyntaxKind.XmlEntityLiteralToken)
            {
                textTokens.Add(this.EatToken());
            }

            return Syntax.XmlText(textTokens.ToList());
        }

        private XmlNodeSyntax ParseXmlElement()
        {
            var lessThan = this.EatToken(SyntaxKind.LessThanToken); // guaranteed
            var saveMode = this.SetMode(LexMode.XmlElementTag);
            var name = this.ParseXmlName();
            var attrs = this.pool.Allocate<XmlAttributeSyntax>();
            try
            {
                var attributesSeen = new HashSet<string>();
                while (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    var attr = this.ParseXmlAttribute();
                    string attrName = attr.Name.GetText();
                    if (attributesSeen.Contains(attrName))
                    {
                        attr = this.WithXmlParseError(attr, XmlParseErrorCode.XML_DuplicateAttribute);
                    }
                    else
                    {
                        attributesSeen.Add(attrName);
                    }

                    attrs.Add(attr);
                }

                if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                {
                    var startTag = Syntax.XmlElementStartTag(lessThan, name, attrs, this.EatToken());
                    this.SetMode(LexMode.XmlDocComment);
                    var nodes = this.pool.Allocate<XmlNodeSyntax>();
                    try
                    {
                        this.ParseXmlNodes(nodes);

                        // end tag
                        var lessThanSlash = this.EatToken(SyntaxKind.LessThanSlashToken);
                        this.SetMode(LexMode.XmlElementTag);
                        var endName = this.ParseXmlName();
                        if (name.GetText() != endName.GetText())
                        {
                            endName = this.WithXmlParseError(endName, XmlParseErrorCode.XML_ElementTypeMatch, endName.GetText(), name.GetText());
                        }

                        var greaterThan = this.EatToken(SyntaxKind.GreaterThanToken);
                        var endTag = Syntax.XmlElementEndTag(lessThanSlash, endName, greaterThan);
                        this.ResetMode(saveMode);
                        return Syntax.XmlElement(startTag, nodes.ToList(), endTag);
                    }
                    finally
                    {
                        this.pool.Free(nodes);
                    }
                }
                else
                {
                    var slashGreater = this.EatToken(SyntaxKind.SlashGreaterThanToken, false);
                    if (slashGreater.IsMissing)
                    {
                        slashGreater = this.WithXmlParseError(slashGreater, XmlParseErrorCode.XML_InvalidNameStartChar);
                    }

                    this.ResetMode(saveMode);
                    return Syntax.XmlEmptyElement(lessThan, name, attrs, slashGreater);
                }
            }
            finally
            {
                this.pool.Free(attrs);
            }
        }

        private XmlAttributeSyntax ParseXmlAttribute()
        {
            var attrName = this.ParseXmlName();
            if (attrName.LeadingWidth == 0)
            {
                // The Xml spec requires whitespace here: STag ::= '<' Name (S Attribute)* S? '>' 
                attrName = this.WithXmlParseError(attrName, XmlParseErrorCode.XML_WhitespaceMissing);
            }

            var equals = this.EatToken(SyntaxKind.EqualsToken, false);
            if (equals.IsMissing)
            {
                equals = this.WithXmlParseError(equals, XmlParseErrorCode.XML_MissingEqualsAttribute);
            }

            var attributeTextStyle = this.CurrentToken.Kind == SyntaxKind.SingleQuoteToken
                ? SyntaxKind.SingleQuoteToken
                : SyntaxKind.DoubleQuoteToken;
            var startQuote = this.EatToken(attributeTextStyle, false);
            if (startQuote.IsMissing)
            {
                startQuote = this.WithXmlParseError(startQuote, XmlParseErrorCode.XML_StringLiteralNoQuote);
            }

            var textTokens = new SyntaxListBuilder<SyntaxToken>(10);
            {
                var saveMode = this.SetMode(attributeTextStyle == SyntaxKind.SingleQuoteToken
                    ? LexMode.XmlAttributeTextQuote
                    : LexMode.XmlAttributeTextDoubleQuote);
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
            }

            var endQuote = this.EatToken(attributeTextStyle);
            return Syntax.XmlAttribute(attrName, equals, startQuote, textTokens, endQuote);
        }

        private XmlNameSyntax ParseXmlName()
        {
            var id = this.EatToken(SyntaxKind.IdentifierToken);
            XmlPrefixSyntax prefix = null;
            if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                var colon = this.EatToken();
                prefix = Syntax.XmlPrefix(id, colon);
                id = this.EatToken(SyntaxKind.IdentifierToken);
            }

            return Syntax.XmlName(prefix, id);
        }

        private XmlCommentSyntax ParseXmlComment()
        {
            var lessThanExclamationMinusMinusToken = this.EatToken(SyntaxKind.XmlCommentStartToken);
            var saveMode = this.SetMode(LexMode.XmlCommentText);
            var textTokens = new SyntaxListBuilder<SyntaxToken>(10);
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

            var minusMinusGreaterThanToken = this.EatToken(SyntaxKind.XmlCommentEndToken);
            this.ResetMode(saveMode);
            return Syntax.XmlComment(lessThanExclamationMinusMinusToken, textTokens, minusMinusGreaterThanToken);
        }

        private XmlCDataSectionSyntax ParseXmlCDataSection()
        {
            var startCDataToken = this.EatToken(SyntaxKind.XmlCDataStartToken);
            var saveMode = this.SetMode(LexMode.XmlCDataSectionText);
            var textTokens = new SyntaxListBuilder<SyntaxToken>(10);
            while (this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralToken
               || this.CurrentToken.Kind == SyntaxKind.XmlTextLiteralNewLineToken)
            {
                textTokens.Add(this.EatToken());
            }

            var endCDataToken = this.EatToken(SyntaxKind.XmlCDataEndToken);
            this.ResetMode(saveMode);
            return Syntax.XmlCDataSection(startCDataToken, textTokens, endCDataToken);
        }

        protected override SyntaxDiagnosticInfo GetExpectedTokenError(SyntaxKind expected, SyntaxKind actual, int offset, int length)
        {
            var code = this.GetExpectedTokenErrorCode(expected, actual);
            Debug.Assert(code == ErrorCode.WRN_XMLParseError);

            switch (expected)
            {
                case SyntaxKind.IdentifierToken:
                    return new SyntaxDiagnosticInfo(offset, length, code, new XmlParseErrorArgument(XmlParseErrorCode.XML_InvalidNameStartChar));

                default:
                    return new SyntaxDiagnosticInfo(offset, length, code, new XmlParseErrorArgument(XmlParseErrorCode.XML_ExpectedOtherToken, SyntaxFacts.GetText(actual)));
            }
        }

        protected override SyntaxDiagnosticInfo GetExpectedTokenError(SyntaxKind expected, SyntaxKind actual)
        {
            var code = this.GetExpectedTokenErrorCode(expected, actual);
            Debug.Assert(code == ErrorCode.WRN_XMLParseError);

            switch (expected)
            {
                case SyntaxKind.IdentifierToken:
                    return new SyntaxDiagnosticInfo(code, new XmlParseErrorArgument(XmlParseErrorCode.XML_InvalidNameStartChar));

                default:
                    return new SyntaxDiagnosticInfo(code, new XmlParseErrorArgument(XmlParseErrorCode.XML_ExpectedOtherToken, SyntaxFacts.GetText(actual)));
            }
        }

        protected override ErrorCode GetExpectedTokenErrorCode(SyntaxKind expected, SyntaxKind actual)
        {
            return ErrorCode.WRN_XMLParseError;
        }

        private TNode WithXmlParseError<TNode>(TNode node, XmlParseErrorCode code) where TNode : SyntaxNode
        {
            XmlParseErrorArgument arg = new XmlParseErrorArgument(code);
            return (TNode)node.WithAdditionalDiagnostics(this.MakeError(0, node.Width, ErrorCode.WRN_XMLParseError, arg));
        }

        private TNode WithXmlParseError<TNode>(TNode node, XmlParseErrorCode code, params string[] args) where TNode : SyntaxNode
        {
            XmlParseErrorArgument arg = new XmlParseErrorArgument(code, args);
            return (TNode)node.WithAdditionalDiagnostics(this.MakeError(0, node.Width, ErrorCode.WRN_XMLParseError, arg));
        }

        private SyntaxToken WithXmlParseError<TNode>(SyntaxToken node, XmlParseErrorCode code)
        {
            XmlParseErrorArgument arg = new XmlParseErrorArgument(code);
            return node.WithAdditionalDiagnostics(this.MakeError(0, node.Width, ErrorCode.WRN_XMLParseError, arg));
        }

        private SyntaxToken WithXmlParseError(SyntaxToken node, XmlParseErrorCode code, params string[] args)
        {
            XmlParseErrorArgument arg = new XmlParseErrorArgument(code, args);
            return node.WithAdditionalDiagnostics(this.MakeError(0, node.Width, ErrorCode.WRN_XMLParseError, arg));
        }
    }
}
