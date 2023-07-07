// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    [Flags]
    internal enum LexerMode
    {
        Syntax = 0x0001,
        DebuggerSyntax = 0x0002,
        Directive = 0x0004,
        XmlDocComment = 0x0008,
        XmlElementTag = 0x0010,
        XmlAttributeTextQuote = 0x0020,
        XmlAttributeTextDoubleQuote = 0x0040,
        XmlCrefQuote = 0x0080,
        XmlCrefDoubleQuote = 0x0100,
        XmlNameQuote = 0x0200,
        XmlNameDoubleQuote = 0x0400,
        XmlCDataSectionText = 0x0800,
        XmlCommentText = 0x1000,
        XmlProcessingInstructionText = 0x2000,
        XmlCharacter = 0x4000,
        MaskLexMode = 0xFFFF,

        // The following are lexer driven, which is to say the lexer can push a change back to the
        // blender. There is in general no need to use a whole bit per enum value, but the debugging
        // experience is bad if you don't do that.

        XmlDocCommentLocationStart = 0x00000,
        XmlDocCommentLocationInterior = 0x10000,
        XmlDocCommentLocationExterior = 0x20000,
        XmlDocCommentLocationEnd = 0x40000,
        MaskXmlDocCommentLocation = 0xF0000,

        XmlDocCommentStyleSingleLine = 0x000000,
        XmlDocCommentStyleDelimited = 0x100000,
        MaskXmlDocCommentStyle = 0x300000,

        None = 0
    }

    // Needs to match LexMode.XmlDocCommentLocation*
    internal enum XmlDocCommentLocation
    {
        Start = 0,
        Interior = 1,
        Exterior = 2,
        End = 4
    }

    // Needs to match LexMode.XmlDocCommentStyle*
    internal enum XmlDocCommentStyle
    {
        SingleLine = 0,
        Delimited = 1
    }

    internal partial class Lexer : AbstractLexer
    {
        private const int TriviaListInitialCapacity = 8;

        private readonly CSharpParseOptions _options;

        private LexerMode _mode;
        private readonly StringBuilder _builder;
        private char[] _identBuffer;
        private int _identLen;
        private DirectiveStack _directives;
        private readonly LexerCache _cache;
        private readonly bool _allowPreprocessorDirectives;
        private readonly bool _interpolationFollowedByColon;
        private DocumentationCommentParser? _xmlParser;
        private int _badTokenCount; // cumulative count of bad tokens produced

        internal struct TokenInfo
        {
            // scanned values
            internal SyntaxKind Kind;
            internal SyntaxKind ContextualKind;
            internal string? Text;
            internal SpecialType ValueKind;
            internal bool RequiresTextForXmlEntity;
            internal bool HasIdentifierEscapeSequence;
            internal string? StringValue;
            internal char CharValue;
            internal int IntValue;
            internal uint UintValue;
            internal long LongValue;
            internal ulong UlongValue;
            internal float FloatValue;
            internal double DoubleValue;
            internal decimal DecimalValue;
            internal bool IsVerbatim;
        }

        public Lexer(SourceText text, CSharpParseOptions options, bool allowPreprocessorDirectives = true, bool interpolationFollowedByColon = false)
            : base(text)
        {
            Debug.Assert(options != null);

            _options = options;
            _builder = new StringBuilder();
            _identBuffer = new char[32];
            _cache = new LexerCache();
            _createQuickTokenFunction = this.CreateQuickToken;
            _allowPreprocessorDirectives = allowPreprocessorDirectives;
            _interpolationFollowedByColon = interpolationFollowedByColon;
        }

        public override void Dispose()
        {
            _cache.Free();

            if (_xmlParser != null)
            {
                _xmlParser.Dispose();
            }

            base.Dispose();
        }

        public bool SuppressDocumentationCommentParse
        {
            get { return _options.DocumentationMode < DocumentationMode.Parse; }
        }

        public CSharpParseOptions Options
        {
            get { return _options; }
        }

        public DirectiveStack Directives
        {
            get { return _directives; }
        }

        /// <summary>
        /// The lexer is for the contents of an interpolation that is followed by a colon that signals the start of the format string.
        /// </summary>
        public bool InterpolationFollowedByColon
        {
            get
            {
                return _interpolationFollowedByColon;
            }
        }

        public void Reset(int position, DirectiveStack directives)
        {
            this.TextWindow.Reset(position);
            _directives = directives;
        }

        private static LexerMode ModeOf(LexerMode mode)
        {
            return mode & LexerMode.MaskLexMode;
        }

        private bool ModeIs(LexerMode mode)
        {
            return ModeOf(_mode) == mode;
        }

        private static XmlDocCommentLocation LocationOf(LexerMode mode)
        {
            return (XmlDocCommentLocation)((int)(mode & LexerMode.MaskXmlDocCommentLocation) >> 16);
        }

        private bool LocationIs(XmlDocCommentLocation location)
        {
            return LocationOf(_mode) == location;
        }

        private void MutateLocation(XmlDocCommentLocation location)
        {
            _mode &= ~LexerMode.MaskXmlDocCommentLocation;
            _mode |= (LexerMode)((int)location << 16);
        }

        private static XmlDocCommentStyle StyleOf(LexerMode mode)
        {
            return (XmlDocCommentStyle)((int)(mode & LexerMode.MaskXmlDocCommentStyle) >> 20);
        }

        private bool StyleIs(XmlDocCommentStyle style)
        {
            return StyleOf(_mode) == style;
        }

        private bool InDocumentationComment
        {
            get
            {
                switch (ModeOf(_mode))
                {
                    case LexerMode.XmlDocComment:
                    case LexerMode.XmlElementTag:
                    case LexerMode.XmlAttributeTextQuote:
                    case LexerMode.XmlAttributeTextDoubleQuote:
                    case LexerMode.XmlCrefQuote:
                    case LexerMode.XmlCrefDoubleQuote:
                    case LexerMode.XmlNameQuote:
                    case LexerMode.XmlNameDoubleQuote:
                    case LexerMode.XmlCDataSectionText:
                    case LexerMode.XmlCommentText:
                    case LexerMode.XmlProcessingInstructionText:
                    case LexerMode.XmlCharacter:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public SyntaxToken Lex(ref LexerMode mode)
        {
            var result = Lex(mode);
            mode = _mode;
            return result;
        }

#if DEBUG
        internal static int TokensLexed;
#endif

        public SyntaxToken Lex(LexerMode mode)
        {
#if DEBUG
            TokensLexed++;
#endif
            _mode = mode;
            switch (_mode)
            {
                case LexerMode.Syntax:
                case LexerMode.DebuggerSyntax:
                    return this.QuickScanSyntaxToken() ?? this.LexSyntaxToken();
                case LexerMode.Directive:
                    return this.LexDirectiveToken();
            }

            switch (ModeOf(_mode))
            {
                case LexerMode.XmlDocComment:
                    return this.LexXmlToken();
                case LexerMode.XmlElementTag:
                    return this.LexXmlElementTagToken();
                case LexerMode.XmlAttributeTextQuote:
                case LexerMode.XmlAttributeTextDoubleQuote:
                    return this.LexXmlAttributeTextToken();
                case LexerMode.XmlCDataSectionText:
                    return this.LexXmlCDataSectionTextToken();
                case LexerMode.XmlCommentText:
                    return this.LexXmlCommentTextToken();
                case LexerMode.XmlProcessingInstructionText:
                    return this.LexXmlProcessingInstructionTextToken();
                case LexerMode.XmlCrefQuote:
                case LexerMode.XmlCrefDoubleQuote:
                    return this.LexXmlCrefOrNameToken();
                case LexerMode.XmlNameQuote:
                case LexerMode.XmlNameDoubleQuote:
                    // Same lexing as a cref attribute, just treat the identifiers a little differently.
                    return this.LexXmlCrefOrNameToken();
                case LexerMode.XmlCharacter:
                    return this.LexXmlCharacter();
                default:
                    throw ExceptionUtilities.UnexpectedValue(ModeOf(_mode));
            }
        }

        private SyntaxListBuilder _leadingTriviaCache = new SyntaxListBuilder(10);
        private SyntaxListBuilder _trailingTriviaCache = new SyntaxListBuilder(10);

        private static int GetFullWidth(SyntaxListBuilder? builder)
        {
            int width = 0;

            if (builder != null)
            {
                for (int i = 0; i < builder.Count; i++)
                {
                    width += builder[i]!.FullWidth;
                }
            }

            return width;
        }

        private SyntaxToken LexSyntaxToken()
        {
            _leadingTriviaCache.Clear();
            this.LexSyntaxTrivia(afterFirstToken: TextWindow.Position > 0, isTrailing: false, triviaList: ref _leadingTriviaCache);
            var leading = _leadingTriviaCache;

            var tokenInfo = default(TokenInfo);

            this.Start();
            this.ScanSyntaxToken(ref tokenInfo);
            var errors = this.GetErrors(GetFullWidth(leading));

            _trailingTriviaCache.Clear();
            this.LexSyntaxTrivia(afterFirstToken: true, isTrailing: true, triviaList: ref _trailingTriviaCache);
            var trailing = _trailingTriviaCache;

            return Create(in tokenInfo, leading, trailing, errors);
        }

        internal SyntaxTriviaList LexSyntaxLeadingTrivia()
        {
            _leadingTriviaCache.Clear();
            this.LexSyntaxTrivia(afterFirstToken: TextWindow.Position > 0, isTrailing: false, triviaList: ref _leadingTriviaCache);
            return new SyntaxTriviaList(default(Microsoft.CodeAnalysis.SyntaxToken),
                _leadingTriviaCache.ToListNode(), position: 0, index: 0);
        }

        internal SyntaxTriviaList LexSyntaxTrailingTrivia()
        {
            _trailingTriviaCache.Clear();
            this.LexSyntaxTrivia(afterFirstToken: true, isTrailing: true, triviaList: ref _trailingTriviaCache);
            return new SyntaxTriviaList(default(Microsoft.CodeAnalysis.SyntaxToken),
                _trailingTriviaCache.ToListNode(), position: 0, index: 0);
        }

        private SyntaxToken Create(in TokenInfo info, SyntaxListBuilder? leading, SyntaxListBuilder? trailing, SyntaxDiagnosticInfo[]? errors)
        {
            Debug.Assert(info.Kind != SyntaxKind.IdentifierToken || info.StringValue != null);

            var leadingNode = leading?.ToListNode();
            var trailingNode = trailing?.ToListNode();

            SyntaxToken token;
            if (info.RequiresTextForXmlEntity)
            {
                token = SyntaxFactory.Token(leadingNode, info.Kind, info.Text, info.StringValue, trailingNode);
            }
            else
            {
                switch (info.Kind)
                {
                    case SyntaxKind.IdentifierToken:
                        token = SyntaxFactory.Identifier(info.ContextualKind, leadingNode, info.Text, info.StringValue, trailingNode);
                        break;
                    case SyntaxKind.NumericLiteralToken:
                        switch (info.ValueKind)
                        {
                            case SpecialType.System_Int32:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.IntValue, trailingNode);
                                break;
                            case SpecialType.System_UInt32:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.UintValue, trailingNode);
                                break;
                            case SpecialType.System_Int64:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.LongValue, trailingNode);
                                break;
                            case SpecialType.System_UInt64:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.UlongValue, trailingNode);
                                break;
                            case SpecialType.System_Single:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.FloatValue, trailingNode);
                                break;
                            case SpecialType.System_Double:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.DoubleValue, trailingNode);
                                break;
                            case SpecialType.System_Decimal:
                                token = SyntaxFactory.Literal(leadingNode, info.Text, info.DecimalValue, trailingNode);
                                break;
                            default:
                                throw ExceptionUtilities.UnexpectedValue(info.ValueKind);
                        }

                        break;
                    case SyntaxKind.InterpolatedStringToken:
                        // we do not record a separate "value" for an interpolated string token, as it must be rescanned during parsing.
                        token = SyntaxFactory.Literal(leadingNode, info.Text, info.Kind, info.Text, trailingNode);
                        break;
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.Utf8StringLiteralToken:
                    case SyntaxKind.SingleLineRawStringLiteralToken:
                    case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                    case SyntaxKind.MultiLineRawStringLiteralToken:
                    case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                        token = SyntaxFactory.Literal(leadingNode, info.Text, info.Kind, info.StringValue, trailingNode);
                        break;
                    case SyntaxKind.CharacterLiteralToken:
                        token = SyntaxFactory.Literal(leadingNode, info.Text, info.CharValue, trailingNode);
                        break;
                    case SyntaxKind.XmlTextLiteralNewLineToken:
                        token = SyntaxFactory.XmlTextNewLine(leadingNode, info.Text, info.StringValue, trailingNode);
                        break;
                    case SyntaxKind.XmlTextLiteralToken:
                        token = SyntaxFactory.XmlTextLiteral(leadingNode, info.Text, info.StringValue, trailingNode);
                        break;
                    case SyntaxKind.XmlEntityLiteralToken:
                        token = SyntaxFactory.XmlEntity(leadingNode, info.Text, info.StringValue, trailingNode);
                        break;
                    case SyntaxKind.EndOfDocumentationCommentToken:
                    case SyntaxKind.EndOfFileToken:
                        token = SyntaxFactory.Token(leadingNode, info.Kind, trailingNode);
                        break;
                    case SyntaxKind.None:
                        token = SyntaxFactory.BadToken(leadingNode, info.Text, trailingNode);
                        break;

                    default:
                        Debug.Assert(SyntaxFacts.IsPunctuationOrKeyword(info.Kind));
                        token = SyntaxFactory.Token(leadingNode, info.Kind, trailingNode);
                        break;
                }
            }

            if (errors != null && (_options.DocumentationMode >= DocumentationMode.Diagnose || !InDocumentationComment))
            {
                token = token.WithDiagnosticsGreen(errors);
            }

            return token;
        }

        private void ScanSyntaxToken(ref TokenInfo info)
        {
            // Initialize for new token scan
            info.Kind = SyntaxKind.None;
            info.ContextualKind = SyntaxKind.None;
            info.Text = null;
            char character;
            char surrogateCharacter = SlidingTextWindow.InvalidCharacter;
            bool isEscaped = false;
            int startingPosition = TextWindow.Position;

            // Start scanning the token
            character = TextWindow.PeekChar();
            switch (character)
            {
                case '\"':
                case '\'':
                    this.ScanStringLiteral(ref info, inDirective: false);
                    break;

                case '/':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.SlashEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.SlashToken;
                    }

                    break;

                case '.':
                    if (!this.ScanNumericLiteral(ref info))
                    {
                        TextWindow.AdvanceChar();
                        if (TextWindow.PeekChar() == '.')
                        {
                            TextWindow.AdvanceChar();
                            if (TextWindow.PeekChar() == '.')
                            {
                                // Triple-dot: explicitly reject this, to allow triple-dot
                                // to be added to the language without a breaking change.
                                // (without this, 0...2 would parse as (0)..(.2), i.e. a range from 0 to 0.2)
                                this.AddError(ErrorCode.ERR_TripleDotNotAllowed);
                            }

                            info.Kind = SyntaxKind.DotDotToken;
                        }
                        else
                        {
                            info.Kind = SyntaxKind.DotToken;
                        }
                    }

                    break;

                case ',':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.CommaToken;
                    break;

                case ':':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == ':')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.ColonColonToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.ColonToken;
                    }

                    break;

                case ';':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.SemicolonToken;
                    break;

                case '~':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.TildeToken;
                    break;

                case '!':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.ExclamationEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.ExclamationToken;
                    }

                    break;

                case '=':
                    TextWindow.AdvanceChar();
                    if ((character = TextWindow.PeekChar()) == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.EqualsEqualsToken;
                    }
                    else if (character == '>')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.EqualsGreaterThanToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.EqualsToken;
                    }

                    break;

                case '*':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.AsteriskEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.AsteriskToken;
                    }

                    break;

                case '(':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.OpenParenToken;
                    break;

                case ')':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.CloseParenToken;
                    break;

                case '{':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.OpenBraceToken;
                    break;

                case '}':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.CloseBraceToken;
                    break;

                case '[':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.OpenBracketToken;
                    break;

                case ']':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.CloseBracketToken;
                    break;

                case '?':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '?')
                    {
                        TextWindow.AdvanceChar();

                        if (TextWindow.PeekChar() == '=')
                        {
                            TextWindow.AdvanceChar();
                            info.Kind = SyntaxKind.QuestionQuestionEqualsToken;
                        }
                        else
                        {
                            info.Kind = SyntaxKind.QuestionQuestionToken;
                        }
                    }
                    else
                    {
                        info.Kind = SyntaxKind.QuestionToken;
                    }

                    break;

                case '+':
                    TextWindow.AdvanceChar();
                    if ((character = TextWindow.PeekChar()) == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.PlusEqualsToken;
                    }
                    else if (character == '+')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.PlusPlusToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.PlusToken;
                    }

                    break;

                case '-':
                    TextWindow.AdvanceChar();
                    if ((character = TextWindow.PeekChar()) == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.MinusEqualsToken;
                    }
                    else if (character == '-')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.MinusMinusToken;
                    }
                    else if (character == '>')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.MinusGreaterThanToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.MinusToken;
                    }

                    break;

                case '%':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.PercentEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.PercentToken;
                    }

                    break;

                case '&':
                    TextWindow.AdvanceChar();
                    if ((character = TextWindow.PeekChar()) == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.AmpersandEqualsToken;
                    }
                    else if (TextWindow.PeekChar() == '&')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.AmpersandAmpersandToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.AmpersandToken;
                    }

                    break;

                case '^':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.CaretEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.CaretToken;
                    }

                    break;

                case '|':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.BarEqualsToken;
                    }
                    else if (TextWindow.PeekChar() == '|')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.BarBarToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.BarToken;
                    }

                    break;

                case '<':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.LessThanEqualsToken;
                    }
                    else if (TextWindow.PeekChar() == '<')
                    {
                        TextWindow.AdvanceChar();
                        if (TextWindow.PeekChar() == '=')
                        {
                            TextWindow.AdvanceChar();
                            info.Kind = SyntaxKind.LessThanLessThanEqualsToken;
                        }
                        else
                        {
                            info.Kind = SyntaxKind.LessThanLessThanToken;
                        }
                    }
                    else
                    {
                        info.Kind = SyntaxKind.LessThanToken;
                    }

                    break;

                case '>':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.GreaterThanEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.GreaterThanToken;
                    }

                    break;

                case '@':
                    if (!this.TryScanAtStringToken(ref info) &&
                        !this.ScanIdentifierOrKeyword(ref info))
                    {
                        Debug.Assert(TextWindow.PeekChar() == '@');
                        this.ConsumeAtSignSequence();
                        info.Text = TextWindow.GetText(intern: true);
                        this.AddError(ErrorCode.ERR_ExpectedVerbatimLiteral);
                    }
                    break;

                case '$':
                    if (TryScanInterpolatedString(ref info))
                    {
                        break;
                    }

                    if (this.ModeIs(LexerMode.DebuggerSyntax))
                    {
                        goto case 'a';
                    }

                    goto default;

                // All the 'common' identifier characters are represented directly in
                // these switch cases for optimal perf.  Calling IsIdentifierChar() functions is relatively
                // expensive.
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    this.ScanIdentifierOrKeyword(ref info);
                    break;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    this.ScanNumericLiteral(ref info);
                    break;

                case '\\':
                    {
                        // Could be unicode escape. Try that.
                        character = PeekCharOrUnicodeEscape(out surrogateCharacter);

                        isEscaped = true;
                        if (SyntaxFacts.IsIdentifierStartCharacter(character))
                        {
                            goto case 'a';
                        }

                        goto default;
                    }

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    if (_directives.HasUnfinishedIf())
                    {
                        this.AddError(ErrorCode.ERR_EndifDirectiveExpected);
                    }

                    if (_directives.HasUnfinishedRegion())
                    {
                        this.AddError(ErrorCode.ERR_EndRegionDirectiveExpected);
                    }

                    info.Kind = SyntaxKind.EndOfFileToken;
                    break;

                default:
                    if (SyntaxFacts.IsIdentifierStartCharacter(character))
                    {
                        goto case 'a';
                    }

                    if (isEscaped)
                    {
                        SyntaxDiagnosticInfo? error;
                        NextCharOrUnicodeEscape(out surrogateCharacter, out error);
                        AddError(error);
                    }
                    else
                    {
                        TextWindow.AdvanceChar();
                    }

                    if (_badTokenCount++ > 200)
                    {
                        // If we get too many characters that we cannot make sense of, absorb the rest of the input.
                        int end = TextWindow.Text.Length;
                        int width = end - startingPosition;
                        info.Text = TextWindow.Text.ToString(new TextSpan(startingPosition, width));
                        TextWindow.Reset(end);
                    }
                    else
                    {
                        info.Text = TextWindow.GetText(intern: true);
                    }

                    this.AddError(ErrorCode.ERR_UnexpectedCharacter, info.Text);
                    break;
            }
        }

        private bool TryScanAtStringToken(ref TokenInfo info)
        {
            Debug.Assert(TextWindow.PeekChar() == '@');

            var index = 0;
            while (TextWindow.PeekChar(index) == '@')
            {
                index++;
            }

            if (TextWindow.PeekChar(index) == '"')
            {
                // @"
                this.ScanVerbatimStringLiteral(ref info);
                return true;
            }
            else if (TextWindow.PeekChar(index) == '$')
            {
                // @$"
                this.ScanInterpolatedStringLiteral(ref info);
                return true;
            }

            return false;
        }

        private bool TryScanInterpolatedString(ref TokenInfo info)
        {
            Debug.Assert(TextWindow.PeekChar() == '$');

            if (TextWindow.PeekChar(1) == '$')
            {
                // $$ - definitely starts a raw interpolated string.
                this.ScanInterpolatedStringLiteral(ref info);
                return true;
            }
            else if (TextWindow.PeekChar(1) == '@' && TextWindow.PeekChar(2) == '@')
            {
                // $@@ - Error case.  Detect if user is trying to user verbatim and raw interpolations together.
                this.ScanInterpolatedStringLiteral(ref info);
                return true;
            }
            else if (TextWindow.PeekChar(1) == '"')
            {
                this.ScanInterpolatedStringLiteral(ref info);
                return true;
            }
            else if (TextWindow.PeekChar(1) == '@')
            {
                this.ScanInterpolatedStringLiteral(ref info);
                return true;
            }

            return false;
        }

        private void CheckFeatureAvailability(MessageID feature)
        {
            var info = feature.GetFeatureAvailabilityDiagnosticInfo(Options);
            if (info != null)
            {
                AddError(info.Code, info.Arguments);
            }
        }

        private bool ScanInteger()
        {
            int start = TextWindow.Position;
            char ch;
            while ((ch = TextWindow.PeekChar()) >= '0' && ch <= '9')
            {
                TextWindow.AdvanceChar();
            }

            return start < TextWindow.Position;
        }

        // Allows underscores in integers, except at beginning for decimal and end
        private void ScanNumericLiteralSingleInteger(ref bool underscoreInWrongPlace, ref bool usedUnderscore, ref bool firstCharWasUnderscore, bool isHex, bool isBinary)
        {
            if (TextWindow.PeekChar() == '_')
            {
                if (isHex || isBinary)
                {
                    firstCharWasUnderscore = true;
                }
                else
                {
                    underscoreInWrongPlace = true;
                }
            }

            bool lastCharWasUnderscore = false;
            while (true)
            {
                char ch = TextWindow.PeekChar();
                if (ch == '_')
                {
                    usedUnderscore = true;
                    lastCharWasUnderscore = true;
                }
                else if (!(isHex ? SyntaxFacts.IsHexDigit(ch) :
                           isBinary ? SyntaxFacts.IsBinaryDigit(ch) :
                           SyntaxFacts.IsDecDigit(ch)))
                {
                    break;
                }
                else
                {
                    _builder.Append(ch);
                    lastCharWasUnderscore = false;
                }
                TextWindow.AdvanceChar();
            }

            if (lastCharWasUnderscore)
            {
                underscoreInWrongPlace = true;
            }
        }

        private bool ScanNumericLiteral(ref TokenInfo info)
        {
            int start = TextWindow.Position;
            char ch;
            bool isHex = false;
            bool isBinary = false;
            bool hasDecimal = false;
            bool hasExponent = false;
            info.Text = null;
            info.ValueKind = SpecialType.None;
            _builder.Clear();
            bool hasUSuffix = false;
            bool hasLSuffix = false;
            bool underscoreInWrongPlace = false;
            bool usedUnderscore = false;
            bool firstCharWasUnderscore = false;

            ch = TextWindow.PeekChar();
            if (ch == '0')
            {
                ch = TextWindow.PeekChar(1);
                if (ch == 'x' || ch == 'X')
                {
                    TextWindow.AdvanceChar(2);
                    isHex = true;
                }
                else if (ch == 'b' || ch == 'B')
                {
                    CheckFeatureAvailability(MessageID.IDS_FeatureBinaryLiteral);
                    TextWindow.AdvanceChar(2);
                    isBinary = true;
                }
            }

            if (isHex || isBinary)
            {
                // It's OK if it has no digits after the '0x' -- we'll catch it in ScanNumericLiteral
                // and give a proper error then.
                ScanNumericLiteralSingleInteger(ref underscoreInWrongPlace, ref usedUnderscore, ref firstCharWasUnderscore, isHex, isBinary);

                if ((ch = TextWindow.PeekChar()) == 'L' || ch == 'l')
                {
                    TextWindow.AdvanceChar();
                    hasLSuffix = true;
                    if ((ch = TextWindow.PeekChar()) == 'u' || ch == 'U')
                    {
                        TextWindow.AdvanceChar();
                        hasUSuffix = true;
                    }
                }
                else if ((ch = TextWindow.PeekChar()) == 'u' || ch == 'U')
                {
                    TextWindow.AdvanceChar();
                    hasUSuffix = true;
                    if ((ch = TextWindow.PeekChar()) == 'L' || ch == 'l')
                    {
                        TextWindow.AdvanceChar();
                        hasLSuffix = true;
                    }
                }
            }
            else
            {
                ScanNumericLiteralSingleInteger(ref underscoreInWrongPlace, ref usedUnderscore, ref firstCharWasUnderscore, isHex: false, isBinary: false);

                if (this.ModeIs(LexerMode.DebuggerSyntax) && TextWindow.PeekChar() == '#')
                {
                    // Previously, in DebuggerSyntax mode, "123#" was a valid identifier.
                    TextWindow.AdvanceChar();
                    info.StringValue = info.Text = TextWindow.GetText(intern: true);
                    info.Kind = SyntaxKind.IdentifierToken;
                    this.AddError(MakeError(ErrorCode.ERR_LegacyObjectIdSyntax));
                    return true;
                }

                if ((ch = TextWindow.PeekChar()) == '.')
                {
                    var ch2 = TextWindow.PeekChar(1);
                    if (ch2 >= '0' && ch2 <= '9')
                    {
                        hasDecimal = true;
                        _builder.Append(ch);
                        TextWindow.AdvanceChar();

                        ScanNumericLiteralSingleInteger(ref underscoreInWrongPlace, ref usedUnderscore, ref firstCharWasUnderscore, isHex: false, isBinary: false);
                    }
                    else if (_builder.Length == 0)
                    {
                        // we only have the dot so far.. (no preceding number or following number)
                        TextWindow.Reset(start);
                        return false;
                    }
                }

                if ((ch = TextWindow.PeekChar()) == 'E' || ch == 'e')
                {
                    _builder.Append(ch);
                    TextWindow.AdvanceChar();
                    hasExponent = true;
                    if ((ch = TextWindow.PeekChar()) == '-' || ch == '+')
                    {
                        _builder.Append(ch);
                        TextWindow.AdvanceChar();
                    }

                    if (!(((ch = TextWindow.PeekChar()) >= '0' && ch <= '9') || ch == '_'))
                    {
                        // use this for now (CS0595), cant use CS0594 as we dont know 'type'
                        this.AddError(MakeError(ErrorCode.ERR_InvalidReal));
                        // add dummy exponent, so parser does not blow up
                        _builder.Append('0');
                    }
                    else
                    {
                        ScanNumericLiteralSingleInteger(ref underscoreInWrongPlace, ref usedUnderscore, ref firstCharWasUnderscore, isHex: false, isBinary: false);
                    }
                }

                if (hasExponent || hasDecimal)
                {
                    if ((ch = TextWindow.PeekChar()) == 'f' || ch == 'F')
                    {
                        TextWindow.AdvanceChar();
                        info.ValueKind = SpecialType.System_Single;
                    }
                    else if (ch == 'D' || ch == 'd')
                    {
                        TextWindow.AdvanceChar();
                        info.ValueKind = SpecialType.System_Double;
                    }
                    else if (ch == 'm' || ch == 'M')
                    {
                        TextWindow.AdvanceChar();
                        info.ValueKind = SpecialType.System_Decimal;
                    }
                    else
                    {
                        info.ValueKind = SpecialType.System_Double;
                    }
                }
                else if ((ch = TextWindow.PeekChar()) == 'f' || ch == 'F')
                {
                    TextWindow.AdvanceChar();
                    info.ValueKind = SpecialType.System_Single;
                }
                else if (ch == 'D' || ch == 'd')
                {
                    TextWindow.AdvanceChar();
                    info.ValueKind = SpecialType.System_Double;
                }
                else if (ch == 'm' || ch == 'M')
                {
                    TextWindow.AdvanceChar();
                    info.ValueKind = SpecialType.System_Decimal;
                }
                else if (ch == 'L' || ch == 'l')
                {
                    TextWindow.AdvanceChar();
                    hasLSuffix = true;
                    if ((ch = TextWindow.PeekChar()) == 'u' || ch == 'U')
                    {
                        TextWindow.AdvanceChar();
                        hasUSuffix = true;
                    }
                }
                else if (ch == 'u' || ch == 'U')
                {
                    hasUSuffix = true;
                    TextWindow.AdvanceChar();
                    if ((ch = TextWindow.PeekChar()) == 'L' || ch == 'l')
                    {
                        TextWindow.AdvanceChar();
                        hasLSuffix = true;
                    }
                }
            }

            if (underscoreInWrongPlace)
            {
                this.AddError(MakeError(start, TextWindow.Position - start, ErrorCode.ERR_InvalidNumber));
            }
            else if (firstCharWasUnderscore)
            {
                CheckFeatureAvailability(MessageID.IDS_FeatureLeadingDigitSeparator);
            }
            else if (usedUnderscore)
            {
                CheckFeatureAvailability(MessageID.IDS_FeatureDigitSeparator);
            }

            info.Kind = SyntaxKind.NumericLiteralToken;
            info.Text = TextWindow.GetText(true);
            Debug.Assert(info.Text != null);
            var valueText = TextWindow.Intern(_builder);
            ulong val;
            switch (info.ValueKind)
            {
                case SpecialType.System_Single:
                    info.FloatValue = this.GetValueSingle(valueText);
                    break;
                case SpecialType.System_Double:
                    info.DoubleValue = this.GetValueDouble(valueText);
                    break;
                case SpecialType.System_Decimal:
                    info.DecimalValue = this.GetValueDecimal(valueText, start, TextWindow.Position);
                    break;
                default:
                    if (string.IsNullOrEmpty(valueText))
                    {
                        if (!underscoreInWrongPlace)
                        {
                            this.AddError(MakeError(ErrorCode.ERR_InvalidNumber));
                        }
                        val = 0; //safe default
                    }
                    else
                    {
                        val = this.GetValueUInt64(valueText, isHex, isBinary);
                    }

                    // 2.4.4.2 Integer literals
                    // ...
                    // The type of an integer literal is determined as follows:

                    // * If the literal has no suffix, it has the first of these types in which its value can be represented: int, uint, long, ulong.
                    if (!hasUSuffix && !hasLSuffix)
                    {
                        if (val <= Int32.MaxValue)
                        {
                            info.ValueKind = SpecialType.System_Int32;
                            info.IntValue = (int)val;
                        }
                        else if (val <= UInt32.MaxValue)
                        {
                            info.ValueKind = SpecialType.System_UInt32;
                            info.UintValue = (uint)val;

                            // TODO: See below, it may be desirable to mark this token
                            // as special for folding if its value is 2147483648.
                        }
                        else if (val <= Int64.MaxValue)
                        {
                            info.ValueKind = SpecialType.System_Int64;
                            info.LongValue = (long)val;
                        }
                        else
                        {
                            info.ValueKind = SpecialType.System_UInt64;
                            info.UlongValue = val;

                            // TODO: See below, it may be desirable to mark this token
                            // as special for folding if its value is 9223372036854775808
                        }
                    }
                    else if (hasUSuffix && !hasLSuffix)
                    {
                        // * If the literal is suffixed by U or u, it has the first of these types in which its value can be represented: uint, ulong.
                        if (val <= UInt32.MaxValue)
                        {
                            info.ValueKind = SpecialType.System_UInt32;
                            info.UintValue = (uint)val;
                        }
                        else
                        {
                            info.ValueKind = SpecialType.System_UInt64;
                            info.UlongValue = val;
                        }
                    }

                    // * If the literal is suffixed by L or l, it has the first of these types in which its value can be represented: long, ulong.
                    else if (!hasUSuffix & hasLSuffix)
                    {
                        if (val <= Int64.MaxValue)
                        {
                            info.ValueKind = SpecialType.System_Int64;
                            info.LongValue = (long)val;
                        }
                        else
                        {
                            info.ValueKind = SpecialType.System_UInt64;
                            info.UlongValue = val;

                            // TODO: See below, it may be desirable to mark this token
                            // as special for folding if its value is 9223372036854775808
                        }
                    }

                    // * If the literal is suffixed by UL, Ul, uL, ul, LU, Lu, lU, or lu, it is of type ulong.
                    else
                    {
                        Debug.Assert(hasUSuffix && hasLSuffix);
                        info.ValueKind = SpecialType.System_UInt64;
                        info.UlongValue = val;
                    }

                    break;

                    // Note, the following portion of the spec is not implemented here. It is implemented
                    // in the unary minus analysis.

                    // * When a decimal-integer-literal with the value 2147483648 (231) and no integer-type-suffix appears
                    //   as the token immediately following a unary minus operator token (§7.7.2), the result is a constant
                    //   of type int with the value −2147483648 (−231). In all other situations, such a decimal-integer-
                    //   literal is of type uint.
                    // * When a decimal-integer-literal with the value 9223372036854775808 (263) and no integer-type-suffix
                    //   or the integer-type-suffix L or l appears as the token immediately following a unary minus operator
                    //   token (§7.7.2), the result is a constant of type long with the value −9223372036854775808 (−263).
                    //   In all other situations, such a decimal-integer-literal is of type ulong.
            }

            return true;
        }

        // TODO: Change to Int64.TryParse when it supports NumberStyles.AllowBinarySpecifier (inline this method into GetValueUInt32/64)
        private static bool TryParseBinaryUInt64(string text, out ulong value)
        {
            value = 0;
            foreach (char c in text)
            {
                // if uppermost bit is set, then the next bitshift will overflow
                if ((value & 0x8000000000000000) != 0)
                {
                    return false;
                }
                // We shouldn't ever get a string that's nonbinary (see ScanNumericLiteral),
                // so don't explicitly check for it (there's a debug assert in SyntaxFacts)
                var bit = (ulong)SyntaxFacts.BinaryValue(c);
                value = (value << 1) | bit;
            }
            return true;
        }

        //used in directives
        private int GetValueInt32(string text, bool isHex)
        {
            int result;
            if (!Int32.TryParse(text, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture, out result))
            {
                //we've already lexed the literal, so the error must be from overflow
                this.AddError(MakeError(ErrorCode.ERR_IntOverflow));
            }

            return result;
        }

        //used for all non-directive integer literals (cast to desired type afterward)
        private ulong GetValueUInt64(string text, bool isHex, bool isBinary)
        {
            ulong result;
            if (isBinary)
            {
                if (!TryParseBinaryUInt64(text, out result))
                {
                    this.AddError(MakeError(ErrorCode.ERR_IntOverflow));
                }
            }
            else if (!UInt64.TryParse(text, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture, out result))
            {
                //we've already lexed the literal, so the error must be from overflow
                this.AddError(MakeError(ErrorCode.ERR_IntOverflow));
            }

            return result;
        }

        private double GetValueDouble(string text)
        {
            double result;
            if (!RealParser.TryParseDouble(text, out result))
            {
                //we've already lexed the literal, so the error must be from overflow
                this.AddError(MakeError(ErrorCode.ERR_FloatOverflow, "double"));
            }

            return result;
        }

        private float GetValueSingle(string text)
        {
            float result;
            if (!RealParser.TryParseFloat(text, out result))
            {
                //we've already lexed the literal, so the error must be from overflow
                this.AddError(MakeError(ErrorCode.ERR_FloatOverflow, "float"));
            }

            return result;
        }

        private decimal GetValueDecimal(string text, int start, int end)
        {
            // Use decimal.TryParse to parse value. Note: the behavior of
            // decimal.TryParse differs from Dev11 in several cases:
            //
            // 1. [-]0eNm where N > 0
            //     The native compiler ignores sign and scale and treats such cases
            //     as 0e0m. decimal.TryParse fails so these cases are compile errors.
            //     [Bug #568475]
            // 2. 1e-Nm where N >= 1000
            //     The native compiler reports CS0594 "Floating-point constant is
            //     outside the range of type 'decimal'". decimal.TryParse allows
            //     N >> 1000 but treats decimals with very small exponents as 0.
            //     [No bug.]
            // 3. Decimals with significant digits below 1e-49
            //     The native compiler considers digits below 1e-49 when rounding.
            //     decimal.TryParse ignores digits below 1e-49 when rounding. This
            //     last difference is perhaps the most significant since existing code
            //     will continue to compile but constant values may be rounded differently.
            //     (Note that the native compiler does not round in all cases either since
            //     the native compiler chops the string at 50 significant digits. For example
            //     ".100000000000000000000000000050000000000000000000001m" is not
            //     rounded up to 0.1000000000000000000000000001.)
            //     [Bug #568494]

            decimal result;
            if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result))
            {
                //we've already lexed the literal, so the error must be from overflow
                this.AddError(this.MakeError(start, end - start, ErrorCode.ERR_FloatOverflow, "decimal"));
            }

            return result;
        }

        private void ResetIdentBuffer()
        {
            _identLen = 0;
        }

        private void AddIdentChar(char ch)
        {
            if (_identLen >= _identBuffer.Length)
            {
                this.GrowIdentBuffer();
            }

            _identBuffer[_identLen++] = ch;
        }

        private void GrowIdentBuffer()
        {
            var tmp = new char[_identBuffer.Length * 2];
            Array.Copy(_identBuffer, tmp, _identBuffer.Length);
            _identBuffer = tmp;
        }

        private bool ScanIdentifier(ref TokenInfo info)
        {
            return
                ScanIdentifier_FastPath(ref info) ||
                (InXmlCrefOrNameAttributeValue ? ScanIdentifier_CrefSlowPath(ref info) : ScanIdentifier_SlowPath(ref info));
        }

        // Implements a faster identifier lexer for the common case in the 
        // language where:
        //
        //   a) identifiers are not verbatim
        //   b) identifiers don't contain unicode characters
        //   c) identifiers don't contain unicode escapes
        //
        // Given that nearly all identifiers will contain [_a-zA-Z0-9] and will
        // be terminated by a small set of known characters (like dot, comma, 
        // etc.), we can sit in a tight loop looking for this pattern and only
        // falling back to the slower (but correct) path if we see something we
        // can't handle.
        //
        // Note: this function also only works if the identifier (and terminator)
        // can be found in the current sliding window of chars we have from our
        // source text.  With this constraint we can avoid the costly overhead 
        // incurred with peek/advance/next.  Because of this we can also avoid
        // the unnecessary stores/reads from identBuffer and all other instance
        // state while lexing.  Instead we just keep track of our start, end,
        // and max positions and use those for quick checks internally.
        //
        // Note: it is critical that this method must only be called from a 
        // code path that checked for IsIdentifierStartChar or '@' first. 
        private bool ScanIdentifier_FastPath(ref TokenInfo info)
        {
            if ((_mode & LexerMode.MaskLexMode) == LexerMode.DebuggerSyntax)
            {
                // Debugger syntax is wonky.  Can't use the fast path for it.
                return false;
            }

            var currentOffset = TextWindow.Offset;
            var characterWindow = TextWindow.CharacterWindow;
            var characterWindowCount = TextWindow.CharacterWindowCount;

            var startOffset = currentOffset;

            while (true)
            {
                if (currentOffset == characterWindowCount)
                {
                    // no more contiguous characters.  Fall back to slow path
                    return false;
                }

                switch (characterWindow[currentOffset])
                {
                    case '&':
                        // CONSIDER: This method is performance critical, so
                        // it might be safer to kick out at the top (as for
                        // LexerMode.DebuggerSyntax).

                        // If we're in a cref, this could be the start of an
                        // xml entity that belongs in the identifier.
                        if (InXmlCrefOrNameAttributeValue)
                        {
                            // Fall back on the slow path.
                            return false;
                        }

                        // Otherwise, end the identifier.
                        goto case '\0';
                    case '\0':
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                    case '!':
                    case '%':
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                    case ',':
                    case '-':
                    case '.':
                    case '/':
                    case ':':
                    case ';':
                    case '<':
                    case '=':
                    case '>':
                    case '?':
                    case '[':
                    case ']':
                    case '^':
                    case '{':
                    case '|':
                    case '}':
                    case '~':
                    case '"':
                    case '\'':
                        // All of the following characters are not valid in an 
                        // identifier.  If we see any of them, then we know we're
                        // done.
                        var length = currentOffset - startOffset;
                        TextWindow.AdvanceChar(length);
                        info.Text = info.StringValue = TextWindow.Intern(characterWindow, startOffset, length);
                        info.IsVerbatim = false;
                        return true;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if (currentOffset == startOffset)
                        {
                            return false;
                        }
                        else
                        {
                            goto case 'A';
                        }
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case '_':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                        // All of these characters are valid inside an identifier.
                        // consume it and keep processing.
                        currentOffset++;
                        continue;

                    // case '@':  verbatim identifiers are handled in the slow path
                    // case '\\': unicode escapes are handled in the slow path
                    default:
                        // Any other character is something we cannot handle.  i.e.
                        // unicode chars or an escape.  Just break out and move to
                        // the slow path.
                        return false;
                }
            }
        }

        private bool ScanIdentifier_SlowPath(ref TokenInfo info)
        {
            int start = TextWindow.Position;
            this.ResetIdentBuffer();

            while (TextWindow.PeekChar() == '@')
            {
                TextWindow.AdvanceChar();
            }

            var atCount = TextWindow.Position - start;
            info.IsVerbatim = atCount > 0;

            bool isObjectAddress = false;
            while (true)
            {
                char surrogateCharacter = SlidingTextWindow.InvalidCharacter;
                bool isEscaped = false;
                char ch = TextWindow.PeekChar();
top:
                switch (ch)
                {
                    case '\\':
                        if (!isEscaped && IsUnicodeEscape())
                        {
                            // ^^^^^^^ otherwise \u005Cu1234 looks just like \u1234! (i.e. escape within escape)
                            info.HasIdentifierEscapeSequence = true;
                            isEscaped = true;
                            ch = PeekUnicodeEscape(out surrogateCharacter);
                            goto top;
                        }

                        goto default;
                    case '$':
                        if (!this.ModeIs(LexerMode.DebuggerSyntax) || _identLen > 0)
                        {
                            goto LoopExit;
                        }

                        break;
                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        goto LoopExit;
                    case '_':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                        {
                            // Again, these are the 'common' identifier characters...
                            break;
                        }

                    case '0':
                        {
                            if (_identLen == 0)
                            {
                                // Debugger syntax allows @0x[hexdigit]+ for object address identifiers.
                                if (info.IsVerbatim &&
                                    this.ModeIs(LexerMode.DebuggerSyntax) &&
                                    (char.ToLower(TextWindow.PeekChar(1)) == 'x'))
                                {
                                    isObjectAddress = true;
                                }
                                else
                                {
                                    goto LoopExit;
                                }
                            }

                            // Again, these are the 'common' identifier characters...
                            break;
                        }
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        {
                            if (_identLen == 0)
                            {
                                goto LoopExit;
                            }

                            // Again, these are the 'common' identifier characters...
                            break;
                        }

                    case ' ':
                    case '\t':
                    case '.':
                    case ';':
                    case '(':
                    case ')':
                    case ',':
                        // ...and these are the 'common' stop characters.
                        goto LoopExit;
                    case '<':
                        if (_identLen == 0 && this.ModeIs(LexerMode.DebuggerSyntax) && TextWindow.PeekChar(1) == '>')
                        {
                            // In DebuggerSyntax mode, identifiers are allowed to begin with <>.
                            TextWindow.AdvanceChar(2);
                            this.AddIdentChar('<');
                            this.AddIdentChar('>');
                            continue;
                        }

                        goto LoopExit;
                    default:
                        {
                            // This is the 'expensive' call
                            if (_identLen == 0 && ch > 127 && SyntaxFacts.IsIdentifierStartCharacter(ch))
                            {
                                break;
                            }
                            else if (_identLen > 0 && ch > 127 && SyntaxFacts.IsIdentifierPartCharacter(ch))
                            {
                                //// BUG 424819 : Handle identifier chars > 0xFFFF via surrogate pairs
                                if (UnicodeCharacterUtilities.IsFormattingChar(ch))
                                {
                                    if (isEscaped)
                                    {
                                        SyntaxDiagnosticInfo? error;
                                        NextCharOrUnicodeEscape(out surrogateCharacter, out error);
                                        AddError(error);
                                    }
                                    else
                                    {
                                        TextWindow.AdvanceChar();
                                    }

                                    continue; // Ignore formatting characters
                                }

                                break;
                            }
                            else
                            {
                                // Not a valid identifier character, so bail.
                                goto LoopExit;
                            }
                        }
                }

                if (isEscaped)
                {
                    SyntaxDiagnosticInfo? error;
                    NextCharOrUnicodeEscape(out surrogateCharacter, out error);
                    AddError(error);
                }
                else
                {
                    TextWindow.AdvanceChar();
                }

                this.AddIdentChar(ch);
                if (surrogateCharacter != SlidingTextWindow.InvalidCharacter)
                {
                    this.AddIdentChar(surrogateCharacter);
                }
            }

LoopExit:
            var width = TextWindow.Width; // exact size of input characters
            if (_identLen > 0)
            {
                info.Text = TextWindow.GetInternedText();

                // id buffer is identical to width in input
                if (_identLen == width)
                {
                    info.StringValue = info.Text;
                }
                else
                {
                    info.StringValue = TextWindow.Intern(_identBuffer, 0, _identLen);
                }

                if (isObjectAddress)
                {
                    // @0x[hexdigit]+
                    const int objectAddressOffset = 2;
                    Debug.Assert(string.Equals(info.Text.Substring(0, objectAddressOffset + 1), "@0x", StringComparison.OrdinalIgnoreCase));
                    var valueText = TextWindow.Intern(_identBuffer, objectAddressOffset, _identLen - objectAddressOffset);
                    // Verify valid hex value.
                    if ((valueText.Length == 0) || !valueText.All(SyntaxFacts.IsHexDigit))
                    {
                        goto Fail;
                    }
                    // Parse hex value to check for overflow.
                    this.GetValueUInt64(valueText, isHex: true, isBinary: false);
                }

                if (atCount >= 2)
                {
                    this.AddError(start, atCount, ErrorCode.ERR_IllegalAtSequence);
                }

                return true;
            }

Fail:
            info.Text = null;
            info.StringValue = null;
            TextWindow.Reset(start);
            return false;
        }

        /// <summary>
        /// This method is essentially the same as ScanIdentifier_SlowPath,
        /// except that it can handle XML entities.  Since ScanIdentifier
        /// is hot code and since this method does extra work, it seem
        /// worthwhile to separate it from the common case.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private bool ScanIdentifier_CrefSlowPath(ref TokenInfo info)
        {
            Debug.Assert(InXmlCrefOrNameAttributeValue);

            int start = TextWindow.Position;
            this.ResetIdentBuffer();

            if (AdvanceIfMatches('@'))
            {
                // In xml name attribute values, the '@' is part of the value text of the identifier
                // (to match dev11).
                if (InXmlNameAttributeValue)
                {
                    AddIdentChar('@');
                }
                else
                {
                    info.IsVerbatim = true;
                }
            }

            while (true)
            {
                int beforeConsumed = TextWindow.Position;
                char consumedChar;
                char consumedSurrogate;

                if (TextWindow.PeekChar() == '&')
                {
                    if (!TryScanXmlEntity(out consumedChar, out consumedSurrogate))
                    {
                        // If it's not a valid entity, then it's not part of the identifier.
                        TextWindow.Reset(beforeConsumed);
                        goto LoopExit;
                    }
                }
                else
                {
                    consumedChar = TextWindow.NextChar();
                    consumedSurrogate = SlidingTextWindow.InvalidCharacter;
                }

                // NOTE: If the surrogate is non-zero, then consumedChar won't match
                // any of the cases below (UTF-16 guarantees that members of surrogate
                // pairs aren't separately valid).

                bool isEscaped = false;
top:
                switch (consumedChar)
                {
                    case '\\':
                        // NOTE: For completeness, we should allow xml entities in unicode escape
                        // sequences (DevDiv #16321).  Since it is not currently a priority, we will
                        // try to make the interim behavior sensible: we will only attempt to scan
                        // a unicode escape if NONE of the characters are XML entities (including
                        // the backslash, which we have already consumed).
                        // When we're ready to implement this behavior, we can drop the position
                        // check and use AdvanceIfMatches instead of PeekChar.
                        if (!isEscaped && (TextWindow.Position == beforeConsumed + 1) &&
                            (TextWindow.PeekChar() == 'u' || TextWindow.PeekChar() == 'U'))
                        {
                            Debug.Assert(consumedSurrogate == SlidingTextWindow.InvalidCharacter, "Since consumedChar == '\\'");

                            info.HasIdentifierEscapeSequence = true;

                            TextWindow.Reset(beforeConsumed);
                            // ^^^^^^^ otherwise \u005Cu1234 looks just like \u1234! (i.e. escape within escape)
                            isEscaped = true;
                            SyntaxDiagnosticInfo? error;
                            consumedChar = NextUnicodeEscape(out consumedSurrogate, out error);
                            AddCrefError(error);
                            goto top;
                        }

                        goto default;

                    case '_':
                    case 'A':
                    case 'B':
                    case 'C':
                    case 'D':
                    case 'E':
                    case 'F':
                    case 'G':
                    case 'H':
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                    case 'a':
                    case 'b':
                    case 'c':
                    case 'd':
                    case 'e':
                    case 'f':
                    case 'g':
                    case 'h':
                    case 'i':
                    case 'j':
                    case 'k':
                    case 'l':
                    case 'm':
                    case 'n':
                    case 'o':
                    case 'p':
                    case 'q':
                    case 'r':
                    case 's':
                    case 't':
                    case 'u':
                    case 'v':
                    case 'w':
                    case 'x':
                    case 'y':
                    case 'z':
                        {
                            // Again, these are the 'common' identifier characters...
                            break;
                        }

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        {
                            if (_identLen == 0)
                            {
                                TextWindow.Reset(beforeConsumed);
                                goto LoopExit;
                            }

                            // Again, these are the 'common' identifier characters...
                            break;
                        }

                    case ' ':
                    case '$':
                    case '\t':
                    case '.':
                    case ';':
                    case '(':
                    case ')':
                    case ',':
                    case '<':
                        // ...and these are the 'common' stop characters.
                        TextWindow.Reset(beforeConsumed);
                        goto LoopExit;
                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        TextWindow.Reset(beforeConsumed);
                        goto LoopExit;
                    default:
                        {
                            // This is the 'expensive' call
                            if (_identLen == 0 && consumedChar > 127 && SyntaxFacts.IsIdentifierStartCharacter(consumedChar))
                            {
                                break;
                            }
                            else if (_identLen > 0 && consumedChar > 127 && SyntaxFacts.IsIdentifierPartCharacter(consumedChar))
                            {
                                //// BUG 424819 : Handle identifier chars > 0xFFFF via surrogate pairs
                                if (UnicodeCharacterUtilities.IsFormattingChar(consumedChar))
                                {
                                    continue; // Ignore formatting characters
                                }

                                break;
                            }
                            else
                            {
                                // Not a valid identifier character, so bail.
                                TextWindow.Reset(beforeConsumed);
                                goto LoopExit;
                            }
                        }
                }

                this.AddIdentChar(consumedChar);
                if (consumedSurrogate != SlidingTextWindow.InvalidCharacter)
                {
                    this.AddIdentChar(consumedSurrogate);
                }
            }

LoopExit:
            if (_identLen > 0)
            {
                // NOTE: If we don't intern the string value, then we won't get a hit
                // in the keyword dictionary!  (It searches for a key using identity.)
                // The text does not have to be interned (and probably shouldn't be
                // if it contains entities (else-case).

                var width = TextWindow.Width; // exact size of input characters

                // id buffer is identical to width in input
                if (_identLen == width)
                {
                    info.StringValue = TextWindow.GetInternedText();
                    info.Text = info.StringValue;
                }
                else
                {
                    info.StringValue = TextWindow.Intern(_identBuffer, 0, _identLen);
                    info.Text = TextWindow.GetText(intern: false);
                }

                return true;
            }
            else
            {
                info.Text = null;
                info.StringValue = null;
                TextWindow.Reset(start);
                return false;
            }
        }

        private bool ScanIdentifierOrKeyword(ref TokenInfo info)
        {
            info.ContextualKind = SyntaxKind.None;

            if (this.ScanIdentifier(ref info))
            {
                RoslynDebug.AssertNotNull(info.Text);

                // check to see if it is an actual keyword
                if (!info.IsVerbatim && !info.HasIdentifierEscapeSequence)
                {
                    if (this.ModeIs(LexerMode.Directive))
                    {
                        SyntaxKind keywordKind = SyntaxFacts.GetPreprocessorKeywordKind(info.Text);
                        if (SyntaxFacts.IsPreprocessorContextualKeyword(keywordKind))
                        {
                            // Let the parser decide which instances are actually keywords.
                            info.Kind = SyntaxKind.IdentifierToken;
                            info.ContextualKind = keywordKind;
                        }
                        else
                        {
                            info.Kind = keywordKind;
                        }
                    }
                    else
                    {
                        if (!_cache.TryGetKeywordKind(info.Text, out info.Kind))
                        {
                            info.ContextualKind = info.Kind = SyntaxKind.IdentifierToken;
                        }
                        else if (SyntaxFacts.IsContextualKeyword(info.Kind))
                        {
                            info.ContextualKind = info.Kind;
                            info.Kind = SyntaxKind.IdentifierToken;
                        }
                    }

                    if (info.Kind == SyntaxKind.None)
                    {
                        info.Kind = SyntaxKind.IdentifierToken;
                    }
                }
                else
                {
                    info.ContextualKind = info.Kind = SyntaxKind.IdentifierToken;
                }

                return true;
            }
            else
            {
                info.Kind = SyntaxKind.None;
                return false;
            }
        }

        private void LexSyntaxTrivia(bool afterFirstToken, bool isTrailing, ref SyntaxListBuilder triviaList)
        {
            bool onlyWhitespaceOnLine = !isTrailing;

            while (true)
            {
                this.Start();
                char ch = TextWindow.PeekChar();
                if (ch == ' ')
                {
                    this.AddTrivia(this.ScanWhitespace(), ref triviaList);
                    continue;
                }
                else if (ch > 127)
                {
                    if (SyntaxFacts.IsWhitespace(ch))
                    {
                        ch = ' ';
                    }
                    else if (SyntaxFacts.IsNewLine(ch))
                    {
                        ch = '\n';
                    }
                }

                switch (ch)
                {
                    case ' ':
                    case '\t':       // Horizontal tab
                    case '\v':       // Vertical Tab
                    case '\f':       // Form-feed
                    case '\u001A':
                        this.AddTrivia(this.ScanWhitespace(), ref triviaList);
                        break;
                    case '/':
                        if ((ch = TextWindow.PeekChar(1)) == '/')
                        {
                            if (!this.SuppressDocumentationCommentParse && TextWindow.PeekChar(2) == '/' && TextWindow.PeekChar(3) != '/')
                            {
                                // Doc comments should never be in trailing trivia.
                                // Stop processing so that it will be leading trivia on the next token.
                                if (isTrailing)
                                {
                                    return;
                                }

                                this.AddTrivia(this.LexXmlDocComment(XmlDocCommentStyle.SingleLine), ref triviaList);
                                break;
                            }

                            // normal single line comment
                            this.ScanToEndOfLine();
                            var text = TextWindow.GetText(false);
                            this.AddTrivia(SyntaxFactory.Comment(text), ref triviaList);
                            onlyWhitespaceOnLine = false;
                            break;
                        }
                        else if (ch == '*')
                        {
                            if (!this.SuppressDocumentationCommentParse && TextWindow.PeekChar(2) == '*' &&
                                TextWindow.PeekChar(3) != '*' && TextWindow.PeekChar(3) != '/')
                            {
                                // Doc comments should never be in trailing trivia.
                                // Stop processing so that it will be leading trivia on the next token.
                                if (isTrailing)
                                {
                                    return;
                                }

                                this.AddTrivia(this.LexXmlDocComment(XmlDocCommentStyle.Delimited), ref triviaList);
                                break;
                            }

                            bool isTerminated;
                            this.ScanMultiLineComment(out isTerminated);
                            if (!isTerminated)
                            {
                                // The comment didn't end.  Report an error at the start point.
                                this.AddError(ErrorCode.ERR_OpenEndedComment);
                            }

                            var text = TextWindow.GetText(false);
                            this.AddTrivia(SyntaxFactory.Comment(text), ref triviaList);
                            onlyWhitespaceOnLine = false;
                            break;
                        }

                        // not trivia
                        return;
                    case '\r':
                    case '\n':
                        var endOfLine = this.ScanEndOfLine();
                        RoslynDebug.AssertNotNull(endOfLine);
                        this.AddTrivia(endOfLine, ref triviaList);
                        if (isTrailing)
                        {
                            return;
                        }

                        onlyWhitespaceOnLine = true;
                        break;
                    case '#':
                        if (_allowPreprocessorDirectives)
                        {
                            this.LexDirectiveAndExcludedTrivia(afterFirstToken, isTrailing || !onlyWhitespaceOnLine, ref triviaList);
                            break;
                        }
                        else
                        {
                            return;
                        }

                    // Note: we specifically do not look for the >>>>>>> pattern as the start of
                    // a conflict marker trivia.  That's because *technically* (albeit unlikely)
                    // >>>>>>> could be the end of a very generic construct.  So, instead, we only
                    // recognize >>>>>>> as we are scanning the trivia after a ======= marker 
                    // (which can never be part of legal code).
                    // case '>':
                    case '|':
                    case '=':
                    case '<':
                        if (!isTrailing)
                        {
                            if (IsConflictMarkerTrivia())
                            {
                                this.LexConflictMarkerTrivia(ref triviaList);
                                break;
                            }
                        }

                        return;

                    default:
                        return;
                }
            }
        }

        // All conflict markers consist of the same character repeated seven times.  If it is
        // a <<<<<<< or >>>>>>> marker then it is also followed by a space.
        private static readonly int s_conflictMarkerLength = "<<<<<<<".Length;

        private bool IsConflictMarkerTrivia()
        {
            var position = TextWindow.Position;
            var text = TextWindow.Text;
            if (position == 0 || SyntaxFacts.IsNewLine(text[position - 1]))
            {
                var firstCh = text[position];
                Debug.Assert(firstCh is '<' or '|' or '=' or '>');

                if ((position + s_conflictMarkerLength) <= text.Length)
                {
                    for (int i = 0, n = s_conflictMarkerLength; i < n; i++)
                    {
                        if (text[position + i] != firstCh)
                        {
                            return false;
                        }
                    }

                    if (firstCh is '|' or '=')
                    {
                        return true;
                    }

                    return (position + s_conflictMarkerLength) < text.Length &&
                        text[position + s_conflictMarkerLength] == ' ';
                }
            }

            return false;
        }

        private void LexConflictMarkerTrivia(ref SyntaxListBuilder triviaList)
        {
            this.Start();

            this.AddError(TextWindow.Position, s_conflictMarkerLength,
                ErrorCode.ERR_Merge_conflict_marker_encountered);

            var startCh = this.TextWindow.PeekChar();

            // First create a trivia from the start of this merge conflict marker to the
            // end of line/file (whichever comes first).
            LexConflictMarkerHeader(ref triviaList);

            // Now add the newlines as the next trivia.
            LexConflictMarkerEndOfLine(ref triviaList);

            // Now, if it was an ||||||| or ======= marker, then also created a DisabledText trivia for
            // the contents of the file after it, up until the next >>>>>>> marker we see.
            if (startCh is '|' or '=')
            {
                LexConflictMarkerDisabledText(startCh == '=', ref triviaList);
            }
        }

        private SyntaxListBuilder LexConflictMarkerDisabledText(bool atSecondMiddleMarker, ref SyntaxListBuilder triviaList)
        {
            // Consume everything from the end of the current mid-conflict marker to the start of the next
            // end-conflict marker
            this.Start();

            var hitNextMarker = false;
            while (true)
            {
                var ch = this.TextWindow.PeekChar();
                if (ch == SlidingTextWindow.InvalidCharacter)
                {
                    break;
                }

                if (!atSecondMiddleMarker && ch == '=' && IsConflictMarkerTrivia())
                {
                    hitNextMarker = true;
                    break;
                }

                // If we hit the end-conflict marker, then lex it out at this point.
                if (ch == '>' && IsConflictMarkerTrivia())
                {
                    hitNextMarker = true;
                    break;
                }

                this.TextWindow.AdvanceChar();
            }

            if (this.TextWindow.Width > 0)
            {
                this.AddTrivia(SyntaxFactory.DisabledText(TextWindow.GetText(false)), ref triviaList);
            }

            if (hitNextMarker)
            {
                LexConflictMarkerTrivia(ref triviaList);
            }

            return triviaList;
        }

        private void LexConflictMarkerEndOfLine(ref SyntaxListBuilder triviaList)
        {
            this.Start();
            while (SyntaxFacts.IsNewLine(this.TextWindow.PeekChar()))
            {
                this.TextWindow.AdvanceChar();
            }

            if (this.TextWindow.Width > 0)
            {
                this.AddTrivia(SyntaxFactory.EndOfLine(TextWindow.GetText(false)), ref triviaList);
            }
        }

        private void LexConflictMarkerHeader(ref SyntaxListBuilder triviaList)
        {
            while (true)
            {
                var ch = this.TextWindow.PeekChar();
                if (ch == SlidingTextWindow.InvalidCharacter || SyntaxFacts.IsNewLine(ch))
                {
                    break;
                }

                this.TextWindow.AdvanceChar();
            }

            this.AddTrivia(SyntaxFactory.ConflictMarker(TextWindow.GetText(false)), ref triviaList);
        }

        private void AddTrivia(CSharpSyntaxNode trivia, [NotNull] ref SyntaxListBuilder? list)
        {
            if (this.HasErrors)
            {
                trivia = trivia.WithDiagnosticsGreen(this.GetErrors(leadingTriviaWidth: 0));
            }

            if (list == null)
            {
                list = new SyntaxListBuilder(TriviaListInitialCapacity);
            }

            list.Add(trivia);
        }

        private bool ScanMultiLineComment(out bool isTerminated)
        {
            if (TextWindow.PeekChar() == '/' && TextWindow.PeekChar(1) == '*')
            {
                TextWindow.AdvanceChar(2);

                char ch;
                while (true)
                {
                    if ((ch = TextWindow.PeekChar()) == SlidingTextWindow.InvalidCharacter && TextWindow.IsReallyAtEnd())
                    {
                        isTerminated = false;
                        break;
                    }
                    else if (ch == '*' && TextWindow.PeekChar(1) == '/')
                    {
                        TextWindow.AdvanceChar(2);
                        isTerminated = true;
                        break;
                    }
                    else
                    {
                        TextWindow.AdvanceChar();
                    }
                }

                return true;
            }
            else
            {
                isTerminated = false;
                return false;
            }
        }

        private void ScanToEndOfLine()
        {
            char ch;
            while (!SyntaxFacts.IsNewLine(ch = TextWindow.PeekChar()) &&
                (ch != SlidingTextWindow.InvalidCharacter || !TextWindow.IsReallyAtEnd()))
            {
                TextWindow.AdvanceChar();
            }
        }

        /// <summary>
        /// Scans a new-line sequence (either a single new-line character or a CR-LF combo).
        /// </summary>
        /// <returns>A trivia node with the new-line text</returns>
        private CSharpSyntaxNode? ScanEndOfLine()
        {
            char ch;
            switch (ch = TextWindow.PeekChar())
            {
                case '\r':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '\n')
                    {
                        TextWindow.AdvanceChar();
                        return SyntaxFactory.CarriageReturnLineFeed;
                    }

                    return SyntaxFactory.CarriageReturn;
                case '\n':
                    TextWindow.AdvanceChar();
                    return SyntaxFactory.LineFeed;
                default:
                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        TextWindow.AdvanceChar();
                        return SyntaxFactory.EndOfLine(ch.ToString());
                    }

                    return null;
            }
        }

        /// <summary>
        /// Scans all of the whitespace (not new-lines) into a trivia node until it runs out.
        /// </summary>
        /// <returns>A trivia node with the whitespace text</returns>
        private SyntaxTrivia ScanWhitespace()
        {
            if (_createWhitespaceTriviaFunction == null)
            {
                _createWhitespaceTriviaFunction = this.CreateWhitespaceTrivia;
            }

            int hashCode = Hash.FnvOffsetBias;  // FNV base
            bool onlySpaces = true;

top:
            char ch = TextWindow.PeekChar();

            switch (ch)
            {
                case '\t':       // Horizontal tab
                case '\v':       // Vertical Tab
                case '\f':       // Form-feed
                case '\u001A':
                    onlySpaces = false;
                    goto case ' ';

                case ' ':
                    TextWindow.AdvanceChar();
                    hashCode = Hash.CombineFNVHash(hashCode, ch);
                    goto top;

                case '\r':      // Carriage Return
                case '\n':      // Line-feed
                    break;

                default:
                    if (ch > 127 && SyntaxFacts.IsWhitespace(ch))
                    {
                        goto case '\t';
                    }

                    break;
            }

            if (TextWindow.Width == 1 && onlySpaces)
            {
                return SyntaxFactory.Space;
            }
            else
            {
                var width = TextWindow.Width;

                if (width < MaxCachedTokenSize)
                {
                    return _cache.LookupTrivia(
                        TextWindow.CharacterWindow,
                        TextWindow.LexemeRelativeStart,
                        width,
                        hashCode,
                        _createWhitespaceTriviaFunction);
                }
                else
                {
                    return _createWhitespaceTriviaFunction();
                }
            }
        }

        private Func<SyntaxTrivia>? _createWhitespaceTriviaFunction;

        private SyntaxTrivia CreateWhitespaceTrivia()
        {
            return SyntaxFactory.Whitespace(TextWindow.GetText(intern: true));
        }

        private void LexDirectiveAndExcludedTrivia(
            bool afterFirstToken,
            bool afterNonWhitespaceOnLine,
            ref SyntaxListBuilder triviaList)
        {
            var directive = this.LexSingleDirective(true, true, afterFirstToken, afterNonWhitespaceOnLine, ref triviaList);

            // also lex excluded stuff            
            var branching = directive as BranchingDirectiveTriviaSyntax;
            if (branching != null && !branching.BranchTaken)
            {
                this.LexExcludedDirectivesAndTrivia(true, ref triviaList);
            }
        }

        private void LexExcludedDirectivesAndTrivia(bool endIsActive, ref SyntaxListBuilder triviaList)
        {
            while (true)
            {
                bool hasFollowingDirective;
                var text = this.LexDisabledText(out hasFollowingDirective);
                if (text != null)
                {
                    this.AddTrivia(text, ref triviaList);
                }

                if (!hasFollowingDirective)
                {
                    break;
                }

                var directive = this.LexSingleDirective(false, endIsActive, false, false, ref triviaList);
                var branching = directive as BranchingDirectiveTriviaSyntax;
                if (directive.Kind == SyntaxKind.EndIfDirectiveTrivia || (branching != null && branching.BranchTaken))
                {
                    break;
                }
                else if (directive.Kind == SyntaxKind.IfDirectiveTrivia)
                {
                    this.LexExcludedDirectivesAndTrivia(false, ref triviaList);
                }
            }
        }

        private CSharpSyntaxNode LexSingleDirective(
            bool isActive,
            bool endIsActive,
            bool afterFirstToken,
            bool afterNonWhitespaceOnLine,
            ref SyntaxListBuilder triviaList)
        {
            if (SyntaxFacts.IsWhitespace(TextWindow.PeekChar()))
            {
                this.Start();
                this.AddTrivia(this.ScanWhitespace(), ref triviaList);
            }

            CSharpSyntaxNode directive;
            var saveMode = _mode;

            using (var dp = new DirectiveParser(this, _directives))
            {
                directive = dp.ParseDirective(isActive, endIsActive, afterFirstToken, afterNonWhitespaceOnLine);
            }

            this.AddTrivia(directive, ref triviaList);
            _directives = directive.ApplyDirectives(_directives);
            _mode = saveMode;
            return directive;
        }

        // consume text up to the next directive
        private CSharpSyntaxNode? LexDisabledText(out bool followedByDirective)
        {
            this.Start();

            int lastLineStart = TextWindow.Position;
            int lines = 0;
            bool allWhitespace = true;

            while (true)
            {
                char ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        followedByDirective = false;
                        return TextWindow.Width > 0 ? SyntaxFactory.DisabledText(TextWindow.GetText(false)) : null;
                    case '#':
                        if (!_allowPreprocessorDirectives) goto default;
                        followedByDirective = true;
                        if (lastLineStart < TextWindow.Position && !allWhitespace)
                        {
                            goto default;
                        }

                        TextWindow.Reset(lastLineStart);  // reset so directive parser can consume the starting whitespace on this line
                        return TextWindow.Width > 0 ? SyntaxFactory.DisabledText(TextWindow.GetText(false)) : null;
                    case '\r':
                    case '\n':
                        this.ScanEndOfLine();
                        lastLineStart = TextWindow.Position;
                        allWhitespace = true;
                        lines++;
                        break;
                    default:
                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        allWhitespace = allWhitespace && SyntaxFacts.IsWhitespace(ch);
                        TextWindow.AdvanceChar();
                        break;
                }
            }
        }

        private SyntaxToken LexDirectiveToken()
        {
            this.Start();
            TokenInfo info = default(TokenInfo);
            this.ScanDirectiveToken(ref info);
            var errors = this.GetErrors(leadingTriviaWidth: 0);
            var trailing = this.LexDirectiveTrailingTrivia(info.Kind == SyntaxKind.EndOfDirectiveToken);
            return Create(in info, null, trailing, errors);
        }

        public SyntaxToken LexEndOfDirectiveWithOptionalPreprocessingMessage()
        {
            PooledStringBuilder? builder = null;

            // Skip the rest of the line until we hit a EOL or EOF.  This follows the PP_Message portion of the specification.
            while (true)
            {
                var ch = this.TextWindow.PeekChar();
                if (SyntaxFacts.IsNewLine(ch))
                {
                    // don't consume EOL characters here
                    break;
                }
                else if (ch is SlidingTextWindow.InvalidCharacter && this.TextWindow.IsReallyAtEnd())
                {
                    // don't consume EOF characters here
                    break;
                }

                builder ??= PooledStringBuilder.GetInstance();
                builder.Builder.Append(ch);
                this.TextWindow.AdvanceChar();
            }

            var leading = builder == null
                ? null
                : SyntaxFactory.PreprocessingMessage(builder.ToStringAndFree());

            // now try to consume the EOL if there.
            var trailing = this.LexDirectiveTrailingTrivia(includeEndOfLine: true)?.ToListNode();
            var endOfDirective = SyntaxFactory.Token(leading, SyntaxKind.EndOfDirectiveToken, trailing);

            return endOfDirective;
        }

        private bool ScanDirectiveToken(ref TokenInfo info)
        {
            char character;
            char surrogateCharacter;
            bool isEscaped = false;

            switch (character = TextWindow.PeekChar())
            {
                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }
                    // don't consume end characters here
                    info.Kind = SyntaxKind.EndOfDirectiveToken;
                    break;

                case '\r':
                case '\n':
                    // don't consume end characters here
                    info.Kind = SyntaxKind.EndOfDirectiveToken;
                    break;

                case '#':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.HashToken;
                    break;

                case '(':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.OpenParenToken;
                    break;

                case ')':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.CloseParenToken;
                    break;

                case ',':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.CommaToken;
                    break;

                case '-':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.MinusToken;
                    break;

                case '!':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.ExclamationEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.ExclamationToken;
                    }

                    break;

                case '=':
                    TextWindow.AdvanceChar();
                    if (TextWindow.PeekChar() == '=')
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.EqualsEqualsToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.EqualsToken;
                    }

                    break;

                case '&':
                    if (TextWindow.PeekChar(1) == '&')
                    {
                        TextWindow.AdvanceChar(2);
                        info.Kind = SyntaxKind.AmpersandAmpersandToken;
                        break;
                    }

                    goto default;

                case '|':
                    if (TextWindow.PeekChar(1) == '|')
                    {
                        TextWindow.AdvanceChar(2);
                        info.Kind = SyntaxKind.BarBarToken;
                        break;
                    }

                    goto default;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    this.ScanInteger();
                    info.Kind = SyntaxKind.NumericLiteralToken;
                    info.Text = TextWindow.GetText(true);
                    info.ValueKind = SpecialType.System_Int32;
                    info.IntValue = this.GetValueInt32(info.Text, false);
                    break;

                case '\"':
                    this.ScanStringLiteral(ref info, inDirective: true);
                    break;

                case '\\':
                    {
                        // Could be unicode escape. Try that.
                        character = PeekCharOrUnicodeEscape(out surrogateCharacter);
                        isEscaped = true;
                        if (SyntaxFacts.IsIdentifierStartCharacter(character))
                        {
                            this.ScanIdentifierOrKeyword(ref info);
                            break;
                        }

                        goto default;
                    }

                default:
                    if (!isEscaped && SyntaxFacts.IsNewLine(character))
                    {
                        goto case '\n';
                    }

                    if (SyntaxFacts.IsIdentifierStartCharacter(character))
                    {
                        this.ScanIdentifierOrKeyword(ref info);
                    }
                    else
                    {
                        // unknown single character
                        if (isEscaped)
                        {
                            SyntaxDiagnosticInfo? error;
                            NextCharOrUnicodeEscape(out surrogateCharacter, out error);
                            AddError(error);
                        }
                        else
                        {
                            TextWindow.AdvanceChar();
                        }

                        info.Kind = SyntaxKind.None;
                        info.Text = TextWindow.GetText(true);
                    }

                    break;
            }

            Debug.Assert(info.Kind != SyntaxKind.None || info.Text != null);
            return info.Kind != SyntaxKind.None;
        }

        private SyntaxListBuilder? LexDirectiveTrailingTrivia(bool includeEndOfLine)
        {
            SyntaxListBuilder? trivia = null;

            CSharpSyntaxNode? tr;
            while (true)
            {
                var pos = TextWindow.Position;
                tr = this.LexDirectiveTrivia();
                if (tr == null)
                {
                    break;
                }
                else if (tr.Kind == SyntaxKind.EndOfLineTrivia)
                {
                    if (includeEndOfLine)
                    {
                        AddTrivia(tr, ref trivia);
                    }
                    else
                    {
                        // don't consume end of line...
                        TextWindow.Reset(pos);
                    }

                    break;
                }
                else
                {
                    AddTrivia(tr, ref trivia);
                }
            }

            return trivia;
        }

        private CSharpSyntaxNode? LexDirectiveTrivia()
        {
            CSharpSyntaxNode? trivia = null;

            this.Start();
            char ch = TextWindow.PeekChar();
            switch (ch)
            {
                case '/':
                    if (TextWindow.PeekChar(1) == '/')
                    {
                        // normal single line comment
                        this.ScanToEndOfLine();
                        var text = TextWindow.GetText(false);
                        trivia = SyntaxFactory.Comment(text);
                    }

                    break;
                case '\r':
                case '\n':
                    trivia = this.ScanEndOfLine();
                    break;
                case ' ':
                case '\t':       // Horizontal tab
                case '\v':       // Vertical Tab
                case '\f':       // Form-feed
                    trivia = this.ScanWhitespace();
                    break;

                default:
                    if (SyntaxFacts.IsWhitespace(ch))
                    {
                        goto case ' ';
                    }

                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        goto case '\n';
                    }

                    break;
            }

            return trivia;
        }

        private CSharpSyntaxNode LexXmlDocComment(XmlDocCommentStyle style)
        {
            var saveMode = _mode;
            bool isTerminated;

            var mode = style == XmlDocCommentStyle.SingleLine
                    ? LexerMode.XmlDocCommentStyleSingleLine
                    : LexerMode.XmlDocCommentStyleDelimited;
            if (_xmlParser == null)
            {
                _xmlParser = new DocumentationCommentParser(this, mode);
            }
            else
            {
                _xmlParser.ReInitialize(mode);
            }

            var docComment = _xmlParser.ParseDocumentationComment(out isTerminated);

            // We better have finished with the whole comment. There should be error
            // code in the implementation of ParseXmlDocComment that ensures this.
            Debug.Assert(this.LocationIs(XmlDocCommentLocation.End) || TextWindow.PeekChar() == SlidingTextWindow.InvalidCharacter);

            _mode = saveMode;

            if (!isTerminated)
            {
                // The comment didn't end.  Report an error at the start point.
                // NOTE: report this error even if the DocumentationMode is less than diagnose - the comment
                // would be malformed as a non-doc comment as well.
                this.AddError(TextWindow.LexemeStartPosition, TextWindow.Width, ErrorCode.ERR_OpenEndedComment);
            }

            return docComment;
        }

        /// <summary>
        /// Lexer entry point for LexMode.XmlDocComment
        /// </summary>
        private SyntaxToken LexXmlToken()
        {
            TokenInfo xmlTokenInfo = default(TokenInfo);

            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTrivia(ref leading);

            this.Start();
            this.ScanXmlToken(ref xmlTokenInfo);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in xmlTokenInfo, leading, null, errors);
        }

        private bool ScanXmlToken(ref TokenInfo info)
        {
            char ch;

            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            switch (ch = TextWindow.PeekChar())
            {
                case '&':
                    this.ScanXmlEntity(ref info);
                    info.Kind = SyntaxKind.XmlEntityLiteralToken;
                    break;

                case '<':
                    this.ScanXmlTagStart(ref info);
                    break;

                case '\r':
                case '\n':
                    ScanXmlTextLiteralNewLineToken(ref info);
                    break;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        goto case '\n';
                    }

                    this.ScanXmlText(ref info);
                    info.Kind = SyntaxKind.XmlTextLiteralToken;
                    break;
            }

            Debug.Assert(info.Kind != SyntaxKind.None || info.Text != null);
            return info.Kind != SyntaxKind.None;
        }

        private void ScanXmlTextLiteralNewLineToken(ref TokenInfo info)
        {
            this.ScanEndOfLine();
            info.StringValue = info.Text = TextWindow.GetText(intern: false);
            info.Kind = SyntaxKind.XmlTextLiteralNewLineToken;
            this.MutateLocation(XmlDocCommentLocation.Exterior);
        }

        private void ScanXmlTagStart(ref TokenInfo info)
        {
            Debug.Assert(TextWindow.PeekChar() == '<');

            if (TextWindow.PeekChar(1) == '!')
            {
                if (TextWindow.PeekChar(2) == '-'
                    && TextWindow.PeekChar(3) == '-')
                {
                    TextWindow.AdvanceChar(4);
                    info.Kind = SyntaxKind.XmlCommentStartToken;
                }
                else if (TextWindow.PeekChar(2) == '['
                    && TextWindow.PeekChar(3) == 'C'
                    && TextWindow.PeekChar(4) == 'D'
                    && TextWindow.PeekChar(5) == 'A'
                    && TextWindow.PeekChar(6) == 'T'
                    && TextWindow.PeekChar(7) == 'A'
                    && TextWindow.PeekChar(8) == '[')
                {
                    TextWindow.AdvanceChar(9);
                    info.Kind = SyntaxKind.XmlCDataStartToken;
                }
                else
                {
                    // TODO: Take the < by itself, I guess?
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.LessThanToken;
                }
            }
            else if (TextWindow.PeekChar(1) == '/')
            {
                TextWindow.AdvanceChar(2);
                info.Kind = SyntaxKind.LessThanSlashToken;
            }
            else if (TextWindow.PeekChar(1) == '?')
            {
                TextWindow.AdvanceChar(2);
                info.Kind = SyntaxKind.XmlProcessingInstructionStartToken;
            }
            else
            {
                TextWindow.AdvanceChar();
                info.Kind = SyntaxKind.LessThanToken;
            }
        }

        private void ScanXmlEntity(ref TokenInfo info)
        {
            info.StringValue = null;

            Debug.Assert(TextWindow.PeekChar() == '&');
            TextWindow.AdvanceChar();
            _builder.Clear();
            XmlParseErrorCode? error = null;
            object[]? errorArgs = null;

            char ch;
            if (IsXmlNameStartChar(ch = TextWindow.PeekChar()))
            {
                while (IsXmlNameChar(ch = TextWindow.PeekChar()))
                {
                    // Important bit of information here: none of \0, \r, \n, and crucially for
                    // delimited comments, * are considered Xml name characters. Also, since
                    // entities appear in xml text and attribute text, it's relevant here that
                    // none of <, /, >, ', ", =, are Xml name characters. Note that - and ] are
                    // irrelevant--entities do not appear in comments or cdata.

                    TextWindow.AdvanceChar();
                    _builder.Append(ch);
                }

                switch (_builder.ToString())
                {
                    case "lt":
                        info.StringValue = "<";
                        break;
                    case "gt":
                        info.StringValue = ">";
                        break;
                    case "amp":
                        info.StringValue = "&";
                        break;
                    case "apos":
                        info.StringValue = "'";
                        break;
                    case "quot":
                        info.StringValue = "\"";
                        break;
                    default:
                        error = XmlParseErrorCode.XML_RefUndefinedEntity_1;
                        errorArgs = new[] { _builder.ToString() };
                        break;
                }
            }
            else if (ch == '#')
            {
                TextWindow.AdvanceChar();
                bool isHex = TextWindow.PeekChar() == 'x';
                uint charValue = 0;

                if (isHex)
                {
                    TextWindow.AdvanceChar(); // x
                    while (SyntaxFacts.IsHexDigit(ch = TextWindow.PeekChar()))
                    {
                        TextWindow.AdvanceChar();

                        // disallow overflow
                        if (charValue <= 0x7FFFFFF)
                        {
                            charValue = (charValue << 4) + (uint)SyntaxFacts.HexValue(ch);
                        }
                    }
                }
                else
                {
                    while (SyntaxFacts.IsDecDigit(ch = TextWindow.PeekChar()))
                    {
                        TextWindow.AdvanceChar();

                        // disallow overflow
                        if (charValue <= 0x7FFFFFF)
                        {
                            charValue = (charValue << 3) + (charValue << 1) + (uint)SyntaxFacts.DecValue(ch);
                        }
                    }
                }

                if (TextWindow.PeekChar() != ';')
                {
                    error = XmlParseErrorCode.XML_InvalidCharEntity;
                }

                if (MatchesProductionForXmlChar(charValue))
                {
                    char lowSurrogate;
                    char highSurrogate = SlidingTextWindow.GetCharsFromUtf32(charValue, out lowSurrogate);

                    _builder.Append(highSurrogate);
                    if (lowSurrogate != SlidingTextWindow.InvalidCharacter)
                    {
                        _builder.Append(lowSurrogate);
                    }

                    info.StringValue = _builder.ToString();
                }
                else
                {
                    if (error == null)
                    {
                        error = XmlParseErrorCode.XML_InvalidUnicodeChar;
                    }
                }
            }
            else
            {
                if (SyntaxFacts.IsWhitespace(ch) || SyntaxFacts.IsNewLine(ch))
                {
                    if (error == null)
                    {
                        error = XmlParseErrorCode.XML_InvalidWhitespace;
                    }
                }
                else
                {
                    if (error == null)
                    {
                        error = XmlParseErrorCode.XML_InvalidToken;
                        errorArgs = new[] { ch.ToString() };
                    }
                }
            }

            ch = TextWindow.PeekChar();
            if (ch == ';')
            {
                TextWindow.AdvanceChar();
            }
            else
            {
                if (error == null)
                {
                    error = XmlParseErrorCode.XML_InvalidToken;
                    errorArgs = new[] { ch.ToString() };
                }
            }

            // If we don't have a value computed from above, then we don't recognize the entity, in which
            // case we will simply use the text.

            info.Text = TextWindow.GetText(true);
            if (info.StringValue == null)
            {
                info.StringValue = info.Text;
            }

            if (error != null)
            {
                this.AddError(error.Value, errorArgs ?? Array.Empty<object>());
            }
        }

        private static bool MatchesProductionForXmlChar(uint charValue)
        {
            // Char ::= #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF] /* any Unicode character, excluding the surrogate blocks, FFFE, and FFFF. */

            return
                charValue == 0x9 ||
                charValue == 0xA ||
                charValue == 0xD ||
                (charValue >= 0x20 && charValue <= 0xD7FF) ||
                (charValue >= 0xE000 && charValue <= 0xFFFD) ||
                (charValue >= 0x10000 && charValue <= 0x10FFFF);
        }

        private void ScanXmlText(ref TokenInfo info)
        {
            // Collect "]]>" strings into their own XmlText.
            if (TextWindow.PeekChar() == ']' && TextWindow.PeekChar(1) == ']' && TextWindow.PeekChar(2) == '>')
            {
                TextWindow.AdvanceChar(3);
                info.StringValue = info.Text = TextWindow.GetText(false);
                this.AddError(XmlParseErrorCode.XML_CDataEndTagNotAllowed);
                return;
            }

            while (true)
            {
                var ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;
                    case '&':
                    case '<':
                    case '\r':
                    case '\n':
                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case '*':
                        if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                        {
                            // we're at the end of the comment, but don't lex it yet.
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    case ']':
                        if (TextWindow.PeekChar(1) == ']' && TextWindow.PeekChar(2) == '>')
                        {
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    default:
                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        TextWindow.AdvanceChar();
                        break;
                }
            }
        }

        /// <summary>
        /// Lexer entry point for LexMode.XmlElementTag
        /// </summary>
        private SyntaxToken LexXmlElementTagToken()
        {
            TokenInfo tagInfo = default(TokenInfo);

            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTriviaWithWhitespace(ref leading);

            this.Start();
            this.ScanXmlElementTagToken(ref tagInfo);
            var errors = this.GetErrors(GetFullWidth(leading));

            // PERF: De-dupe common XML element tags
            if (errors == null && tagInfo.ContextualKind == SyntaxKind.None && tagInfo.Kind == SyntaxKind.IdentifierToken)
            {
                RoslynDebug.AssertNotNull(tagInfo.Text);
                SyntaxToken? token = DocumentationCommentXmlTokens.LookupToken(tagInfo.Text, leading);
                if (token != null)
                {
                    return token;
                }
            }

            return Create(in tagInfo, leading, null, errors);
        }

        private bool ScanXmlElementTagToken(ref TokenInfo info)
        {
            char ch;

            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            switch (ch = TextWindow.PeekChar())
            {
                case '<':
                    this.ScanXmlTagStart(ref info);
                    break;

                case '>':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.GreaterThanToken;
                    break;

                case '/':
                    if (TextWindow.PeekChar(1) == '>')
                    {
                        TextWindow.AdvanceChar(2);
                        info.Kind = SyntaxKind.SlashGreaterThanToken;
                        break;
                    }

                    goto default;

                case '"':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.DoubleQuoteToken;
                    break;

                case '\'':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.SingleQuoteToken;
                    break;

                case '=':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.EqualsToken;
                    break;

                case ':':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.ColonToken;
                    break;

                case '\r':
                case '\n':
                    // Assert?
                    break;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    break;

                case '*':
                    if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                    {
                        // Assert? We should have gotten this in the leading trivia.
                        Debug.Assert(false, "Should have picked up leading indentationTrivia, but didn't.");
                        break;
                    }

                    goto default;

                default:
                    if (IsXmlNameStartChar(ch))
                    {
                        this.ScanXmlName(ref info);
                        info.StringValue = info.Text;
                        info.Kind = SyntaxKind.IdentifierToken;
                    }
                    else if (SyntaxFacts.IsWhitespace(ch) || SyntaxFacts.IsNewLine(ch))
                    {
                        // whitespace! needed to do a better job with trivia
                        Debug.Assert(false, "Should have picked up leading indentationTrivia, but didn't.");
                    }
                    else
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.None;
                        info.StringValue = info.Text = TextWindow.GetText(false);
                    }

                    break;
            }

            Debug.Assert(info.Kind != SyntaxKind.None || info.Text != null);
            return info.Kind != SyntaxKind.None;
        }

        private void ScanXmlName(ref TokenInfo info)
        {
            int start = TextWindow.Position;

            while (true)
            {
                char ch = TextWindow.PeekChar();

                // Important bit of information here: none of \0, \r, \n, and crucially for
                // delimited comments, * are considered Xml name characters.
                if (ch != ':' && IsXmlNameChar(ch))
                {
                    // Although ':' is a name char, we don't include it in ScanXmlName
                    // since it is its own token. This enables the parser to add structure
                    // to colon-separated names.

                    // TODO: Could put a big switch here for common cases
                    // if this is a perf bottleneck.
                    TextWindow.AdvanceChar();
                }
                else
                {
                    break;
                }
            }

            info.Text = TextWindow.GetText(start, TextWindow.Position - start, intern: true);
        }

        /// <summary>
        /// Determines whether this Unicode character can start a XMLName.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        private static bool IsXmlNameStartChar(char ch)
        {
            // TODO: which is the right one?
            return XmlCharType.IsStartNCNameCharXml4e(ch);
            // return XmlCharType.IsStartNameSingleChar(ch);
        }

        /// <summary>
        /// Determines if this Unicode character can be part of an XML Name.
        /// </summary>
        /// <param name="ch">The Unicode character.</param>
        private static bool IsXmlNameChar(char ch)
        {
            // TODO: which is the right one?
            return XmlCharType.IsNCNameCharXml4e(ch);
            //return XmlCharType.IsNameSingleChar(ch);
        }

        // TODO: There is a lot of duplication between attribute text, CDATA text, and comment text.
        // It would be nice to factor them together.

        /// <summary>
        /// Lexer entry point for LexMode.XmlAttributeText
        /// </summary>
        private SyntaxToken LexXmlAttributeTextToken()
        {
            TokenInfo info = default(TokenInfo);

            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTrivia(ref leading);

            this.Start();
            this.ScanXmlAttributeTextToken(ref info);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in info, leading, null, errors);
        }

        private bool ScanXmlAttributeTextToken(ref TokenInfo info)
        {
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            char ch;
            switch (ch = TextWindow.PeekChar())
            {
                case '"':
                    if (this.ModeIs(LexerMode.XmlAttributeTextDoubleQuote))
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.DoubleQuoteToken;
                        break;
                    }

                    goto default;

                case '\'':
                    if (this.ModeIs(LexerMode.XmlAttributeTextQuote))
                    {
                        TextWindow.AdvanceChar();
                        info.Kind = SyntaxKind.SingleQuoteToken;
                        break;
                    }

                    goto default;

                case '&':
                    this.ScanXmlEntity(ref info);
                    info.Kind = SyntaxKind.XmlEntityLiteralToken;
                    break;

                case '<':
                    TextWindow.AdvanceChar();
                    info.Kind = SyntaxKind.LessThanToken;
                    break;

                case '\r':
                case '\n':
                    ScanXmlTextLiteralNewLineToken(ref info);
                    break;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        goto case '\n';
                    }

                    this.ScanXmlAttributeText(ref info);
                    info.Kind = SyntaxKind.XmlTextLiteralToken;
                    break;
            }

            Debug.Assert(info.Kind != SyntaxKind.None || info.Text != null);
            return info.Kind != SyntaxKind.None;
        }

        private void ScanXmlAttributeText(ref TokenInfo info)
        {
            while (true)
            {
                var ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case '"':
                        if (this.ModeIs(LexerMode.XmlAttributeTextDoubleQuote))
                        {
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    case '\'':
                        if (this.ModeIs(LexerMode.XmlAttributeTextQuote))
                        {
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    case '&':
                    case '<':
                    case '\r':
                    case '\n':
                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case '*':
                        if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                        {
                            // we're at the end of the comment, but don't lex it yet.
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    default:
                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        TextWindow.AdvanceChar();
                        break;
                }
            }
        }

        /// <summary>
        /// Lexer entry point for LexerMode.XmlCharacter.
        /// </summary>
        private SyntaxToken LexXmlCharacter()
        {
            TokenInfo info = default(TokenInfo);

            //TODO: Dev11 allows C# comments and newlines in cref trivia (DevDiv #530523).
            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTriviaWithWhitespace(ref leading);

            this.Start();
            this.ScanXmlCharacter(ref info);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in info, leading, null, errors);
        }

        /// <summary>
        /// Scan a single XML character (or entity).  Assumes that leading trivia has already
        /// been consumed.
        /// </summary>
        private bool ScanXmlCharacter(ref TokenInfo info)
        {
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            switch (TextWindow.PeekChar())
            {
                case '&':
                    this.ScanXmlEntity(ref info);
                    info.Kind = SyntaxKind.XmlEntityLiteralToken;
                    break;
                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }
                    info.Kind = SyntaxKind.EndOfFileToken;
                    break;
                default:
                    info.Kind = SyntaxKind.XmlTextLiteralToken;
                    info.Text = info.StringValue = TextWindow.NextChar().ToString();
                    break;
            }

            return true;
        }

        /// <summary>
        /// Lexer entry point for LexerMode.XmlCrefQuote, LexerMode.XmlCrefDoubleQuote, 
        /// LexerMode.XmlNameQuote, and LexerMode.XmlNameDoubleQuote.
        /// </summary>
        private SyntaxToken LexXmlCrefOrNameToken()
        {
            TokenInfo info = default(TokenInfo);

            //TODO: Dev11 allows C# comments and newlines in cref trivia (DevDiv #530523).
            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTriviaWithWhitespace(ref leading);

            this.Start();
            this.ScanXmlCrefToken(ref info);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in info, leading, null, errors);
        }

        /// <summary>
        /// Scan a single cref attribute token.  Assumes that leading trivia has already
        /// been consumed.
        /// </summary>
        /// <remarks>
        /// Within this method, characters that are not XML meta-characters can be seamlessly
        /// replaced with the corresponding XML entities.
        /// </remarks>
        private bool ScanXmlCrefToken(ref TokenInfo info)
        {
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            int beforeConsumed = TextWindow.Position;
            char consumedChar = TextWindow.NextChar();
            char consumedSurrogate = SlidingTextWindow.InvalidCharacter;

            // This first switch is for special characters.  If we see the corresponding
            // XML entities, we DO NOT want to take these actions.
            switch (consumedChar)
            {
                case '"':
                    if (this.ModeIs(LexerMode.XmlCrefDoubleQuote) || this.ModeIs(LexerMode.XmlNameDoubleQuote))
                    {
                        info.Kind = SyntaxKind.DoubleQuoteToken;
                        return true;
                    }

                    break;

                case '\'':
                    if (this.ModeIs(LexerMode.XmlCrefQuote) || this.ModeIs(LexerMode.XmlNameQuote))
                    {
                        info.Kind = SyntaxKind.SingleQuoteToken;
                        return true;
                    }

                    break;

                case '<':
                    info.Text = TextWindow.GetText(intern: false);
                    this.AddError(XmlParseErrorCode.XML_LessThanInAttributeValue, info.Text); //ErrorCode.WRN_XMLParseError
                    return true;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    return true;

                case '\r':
                case '\n':
                    TextWindow.Reset(beforeConsumed);
                    ScanXmlTextLiteralNewLineToken(ref info);
                    break;

                case '&':
                    TextWindow.Reset(beforeConsumed);
                    if (!TryScanXmlEntity(out consumedChar, out consumedSurrogate))
                    {
                        TextWindow.Reset(beforeConsumed);
                        this.ScanXmlEntity(ref info);
                        info.Kind = SyntaxKind.XmlEntityLiteralToken;
                        return true;
                    }

                    // TryScanXmlEntity advances even when it returns false.
                    break;

                case '{':
                    consumedChar = '<';
                    break;

                case '}':
                    consumedChar = '>';
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(consumedChar))
                    {
                        goto case '\n';
                    }

                    break;
            }

            Debug.Assert(TextWindow.Position > beforeConsumed, "First character or entity has been consumed.");

            // NOTE: None of these cases will be matched if the surrogate is non-zero (UTF-16 rules)
            // so we don't need to check for that explicitly.

            // NOTE: there's a lot of overlap between this switch and the one in
            // ScanSyntaxToken, but we probably don't want to share code because
            // ScanSyntaxToken is really hot code and this switch does some extra
            // work.
            switch (consumedChar)
            {
                //// Single-Character Punctuation/Operators ////
                case '(':
                    info.Kind = SyntaxKind.OpenParenToken;
                    break;
                case ')':
                    info.Kind = SyntaxKind.CloseParenToken;
                    break;
                case '[':
                    info.Kind = SyntaxKind.OpenBracketToken;
                    break;
                case ']':
                    info.Kind = SyntaxKind.CloseBracketToken;
                    break;
                case ',':
                    info.Kind = SyntaxKind.CommaToken;
                    break;
                case '.':
                    if (AdvanceIfMatches('.'))
                    {
                        if (TextWindow.PeekChar() == '.')
                        {
                            // See documentation in ScanSyntaxToken
                            this.AddCrefError(ErrorCode.ERR_UnexpectedCharacter, ".");
                        }

                        info.Kind = SyntaxKind.DotDotToken;
                    }
                    else
                    {
                        info.Kind = SyntaxKind.DotToken;
                    }
                    break;
                case '?':
                    info.Kind = SyntaxKind.QuestionToken;
                    break;
                case '&':
                    info.Kind = SyntaxKind.AmpersandToken;
                    break;
                case '*':
                    info.Kind = SyntaxKind.AsteriskToken;
                    break;
                case '|':
                    info.Kind = SyntaxKind.BarToken;
                    break;
                case '^':
                    info.Kind = SyntaxKind.CaretToken;
                    break;
                case '%':
                    info.Kind = SyntaxKind.PercentToken;
                    break;
                case '/':
                    info.Kind = SyntaxKind.SlashToken;
                    break;
                case '~':
                    info.Kind = SyntaxKind.TildeToken;
                    break;

                // NOTE: Special case - convert curly brackets into angle brackets.
                case '{':
                    info.Kind = SyntaxKind.LessThanToken;
                    break;
                case '}':
                    info.Kind = SyntaxKind.GreaterThanToken;
                    break;

                //// Multi-Character Punctuation/Operators ////
                case ':':
                    if (AdvanceIfMatches(':')) info.Kind = SyntaxKind.ColonColonToken;
                    else info.Kind = SyntaxKind.ColonToken;
                    break;
                case '=':
                    if (AdvanceIfMatches('=')) info.Kind = SyntaxKind.EqualsEqualsToken;
                    else info.Kind = SyntaxKind.EqualsToken;
                    break;
                case '!':
                    if (AdvanceIfMatches('=')) info.Kind = SyntaxKind.ExclamationEqualsToken;
                    else info.Kind = SyntaxKind.ExclamationToken;
                    break;
                case '>':
                    if (AdvanceIfMatches('=')) info.Kind = SyntaxKind.GreaterThanEqualsToken;
                    // GreaterThanGreaterThanToken/GreaterThanGreaterThanGreaterThanToken is synthesized in the parser since it is ambiguous (with closing nested type parameter lists)
                    else info.Kind = SyntaxKind.GreaterThanToken;
                    break;
                case '<':
                    if (AdvanceIfMatches('=')) info.Kind = SyntaxKind.LessThanEqualsToken;
                    else if (AdvanceIfMatches('<')) info.Kind = SyntaxKind.LessThanLessThanToken;
                    else info.Kind = SyntaxKind.LessThanToken;
                    break;
                case '+':
                    if (AdvanceIfMatches('+')) info.Kind = SyntaxKind.PlusPlusToken;
                    else info.Kind = SyntaxKind.PlusToken;
                    break;
                case '-':
                    if (AdvanceIfMatches('-')) info.Kind = SyntaxKind.MinusMinusToken;
                    else info.Kind = SyntaxKind.MinusToken;
                    break;
            }

            if (info.Kind != SyntaxKind.None)
            {
                Debug.Assert(info.Text == null, "Haven't tried to set it yet.");
                Debug.Assert(info.StringValue == null, "Haven't tried to set it yet.");

                string valueText = SyntaxFacts.GetText(info.Kind);
                string actualText = TextWindow.GetText(intern: false);
                if (!string.IsNullOrEmpty(valueText) && actualText != valueText)
                {
                    info.RequiresTextForXmlEntity = true;
                    info.Text = actualText;
                    info.StringValue = valueText;
                }
            }
            else
            {
                // If we didn't match any of the above cases, then we either have an
                // identifier or an unexpected character.

                TextWindow.Reset(beforeConsumed);

                if (this.ScanIdentifier(ref info) && info.Text!.Length > 0)
                {
                    // ACASEY:  All valid identifier characters should be valid in XML attribute values,
                    // but I don't want to add an assert because XML character classification is expensive.
                    // check to see if it is an actual keyword
                    // NOTE: name attribute values don't respect keywords - everything is an identifier.
                    SyntaxKind keywordKind;
                    if (!InXmlNameAttributeValue && !info.IsVerbatim && !info.HasIdentifierEscapeSequence && _cache.TryGetKeywordKind(info.StringValue, out keywordKind))
                    {
                        if (SyntaxFacts.IsContextualKeyword(keywordKind))
                        {
                            info.Kind = SyntaxKind.IdentifierToken;
                            info.ContextualKind = keywordKind;
                            // Don't need to set any special flags to store the original text of an identifier.
                        }
                        else
                        {
                            info.Kind = keywordKind;
                            info.RequiresTextForXmlEntity = info.Text != info.StringValue;
                        }
                    }
                    else
                    {
                        info.ContextualKind = info.Kind = SyntaxKind.IdentifierToken;
                    }
                }
                else
                {
                    if (consumedChar == '@')
                    {
                        // Saw '@', but it wasn't followed by an identifier (otherwise ScanIdentifier would have succeeded).
                        if (TextWindow.PeekChar() == '@')
                        {
                            TextWindow.NextChar();
                            info.Text = TextWindow.GetText(intern: true);
                            info.StringValue = ""; // Can't be null for an identifier.
                        }
                        else
                        {
                            this.ScanXmlEntity(ref info);
                        }
                        info.Kind = SyntaxKind.IdentifierToken;
                        this.AddError(ErrorCode.ERR_ExpectedVerbatimLiteral);
                    }
                    else if (TextWindow.PeekChar() == '&')
                    {
                        this.ScanXmlEntity(ref info);
                        RoslynDebug.AssertNotNull(info.Text);
                        info.Kind = SyntaxKind.XmlEntityLiteralToken;
                        this.AddCrefError(ErrorCode.ERR_UnexpectedCharacter, info.Text);
                    }
                    else
                    {
                        char bad = TextWindow.NextChar();
                        info.Text = TextWindow.GetText(intern: false);

                        // If it's valid in XML, then it was unexpected in cref mode.
                        // Otherwise, it's just bad XML.
                        if (MatchesProductionForXmlChar((uint)bad))
                        {
                            this.AddCrefError(ErrorCode.ERR_UnexpectedCharacter, info.Text);
                        }
                        else
                        {
                            this.AddError(XmlParseErrorCode.XML_InvalidUnicodeChar);
                        }
                    }
                }
            }

            Debug.Assert(info.Kind != SyntaxKind.None || info.Text != null);
            return info.Kind != SyntaxKind.None;
        }

        /// <summary>
        /// Given a character, advance the input if either the character or the
        /// corresponding XML entity appears next in the text window.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool AdvanceIfMatches(char ch)
        {
            char peekCh = TextWindow.PeekChar();
            if ((peekCh == ch) ||
                (peekCh == '{' && ch == '<') ||
                (peekCh == '}' && ch == '>'))
            {
                TextWindow.AdvanceChar();
                return true;
            }

            if (peekCh == '&')
            {
                int pos = TextWindow.Position;

                char nextChar;
                char nextSurrogate;
                if (TryScanXmlEntity(out nextChar, out nextSurrogate)
                    && nextChar == ch && nextSurrogate == SlidingTextWindow.InvalidCharacter)
                {
                    return true;
                }

                TextWindow.Reset(pos);
            }

            return false;
        }

        /// <summary>
        /// Convenience property for determining whether we are currently lexing the
        /// value of a cref or name attribute.
        /// </summary>
        private bool InXmlCrefOrNameAttributeValue
        {
            get
            {
                switch (_mode & LexerMode.MaskLexMode)
                {
                    case LexerMode.XmlCrefQuote:
                    case LexerMode.XmlCrefDoubleQuote:
                    case LexerMode.XmlNameQuote:
                    case LexerMode.XmlNameDoubleQuote:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Convenience property for determining whether we are currently lexing the
        /// value of a name attribute.
        /// </summary>
        private bool InXmlNameAttributeValue
        {
            get
            {
                switch (_mode & LexerMode.MaskLexMode)
                {
                    case LexerMode.XmlNameQuote:
                    case LexerMode.XmlNameDoubleQuote:
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Diagnostics that occur within cref attributes need to be
        /// wrapped with ErrorCode.WRN_ErrorOverride.
        /// </summary>
        private void AddCrefError(ErrorCode code, params object[] args)
        {
            this.AddCrefError(MakeError(code, args));
        }

        /// <summary>
        /// Diagnostics that occur within cref attributes need to be
        /// wrapped with ErrorCode.WRN_ErrorOverride.
        /// </summary>
        private void AddCrefError(DiagnosticInfo? info)
        {
            if (info != null)
            {
                this.AddError(ErrorCode.WRN_ErrorOverride, info, info.Code);
            }
        }

        /// <summary>
        /// Lexer entry point for LexMode.XmlCDataSectionText
        /// </summary>
        private SyntaxToken LexXmlCDataSectionTextToken()
        {
            TokenInfo info = default(TokenInfo);

            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTrivia(ref leading);

            this.Start();
            this.ScanXmlCDataSectionTextToken(ref info);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in info, leading, null, errors);
        }

        private bool ScanXmlCDataSectionTextToken(ref TokenInfo info)
        {
            char ch;

            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            switch (ch = TextWindow.PeekChar())
            {
                case ']':
                    if (TextWindow.PeekChar(1) == ']' && TextWindow.PeekChar(2) == '>')
                    {
                        TextWindow.AdvanceChar(3);
                        info.Kind = SyntaxKind.XmlCDataEndToken;
                        break;
                    }

                    goto default;

                case '\r':
                case '\n':
                    ScanXmlTextLiteralNewLineToken(ref info);
                    break;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        goto case '\n';
                    }

                    this.ScanXmlCDataSectionText(ref info);
                    info.Kind = SyntaxKind.XmlTextLiteralToken;
                    break;
            }

            return true;
        }

        private void ScanXmlCDataSectionText(ref TokenInfo info)
        {
            while (true)
            {
                var ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case ']':
                        if (TextWindow.PeekChar(1) == ']' && TextWindow.PeekChar(2) == '>')
                        {
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    case '\r':
                    case '\n':
                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case '*':
                        if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                        {
                            // we're at the end of the comment, but don't lex it yet.
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    default:
                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        TextWindow.AdvanceChar();
                        break;
                }
            }
        }

        /// <summary>
        /// Lexer entry point for LexMode.XmlCommentText
        /// </summary>
        private SyntaxToken LexXmlCommentTextToken()
        {
            TokenInfo info = default(TokenInfo);

            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTrivia(ref leading);

            this.Start();
            this.ScanXmlCommentTextToken(ref info);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in info, leading, null, errors);
        }

        private bool ScanXmlCommentTextToken(ref TokenInfo info)
        {
            char ch;

            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            switch (ch = TextWindow.PeekChar())
            {
                case '-':
                    if (TextWindow.PeekChar(1) == '-')
                    {
                        if (TextWindow.PeekChar(2) == '>')
                        {
                            TextWindow.AdvanceChar(3);
                            info.Kind = SyntaxKind.XmlCommentEndToken;
                            break;
                        }
                        else
                        {
                            TextWindow.AdvanceChar(2);
                            info.Kind = SyntaxKind.MinusMinusToken;
                            break;
                        }
                    }

                    goto default;

                case '\r':
                case '\n':
                    ScanXmlTextLiteralNewLineToken(ref info);
                    break;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }
                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        goto case '\n';
                    }

                    this.ScanXmlCommentText(ref info);
                    info.Kind = SyntaxKind.XmlTextLiteralToken;
                    break;
            }

            return true;
        }

        private void ScanXmlCommentText(ref TokenInfo info)
        {
            while (true)
            {
                var ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case '-':
                        if (TextWindow.PeekChar(1) == '-')
                        {
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    case '\r':
                    case '\n':
                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case '*':
                        if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                        {
                            // we're at the end of the comment, but don't lex it yet.
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    default:
                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        TextWindow.AdvanceChar();
                        break;
                }
            }
        }

        /// <summary>
        /// Lexer entry point for LexMode.XmlProcessingInstructionText
        /// </summary>
        private SyntaxToken LexXmlProcessingInstructionTextToken()
        {
            TokenInfo info = default(TokenInfo);

            SyntaxListBuilder? leading = null;
            this.LexXmlDocCommentLeadingTrivia(ref leading);

            this.Start();
            this.ScanXmlProcessingInstructionTextToken(ref info);
            var errors = this.GetErrors(GetFullWidth(leading));

            return Create(in info, leading, null, errors);
        }

        // CONSIDER: This could easily be merged with ScanXmlCDataSectionTextToken
        private bool ScanXmlProcessingInstructionTextToken(ref TokenInfo info)
        {
            char ch;

            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Start));
            Debug.Assert(!this.LocationIs(XmlDocCommentLocation.Exterior));

            if (this.LocationIs(XmlDocCommentLocation.End))
            {
                info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                return true;
            }

            switch (ch = TextWindow.PeekChar())
            {
                case '?':
                    if (TextWindow.PeekChar(1) == '>')
                    {
                        TextWindow.AdvanceChar(2);
                        info.Kind = SyntaxKind.XmlProcessingInstructionEndToken;
                        break;
                    }

                    goto default;

                case '\r':
                case '\n':
                    ScanXmlTextLiteralNewLineToken(ref info);
                    break;

                case SlidingTextWindow.InvalidCharacter:
                    if (!TextWindow.IsReallyAtEnd())
                    {
                        goto default;
                    }

                    info.Kind = SyntaxKind.EndOfDocumentationCommentToken;
                    break;

                default:
                    if (SyntaxFacts.IsNewLine(ch))
                    {
                        goto case '\n';
                    }

                    this.ScanXmlProcessingInstructionText(ref info);
                    info.Kind = SyntaxKind.XmlTextLiteralToken;
                    break;
            }

            return true;
        }

        // CONSIDER: This could easily be merged with ScanXmlCDataSectionText
        private void ScanXmlProcessingInstructionText(ref TokenInfo info)
        {
            while (true)
            {
                var ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case '?':
                        if (TextWindow.PeekChar(1) == '>')
                        {
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    case '\r':
                    case '\n':
                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case SlidingTextWindow.InvalidCharacter:
                        if (!TextWindow.IsReallyAtEnd())
                        {
                            goto default;
                        }

                        info.StringValue = info.Text = TextWindow.GetText(false);
                        return;

                    case '*':
                        if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                        {
                            // we're at the end of the comment, but don't lex it yet.
                            info.StringValue = info.Text = TextWindow.GetText(false);
                            return;
                        }

                        goto default;

                    default:
                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        TextWindow.AdvanceChar();
                        break;
                }
            }
        }

        /// <summary>
        /// Collects XML doc comment exterior trivia, and therefore is a no op unless we are in the Start or Exterior of an XML doc comment.
        /// </summary>
        /// <param name="trivia">List in which to collect the trivia</param>
        private void LexXmlDocCommentLeadingTrivia(ref SyntaxListBuilder? trivia)
        {
            var start = TextWindow.Position;
            this.Start();

            if (this.LocationIs(XmlDocCommentLocation.Start) && this.StyleIs(XmlDocCommentStyle.Delimited))
            {
                // Read the /** that begins an XML doc comment. Since these are recognized only
                // when the trailing character is not a '*', we wind up in the interior of the
                // doc comment at the end.

                if (TextWindow.PeekChar() == '/'
                    && TextWindow.PeekChar(1) == '*'
                    && TextWindow.PeekChar(2) == '*'
                    && TextWindow.PeekChar(3) != '*')
                {
                    TextWindow.AdvanceChar(3);
                    var text = TextWindow.GetText(true);
                    this.AddTrivia(SyntaxFactory.DocumentationCommentExteriorTrivia(text), ref trivia);
                    this.MutateLocation(XmlDocCommentLocation.Interior);
                    return;
                }
            }
            else if (this.LocationIs(XmlDocCommentLocation.Start) || this.LocationIs(XmlDocCommentLocation.Exterior))
            {
                // We're in the exterior of an XML doc comment and need to eat the beginnings of
                // lines, for single line and delimited comments. We chew up white space until
                // a non-whitespace character, and then make the right decision depending on
                // what kind of comment we're in.

                while (true)
                {
                    char ch = TextWindow.PeekChar();
                    switch (ch)
                    {
                        case ' ':
                        case '\t':
                        case '\v':
                        case '\f':
                            TextWindow.AdvanceChar();
                            break;

                        case '/':
                            if (this.StyleIs(XmlDocCommentStyle.SingleLine) && TextWindow.PeekChar(1) == '/' && TextWindow.PeekChar(2) == '/' && TextWindow.PeekChar(3) != '/')
                            {
                                TextWindow.AdvanceChar(3);
                                var text = TextWindow.GetText(true);
                                this.AddTrivia(SyntaxFactory.DocumentationCommentExteriorTrivia(text), ref trivia);
                                this.MutateLocation(XmlDocCommentLocation.Interior);
                                return;
                            }

                            goto default;

                        case '*':
                            if (this.StyleIs(XmlDocCommentStyle.Delimited))
                            {
                                while (TextWindow.PeekChar() == '*' && TextWindow.PeekChar(1) != '/')
                                {
                                    TextWindow.AdvanceChar();
                                }

                                var text = TextWindow.GetText(true);
                                if (!String.IsNullOrEmpty(text))
                                {
                                    this.AddTrivia(SyntaxFactory.DocumentationCommentExteriorTrivia(text), ref trivia);
                                }

                                // This setup ensures that on the final line of a comment, if we have
                                // the string "  */", the "*/" part is separated from the whitespace
                                // and therefore recognizable as the end of the comment.

                                if (TextWindow.PeekChar() == '*' && TextWindow.PeekChar(1) == '/')
                                {
                                    TextWindow.AdvanceChar(2);
                                    this.AddTrivia(SyntaxFactory.DocumentationCommentExteriorTrivia("*/"), ref trivia);
                                    this.MutateLocation(XmlDocCommentLocation.End);
                                }
                                else
                                {
                                    this.MutateLocation(XmlDocCommentLocation.Interior);
                                }

                                return;
                            }

                            goto default;

                        default:
                            if (SyntaxFacts.IsWhitespace(ch))
                            {
                                goto case ' ';
                            }

                            // so here we have something else. if this is a single-line xml
                            // doc comment, that means we're on a line that's no longer a doc
                            // comment, so we need to rewind. if we're in a delimited doc comment,
                            // then that means we hit pay dirt and we're back into xml text.

                            if (this.StyleIs(XmlDocCommentStyle.SingleLine))
                            {
                                TextWindow.Reset(start);
                                this.MutateLocation(XmlDocCommentLocation.End);
                            }
                            else // XmlDocCommentStyle.Delimited
                            {
                                Debug.Assert(this.StyleIs(XmlDocCommentStyle.Delimited));

                                var text = TextWindow.GetText(true);
                                if (!String.IsNullOrEmpty(text))
                                    this.AddTrivia(SyntaxFactory.DocumentationCommentExteriorTrivia(text), ref trivia);
                                this.MutateLocation(XmlDocCommentLocation.Interior);
                            }

                            return;
                    }
                }
            }
            else if (!this.LocationIs(XmlDocCommentLocation.End) && this.StyleIs(XmlDocCommentStyle.Delimited))
            {
                if (TextWindow.PeekChar() == '*' && TextWindow.PeekChar(1) == '/')
                {
                    TextWindow.AdvanceChar(2);
                    var text = TextWindow.GetText(true);
                    this.AddTrivia(SyntaxFactory.DocumentationCommentExteriorTrivia(text), ref trivia);
                    this.MutateLocation(XmlDocCommentLocation.End);
                }
            }
        }

        private void LexXmlDocCommentLeadingTriviaWithWhitespace(ref SyntaxListBuilder? trivia)
        {
            while (true)
            {
                this.LexXmlDocCommentLeadingTrivia(ref trivia);

                char ch = TextWindow.PeekChar();
                if (this.LocationIs(XmlDocCommentLocation.Interior)
                    && (SyntaxFacts.IsWhitespace(ch) || SyntaxFacts.IsNewLine(ch)))
                {
                    this.LexXmlWhitespaceAndNewLineTrivia(ref trivia);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Collects whitespace and new line trivia for XML doc comments. Does not see XML doc comment exterior trivia, and is a no op unless we are in the interior.
        /// </summary>
        /// <param name="trivia">List in which to collect the trivia</param>
        private void LexXmlWhitespaceAndNewLineTrivia(ref SyntaxListBuilder? trivia)
        {
            this.Start();
            if (this.LocationIs(XmlDocCommentLocation.Interior))
            {
                char ch = TextWindow.PeekChar();
                switch (ch)
                {
                    case ' ':
                    case '\t':       // Horizontal tab
                    case '\v':       // Vertical Tab
                    case '\f':       // Form-feed
                        this.AddTrivia(this.ScanWhitespace(), ref trivia);
                        break;

                    case '\r':
                    case '\n':
                        var endOfLine = this.ScanEndOfLine();
                        RoslynDebug.AssertNotNull(endOfLine);
                        this.AddTrivia(endOfLine, ref trivia);
                        this.MutateLocation(XmlDocCommentLocation.Exterior);
                        return;

                    case '*':
                        if (this.StyleIs(XmlDocCommentStyle.Delimited) && TextWindow.PeekChar(1) == '/')
                        {
                            // we're at the end of the comment, but don't add as trivia here.
                            return;
                        }

                        goto default;

                    default:
                        if (SyntaxFacts.IsWhitespace(ch))
                        {
                            goto case ' ';
                        }

                        if (SyntaxFacts.IsNewLine(ch))
                        {
                            goto case '\n';
                        }

                        return;
                }
            }
        }

        private bool IsUnicodeEscape()
        {
            if (TextWindow.PeekChar() == '\\')
            {
                var ch2 = TextWindow.PeekChar(1);
                if (ch2 == 'U' || ch2 == 'u')
                {
                    return true;
                }
            }

            return false;
        }

        private char PeekCharOrUnicodeEscape(out char surrogateCharacter)
        {
            if (IsUnicodeEscape())
            {
                return PeekUnicodeEscape(out surrogateCharacter);
            }
            else
            {
                surrogateCharacter = SlidingTextWindow.InvalidCharacter;
                return TextWindow.PeekChar();
            }
        }

        private char PeekUnicodeEscape(out char surrogateCharacter)
        {
            int position = TextWindow.Position;

            // if we're peeking, then we don't want to change the position
            SyntaxDiagnosticInfo? info;
            var ch = ScanUnicodeEscape(peek: true, surrogateCharacter: out surrogateCharacter, info: out info);
            Debug.Assert(info == null, "Never produce a diagnostic while peeking.");
            TextWindow.Reset(position);
            return ch;
        }

        private char NextCharOrUnicodeEscape(out char surrogateCharacter, out SyntaxDiagnosticInfo? info)
        {
            var ch = TextWindow.PeekChar();
            Debug.Assert(ch != SlidingTextWindow.InvalidCharacter, "Precondition established by all callers; required for correctness of AdvanceChar() call.");
            if (ch == '\\')
            {
                var ch2 = TextWindow.PeekChar(1);
                if (ch2 == 'U' || ch2 == 'u')
                {
                    return ScanUnicodeEscape(peek: false, surrogateCharacter: out surrogateCharacter, info: out info);
                }
            }

            surrogateCharacter = SlidingTextWindow.InvalidCharacter;
            info = null;
            TextWindow.AdvanceChar();
            return ch;
        }

        private char NextUnicodeEscape(out char surrogateCharacter, out SyntaxDiagnosticInfo? info)
        {
            return ScanUnicodeEscape(peek: false, surrogateCharacter: out surrogateCharacter, info: out info);
        }

        private char ScanUnicodeEscape(bool peek, out char surrogateCharacter, out SyntaxDiagnosticInfo? info)
        {
            surrogateCharacter = SlidingTextWindow.InvalidCharacter;
            info = null;

            int start = TextWindow.Position;
            char character = TextWindow.PeekChar();
            Debug.Assert(character == '\\');
            TextWindow.AdvanceChar();

            character = TextWindow.PeekChar();
            if (character == 'U')
            {
                uint uintChar = 0;

                TextWindow.AdvanceChar();
                if (!SyntaxFacts.IsHexDigit(TextWindow.PeekChar()))
                {
                    if (!peek)
                    {
                        info = CreateIllegalEscapeDiagnostic(start);
                    }
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        character = TextWindow.PeekChar();
                        if (!SyntaxFacts.IsHexDigit(character))
                        {
                            if (!peek)
                            {
                                info = CreateIllegalEscapeDiagnostic(start);
                            }

                            break;
                        }

                        uintChar = (uint)((uintChar << 4) + SyntaxFacts.HexValue(character));
                        TextWindow.AdvanceChar();
                    }

                    if (uintChar > 0x0010FFFF)
                    {
                        if (!peek)
                        {
                            info = CreateIllegalEscapeDiagnostic(start);
                        }
                    }
                    else
                    {
                        character = GetCharsFromUtf32(uintChar, out surrogateCharacter);
                    }
                }
            }
            else
            {
                Debug.Assert(character == 'u' || character == 'x');

                int intChar = 0;
                TextWindow.AdvanceChar();
                if (!SyntaxFacts.IsHexDigit(TextWindow.PeekChar()))
                {
                    if (!peek)
                    {
                        info = CreateIllegalEscapeDiagnostic(start);
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        char ch2 = TextWindow.PeekChar();
                        if (!SyntaxFacts.IsHexDigit(ch2))
                        {
                            if (character == 'u')
                            {
                                if (!peek)
                                {
                                    info = CreateIllegalEscapeDiagnostic(start);
                                }
                            }

                            break;
                        }

                        intChar = (intChar << 4) + SyntaxFacts.HexValue(ch2);
                        TextWindow.AdvanceChar();
                    }

                    character = (char)intChar;
                }
            }

            return character;
        }

        /// <summary>
        /// Given that the next character is an ampersand ('&amp;'), attempt to interpret the
        /// following characters as an XML entity.  On success, populate the out parameters
        /// with the low and high UTF-16 surrogates for the character represented by the
        /// entity.
        /// </summary>
        /// <param name="ch">e.g. '&lt;' for &amp;lt;.</param>
        /// <param name="surrogate">e.g. '\uDC00' for &amp;#x10000; (ch == '\uD800').</param>
        /// <returns>True if a valid XML entity was consumed.</returns>
        /// <remarks>
        /// NOTE: Always advances, even on failure.
        /// </remarks>
        public bool TryScanXmlEntity(out char ch, out char surrogate)
        {
            Debug.Assert(TextWindow.PeekChar() == '&');

            ch = '&';
            TextWindow.AdvanceChar();

            surrogate = SlidingTextWindow.InvalidCharacter;

            switch (TextWindow.PeekChar())
            {
                case 'l':
                    if (TextWindow.AdvanceIfMatches("lt;"))
                    {
                        ch = '<';
                        return true;
                    }
                    break;
                case 'g':
                    if (TextWindow.AdvanceIfMatches("gt;"))
                    {
                        ch = '>';
                        return true;
                    }
                    break;
                case 'a':
                    if (TextWindow.AdvanceIfMatches("amp;"))
                    {
                        ch = '&';
                        return true;
                    }
                    else if (TextWindow.AdvanceIfMatches("apos;"))
                    {
                        ch = '\'';
                        return true;
                    }
                    break;
                case 'q':
                    if (TextWindow.AdvanceIfMatches("quot;"))
                    {
                        ch = '"';
                        return true;
                    }
                    break;
                case '#':
                    {
                        TextWindow.AdvanceChar(); //#

                        uint uintChar = 0;

                        if (TextWindow.AdvanceIfMatches("x"))
                        {
                            char digit;
                            while (SyntaxFacts.IsHexDigit(digit = TextWindow.PeekChar()))
                            {
                                TextWindow.AdvanceChar();

                                // disallow overflow
                                if (uintChar <= 0x7FFFFFF)
                                {
                                    uintChar = (uintChar << 4) + (uint)SyntaxFacts.HexValue(digit);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            char digit;
                            while (SyntaxFacts.IsDecDigit(digit = TextWindow.PeekChar()))
                            {
                                TextWindow.AdvanceChar();

                                // disallow overflow
                                if (uintChar <= 0x7FFFFFF)
                                {
                                    uintChar = (uintChar << 3) + (uintChar << 1) + (uint)SyntaxFacts.DecValue(digit);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }

                        if (TextWindow.AdvanceIfMatches(";"))
                        {
                            ch = GetCharsFromUtf32(uintChar, out surrogate);
                            return true;
                        }

                        break;
                    }
            }

            return false;
        }

        private SyntaxDiagnosticInfo CreateIllegalEscapeDiagnostic(int start)
        {
            return new SyntaxDiagnosticInfo(start - TextWindow.LexemeStartPosition,
                TextWindow.Position - start,
                ErrorCode.ERR_IllegalEscape);
        }

        internal static char GetCharsFromUtf32(uint codepoint, out char lowSurrogate)
        {
            if (codepoint < (uint)0x00010000)
            {
                lowSurrogate = SlidingTextWindow.InvalidCharacter;
                return (char)codepoint;
            }
            else
            {
                Debug.Assert(codepoint > 0x0000FFFF && codepoint <= 0x0010FFFF);
                lowSurrogate = (char)((codepoint - 0x00010000) % 0x0400 + 0xDC00);
                return (char)((codepoint - 0x00010000) / 0x0400 + 0xD800);
            }
        }
    }
}
