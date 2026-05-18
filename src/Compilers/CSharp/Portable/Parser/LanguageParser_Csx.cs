// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal sealed partial class LanguageParser
    {
        // -----------------------------------------------------------------------
        // CSX (JSX-like component syntax) parsing
        //
        // CSX is enabled when Options.CsxFactory is non-null.
        //
        // Grammar (classic runtime, value-based components only):
        //
        //   csx_element
        //     : '<' name csx_attributes '>' csx_children '<' '/' name '>'
        //     | '<' name csx_attributes '/' '>'
        //
        //   csx_children
        //     : ( csx_element | csx_expression | csx_text )*
        //
        //   csx_expression
        //     : '{' expression '}'
        //
        //   csx_text
        //     : <any text not containing '<' or '{'>
        //
        //   csx_attribute
        //     : identifier '=' string_literal
        //     | identifier '=' '{' expression '}'
        //     | identifier                         (boolean shorthand: Disabled == Disabled={true})
        //
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns true when the current token is '&lt;' and the following tokens look like
        /// a CSX element (value-based: tag name starts with an uppercase letter or is dotted).
        /// Only called when <see cref="CSharpParseOptions.CsxFactory"/> is non-null.
        /// </summary>
        private bool IsPossibleCsxElement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);
            Debug.Assert(this.Options.CsxFactory != null);

            var next = this.PeekToken(1);

            // Fragment: <> — not yet supported in Phase 1, but don't accidentally parse it.
            if (next.Kind == SyntaxKind.GreaterThanToken)
                return false;

            // Must be an identifier to be a component name.
            if (next.Kind != SyntaxKind.IdentifierToken)
                return false;

            // Value-based components must start with an uppercase letter.
            // (Lowercase tags will be intrinsic elements, added in a future phase.)
            var nameText = next.ValueText;
            if (nameText.Length == 0 || !char.IsUpper(nameText[0]))
                return false;

            // Walk through the dotted name (e.g. MyLib.Card) to find the token
            // immediately after it, then decide if it looks like a CSX element.
            // We also check for newlines between tokens — if the lookahead crosses
            // a line boundary, it's not CSX (e.g. <B\nConsole... is not <B attr>).
            int offset = 1;
            while (true)
            {
                var current = this.PeekToken(offset);
                var peek = this.PeekToken(offset + 1);

                // If there is a newline in the trailing trivia of `current` or the
                // leading trivia of `peek`, the tokens are on different lines — not CSX.
                if (TokenHasNewlineTrivia(current, trailing: true) ||
                    TokenHasNewlineTrivia(peek, trailing: false))
                    return false;

                if (peek.Kind == SyntaxKind.DotToken)
                {
                    // Expect another identifier segment after the dot.
                    var afterDot = this.PeekToken(offset + 2);
                    if (afterDot.Kind != SyntaxKind.IdentifierToken)
                        return false;
                    offset += 2;
                    continue;
                }

                // peek is the token immediately after the (possibly dotted) name.
                switch (peek.Kind)
                {
                    case SyntaxKind.GreaterThanToken:   // <Name>
                    case SyntaxKind.SlashToken:          // <Name />
                    case SyntaxKind.IdentifierToken:     // <Name Attr=...
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static bool TokenHasNewlineTrivia(SyntaxToken token, bool trailing)
        {
            var triviaList = trailing ? token.TrailingTrivia : token.LeadingTrivia;
            foreach (var trivia in triviaList)
            {
                if (trivia.RawKind == (int)SyntaxKind.EndOfLineTrivia)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Entry point for parsing a CSX element in expression position.
        /// Dispatches to self-closing or element-with-children form.
        /// </summary>
        internal CsxNodeSyntax ParseCsxElement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);

            var lessThan = this.EatToken(SyntaxKind.LessThanToken);
            var name = this.ParseCsxName();
            var attributes = this.ParseCsxAttributes();

            if (this.CurrentToken.Kind == SyntaxKind.SlashToken)
            {
                // Self-closing: <Name attrs />
                var slash = this.EatToken(SyntaxKind.SlashToken);
                // Only merge when '>' is actually present in source — if it's missing,
                // build a missing '/>' token that preserves the slash's trivia exactly,
                // so the incremental reparser never sees a width mismatch.
                SyntaxToken slashGreaterThan;
                if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                {
                    slashGreaterThan = MergeAdjacent(slash, this.EatToken(SyntaxKind.GreaterThanToken));
                }
                else
                {
                    // '>' is missing — synthesise a missing '/>' that carries the slash's trivia.
                    slashGreaterThan = this.AddError(
                        SyntaxFactory.Token(
                            slash.LeadingTrivia.Node,
                            SyntaxKind.SlashToken,
                            "/",
                            "/>",
                            slash.TrailingTrivia.Node),
                        ErrorCode.ERR_SyntaxError,
                        ">");
                }
                return _syntaxFactory.CsxSelfClosingElement(lessThan, name, attributes, slashGreaterThan);
            }
            else
            {
                // Opening tag: <Name attrs>
                var greaterThan = this.EatToken(SyntaxKind.GreaterThanToken);
                var opening = _syntaxFactory.CsxOpeningElement(lessThan, name, attributes, greaterThan);

                // Parse children
                var children = this.ParseCsxChildren();

                // Closing tag: </Name>
                var closing = this.ParseCsxClosingElement(name);

                return _syntaxFactory.CsxElement(opening, children, closing);
            }
        }

        /// <summary>
        /// Parses a possibly-qualified CSX tag name: <c>Button</c>, <c>MyLib.Card</c>, etc.
        /// </summary>
        private NameSyntax ParseCsxName()
        {
            // Start with simple identifier
            NameSyntax name = _syntaxFactory.IdentifierName(this.EatToken(SyntaxKind.IdentifierToken));

            // Handle dotted names: MyLib.Card
            while (this.CurrentToken.Kind == SyntaxKind.DotToken)
            {
                var dot = this.EatToken(SyntaxKind.DotToken);
                var right = this.EatToken(SyntaxKind.IdentifierToken);
                name = _syntaxFactory.QualifiedName(name, dot, _syntaxFactory.IdentifierName(right));
            }

            return name;
        }

        /// <summary>
        /// Parses a list of CSX attributes: zero or more <c>Name</c>, <c>Name="str"</c>,
        /// or <c>Name={expr}</c> items.  Stops at <c>/</c> or <c>&gt;</c>.
        /// </summary>
        private Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList<CsxAttributeSyntax> ParseCsxAttributes()
        {
            var builder = _pool.Allocate<CsxAttributeSyntax>();
            try
            {
                while (this.CurrentToken.Kind != SyntaxKind.GreaterThanToken
                    && this.CurrentToken.Kind != SyntaxKind.SlashToken
                    && this.CurrentToken.Kind != SyntaxKind.EndOfFileToken
                    && this.CurrentToken.Kind != SyntaxKind.CloseBraceToken
                    && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                    {
                        builder.Add(this.ParseCsxAttribute());
                    }
                    else
                    {
                        // Bad token in attribute position — skip it with an error attached to a synthetic text node.
                        var skipped = this.EatToken();
                        var errorText = this.AddError(
                            _syntaxFactory.CsxText(skipped),
                            ErrorCode.ERR_IdentifierExpected);
                        // We can't add a CsxTextSyntax as a CsxAttributeSyntax — break to avoid infinite loop.
                        break;
                    }
                }

                return builder.ToList();
            }
            finally
            {
                _pool.Free(builder);
            }
        }

        /// <summary>
        /// Parses a single CSX attribute:
        /// <list type="bullet">
        ///   <item><c>Disabled</c> — boolean shorthand (no value)</item>
        ///   <item><c>Color="red"</c> — string literal value</item>
        ///   <item><c>Count={expr}</c> — arbitrary C# expression</item>
        /// </list>
        /// </summary>
        private CsxAttributeSyntax ParseCsxAttribute()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IdentifierToken);

            var name = _syntaxFactory.IdentifierName(this.EatToken(SyntaxKind.IdentifierToken));

            // No '=' — boolean shorthand attribute
            if (this.CurrentToken.Kind != SyntaxKind.EqualsToken)
            {
                return _syntaxFactory.CsxAttribute(name, equalsToken: null, value: null);
            }

            var equals = this.EatToken(SyntaxKind.EqualsToken);
            ExpressionSyntax value;

            if (this.CurrentToken.Kind == SyntaxKind.StringLiteralToken)
            {
                // Quoted string: Color="red"
                value = _syntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    this.EatToken(SyntaxKind.StringLiteralToken));
            }
            else if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                // Expression block: Count={someExpr}
                var open = this.EatToken(SyntaxKind.OpenBraceToken);
                var expr = this.ParseExpressionCore();
                var close = this.EatToken(SyntaxKind.CloseBraceToken);
                value = _syntaxFactory.CsxExpression(open, expr, close);
            }
            else
            {
                // Missing value — recover with a missing identifier
                value = this.AddError(
                    this.CreateMissingIdentifierName(),
                    ErrorCode.ERR_InvalidExprTerm,
                    this.CurrentToken.Text);
            }

            return _syntaxFactory.CsxAttribute(name, equals, value);
        }

        /// <summary>
        /// Parses CSX child content between an opening and closing tag.
        /// Children may be nested CSX elements, <c>{expr}</c> expressions, or literal text.
        /// </summary>
        private Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList<CsxNodeSyntax> ParseCsxChildren()
        {
            var builder = _pool.Allocate<CsxNodeSyntax>();
            try
            {
                while (true)
                {
                    var tk = this.CurrentToken.Kind;

                    if (tk == SyntaxKind.EndOfFileToken)
                        break;

                    // A closing brace ends an enclosing {expr} or method body — stop.
                    if (tk == SyntaxKind.CloseBraceToken)
                        break;

                    if (tk == SyntaxKind.LessThanToken)
                    {
                        // Look ahead: if next is '/' it's the closing tag — stop.
                        if (this.PeekToken(1).Kind == SyntaxKind.SlashToken)
                            break;

                        // Otherwise it's a nested CSX element.
                        if (IsPossibleCsxElement())
                        {
                            builder.Add(this.ParseCsxElement());
                        }
                        else
                        {
                            // Unexpected '<' that doesn't look like CSX — consume and attach error.
                            var bad = this.EatToken();
                            builder.Add(this.AddError(
                                _syntaxFactory.CsxText(bad),
                                ErrorCode.ERR_InvalidExprTerm,
                                "<"));
                        }
                    }
                    else if (tk == SyntaxKind.OpenBraceToken)
                    {
                        // Embedded C# expression: {someExpr}
                        var open = this.EatToken(SyntaxKind.OpenBraceToken);
                        var expr = this.ParseExpressionCore();
                        var close = this.EatToken(SyntaxKind.CloseBraceToken);
                        builder.Add(_syntaxFactory.CsxExpression(open, expr, close));
                    }
                    else
                    {
                        // Literal text — consume one token at a time as a raw text chunk.
                        // Adjacent CsxTextSyntax nodes are merged and normalised by the binder.
                        builder.Add(_syntaxFactory.CsxText(this.EatToken()));
                    }
                }

                return builder.ToList();
            }
            finally
            {
                _pool.Free(builder);
            }
        }

        /// <summary>
        /// Parses a CSX closing tag <c>&lt;/Name&gt;</c> and validates it matches
        /// the given <paramref name="openingName"/>.
        /// </summary>
        private CsxClosingElementSyntax ParseCsxClosingElement(NameSyntax openingName)
        {
            var lessThan = this.EatToken(SyntaxKind.LessThanToken);
            var slash = this.EatToken(SyntaxKind.SlashToken);
            var name = this.ParseCsxName();
            var greaterThan = this.EatToken(SyntaxKind.GreaterThanToken);

            var closing = _syntaxFactory.CsxClosingElement(lessThan, slash, name, greaterThan);

            // Validate tag name matches — add error on the closing node if not.
            if (name.ToString() != openingName.ToString())
            {
                closing = this.AddError(
                    closing,
                    ErrorCode.ERR_CsxOpenClosingTagMismatch,
                    name.ToString(),
                    openingName.ToString());
            }

            return closing;
        }

        /// <summary>
        /// Merges two adjacent tokens (slash + greater-than) into a single token.
        /// Used to produce a clean <c>/&gt;</c> token for self-closing elements.
        /// </summary>
        private static SyntaxToken MergeAdjacent(SyntaxToken first, SyntaxToken second)
        {
            // Combine the text of both tokens into a single synthetic token.
            // Leading trivia comes from `first`, trailing trivia from `second`.
            // The text parameter must be just the token characters (no trivia).
            return SyntaxFactory.Token(
                first.LeadingTrivia.Node,
                SyntaxKind.SlashToken,
                first.Text + second.Text,
                "/>",
                second.TrailingTrivia.Node);
        }
    }
}
