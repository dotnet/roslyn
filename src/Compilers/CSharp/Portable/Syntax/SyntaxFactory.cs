// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A class containing factory methods for constructing syntax nodes, tokens and trivia.
    /// </summary>
    public static partial class SyntaxFactory
    {
        /// <summary>
        /// A trivia with kind EndOfLineTrivia containing both the carriage return and line feed characters.
        /// </summary>
        public static SyntaxTrivia CarriageReturnLineFeed { get; } = Syntax.InternalSyntax.SyntaxFactory.CarriageReturnLineFeed;

        /// <summary>
        /// A trivia with kind EndOfLineTrivia containing a single line feed character.
        /// </summary>
        public static SyntaxTrivia LineFeed { get; } = Syntax.InternalSyntax.SyntaxFactory.LineFeed;

        /// <summary>
        /// A trivia with kind EndOfLineTrivia containing a single carriage return character.
        /// </summary>
        public static SyntaxTrivia CarriageReturn { get; } = Syntax.InternalSyntax.SyntaxFactory.CarriageReturn;

        /// <summary>
        ///  A trivia with kind WhitespaceTrivia containing a single space character.
        /// </summary>
        public static SyntaxTrivia Space { get; } = Syntax.InternalSyntax.SyntaxFactory.Space;

        /// <summary>
        /// A trivia with kind WhitespaceTrivia containing a single tab character.
        /// </summary>
        public static SyntaxTrivia Tab { get; } = Syntax.InternalSyntax.SyntaxFactory.Tab;

        /// <summary>
        /// An elastic trivia with kind EndOfLineTrivia containing both the carriage return and line feed characters.
        /// Elastic trivia are used to denote trivia that was not produced by parsing source text, and are usually not
        /// preserved during formatting.
        /// </summary>
        public static SyntaxTrivia ElasticCarriageReturnLineFeed { get; } = Syntax.InternalSyntax.SyntaxFactory.ElasticCarriageReturnLineFeed;

        /// <summary>
        /// An elastic trivia with kind EndOfLineTrivia containing a single line feed character. Elastic trivia are used
        /// to denote trivia that was not produced by parsing source text, and are usually not preserved during
        /// formatting.
        /// </summary>
        public static SyntaxTrivia ElasticLineFeed { get; } = Syntax.InternalSyntax.SyntaxFactory.ElasticLineFeed;

        /// <summary>
        /// An elastic trivia with kind EndOfLineTrivia containing a single carriage return character. Elastic trivia
        /// are used to denote trivia that was not produced by parsing source text, and are usually not preserved during
        /// formatting.
        /// </summary>
        public static SyntaxTrivia ElasticCarriageReturn { get; } = Syntax.InternalSyntax.SyntaxFactory.ElasticCarriageReturn;

        /// <summary>
        /// An elastic trivia with kind WhitespaceTrivia containing a single space character. Elastic trivia are used to
        /// denote trivia that was not produced by parsing source text, and are usually not preserved during formatting.
        /// </summary>
        public static SyntaxTrivia ElasticSpace { get; } = Syntax.InternalSyntax.SyntaxFactory.ElasticSpace;

        /// <summary>
        /// An elastic trivia with kind WhitespaceTrivia containing a single tab character. Elastic trivia are used to
        /// denote trivia that was not produced by parsing source text, and are usually not preserved during formatting.
        /// </summary>
        public static SyntaxTrivia ElasticTab { get; } = Syntax.InternalSyntax.SyntaxFactory.ElasticTab;

        /// <summary>
        /// An elastic trivia with kind WhitespaceTrivia containing no characters. Elastic marker trivia are included
        /// automatically by factory methods when trivia is not specified. Syntax formatting will replace elastic
        /// markers with appropriate trivia.
        /// </summary>
        public static SyntaxTrivia ElasticMarker { get; } = Syntax.InternalSyntax.SyntaxFactory.ElasticZeroSpace;

        /// <summary>
        /// Creates a trivia with kind EndOfLineTrivia containing the specified text. 
        /// </summary>
        /// <param name="text">The text of the end of line. Any text can be specified here, however only carriage return and
        /// line feed characters are recognized by the parser as end of line.</param>
        public static SyntaxTrivia EndOfLine(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.EndOfLine(text, elastic: false);
        }

        /// <summary>
        /// Creates a trivia with kind EndOfLineTrivia containing the specified text. Elastic trivia are used to
        /// denote trivia that was not produced by parsing source text, and are usually not preserved during formatting.
        /// </summary>
        /// <param name="text">The text of the end of line. Any text can be specified here, however only carriage return and
        /// line feed characters are recognized by the parser as end of line.</param>
        public static SyntaxTrivia ElasticEndOfLine(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.EndOfLine(text, elastic: true);
        }

        [Obsolete("Use SyntaxFactory.EndOfLine or SyntaxFactory.ElasticEndOfLine")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static SyntaxTrivia EndOfLine(string text, bool elastic)
        {
            return Syntax.InternalSyntax.SyntaxFactory.EndOfLine(text, elastic);
        }

        /// <summary>
        /// Creates a trivia with kind WhitespaceTrivia containing the specified text.
        /// </summary>
        /// <param name="text">The text of the whitespace. Any text can be specified here, however only specific
        /// whitespace characters are recognized by the parser.</param>
        public static SyntaxTrivia Whitespace(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.Whitespace(text, elastic: false);
        }

        /// <summary>
        /// Creates a trivia with kind WhitespaceTrivia containing the specified text. Elastic trivia are used to
        /// denote trivia that was not produced by parsing source text, and are usually not preserved during formatting.
        /// </summary>
        /// <param name="text">The text of the whitespace. Any text can be specified here, however only specific
        /// whitespace characters are recognized by the parser.</param>
        public static SyntaxTrivia ElasticWhitespace(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.Whitespace(text, elastic: false);
        }

        [Obsolete("Use SyntaxFactory.Whitespace or SyntaxFactory.ElasticWhitespace")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static SyntaxTrivia Whitespace(string text, bool elastic)
        {
            return Syntax.InternalSyntax.SyntaxFactory.Whitespace(text, elastic);
        }

        /// <summary>
        /// Creates a trivia with kind either SingleLineCommentTrivia or MultiLineCommentTrivia containing the specified
        /// text.
        /// </summary>
        /// <param name="text">The entire text of the comment including the leading '//' token for single line comments
        /// or stop or start tokens for multiline comments.</param>
        public static SyntaxTrivia Comment(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.Comment(text);
        }

        /// <summary>
        /// Creates a trivia with kind DisabledTextTrivia. Disabled text corresponds to any text between directives that
        /// is not considered active.
        /// </summary>
        public static SyntaxTrivia DisabledText(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.DisabledText(text);
        }

        /// <summary>
        /// Creates a trivia with kind PreprocessingMessageTrivia.
        /// </summary>
        public static SyntaxTrivia PreprocessingMessage(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.PreprocessingMessage(text);
        }

        /// <summary>
        /// Trivia nodes represent parts of the program text that are not parts of the
        /// syntactic grammar, such as spaces, newlines, comments, preprocessor
        /// directives, and disabled code.
        /// </summary>
        /// <param name="kind">
        /// A <see cref="SyntaxKind"/> representing the specific kind of <see cref="SyntaxTrivia"/>. One of
        /// <see cref="SyntaxKind.WhitespaceTrivia"/>, <see cref="SyntaxKind.EndOfLineTrivia"/>,
        /// <see cref="SyntaxKind.SingleLineCommentTrivia"/>, <see cref="SyntaxKind.MultiLineCommentTrivia"/>,
        /// <see cref="SyntaxKind.DocumentationCommentExteriorTrivia"/>, <see cref="SyntaxKind.DisabledTextTrivia"/>
        /// </param>
        /// <param name="text">
        /// The actual text of this token.
        /// </param>
        public static SyntaxTrivia SyntaxTrivia(SyntaxKind kind, string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            switch (kind)
            {
                case SyntaxKind.DisabledTextTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.EndOfLineTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.WhitespaceTrivia:
                    return new SyntaxTrivia(default(SyntaxToken), new Syntax.InternalSyntax.SyntaxTrivia(kind, text, null, null), 0, 0);
                default:
                    throw new ArgumentException("kind");
            }
        }

        /// <summary>
        /// Creates a token corresponding to a syntax kind. This method can be used for token syntax kinds whose text
        /// can be inferred by the kind alone.
        /// </summary>
        /// <param name="kind">A syntax kind value for a token. These have the suffix Token or Keyword.</param>
        /// <returns></returns>
        public static SyntaxToken Token(SyntaxKind kind)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Token((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, kind, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token corresponding to syntax kind. This method can be used for token syntax kinds whose text can
        /// be inferred by the kind alone.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="kind">A syntax kind value for a token. These have the suffix Token or Keyword.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Token(SyntaxTriviaList leading, SyntaxKind kind, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Token((InternalSyntax.CSharpSyntaxNode)leading.Node, kind, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token corresponding to syntax kind. This method gives control over token Text and ValueText.
        /// 
        /// For example, consider the text '&lt;see cref="operator &amp;#43;"/&gt;'.  To create a token for the value of
        /// the operator symbol (&amp;#43;), one would call 
        /// Token(default(SyntaxTriviaList), SyntaxKind.PlusToken, "&amp;#43;", "+", default(SyntaxTriviaList)).
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="kind">A syntax kind value for a token. These have the suffix Token or Keyword.</param>
        /// <param name="text">The text from which this token was created (e.g. lexed).</param>
        /// <param name="valueText">How C# should interpret the text of this token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Token(SyntaxTriviaList leading, SyntaxKind kind, string text, string valueText, SyntaxTriviaList trailing)
        {
            switch (kind)
            {
                case SyntaxKind.IdentifierToken:
                    // Have a different representation.
                    throw new ArgumentException(CSharpResources.UseVerbatimIdentifier, nameof(kind));
                case SyntaxKind.CharacterLiteralToken:
                    // Value should not have type string.
                    throw new ArgumentException(CSharpResources.UseLiteralForTokens, nameof(kind));
                case SyntaxKind.NumericLiteralToken:
                    // Value should not have type string.
                    throw new ArgumentException(CSharpResources.UseLiteralForNumeric, nameof(kind));
            }

            if (!SyntaxFacts.IsAnyToken(kind))
            {
                throw new ArgumentException(string.Format(CSharpResources.ThisMethodCanOnlyBeUsedToCreateTokens, kind), nameof(kind));
            }

            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Token((InternalSyntax.CSharpSyntaxNode)leading.Node, kind, text, valueText, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a missing token corresponding to syntax kind. A missing token is produced by the parser when an
        /// expected token is not found. A missing token has no text and normally has associated diagnostics.
        /// </summary>
        /// <param name="kind">A syntax kind value for a token. These have the suffix Token or Keyword.</param>
        public static SyntaxToken MissingToken(SyntaxKind kind)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.MissingToken((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, kind, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a missing token corresponding to syntax kind. A missing token is produced by the parser when an
        /// expected token is not found. A missing token has no text and normally has associated diagnostics.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="kind">A syntax kind value for a token. These have the suffix Token or Keyword.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken MissingToken(SyntaxTriviaList leading, SyntaxKind kind, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.MissingToken((InternalSyntax.CSharpSyntaxNode)leading.Node, kind, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind IdentifierToken containing the specified text.
        /// <param name="text">The raw text of the identifier name, including any escapes or leading '@'
        /// character.</param>
        /// </summary>
        public static SyntaxToken Identifier(string text)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Identifier((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind IdentifierToken containing the specified text.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the identifier name, including any escapes or leading '@'
        /// character.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Identifier(SyntaxTriviaList leading, string text, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Identifier((InternalSyntax.CSharpSyntaxNode)leading.Node, text, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a verbatim token with kind IdentifierToken containing the specified text.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the identifier name, including any escapes or leading '@'
        /// character as it is in source.</param>
        /// <param name="valueText">The canonical value of the token's text.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken VerbatimIdentifier(SyntaxTriviaList leading, string text, string valueText, SyntaxTriviaList trailing)
        {
            if (text.StartsWith("@", StringComparison.Ordinal))
            {
                throw new ArgumentException("text should not start with an @ character.");
            }

            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Identifier(SyntaxKind.IdentifierName, (InternalSyntax.CSharpSyntaxNode)leading.Node, "@" + text, valueText, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind IdentifierToken containing the specified text.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="contextualKind">An alternative SyntaxKind that can be inferred for this token in special
        /// contexts. These are usually keywords.</param>
        /// <param name="text">The raw text of the identifier name, including any escapes or leading '@'
        /// character.</param>
        /// <param name="valueText">The text of the identifier name without escapes or leading '@' character.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        /// <returns></returns>
        public static SyntaxToken Identifier(SyntaxTriviaList leading, SyntaxKind contextualKind, string text, string valueText, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Identifier(contextualKind, (InternalSyntax.CSharpSyntaxNode)leading.Node, text, valueText, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from a 4-byte signed integer value.
        /// </summary>
        /// <param name="value">The 4-byte signed integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(int value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 4-byte signed integer value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 4-byte signed integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, int value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 4-byte signed integer value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 4-byte signed integer value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, int value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from a 4-byte unsigned integer value.
        /// </summary>
        /// <param name="value">The 4-byte unsigned integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(uint value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 4-byte unsigned integer value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 4-byte unsigned integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, uint value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 4-byte unsigned integer value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 4-byte unsigned integer value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, uint value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from an 8-byte signed integer value.
        /// </summary>
        /// <param name="value">The 8-byte signed integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(long value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 8-byte signed integer value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 8-byte signed integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, long value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 8-byte signed integer value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 8-byte signed integer value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, long value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from an 8-byte unsigned integer value.
        /// </summary>
        /// <param name="value">The 8-byte unsigned integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(ulong value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 8-byte unsigned integer value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 8-byte unsigned integer value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, ulong value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 8-byte unsigned integer value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 8-byte unsigned integer value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, ulong value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from a 4-byte floating point value.
        /// </summary>
        /// <param name="value">The 4-byte floating point value to be represented by the returned token.</param>
        public static SyntaxToken Literal(float value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 4-byte floating point value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 4-byte floating point value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, float value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 4-byte floating point value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 4-byte floating point value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, float value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from an 8-byte floating point value.
        /// </summary>
        /// <param name="value">The 8-byte floating point value to be represented by the returned token.</param>
        public static SyntaxToken Literal(double value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 8-byte floating point value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 8-byte floating point value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, double value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding 8-byte floating point value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The 8-byte floating point value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, double value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from a decimal value.
        /// </summary>
        /// <param name="value">The decimal value to be represented by the returned token.</param>
        public static SyntaxToken Literal(decimal value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.IncludeTypeSuffix), value);
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding decimal value.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The decimal value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, decimal value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind NumericLiteralToken from the text and corresponding decimal value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The decimal value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, decimal value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind StringLiteralToken from a string value.
        /// </summary>
        /// <param name="value">The string value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string value)
        {
            return Literal(SymbolDisplay.FormatLiteral(value, quote: true), value);
        }

        /// <summary>
        /// Creates a token with kind StringLiteralToken from the text and corresponding string value.
        /// </summary>
        /// <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        /// <param name="value">The string value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, string value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind StringLiteralToken from the text and corresponding string value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        /// <param name="value">The string value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind CharacterLiteralToken from a character value.
        /// </summary>
        /// <param name="value">The character value to be represented by the returned token.</param>
        public static SyntaxToken Literal(char value)
        {
            return Literal(ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters), value);
        }

        /// <summary>
        /// Creates a token with kind CharacterLiteralToken from the text and corresponding character value.
        /// </summary>
        /// <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        /// <param name="value">The character value to be represented by the returned token.</param>
        public static SyntaxToken Literal(string text, char value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Creates a token with kind CharacterLiteralToken from the text and corresponding character value.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal, including quotes and escape sequences.</param>
        /// <param name="value">The character value to be represented by the returned token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken Literal(SyntaxTriviaList leading, string text, char value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.Literal((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind BadToken.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the bad token.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken BadToken(SyntaxTriviaList leading, string text, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.BadToken((InternalSyntax.CSharpSyntaxNode)leading.Node, text, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind XmlTextLiteralToken.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The xml text value.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken XmlTextLiteral(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.XmlTextLiteral((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates a token with kind XmlEntityLiteralToken.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The xml entity value.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken XmlEntity(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.XmlEntity((InternalSyntax.CSharpSyntaxNode)leading.Node, text, value, (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates an xml documentation comment that abstracts xml syntax creation.
        /// </summary>
        /// <param name="content">
        /// A list of xml node syntax that will be the content within the xml documentation comment
        /// (e.g. a summary element, a returns element, exception element and so on).
        /// </param>
        public static DocumentationCommentTriviaSyntax DocumentationComment(params XmlNodeSyntax[] content)
        {
            return DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, List(content))
                .WithLeadingTrivia(DocumentationCommentExterior("/// "))
                .WithTrailingTrivia(EndOfLine(""));
        }

        /// <summary>
        /// Creates a summary element within an xml documentation comment.
        /// </summary>
        /// <param name="content">A list of xml node syntax that will be the content within the summary element.</param>
        public static XmlElementSyntax XmlSummaryElement(params XmlNodeSyntax[] content)
        {
            return XmlSummaryElement(List(content));
        }

        /// <summary>
        /// Creates a summary element within an xml documentation comment.
        /// </summary>
        /// <param name="content">A list of xml node syntax that will be the content within the summary element.</param>
        public static XmlElementSyntax XmlSummaryElement(SyntaxList<XmlNodeSyntax> content)
        {
            return XmlMultiLineElement(DocumentationCommentXmlNames.SummaryElementName, content);
        }

        /// <summary>
        /// Creates a see element within an xml documentation comment.
        /// </summary>
        /// <param name="cref">A cref syntax node that points to the referenced item (e.g. a class, struct).</param>
        public static XmlEmptyElementSyntax XmlSeeElement(CrefSyntax cref)
        {
            return XmlEmptyElement(DocumentationCommentXmlNames.SeeElementName).AddAttributes(XmlCrefAttribute(cref));
        }

        /// <summary>
        /// Creates a seealso element within an xml documentation comment.
        /// </summary>
        /// <param name="cref">A cref syntax node that points to the referenced item (e.g. a class, struct).</param>
        public static XmlEmptyElementSyntax XmlSeeAlsoElement(CrefSyntax cref)
        {
            return XmlEmptyElement(DocumentationCommentXmlNames.SeeAlsoElementName).AddAttributes(XmlCrefAttribute(cref));
        }

        /// <summary>
        /// Creates a seealso element within an xml documentation comment.
        /// </summary>
        /// <param name="linkAddress">The uri of the referenced item.</param>
        /// <param name="linkText">A list of xml node syntax that will be used as the link text for the referenced item.</param>
        public static XmlElementSyntax XmlSeeAlsoElement(Uri linkAddress, SyntaxList<XmlNodeSyntax> linkText)
        {
            XmlElementSyntax element = XmlElement(DocumentationCommentXmlNames.SeeAlsoElementName, linkText);
            return element.WithStartTag(element.StartTag.AddAttributes(XmlTextAttribute(DocumentationCommentXmlNames.CrefAttributeName, linkAddress.ToString())));
        }

        /// <summary>
        /// Creates a threadsafty element within an xml documentation comment.
        /// </summary>
        public static XmlEmptyElementSyntax XmlThreadSafetyElement()
        {
            return XmlThreadSafetyElement(true, false);
        }

        /// <summary>
        /// Creates a threadsafty element within an xml documentation comment.
        /// </summary>
        /// <param name="static">Indicates whether static member of this class are safe for multi-threaded operations.</param>
        /// <param name="instance">Indicates whether members of instances of this type are safe for multi-threaded operations.</param>
        public static XmlEmptyElementSyntax XmlThreadSafetyElement(bool @static, bool instance)
        {
            return XmlEmptyElement(DocumentationCommentXmlNames.ThreadSafetyElementName).AddAttributes(
                XmlTextAttribute(DocumentationCommentXmlNames.StaticAttributeName, @static.ToString().ToLowerInvariant()),
                XmlTextAttribute(DocumentationCommentXmlNames.InstanceAttributeName, instance.ToString().ToLowerInvariant()));
        }

        /// <summary>
        /// Creates a syntax node for a name attribute in a xml element within a xml documentation comment.
        /// </summary>
        /// <param name="parameterName">The value of the name attribute.</param>
        public static XmlNameAttributeSyntax XmlNameAttribute(string parameterName)
        {
            return XmlNameAttribute(
                XmlName(DocumentationCommentXmlNames.NameAttributeName),
                Token(SyntaxKind.DoubleQuoteToken),
                parameterName,
                Token(SyntaxKind.DoubleQuoteToken))
                .WithLeadingTrivia(Whitespace(" "));
        }

        /// <summary>
        /// Creates a syntax node for a priliminary element within a xml documentation comment.
        /// </summary>
        public static XmlEmptyElementSyntax XmlPreliminaryElement()
        {
            return XmlEmptyElement(DocumentationCommentXmlNames.PreliminaryElementName);
        }

        /// <summary>
        /// Creates a syntax node for a cref attribute within a xml documentation comment.
        /// </summary>
        /// <param name="cref">The <see cref="CrefSyntax"/> used for the xml cref attribute syntax.</param>
        public static XmlCrefAttributeSyntax XmlCrefAttribute(CrefSyntax cref)
        {
            return XmlCrefAttribute(cref, SyntaxKind.DoubleQuoteToken);
        }

        /// <summary>
        /// Creates a syntax node for a cref attribute within a xml documentation comment.
        /// </summary>
        /// <param name="cref">The <see cref="CrefSyntax"/> used for the xml cref attribute syntax.</param>
        /// <param name="quoteKind">The kind of the quote for the referenced item in the cref attribute.</param>
        public static XmlCrefAttributeSyntax XmlCrefAttribute(CrefSyntax cref, SyntaxKind quoteKind)
        {
            cref = cref.ReplaceTokens(cref.DescendantTokens(), XmlReplaceBracketTokens);
            return XmlCrefAttribute(
                XmlName(DocumentationCommentXmlNames.CrefAttributeName),
                Token(quoteKind),
                cref,
                Token(quoteKind))
                .WithLeadingTrivia(Whitespace(" "));
        }

        /// <summary>
        /// Creates a remarks element within an xml documentation comment.
        /// </summary>
        /// <param name="content">A list of xml node syntax that will be the content within the remarks element.</param>
        public static XmlElementSyntax XmlRemarksElement(params XmlNodeSyntax[] content)
        {
            return XmlRemarksElement(List(content));
        }

        /// <summary>
        /// Creates a remarks element within an xml documentation comment.
        /// </summary>
        /// <param name="content">A list of xml node syntax that will be the content within the remarks element.</param>
        public static XmlElementSyntax XmlRemarksElement(SyntaxList<XmlNodeSyntax> content)
        {
            return XmlMultiLineElement(DocumentationCommentXmlNames.RemarksElementName, content);
        }

        /// <summary>
        /// Creates a returns element within an xml documentation comment.
        /// </summary>
        /// <param name="content">A list of xml node syntax that will be the content within the returns element.</param>
        public static XmlElementSyntax XmlReturnsElement(params XmlNodeSyntax[] content)
        {
            return XmlReturnsElement(List(content));
        }

        /// <summary>
        /// Creates a returns element within an xml documentation comment.
        /// </summary>
        /// <param name="content">A list of xml node syntax that will be the content within the returns element.</param>
        public static XmlElementSyntax XmlReturnsElement(SyntaxList<XmlNodeSyntax> content)
        {
            return XmlMultiLineElement(DocumentationCommentXmlNames.ReturnsElementName, content);
        }

        /// <summary>
        /// Creates the the syntax representation of an xml value element (e.g. for xml documentation comments).
        /// </summary>
        /// <param name="content">A list of xml syntax nodes that represents the content of the value element.</param>
        public static XmlElementSyntax XmlValueElement(params XmlNodeSyntax[] content)
        {
            return XmlValueElement(List(content));
        }

        /// <summary>
        /// Creates the the syntax representation of an xml value element (e.g. for xml documentation comments).
        /// </summary>
        /// <param name="content">A list of xml syntax nodes that represents the content of the value element.</param>
        public static XmlElementSyntax XmlValueElement(SyntaxList<XmlNodeSyntax> content)
        {
            return XmlMultiLineElement(DocumentationCommentXmlNames.ValueElementName, content);
        }

        /// <summary>
        /// Creates the syntax representation of an exception element within xml documentation comments.
        /// </summary>
        /// <param name="cref">Syntax representation of the reference to the exception type.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the exception element.</param>
        public static XmlElementSyntax XmlExceptionElement(CrefSyntax cref, params XmlNodeSyntax[] content)
        {
            return XmlExceptionElement(cref, List(content));
        }

        /// <summary>
        /// Creates the syntax representation of an exception element within xml documentation comments.
        /// </summary>
        /// <param name="cref">Syntax representation of the reference to the exception type.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the exception element.</param>
        public static XmlElementSyntax XmlExceptionElement(CrefSyntax cref, SyntaxList<XmlNodeSyntax> content)
        {
            XmlElementSyntax element = XmlElement(DocumentationCommentXmlNames.ExceptionElementName, content);
            return element.WithStartTag(element.StartTag.AddAttributes(XmlCrefAttribute(cref)));
        }

        /// <summary>
        /// Creates the syntax representation of a permission element within xml documentation comments.
        /// </summary>
        /// <param name="cref">Syntax representation of the reference to the permission type.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the permission element.</param>
        public static XmlElementSyntax XmlPermissionElement(CrefSyntax cref, params XmlNodeSyntax[] content)
        {
            return XmlPermissionElement(cref, List(content));
        }

        /// <summary>
        /// Creates the syntax representation of a permission element within xml documentation comments.
        /// </summary>
        /// <param name="cref">Syntax representation of the reference to the permission type.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the permission element.</param>
        public static XmlElementSyntax XmlPermissionElement(CrefSyntax cref, SyntaxList<XmlNodeSyntax> content)
        {
            XmlElementSyntax element = XmlElement(DocumentationCommentXmlNames.PermissionElementName, content);
            return element.WithStartTag(element.StartTag.AddAttributes(XmlCrefAttribute(cref)));
        }

        /// <summary>
        /// Creates the syntax representation of an example element within xml documentation comments.
        /// </summary>
        /// <param name="content">A list of syntax nodes that represents the content of the example element.</param>
        public static XmlElementSyntax XmlExampleElement(params XmlNodeSyntax[] content)
        {
            return XmlExampleElement(List(content));
        }

        /// <summary>
        /// Creates the syntax representation of an example element within xml documentation comments.
        /// </summary>
        /// <param name="content">A list of syntax nodes that represents the content of the example element.</param>
        public static XmlElementSyntax XmlExampleElement(SyntaxList<XmlNodeSyntax> content)
        {
            XmlElementSyntax element = XmlElement(DocumentationCommentXmlNames.ExampleElementName, content);
            return element.WithStartTag(element.StartTag);
        }

        /// <summary>
        /// Creates the syntax representation of a para element within xml documentation comments.
        /// </summary>
        /// <param name="content">A list of syntax nodes that represents the content of the para element.</param>
        public static XmlElementSyntax XmlParaElement(params XmlNodeSyntax[] content)
        {
            return XmlParaElement(List(content));
        }

        /// <summary>
        /// Creates the syntax representation of a para element within xml documentation comments.
        /// </summary>
        /// <param name="content">A list of syntax nodes that represents the content of the para element.</param>
        public static XmlElementSyntax XmlParaElement(SyntaxList<XmlNodeSyntax> content)
        {
            return XmlElement(DocumentationCommentXmlNames.ParaElementName, content);
        }

        /// <summary>
        /// Creates the syntax representation of a param element within xml documentation comments (e.g. for
        /// documentation of method parameters).
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the param element (e.g. 
        /// the description and meaning of the parameter).</param>
        public static XmlElementSyntax XmlParamElement(string parameterName, params XmlNodeSyntax[] content)
        {
            return XmlParamElement(parameterName, List(content));
        }

        /// <summary>
        /// Creates the syntax representation of a param element within xml documentation comments (e.g. for
        /// documentation of method parameters).
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the param element (e.g. 
        /// the description and meaning of the parameter).</param>
        public static XmlElementSyntax XmlParamElement(string parameterName, SyntaxList<XmlNodeSyntax> content)
        {
            XmlElementSyntax element = XmlElement(DocumentationCommentXmlNames.ParameterElementName, content);
            return element.WithStartTag(element.StartTag.AddAttributes(XmlNameAttribute(parameterName)));
        }

        /// <summary>
        /// Creates the syntax representation of a paramref element within xml documentation comments (e.g. for
        /// referencing particular parameters of a method).
        /// </summary>
        /// <param name="parameterName">The name of the referenced parameter.</param>
        public static XmlEmptyElementSyntax XmlParamRefElement(string parameterName)
        {
            return XmlEmptyElement(DocumentationCommentXmlNames.ParameterReferenceElementName).AddAttributes(XmlNameAttribute(parameterName));
        }

        /// <summary>
        /// Creates the syntax representation of a see element within xml documentation comments,
        /// that points to the 'null' language keyword.
        /// </summary>
        public static XmlEmptyElementSyntax XmlNullKeywordElement()
        {
            return XmlKeywordElement("null");
        }

        /// <summary>
        /// Creates the syntax representation of a see element within xml documentation comments,
        /// that points to a language keyword.
        /// </summary>
        /// <param name="keyword">The language keyword to which the see element points to.</param>
        private static XmlEmptyElementSyntax XmlKeywordElement(string keyword)
        {
            return XmlEmptyElement(DocumentationCommentXmlNames.SeeElementName).AddAttributes(
                XmlTextAttribute(DocumentationCommentXmlNames.KeywordElementName, keyword));
        }

        /// <summary>
        /// Creates the syntax representation of a placeholder element within xml documentation comments.
        /// </summary>
        /// <param name="content">A list of syntax nodes that represents the content of the placeholder element.</param>
        public static XmlElementSyntax XmlPlaceholderElement(params XmlNodeSyntax[] content)
        {
            return XmlPlaceholderElement(List(content));
        }

        /// <summary>
        /// Creates the syntax representation of a placeholder element within xml documentation comments.
        /// </summary>
        /// <param name="content">A list of syntax nodes that represents the content of the placeholder element.</param>
        public static XmlElementSyntax XmlPlaceholderElement(SyntaxList<XmlNodeSyntax> content)
        {
            return XmlElement(DocumentationCommentXmlNames.PlaceholderElementName, content);
        }

        /// <summary>
        /// Creates the syntax representation of a named empty xml element within xml documentation comments.
        /// </summary>
        /// <param name="localName">The name of the empty xml element.</param>
        public static XmlEmptyElementSyntax XmlEmptyElement(string localName)
        {
            return XmlEmptyElement(XmlName(localName));
        }

        /// <summary>
        /// Creates the syntax representation of a named xml element within xml documentation comments.
        /// </summary>
        /// <param name="localName">The name of the empty xml element.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the xml element.</param>
        public static XmlElementSyntax XmlElement(string localName, SyntaxList<XmlNodeSyntax> content)
        {
            return XmlElement(XmlName(localName), content);
        }

        /// <summary>
        /// Creates the syntax representation of a named xml element within xml documentation comments.
        /// </summary>
        /// <param name="name">The name of the empty xml element.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the xml element.</param>
        public static XmlElementSyntax XmlElement(XmlNameSyntax name, SyntaxList<XmlNodeSyntax> content)
        {
            return XmlElement(
                XmlElementStartTag(name),
                content,
                XmlElementEndTag(name));
        }

        /// <summary>
        /// Creates the syntax representation of an xml text attribute.
        /// </summary>
        /// <param name="name">The name of the xml text attribute.</param>
        /// <param name="value">The value of the xml text attribute.</param>
        public static XmlTextAttributeSyntax XmlTextAttribute(string name, string value)
        {
            return XmlTextAttribute(name, XmlTextLiteral(value));
        }

        /// <summary>
        /// Creates the syntax representation of an xml text attribute.
        /// </summary>
        /// <param name="name">The name of the xml text attribute.</param>
        /// <param name="textTokens">A list of tokens used for the value of the xml text attribute.</param>
        public static XmlTextAttributeSyntax XmlTextAttribute(string name, params SyntaxToken[] textTokens)
        {
            return XmlTextAttribute(XmlName(name), SyntaxKind.DoubleQuoteToken, TokenList(textTokens));
        }

        /// <summary>
        /// Creates the syntax representation of an xml text attribute.
        /// </summary>
        /// <param name="name">The name of the xml text attribute.</param>
        /// <param name="quoteKind">The kind of the quote token to be used to quote the value (e.g. " or ').</param>
        /// <param name="textTokens">A list of tokens used for the value of the xml text attribute.</param>
        public static XmlTextAttributeSyntax XmlTextAttribute(string name, SyntaxKind quoteKind, SyntaxTokenList textTokens)
        {
            return XmlTextAttribute(XmlName(name), SyntaxKind.DoubleQuoteToken, textTokens);
        }

        /// <summary>
        /// Creates the syntax representation of an xml text attribute.
        /// </summary>
        /// <param name="name">The name of the xml text attribute.</param>
        /// <param name="quoteKind">The kind of the quote token to be used to quote the value (e.g. " or ').</param>
        /// <param name="textTokens">A list of tokens used for the value of the xml text attribute.</param>
        public static XmlTextAttributeSyntax XmlTextAttribute(XmlNameSyntax name, SyntaxKind quoteKind, SyntaxTokenList textTokens)
        {
            return XmlTextAttribute(name, Token(quoteKind), textTokens, Token(quoteKind))
                .WithLeadingTrivia(Whitespace(" "));
        }

        /// <summary>
        /// Creates the syntax representation of an xml element that spans multiple text lines.
        /// </summary>
        /// <param name="localName">The name of the xml element.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the xml multi line element.</param>
        public static XmlElementSyntax XmlMultiLineElement(string localName, SyntaxList<XmlNodeSyntax> content)
        {
            return XmlMultiLineElement(XmlName(localName), content);
        }

        /// <summary>
        /// Creates the syntax representation of an xml element that spans multiple text lines.
        /// </summary>
        /// <param name="name">The name of the xml element.</param>
        /// <param name="content">A list of syntax nodes that represents the content of the xml multi line element.</param>
        public static XmlElementSyntax XmlMultiLineElement(XmlNameSyntax name, SyntaxList<XmlNodeSyntax> content)
        {
            return XmlElement(
                XmlElementStartTag(name),
                content,
                XmlElementEndTag(name));
        }

        /// <summary>
        /// Creates the syntax representation of an xml text that contains a newline token with a documentation comment 
        /// exterior trivia at the end (continued documentation comment).
        /// </summary>
        /// <param name="text">The raw text within the new line.</param>
        public static XmlTextSyntax XmlNewLine(string text)
        {
            return XmlText(XmlTextNewLine(text));
        }

        /// <summary>
        /// Creates the syntax representation of an xml newline token with a documentation comment exterior trivia at 
        /// the end (continued documentation comment).
        /// </summary>
        /// <param name="text">The raw text within the new line.</param>
        public static SyntaxToken XmlTextNewLine(string text)
        {
            return XmlTextNewLine(text, true);
        }

        /// <summary>
        /// Creates a token with kind XmlTextLiteralNewLineToken.
        /// </summary>
        /// <param name="leading">A list of trivia immediately preceding the token.</param>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The xml text new line value.</param>
        /// <param name="trailing">A list of trivia immediately following the token.</param>
        public static SyntaxToken XmlTextNewLine(SyntaxTriviaList leading, string text, string value, SyntaxTriviaList trailing)
        {
            return new SyntaxToken(
                InternalSyntax.SyntaxFactory.XmlTextNewLine(
                    (InternalSyntax.CSharpSyntaxNode)leading.Node,
                    text,
                    value,
                    (InternalSyntax.CSharpSyntaxNode)trailing.Node));
        }

        /// <summary>
        /// Creates the syntax representation of an xml newline token for xml documentation comments.
        /// </summary>
        /// <param name="text">The raw text within the new line.</param>
        /// <param name="continueXmlDocumentationComment">
        /// If set to true, a documentation comment exterior token will be added to the trailing trivia
        /// of the new token.</param>
        public static SyntaxToken XmlTextNewLine(string text, bool continueXmlDocumentationComment)
        {
            var token = new SyntaxToken(
                InternalSyntax.SyntaxFactory.XmlTextNewLine(
                    (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode,
                    text,
                    text,
                    (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));

            if (continueXmlDocumentationComment)
                token = token.WithTrailingTrivia(token.TrailingTrivia.Add(DocumentationCommentExterior("/// ")));

            return token;
        }

        /// <summary>
        /// Generates the syntax representation of a xml text node (e.g. for xml documentation comments).
        /// </summary>
        /// <param name="value">The string literal used as the text of the xml text node.</param>
        public static XmlTextSyntax XmlText(string value)
        {
            return XmlText(XmlTextLiteral(value));
        }

        /// <summary>
        /// Generates the syntax representation of a xml text node (e.g. for xml documentation comments).
        /// </summary>
        /// <param name="textTokens">A list of text tokens used as the text of the xml text node.</param>
        public static XmlTextSyntax XmlText(params SyntaxToken[] textTokens)
        {
            return XmlText(TokenList(textTokens));
        }

        /// <summary>
        /// Generates the syntax representation of an xml text literal.
        /// </summary>
        /// <param name="value">The text used within the xml text literal.</param>
        public static SyntaxToken XmlTextLiteral(string value)
        {
            // TODO: [RobinSedlaczek] It is no compiler hot path here I think. But the contribution guide
            //       states to avoid LINQ (https://github.com/dotnet/roslyn/wiki/Contributing-Code). With
            //       XText we have a reference to System.Xml.Linq. Isn't this rule valid here? 
            string encoded = new XText(value).ToString();

            return XmlTextLiteral(
                TriviaList(),
                encoded,
                value,
                TriviaList());
        }

        /// <summary>
        /// Generates the syntax representation of an xml text literal.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        /// <param name="value">The text used within the xml text literal.</param>
        public static SyntaxToken XmlTextLiteral(string text, string value)
        {
            return new SyntaxToken(Syntax.InternalSyntax.SyntaxFactory.XmlTextLiteral((InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode, text, value, (InternalSyntax.CSharpSyntaxNode)ElasticMarker.UnderlyingNode));
        }

        /// <summary>
        /// Helper method that replaces less-than and greater-than characters with brackets. 
        /// </summary>
        /// <param name="originalToken">The original token that is to be replaced.</param>
        /// <param name="rewrittenToken">The new rewritten token.</param>
        /// <returns>Returns the new rewritten token with replaced characters.</returns>
        private static SyntaxToken XmlReplaceBracketTokens(SyntaxToken originalToken, SyntaxToken rewrittenToken)
        {
            if (rewrittenToken.IsKind(SyntaxKind.LessThanToken) && string.Equals("<", rewrittenToken.Text, StringComparison.Ordinal))
                return Token(rewrittenToken.LeadingTrivia, SyntaxKind.LessThanToken, "{", rewrittenToken.ValueText, rewrittenToken.TrailingTrivia);

            if (rewrittenToken.IsKind(SyntaxKind.GreaterThanToken) && string.Equals(">", rewrittenToken.Text, StringComparison.Ordinal))
                return Token(rewrittenToken.LeadingTrivia, SyntaxKind.GreaterThanToken, "}", rewrittenToken.ValueText, rewrittenToken.TrailingTrivia);

            return rewrittenToken;
        }

        /// <summary>
        /// Creates a trivia with kind DocumentationCommentExteriorTrivia.
        /// </summary>
        /// <param name="text">The raw text of the literal.</param>
        public static SyntaxTrivia DocumentationCommentExterior(string text)
        {
            return Syntax.InternalSyntax.SyntaxFactory.DocumentationCommentExteriorTrivia(text);
        }

        /// <summary>
        /// Creates an empty list of syntax nodes.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        public static SyntaxList<TNode> List<TNode>() where TNode : SyntaxNode
        {
            return default(SyntaxList<TNode>);
        }

        /// <summary>
        /// Creates a singleton list of syntax nodes.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="node">The single element node.</param>
        /// <returns></returns>
        public static SyntaxList<TNode> SingletonList<TNode>(TNode node) where TNode : SyntaxNode
        {
            return new SyntaxList<TNode>(node);
        }


        /// <summary>
        /// Creates a list of syntax nodes.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="nodes">A sequence of element nodes.</param>
        public static SyntaxList<TNode> List<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode
        {
            if (nodes != null)
            {
                var collection = nodes as ICollection<TNode>;
                var builder = (collection != null) ? new SyntaxListBuilder<TNode>(collection.Count) : SyntaxListBuilder<TNode>.Create();

                foreach (TNode node in nodes)
                {
                    builder.Add(node);
                }

                return builder.ToList();
            }

            return default(SyntaxList<TNode>);
        }

        /// <summary>
        /// Creates an empty list of tokens.
        /// </summary>
        public static SyntaxTokenList TokenList()
        {
            return default(SyntaxTokenList);
        }

        /// <summary>
        /// Creates a singleton list of tokens.
        /// </summary>
        /// <param name="token">The single token.</param>
        public static SyntaxTokenList TokenList(SyntaxToken token)
        {
            return new SyntaxTokenList(token);
        }

        /// <summary>
        /// Creates a list of tokens.
        /// </summary>
        /// <param name="tokens">An array of tokens.</param>
        public static SyntaxTokenList TokenList(params SyntaxToken[] tokens)
        {
            if (tokens == null)
            {
                return default(SyntaxTokenList);
            }

            SyntaxTokenListBuilder builder = new SyntaxTokenListBuilder(tokens.Length);
            for (int i = 0; i < tokens.Length; i++)
            {
                builder.Add((InternalSyntax.SyntaxToken)tokens[i].Node);
            }

            return builder.ToList();
        }

        /// <summary>
        /// Creates a list of tokens.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public static SyntaxTokenList TokenList(IEnumerable<SyntaxToken> tokens)
        {
            if (tokens == null)
            {
                return default(SyntaxTokenList);
            }

            SyntaxTokenListBuilder builder = SyntaxTokenListBuilder.Create();
            foreach (SyntaxToken token in tokens)
            {
                builder.Add((InternalSyntax.SyntaxToken)token.Node);
            }

            return builder.ToList();
        }

        /// <summary>
        /// Creates a trivia from a StructuredTriviaSyntax node.
        /// </summary>
        public static SyntaxTrivia Trivia(StructuredTriviaSyntax node)
        {
            return new SyntaxTrivia(default(SyntaxToken), node.Green, position: 0, index: 0);
        }

        /// <summary>
        /// Creates an empty list of trivia.
        /// </summary>
        public static SyntaxTriviaList TriviaList()
        {
            return default(SyntaxTriviaList);
        }

        /// <summary>
        /// Creates a singleton list of trivia.
        /// </summary>
        /// <param name="trivia">A single trivia.</param>
        public static SyntaxTriviaList TriviaList(SyntaxTrivia trivia)
        {
            return new SyntaxTriviaList(trivia);
        }

        /// <summary>
        /// Creates a list of trivia.
        /// </summary>
        /// <param name="trivias">An array of trivia.</param>
        public static SyntaxTriviaList TriviaList(params SyntaxTrivia[] trivias)
        {
            if (trivias != null)
            {
                SyntaxTriviaListBuilder builder = new SyntaxTriviaListBuilder(trivias.Length);
                builder.Add(trivias);
                return builder.ToList();
            }

            return default(SyntaxTriviaList);
        }

        /// <summary>
        /// Creates a list of trivia.
        /// </summary>
        /// <param name="trivias">A sequence of trivia.</param>
        public static SyntaxTriviaList TriviaList(IEnumerable<SyntaxTrivia> trivias)
        {
            if (trivias == null)
            {
                return default(SyntaxTriviaList);
            }

            SyntaxTriviaListBuilder builder = SyntaxTriviaListBuilder.Create();
            foreach (SyntaxTrivia trivia in trivias)
            {
                builder.Add(trivia);
            }

            return builder.ToList();
        }

        /// <summary>
        /// Creates an empty separated list.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>() where TNode : SyntaxNode
        {
            return default(SeparatedSyntaxList<TNode>);
        }

        /// <summary>
        /// Creates a singleton separated list.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="node">A single node.</param>
        public static SeparatedSyntaxList<TNode> SingletonSeparatedList<TNode>(TNode node) where TNode : SyntaxNode
        {
            return new SeparatedSyntaxList<TNode>(new SyntaxNodeOrTokenList(node, index: 0));
        }

        /// <summary>
        /// Creates a separated list of nodes from a sequence of nodes, synthesizing comma separators in between.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="nodes">A sequence of syntax nodes.</param>
        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(IEnumerable<TNode> nodes) where TNode : SyntaxNode
        {
            if (nodes == null)
            {
                return default(SeparatedSyntaxList<TNode>);
            }

            var collection = nodes as ICollection<TNode>;

            if (collection != null && collection.Count == 0)
            {
                return default(SeparatedSyntaxList<TNode>);
            }

            using (var enumerator = nodes.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return default(SeparatedSyntaxList<TNode>);
                }

                var firstNode = enumerator.Current;

                if (!enumerator.MoveNext())
                {
                    return SingletonSeparatedList<TNode>(firstNode);
                }

                var builder = new SeparatedSyntaxListBuilder<TNode>(collection != null ? collection.Count : 3);

                builder.Add(firstNode);

                var commaToken = Token(SyntaxKind.CommaToken);

                do
                {
                    builder.AddSeparator(commaToken);
                    builder.Add(enumerator.Current);
                }
                while (enumerator.MoveNext());

                return builder.ToList();
            }
        }

        /// <summary>
        /// Creates a separated list of nodes from a sequence of nodes and a sequence of separator tokens.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="nodes">A sequence of syntax nodes.</param>
        /// <param name="separators">A sequence of token to be interleaved between the nodes. The number of tokens must
        /// be one less than the number of nodes.</param>
        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(IEnumerable<TNode> nodes, IEnumerable<SyntaxToken> separators) where TNode : SyntaxNode
        {
            // Interleave the nodes and the separators.  The number of separators must be equal to or 1 less than the number of nodes or
            // an argument exception is thrown.

            if (nodes != null)
            {
                IEnumerator<TNode> enumerator = nodes.GetEnumerator();
                SeparatedSyntaxListBuilder<TNode> builder = SeparatedSyntaxListBuilder<TNode>.Create();
                if (separators != null)
                {
                    foreach (SyntaxToken token in separators)
                    {
                        if (!enumerator.MoveNext())
                        {
                            throw new ArgumentException();
                        }

                        builder.Add(enumerator.Current);
                        builder.AddSeparator(token);
                    }
                }

                if (enumerator.MoveNext())
                {
                    builder.Add(enumerator.Current);
                    if (enumerator.MoveNext())
                    {
                        throw new ArgumentException();
                    }
                }

                return builder.ToList();
            }

            if (separators != null)
            {
                throw new ArgumentException();
            }

            return default(SeparatedSyntaxList<TNode>);
        }

        /// <summary>
        /// Creates a separated list from a sequence of nodes and tokens, starting with a node and alternating between additional nodes and separator tokens.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="nodesAndTokens">A sequence of nodes or tokens, alternating between nodes and separator tokens.</param>
        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(IEnumerable<SyntaxNodeOrToken> nodesAndTokens) where TNode : SyntaxNode
        {
            return SeparatedList<TNode>(NodeOrTokenList(nodesAndTokens));
        }

        /// <summary>
        /// Creates a separated list from a <see cref="SyntaxNodeOrTokenList"/>, where the list elements start with a node and then alternate between
        /// additional nodes and separator tokens.
        /// </summary>
        /// <typeparam name="TNode">The specific type of the element nodes.</typeparam>
        /// <param name="nodesAndTokens">The list of nodes and tokens.</param>
        public static SeparatedSyntaxList<TNode> SeparatedList<TNode>(SyntaxNodeOrTokenList nodesAndTokens) where TNode : SyntaxNode
        {
            if (!HasSeparatedNodeTokenPattern(nodesAndTokens))
            {
                throw new ArgumentException(CodeAnalysisResources.NodeOrTokenOutOfSequence);
            }

            if (!NodesAreCorrectType<TNode>(nodesAndTokens))
            {
                throw new ArgumentException(CodeAnalysisResources.UnexpectedTypeOfNodeInList);
            }

            return new SeparatedSyntaxList<TNode>(nodesAndTokens);
        }

        private static bool NodesAreCorrectType<TNode>(SyntaxNodeOrTokenList list)
        {
            for (int i = 0, n = list.Count; i < n; i++)
            {
                var element = list[i];
                if (element.IsNode && !(element.AsNode() is TNode))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasSeparatedNodeTokenPattern(SyntaxNodeOrTokenList list)
        {
            for (int i = 0, n = list.Count; i < n; i++)
            {
                var element = list[i];
                if (element.IsToken == ((i & 1) == 0))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates an empty <see cref="SyntaxNodeOrTokenList"/>.
        /// </summary>
        public static SyntaxNodeOrTokenList NodeOrTokenList()
        {
            return default(SyntaxNodeOrTokenList);
        }

        /// <summary>
        /// Create a <see cref="SyntaxNodeOrTokenList"/> from a sequence of <see cref="SyntaxNodeOrToken"/>.
        /// </summary>
        /// <param name="nodesAndTokens">The sequence of nodes and tokens</param>
        public static SyntaxNodeOrTokenList NodeOrTokenList(IEnumerable<SyntaxNodeOrToken> nodesAndTokens)
        {
            if (nodesAndTokens == null)
            {
                throw new ArgumentNullException(nameof(nodesAndTokens));
            }

            var builder = new SyntaxNodeOrTokenListBuilder(8);
            builder.Add(nodesAndTokens);
            return builder.ToList();
        }

        /// <summary>
        /// Create a <see cref="SyntaxNodeOrTokenList"/> from one or more <see cref="SyntaxNodeOrToken"/>.
        /// </summary>
        /// <param name="nodesAndTokens">The nodes and tokens</param>
        public static SyntaxNodeOrTokenList NodeOrTokenList(params SyntaxNodeOrToken[] nodesAndTokens)
        {
            return NodeOrTokenList((IEnumerable<SyntaxNodeOrToken>)nodesAndTokens);
        }

        /// <summary>
        /// Creates an IdentifierNameSyntax node.
        /// </summary>
        /// <param name="name">The identifier name.</param>
        public static IdentifierNameSyntax IdentifierName(string name)
        {
            return IdentifierName(Identifier(name));
        }

        // direct access to parsing for common grammar areas

        /// <summary>
        /// Create a new syntax tree from a syntax node.
        /// </summary>
        public static SyntaxTree SyntaxTree(SyntaxNode root, ParseOptions options = null, string path = "", Encoding encoding = null)
        {
            return CSharpSyntaxTree.Create((CSharpSyntaxNode)root, (CSharpParseOptions)options, path, encoding);
        }

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        public static SyntaxTree ParseSyntaxTree(
            string text,
            ParseOptions options = null,
            string path = "",
            Encoding encoding = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return ParseSyntaxTree(SourceText.From(text, encoding), options, path, cancellationToken);
        }

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        public static SyntaxTree ParseSyntaxTree(
            SourceText text,
            ParseOptions options = null,
            string path = "",
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return CSharpSyntaxTree.ParseText(text, (CSharpParseOptions)options, path, cancellationToken);
        }

        /// <summary>
        /// Parse a list of trivia rules for leading trivia.
        /// </summary>
        public static SyntaxTriviaList ParseLeadingTrivia(string text, int offset = 0)
        {
            return ParseLeadingTrivia(text, CSharpParseOptions.Default, offset);
        }

        /// <summary>
        /// Parse a list of trivia rules for leading trivia.
        /// </summary>
        internal static SyntaxTriviaList ParseLeadingTrivia(string text, CSharpParseOptions options, int offset = 0)
        {
            using (var lexer = new InternalSyntax.Lexer(MakeSourceText(text, offset), options))
            {
                return lexer.LexSyntaxLeadingTrivia();
            }
        }

        /// <summary>
        /// Parse a list of trivia using the parsing rules for trailing trivia.
        /// </summary>
        public static SyntaxTriviaList ParseTrailingTrivia(string text, int offset = 0)
        {
            using (var lexer = new InternalSyntax.Lexer(MakeSourceText(text, offset), CSharpParseOptions.Default))
            {
                return lexer.LexSyntaxTrailingTrivia();
            }
        }

        // TODO: If this becomes a real API, we'll need to add an offset parameter to
        // match the pattern followed by the other ParseX methods.
        internal static CrefSyntax ParseCref(string text)
        {
            // NOTE: Conceivably, we could introduce a new code path that directly calls
            // DocumentationCommentParser.ParseCrefAttributeValue, but that method won't
            // work unless the lexer makes the appropriate mode transitions.  Rather than
            // introducing a new code path that will have to be kept in sync with other
            // mode changes distributed throughout Lexer, SyntaxParser, and 
            // DocumentationCommentParser, we'll just wrap the text in some lexable syntax
            // and then extract the piece we want.
            string commentText = string.Format(@"/// <see cref=""{0}""/>", text);

            SyntaxTriviaList leadingTrivia = ParseLeadingTrivia(commentText, CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
            Debug.Assert(leadingTrivia.Count == 1);
            SyntaxTrivia trivia = leadingTrivia.First();
            DocumentationCommentTriviaSyntax structure = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
            Debug.Assert(structure.Content.Count == 2);
            XmlEmptyElementSyntax elementSyntax = (XmlEmptyElementSyntax)structure.Content[1];
            Debug.Assert(elementSyntax.Attributes.Count == 1);
            XmlAttributeSyntax attributeSyntax = (XmlAttributeSyntax)elementSyntax.Attributes[0];
            return attributeSyntax.Kind() == SyntaxKind.XmlCrefAttribute ? ((XmlCrefAttributeSyntax)attributeSyntax).Cref : null;
        }

        /// <summary>
        /// Parse a C# language token.
        /// </summary>
        /// <param name="text">The text of the token including leading and trailing trivia.</param>
        /// <param name="offset">Optional offset into text.</param>
        public static SyntaxToken ParseToken(string text, int offset = 0)
        {
            using (var lexer = new InternalSyntax.Lexer(MakeSourceText(text, offset), CSharpParseOptions.Default))
            {
                return new SyntaxToken(lexer.Lex(InternalSyntax.LexerMode.Syntax));
            }
        }

        /// <summary>
        /// Parse a sequence of C# language tokens.
        /// </summary>
        /// <param name="text">The text of all the tokens.</param>
        /// <param name="initialTokenPosition">An integer to use as the starting position of the first token.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">Parse options.</param>
        public static IEnumerable<SyntaxToken> ParseTokens(string text, int offset = 0, int initialTokenPosition = 0, CSharpParseOptions options = null)
        {
            using (var lexer = new InternalSyntax.Lexer(MakeSourceText(text, offset), options ?? CSharpParseOptions.Default))
            {
                var position = initialTokenPosition;
                while (true)
                {
                    var token = lexer.Lex(InternalSyntax.LexerMode.Syntax);
                    yield return new SyntaxToken(parent: null, token: token, position: position, index: 0);

                    position += token.FullWidth;

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Parse a NameSyntax node using the grammar rule for names.
        /// </summary>
        public static NameSyntax ParseName(string text, int offset = 0, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseName();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (NameSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse a TypeNameSyntax node using the grammar rule for type names.
        /// </summary>
        public static TypeSyntax ParseTypeName(string text, int offset = 0, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseTypeName();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (TypeSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse an ExpressionSyntax node using the lowest precedence grammar rule for expressions.
        /// </summary>
        /// <param name="text">The text of the expression.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static ExpressionSyntax ParseExpression(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseExpression();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (ExpressionSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse a StatementSyntaxNode using grammar rule for statements.
        /// </summary>
        /// <param name="text">The text of the statement.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static StatementSyntax ParseStatement(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseStatement();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (StatementSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse a CompilationUnitSyntax using the grammar rule for an entire compilation unit (file). To produce a
        /// SyntaxTree instance, use CSharpSyntaxTree.ParseText instead.
        /// </summary>
        /// <param name="text">The text of the compilation unit.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        public static CompilationUnitSyntax ParseCompilationUnit(string text, int offset = 0, CSharpParseOptions options = null)
        {
            // note that we do not need a "consumeFullText" parameter, because parsing a compilation unit always must
            // consume input until the end-of-file
            using (var lexer = MakeLexer(text, offset, options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseCompilationUnit();
                // if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (CompilationUnitSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse a ParameterListSyntax node.
        /// </summary>
        /// <param name="text">The text of the parenthesized parameter list.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static ParameterListSyntax ParseParameterList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseParenthesizedParameterList(allowThisKeyword: true, allowDefaults: true, allowAttributes: true);
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (ParameterListSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse a BracketedParameterListSyntax node.
        /// </summary>
        /// <param name="text">The text of the bracketed parameter list.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static BracketedParameterListSyntax ParseBracketedParameterList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseBracketedParameterList();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (BracketedParameterListSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse an ArgumentListSyntax node.
        /// </summary>
        /// <param name="text">The text of the parenthesized argument list.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static ArgumentListSyntax ParseArgumentList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseParenthesizedArgumentList();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (ArgumentListSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse a BracketedArgumentListSyntax node.
        /// </summary>
        /// <param name="text">The text of the bracketed argument list.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static BracketedArgumentListSyntax ParseBracketedArgumentList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseBracketedArgumentList();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (BracketedArgumentListSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Parse an AttributeArgumentListSyntax node.
        /// </summary>
        /// <param name="text">The text of the attribute argument list.</param>
        /// <param name="offset">Optional offset into text.</param>
        /// <param name="options">The optional parse options to use. If no options are specified default options are
        /// used.</param>
        /// <param name="consumeFullText">True if extra tokens in the input should be treated as an error</param>
        public static AttributeArgumentListSyntax ParseAttributeArgumentList(string text, int offset = 0, ParseOptions options = null, bool consumeFullText = true)
        {
            using (var lexer = MakeLexer(text, offset, (CSharpParseOptions)options))
            using (var parser = MakeParser(lexer))
            {
                var node = parser.ParseAttributeArgumentList();
                if (consumeFullText) node = parser.ConsumeUnexpectedTokens(node);
                return (AttributeArgumentListSyntax)node.CreateRed();
            }
        }

        /// <summary>
        /// Helper method for wrapping a string in an SourceText.
        /// </summary>
        private static SourceText MakeSourceText(string text, int offset)
        {
            return SourceText.From(text, Encoding.UTF8).GetSubText(offset);
        }

        private static InternalSyntax.Lexer MakeLexer(string text, int offset, CSharpParseOptions options = null)
        {
            return new InternalSyntax.Lexer(
                text: MakeSourceText(text, offset),
                options: options ?? CSharpParseOptions.Default);
        }

        private static InternalSyntax.LanguageParser MakeParser(InternalSyntax.Lexer lexer)
        {
            return new InternalSyntax.LanguageParser(lexer, oldTree: null, changes: null);
        }

        /// <summary>
        /// Determines if two trees are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldTree">The original tree.</param>
        /// <param name="newTree">The new tree.</param>
        /// <param name="topLevel"> 
        /// If true then the trees are equivalent if the contained nodes and tokens declaring
        /// metadata visible symbolic information are equivalent, ignoring any differences of nodes inside method bodies
        /// or initializer expressions, otherwise all nodes and tokens must be equivalent. 
        /// </param>
        public static bool AreEquivalent(SyntaxTree oldTree, SyntaxTree newTree, bool topLevel)
        {
            var csOld = oldTree as SyntaxTree;
            var csNew = newTree as SyntaxTree;

            if (csOld == null && csNew == null)
                return true;

            if (csOld == null || csNew == null)
                return false;

            return SyntaxEquivalence.AreEquivalent(csOld, csNew, ignoreChildNode: null, topLevel: topLevel);
        }

        /// <summary>
        /// Determines if two syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldNode">The old node.</param>
        /// <param name="newNode">The new node.</param>
        /// <param name="topLevel"> 
        /// If true then the nodes are equivalent if the contained nodes and tokens declaring
        /// metadata visible symbolic information are equivalent, ignoring any differences of nodes inside method bodies
        /// or initializer expressions, otherwise all nodes and tokens must be equivalent. 
        /// </param>
        public static bool AreEquivalent(SyntaxNode oldNode, SyntaxNode newNode, bool topLevel)
        {
            return SyntaxEquivalence.AreEquivalent(oldNode, newNode, ignoreChildNode: null, topLevel: topLevel);
        }

        /// <summary>
        /// Determines if two syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldNode">The old node.</param>
        /// <param name="newNode">The new node.</param>
        /// <param name="ignoreChildNode">
        /// If specified called for every child syntax node (not token) that is visited during the comparison. 
        /// If it returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        /// </param>
        public static bool AreEquivalent(SyntaxNode oldNode, SyntaxNode newNode, Func<SyntaxKind, bool> ignoreChildNode = null)
        {
            return SyntaxEquivalence.AreEquivalent(oldNode, newNode, ignoreChildNode: ignoreChildNode, topLevel: false);
        }

        /// <summary>
        /// Determines if two syntax tokens are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldToken">The old token.</param>
        /// <param name="newToken">The new token.</param>
        public static bool AreEquivalent(SyntaxToken oldToken, SyntaxToken newToken)
        {
            return SyntaxEquivalence.AreEquivalent(oldToken, newToken);
        }

        /// <summary>
        /// Determines if two lists of tokens are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldList">The old token list.</param>
        /// <param name="newList">The new token list.</param>
        public static bool AreEquivalent(SyntaxTokenList oldList, SyntaxTokenList newList)
        {
            return SyntaxEquivalence.AreEquivalent(oldList, newList);
        }

        /// <summary>
        /// Determines if two lists of syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldList">The old list.</param>
        /// <param name="newList">The new list.</param>
        /// <param name="topLevel"> 
        /// If true then the nodes are equivalent if the contained nodes and tokens declaring
        /// metadata visible symbolic information are equivalent, ignoring any differences of nodes inside method bodies
        /// or initializer expressions, otherwise all nodes and tokens must be equivalent. 
        /// </param>
        public static bool AreEquivalent<TNode>(SyntaxList<TNode> oldList, SyntaxList<TNode> newList, bool topLevel)
            where TNode : CSharpSyntaxNode
        {
            return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, null, topLevel);
        }

        /// <summary>
        /// Determines if two lists of syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldList">The old list.</param>
        /// <param name="newList">The new list.</param>
        /// <param name="ignoreChildNode">
        /// If specified called for every child syntax node (not token) that is visited during the comparison. 
        /// If it returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        /// </param>
        public static bool AreEquivalent<TNode>(SyntaxList<TNode> oldList, SyntaxList<TNode> newList, Func<SyntaxKind, bool> ignoreChildNode = null)
            where TNode : SyntaxNode
        {
            return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, ignoreChildNode, topLevel: false);
        }

        /// <summary>
        /// Determines if two lists of syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldList">The old list.</param>
        /// <param name="newList">The new list.</param>
        /// <param name="topLevel"> 
        /// If true then the nodes are equivalent if the contained nodes and tokens declaring
        /// metadata visible symbolic information are equivalent, ignoring any differences of nodes inside method bodies
        /// or initializer expressions, otherwise all nodes and tokens must be equivalent. 
        /// </param>
        public static bool AreEquivalent<TNode>(SeparatedSyntaxList<TNode> oldList, SeparatedSyntaxList<TNode> newList, bool topLevel)
            where TNode : SyntaxNode
        {
            return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, null, topLevel);
        }

        /// <summary>
        /// Determines if two lists of syntax nodes are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="oldList">The old list.</param>
        /// <param name="newList">The new list.</param>
        /// <param name="ignoreChildNode">
        /// If specified called for every child syntax node (not token) that is visited during the comparison. 
        /// If it returns true the child is recursively visited, otherwise the child and its subtree is disregarded.
        /// </param>
        public static bool AreEquivalent<TNode>(SeparatedSyntaxList<TNode> oldList, SeparatedSyntaxList<TNode> newList, Func<SyntaxKind, bool> ignoreChildNode = null)
            where TNode : SyntaxNode
        {
            return SyntaxEquivalence.AreEquivalent(oldList.Node, newList.Node, ignoreChildNode, topLevel: false);
        }

        internal static TypeSyntax GetStandaloneType(TypeSyntax node)
        {
            if (node != null)
            {
                var parent = node.Parent as ExpressionSyntax;
                if (parent != null && (node.Kind() == SyntaxKind.IdentifierName || node.Kind() == SyntaxKind.GenericName))
                {
                    switch (parent.Kind())
                    {
                        case SyntaxKind.QualifiedName:
                            var qualifiedName = (QualifiedNameSyntax)parent;
                            if (qualifiedName.Right == node)
                            {
                                return qualifiedName;
                            }

                            break;
                        case SyntaxKind.AliasQualifiedName:
                            var aliasQualifiedName = (AliasQualifiedNameSyntax)parent;
                            if (aliasQualifiedName.Name == node)
                            {
                                return aliasQualifiedName;
                            }

                            break;
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Gets the containing expression that is actually a language expression and not just typed
        /// as an ExpressionSyntax for convenience. For example, NameSyntax nodes on the right side
        /// of qualified names and member access expressions are not language expressions, yet the
        /// containing qualified names or member access expressions are indeed expressions.
        /// </summary>
        public static ExpressionSyntax GetStandaloneExpression(ExpressionSyntax expression)
        {
            return SyntaxFactory.GetStandaloneNode(expression) as ExpressionSyntax ?? expression;
        }

        /// <summary>
        /// Gets the containing expression that is actually a language expression and not just typed
        /// as an ExpressionSyntax for convenience. For example, NameSyntax nodes on the right side
        /// of qualified names and member access expressions are not language expressions, yet the
        /// containing qualified names or member access expressions are indeed expressions.
        /// Similarly, if the input node is a cref part that is not independently meaningful, then
        /// the result will be the full cref.
        /// </summary>
        internal static CSharpSyntaxNode GetStandaloneNode(CSharpSyntaxNode node)
        {
            if (node == null || !(node is ExpressionSyntax || node is CrefSyntax))
            {
                return node;
            }

            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                case SyntaxKind.GenericName:
                case SyntaxKind.NameMemberCref:
                case SyntaxKind.IndexerMemberCref:
                case SyntaxKind.OperatorMemberCref:
                case SyntaxKind.ConversionOperatorMemberCref:
                case SyntaxKind.ArrayType:
                case SyntaxKind.NullableType:
                    // Adjustment may be required.
                    break;
                default:
                    return node;
            }

            CSharpSyntaxNode parent = node.Parent;

            if (parent == null)
            {
                return node;
            }

            switch (parent.Kind())
            {
                case SyntaxKind.QualifiedName:
                    if (((QualifiedNameSyntax)parent).Right == node)
                    {
                        return parent;
                    }

                    break;
                case SyntaxKind.AliasQualifiedName:
                    if (((AliasQualifiedNameSyntax)parent).Name == node)
                    {
                        return parent;
                    }

                    break;
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                    if (((MemberAccessExpressionSyntax)parent).Name == node)
                    {
                        return parent;
                    }

                    break;

                case SyntaxKind.MemberBindingExpression:
                    {
                        if (((MemberBindingExpressionSyntax)parent).Name == node)
                        {
                            return parent;
                        }

                        break;
                    }

                // Only care about name member crefs because the other cref members
                // are identifier by keywords, not syntax nodes.
                case SyntaxKind.NameMemberCref:
                    if (((NameMemberCrefSyntax)parent).Name == node)
                    {
                        CSharpSyntaxNode grandparent = parent.Parent;
                        return grandparent != null && grandparent.Kind() == SyntaxKind.QualifiedCref
                            ? grandparent
                            : parent;
                    }

                    break;

                case SyntaxKind.QualifiedCref:
                    if (((QualifiedCrefSyntax)parent).Member == node)
                    {
                        return parent;
                    }

                    break;

                case SyntaxKind.ArrayCreationExpression:
                    if (((ArrayCreationExpressionSyntax)parent).Type == node)
                    {
                        return parent;
                    }

                    break;

                case SyntaxKind.ObjectCreationExpression:
                    if (node.Kind() == SyntaxKind.NullableType && ((ObjectCreationExpressionSyntax)parent).Type == node)
                    {
                        return parent;
                    }

                    break;

                case SyntaxKind.StackAllocArrayCreationExpression:
                    if (((StackAllocArrayCreationExpressionSyntax)parent).Type == node)
                    {
                        return parent;
                    }

                    break;
            }

            return node;
        }

        /// <summary>
        /// Given a conditional binding expression, find corresponding conditional access node.
        /// </summary>
        internal static ConditionalAccessExpressionSyntax FindConditionalAccessNodeForBinding(CSharpSyntaxNode node)
        {
            var currentNode = node;

            Debug.Assert(currentNode.Kind() == SyntaxKind.MemberBindingExpression ||
                         currentNode.Kind() == SyntaxKind.ElementBindingExpression);

            // In a well formed tree, the corresponding access node should be one of the ancestors
            // and its "?" token should precede the binding syntax.
            while (currentNode != null)
            {
                currentNode = currentNode.Parent;
                Debug.Assert(currentNode != null, "binding should be enclosed in a conditional access");

                if (currentNode.Kind() == SyntaxKind.ConditionalAccessExpression)
                {
                    var condAccess = (ConditionalAccessExpressionSyntax)currentNode;
                    if (condAccess.OperatorToken.EndPosition == node.Position)
                    {
                        return condAccess;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a generic name expression into one without the generic arguments.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static ExpressionSyntax GetNonGenericExpression(ExpressionSyntax expression)
        {
            if (expression != null)
            {
                switch (expression.Kind())
                {
                    case SyntaxKind.SimpleMemberAccessExpression:
                    case SyntaxKind.PointerMemberAccessExpression:
                        var max = (MemberAccessExpressionSyntax)expression;
                        if (max.Name.Kind() == SyntaxKind.GenericName)
                        {
                            var gn = (GenericNameSyntax)max.Name;
                            return SyntaxFactory.BinaryExpression(expression.Kind(), max.Expression, max.OperatorToken, SyntaxFactory.IdentifierName(gn.Identifier));
                        }
                        break;
                    case SyntaxKind.QualifiedName:
                        var qn = (QualifiedNameSyntax)expression;
                        if (qn.Right.Kind() == SyntaxKind.GenericName)
                        {
                            var gn = (GenericNameSyntax)qn.Right;
                            return SyntaxFactory.QualifiedName(qn.Left, qn.DotToken, SyntaxFactory.IdentifierName(gn.Identifier));
                        }
                        break;
                    case SyntaxKind.AliasQualifiedName:
                        var an = (AliasQualifiedNameSyntax)expression;
                        if (an.Name.Kind() == SyntaxKind.GenericName)
                        {
                            var gn = (GenericNameSyntax)an.Name;
                            return SyntaxFactory.AliasQualifiedName(an.Alias, an.ColonColonToken, SyntaxFactory.IdentifierName(gn.Identifier));
                        }
                        break;
                }
            }
            return expression;
        }

        /// <summary>
        /// Determines whether the given text is considered a syntactically complete submission.
        /// Throws <see cref="ArgumentException"/> if the tree was not compiled as an interactive submission.
        /// </summary>
        public static bool IsCompleteSubmission(SyntaxTree tree)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }
            if (tree.Options.Kind != SourceCodeKind.Script)
            {
                throw new ArgumentException(CSharpResources.SyntaxTreeIsNotASubmission);
            }

            if (!tree.HasCompilationUnitRoot)
            {
                return false;
            }

            var compilation = (CompilationUnitSyntax)tree.GetRoot();
            if (!compilation.HasErrors)
            {
                return true;
            }

            foreach (var error in compilation.EndOfFileToken.GetDiagnostics())
            {
                switch ((ErrorCode)error.Code)
                {
                    case ErrorCode.ERR_OpenEndedComment:
                    case ErrorCode.ERR_EndifDirectiveExpected:
                    case ErrorCode.ERR_EndRegionDirectiveExpected:
                        return false;
                }
            }

            var lastNode = compilation.ChildNodes().LastOrDefault();
            if (lastNode == null)
            {
                return true;
            }

            // unterminated multi-line comment:
            if (lastNode.HasTrailingTrivia && lastNode.ContainsDiagnostics && HasUnterminatedMultiLineComment(lastNode.GetTrailingTrivia()))
            {
                return false;
            }

            if (lastNode.IsKind(SyntaxKind.IncompleteMember))
            {
                return false;
            }

            // All top-level constructs but global statement (i.e. extern alias, using directive, global attribute, and declarations)
            // should have a closing token (semicolon, closing brace or bracket) to be complete.
            if (!lastNode.IsKind(SyntaxKind.GlobalStatement))
            {
                var closingToken = lastNode.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
                return !closingToken.IsMissing;
            }

            var globalStatement = (GlobalStatementSyntax)lastNode;
            var token = lastNode.GetLastToken(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

            if (token.IsMissing)
            {
                // expression statement terminating semicolon might be missing in script code:
                if (tree.Options.Kind == SourceCodeKind.Regular ||
                    !globalStatement.Statement.IsKind(SyntaxKind.ExpressionStatement) ||
                    !token.IsKind(SyntaxKind.SemicolonToken))
                {
                    return false;
                }

                token = token.GetPreviousToken(predicate: SyntaxToken.Any, stepInto: CodeAnalysis.SyntaxTrivia.Any);
                if (token.IsMissing)
                {
                    return false;
                }
            }

            foreach (var error in token.GetDiagnostics())
            {
                switch ((ErrorCode)error.Code)
                {
                    // unterminated character or string literal:
                    case ErrorCode.ERR_NewlineInConst:

                    // unterminated verbatim string literal:
                    case ErrorCode.ERR_UnterminatedStringLit:

                    // unexpected token following a global statement:
                    case ErrorCode.ERR_GlobalDefinitionOrStatementExpected:
                    case ErrorCode.ERR_EOFExpected:
                        return false;
                }
            }

            return true;
        }

        private static bool HasUnterminatedMultiLineComment(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.ContainsDiagnostics && trivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Creates a new CaseSwitchLabelSyntax instance.</summary>
        public static CaseSwitchLabelSyntax CaseSwitchLabel(ExpressionSyntax value)
        {
            return SyntaxFactory.CaseSwitchLabel(SyntaxFactory.Token(SyntaxKind.CaseKeyword), value, SyntaxFactory.Token(SyntaxKind.ColonToken));
        }

        /// <summary>Creates a new DefaultSwitchLabelSyntax instance.</summary>
        public static DefaultSwitchLabelSyntax DefaultSwitchLabel()
        {
            return SyntaxFactory.DefaultSwitchLabel(SyntaxFactory.Token(SyntaxKind.DefaultKeyword), SyntaxFactory.Token(SyntaxKind.ColonToken));
        }

        /// <summary>Creates a new BlockSyntax instance.</summary>
        public static BlockSyntax Block(params StatementSyntax[] statements)
        {
            return Block(List(statements));
        }

        /// <summary>Creates a new BlockSyntax instance.</summary>
        public static BlockSyntax Block(IEnumerable<StatementSyntax> statements)
        {
            return Block(List(statements));
        }

        public static PropertyDeclarationSyntax PropertyDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken refKeyword,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken identifier,
            AccessorListSyntax accessorList)
        {
            return SyntaxFactory.PropertyDeclaration(
                attributeLists,
                modifiers,
                refKeyword,
                type,
                explicitInterfaceSpecifier,
                identifier,
                accessorList,
                default(ArrowExpressionClauseSyntax),
                default(EqualsValueClauseSyntax),
                default(SyntaxToken));
        }

        public static MethodDeclarationSyntax MethodDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken refKeyword,
            TypeSyntax returnType,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            BlockSyntax body,
            SyntaxToken semicolonToken)
        {
            return SyntaxFactory.MethodDeclaration(
                attributeLists,
                modifiers,
                refKeyword,
                returnType,
                explicitInterfaceSpecifier,
                identifier,
                typeParameterList,
                parameterList,
                constraintClauses,
                body,
                default(ArrowExpressionClauseSyntax),
                semicolonToken);
        }

        public static ConversionOperatorDeclarationSyntax ConversionOperatorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken implicitOrExplicitKeyword,
            SyntaxToken operatorKeyword,
            TypeSyntax type,
            ParameterListSyntax parameterList,
            BlockSyntax body,
            SyntaxToken semicolonToken)
        {
            return SyntaxFactory.ConversionOperatorDeclaration(
                attributeLists: attributeLists,
                modifiers: modifiers,
                implicitOrExplicitKeyword: implicitOrExplicitKeyword,
                operatorKeyword: operatorKeyword,
                type: type,
                parameterList: parameterList,
                body: body,
                expressionBody: default(ArrowExpressionClauseSyntax),
                semicolonToken: semicolonToken);
        }

        public static OperatorDeclarationSyntax OperatorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            SyntaxToken operatorKeyword,
            SyntaxToken operatorToken,
            ParameterListSyntax parameterList,
            BlockSyntax body,
            SyntaxToken semicolonToken)
        {
            return SyntaxFactory.OperatorDeclaration(
                attributeLists: attributeLists,
                modifiers: modifiers,
                returnType: returnType,
                operatorKeyword: operatorKeyword,
                operatorToken: operatorToken,
                parameterList: parameterList,
                body: body,
                expressionBody: default(ArrowExpressionClauseSyntax),
                semicolonToken: semicolonToken);
        }

        public static IndexerDeclarationSyntax IndexerDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken refKeyword,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier, 
            BracketedParameterListSyntax parameterList,
            AccessorListSyntax accessorList)
        {
            return SyntaxFactory.IndexerDeclaration(
                attributeLists: attributeLists,
                modifiers: modifiers,
                refKeyword: refKeyword,
                type: type,
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                parameterList: parameterList,
                accessorList: accessorList);
        }

        /// <summary>Creates a new UsingDirectiveSyntax instance.</summary>
        public static UsingDirectiveSyntax UsingDirective(NameEqualsSyntax alias, NameSyntax name)
        {
            return UsingDirective(
                usingKeyword: Token(SyntaxKind.UsingKeyword),
                staticKeyword: default(SyntaxToken),
                alias: alias,
                name: name,
                semicolonToken: Token(SyntaxKind.SemicolonToken));
        }
    }
}
