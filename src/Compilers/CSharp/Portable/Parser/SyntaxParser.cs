// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal abstract partial class SyntaxParser : IDisposable
    {
        protected readonly Lexer lexer;
        private readonly bool _isIncremental;
        private readonly bool _allowModeReset;
        protected readonly CancellationToken cancellationToken;

        private LexerMode _mode;
        private Blender _firstBlender;
        private BlendedNode _currentNode;
        private SyntaxToken _currentToken;
        private ArrayElement<SyntaxToken>[] _lexedTokens;
        private GreenNode _prevTokenTrailingTrivia;
        private int _firstToken; // The position of _lexedTokens[0] (or _blendedTokens[0]).
        private int _tokenOffset; // The index of the current token within _lexedTokens or _blendedTokens.
        private int _tokenCount;
        private int _resetCount;
        private int _resetStart;

        private static readonly ObjectPool<BlendedNode[]> s_blendedNodesPool = new ObjectPool<BlendedNode[]>(() => new BlendedNode[32]);
        private static readonly ObjectPool<ArrayElement<SyntaxToken>[]> s_lexedTokensPool = new ObjectPool<ArrayElement<SyntaxToken>[]>(() => new ArrayElement<SyntaxToken>[CachedTokenArraySize]);

        // Array size held in token pool. This should be large enough to prevent most allocations, but
        //  not so large as to be wasteful when not in use.
        private const int CachedTokenArraySize = 4096;

        // Maximum index where a value has been written in _lexedTokens. This will allow Dispose
        //   to limit the range needed to clear when releasing the lexed token array back to the pool.
        private int _maxWrittenLexedTokenIndex = -1;

        private BlendedNode[] _blendedTokens;

        protected SyntaxParser(
            Lexer lexer,
            LexerMode mode,
            CSharp.CSharpSyntaxNode oldTree,
            IEnumerable<TextChangeRange> changes,
            bool allowModeReset,
            bool preLexIfNotIncremental = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            this.lexer = lexer;
            _mode = mode;
            _allowModeReset = allowModeReset;
            this.cancellationToken = cancellationToken;
            _currentNode = default(BlendedNode);
            _isIncremental = oldTree != null;

            if (this.IsIncremental || allowModeReset)
            {
                _firstBlender = new Blender(lexer, oldTree, changes);
                _blendedTokens = s_blendedNodesPool.Allocate();
            }
            else
            {
                _firstBlender = default(Blender);
                _lexedTokens = s_lexedTokensPool.Allocate();
            }

            // PreLex is not cancellable. 
            //      If we may cancel why would we aggressively lex ahead?
            //      Cancellations in a constructor make disposing complicated
            //
            // So, if we have a real cancellation token, do not do prelexing.
            if (preLexIfNotIncremental && !this.IsIncremental && !cancellationToken.CanBeCanceled)
            {
                this.PreLex();
            }
        }

        public void Dispose()
        {
            var blendedTokens = _blendedTokens;
            if (blendedTokens != null)
            {
                _blendedTokens = null;
                if (blendedTokens.Length < 4096)
                {
                    Array.Clear(blendedTokens, 0, blendedTokens.Length);
                    s_blendedNodesPool.Free(blendedTokens);
                }
                else
                {
                    s_blendedNodesPool.ForgetTrackedObject(blendedTokens);
                }
            }

            var lexedTokens = _lexedTokens;
            if (lexedTokens != null)
            {
                _lexedTokens = null;

                ReturnLexedTokensToPool(lexedTokens);
            }
        }

        protected void ReInitialize()
        {
            _firstToken = 0;
            _tokenOffset = 0;
            _tokenCount = 0;
            _resetCount = 0;
            _resetStart = 0;
            _currentToken = null;
            _prevTokenTrailingTrivia = null;
            if (this.IsIncremental || _allowModeReset)
            {
                _firstBlender = new Blender(this.lexer, oldTree: null, changes: null);
            }
        }

        protected bool IsIncremental
        {
            get
            {
                return _isIncremental;
            }
        }

        private void PreLex()
        {
            // NOTE: Do not cancel in this method. It is called from the constructor.
            var size = Math.Min(CachedTokenArraySize, this.lexer.TextWindow.Text.Length / 2);
            var lexer = this.lexer;
            var mode = _mode;

            _lexedTokens ??= s_lexedTokensPool.Allocate();

            for (int i = 0; i < size; i++)
            {
                var token = lexer.Lex(mode);
                this.AddLexedToken(token);
                if (token.Kind == SyntaxKind.EndOfFileToken)
                {
                    break;
                }
            }
        }

        protected ResetPoint GetResetPoint()
        {
            var pos = CurrentTokenPosition;
            if (_resetCount == 0)
            {
                _resetStart = pos; // low water mark
            }

            _resetCount++;
            return new ResetPoint(_resetCount, _mode, pos, _prevTokenTrailingTrivia);
        }

        protected void Reset(ref ResetPoint point)
        {
            var offset = point.Position - _firstToken;
            Debug.Assert(offset >= 0);

            if (offset >= _tokenCount)
            {
                // Re-fetch tokens to the position in the reset point
                PeekToken(offset - _tokenOffset);

                // Re-calculate new offset in case tokens got shifted to the left while we were peeking. 
                offset = point.Position - _firstToken;
            }

            _mode = point.Mode;
            Debug.Assert(offset >= 0 && offset < _tokenCount);
            _tokenOffset = offset;
            _currentToken = null;
            _currentNode = default(BlendedNode);
            _prevTokenTrailingTrivia = point.PrevTokenTrailingTrivia;
            if (_blendedTokens != null)
            {
                // look forward for slots not holding a token
                for (int i = _tokenOffset; i < _tokenCount; i++)
                {
                    if (_blendedTokens[i].Token == null)
                    {
                        // forget anything after and including any slot not holding a token
                        _tokenCount = i;
                        if (_tokenCount == _tokenOffset)
                        {
                            FetchCurrentToken();
                        }
                        break;
                    }
                }
            }
        }

        protected void Release(ref ResetPoint point)
        {
            Debug.Assert(_resetCount == point.ResetCount);
            _resetCount--;
            if (_resetCount == 0)
            {
                _resetStart = -1;
            }
        }

        public CSharpParseOptions Options
        {
            get { return this.lexer.Options; }
        }

        public bool IsScript
        {
            get { return Options.Kind == SourceCodeKind.Script; }
        }

        protected LexerMode Mode
        {
            get
            {
                return _mode;
            }

            set
            {
                if (_mode != value)
                {
                    Debug.Assert(_allowModeReset);

                    _mode = value;
                    _currentToken = null;
                    _currentNode = default(BlendedNode);
                    _tokenCount = _tokenOffset;
                }
            }
        }

        protected CSharp.CSharpSyntaxNode CurrentNode
        {
            get
            {
                // we will fail anyways. Assert is just to catch that earlier.
                Debug.Assert(_blendedTokens != null);

                //PERF: currentNode is a BlendedNode, which is a fairly large struct.
                // the following code tries not to pull the whole struct into a local
                // we only need .Node
                var node = _currentNode.Node;
                if (node != null)
                {
                    return node;
                }

                this.ReadCurrentNode();
                return _currentNode.Node;
            }
        }

        protected SyntaxKind CurrentNodeKind
        {
            get
            {
                var cn = this.CurrentNode;
                return cn != null ? cn.Kind() : SyntaxKind.None;
            }
        }

        private void ReadCurrentNode()
        {
            if (_tokenOffset == 0)
            {
                _currentNode = _firstBlender.ReadNode(_mode);
            }
            else
            {
                _currentNode = _blendedTokens[_tokenOffset - 1].Blender.ReadNode(_mode);
            }
        }

        protected GreenNode EatNode()
        {
            // we will fail anyways. Assert is just to catch that earlier.
            Debug.Assert(_blendedTokens != null);

            // remember result
            var result = CurrentNode.Green;

            // store possible non-token in token sequence 
            if (_tokenOffset >= _blendedTokens.Length)
            {
                this.AddTokenSlot();
            }

            _blendedTokens[_tokenOffset++] = _currentNode;
            _tokenCount = _tokenOffset; // forget anything after this slot

            // erase current state
            _currentNode = default(BlendedNode);
            _currentToken = null;

            return result;
        }

        protected SyntaxToken CurrentToken
        {
            get
            {
                return _currentToken ??= this.FetchCurrentToken();
            }
        }

        private SyntaxToken FetchCurrentToken()
        {
            if (_tokenOffset >= _tokenCount)
            {
                this.AddNewToken();
            }

            if (_blendedTokens != null)
            {
                return _blendedTokens[_tokenOffset].Token;
            }
            else
            {
                return _lexedTokens[_tokenOffset];
            }
        }

        private void AddNewToken()
        {
            if (_blendedTokens != null)
            {
                if (_tokenCount > 0)
                {
                    this.AddToken(_blendedTokens[_tokenCount - 1].Blender.ReadToken(_mode));
                }
                else
                {
                    if (_currentNode.Token != null)
                    {
                        this.AddToken(_currentNode);
                    }
                    else
                    {
                        this.AddToken(_firstBlender.ReadToken(_mode));
                    }
                }
            }
            else
            {
                this.AddLexedToken(this.lexer.Lex(_mode));
            }
        }

        // adds token to end of current token array
        private void AddToken(in BlendedNode tokenResult)
        {
            Debug.Assert(tokenResult.Token != null);
            if (_tokenCount >= _blendedTokens.Length)
            {
                this.AddTokenSlot();
            }

            _blendedTokens[_tokenCount] = tokenResult;
            _tokenCount++;
        }

        private void AddLexedToken(SyntaxToken token)
        {
            Debug.Assert(token != null);
            if (_tokenCount >= _lexedTokens.Length)
            {
                this.AddLexedTokenSlot();
            }

            if (_tokenCount > _maxWrittenLexedTokenIndex)
            {
                _maxWrittenLexedTokenIndex = _tokenCount;
            }

            _lexedTokens[_tokenCount].Value = token;
            _tokenCount++;
        }

        private void AddTokenSlot()
        {
            // shift tokens to left if we are far to the right
            // don't shift if reset points have fixed locked the starting point at the token in the window
            if (_tokenOffset > (_blendedTokens.Length >> 1)
                && (_resetStart == -1 || _resetStart > _firstToken))
            {
                int shiftOffset = (_resetStart == -1) ? _tokenOffset : _resetStart - _firstToken;
                int shiftCount = _tokenCount - shiftOffset;
                Debug.Assert(shiftOffset > 0);
                _firstBlender = _blendedTokens[shiftOffset - 1].Blender;
                if (shiftCount > 0)
                {
                    Array.Copy(_blendedTokens, shiftOffset, _blendedTokens, 0, shiftCount);
                }

                _firstToken += shiftOffset;
                _tokenCount -= shiftOffset;
                _tokenOffset -= shiftOffset;
            }
            else
            {
                var old = _blendedTokens;
                Array.Resize(ref _blendedTokens, _blendedTokens.Length * 2);
                s_blendedNodesPool.ForgetTrackedObject(old, replacement: _blendedTokens);
            }
        }

        private void AddLexedTokenSlot()
        {
            // shift tokens to left if we are far to the right
            // don't shift if reset points have fixed locked the starting point at the token in the window
            if (_tokenOffset > (_lexedTokens.Length >> 1)
                && (_resetStart == -1 || _resetStart > _firstToken))
            {
                int shiftOffset = (_resetStart == -1) ? _tokenOffset : _resetStart - _firstToken;
                int shiftCount = _tokenCount - shiftOffset;
                Debug.Assert(shiftOffset > 0);
                if (shiftCount > 0)
                {
                    Array.Copy(_lexedTokens, shiftOffset, _lexedTokens, 0, shiftCount);
                }

                _firstToken += shiftOffset;
                _tokenCount -= shiftOffset;
                _tokenOffset -= shiftOffset;
            }
            else
            {
                var lexedTokens = _lexedTokens;

                Array.Resize(ref _lexedTokens, _lexedTokens.Length * 2);

                ReturnLexedTokensToPool(lexedTokens);
            }
        }

        private void ReturnLexedTokensToPool(ArrayElement<SyntaxToken>[] lexedTokens)
        {
            // Put lexedTokens back into the pool if it's correctly sized.
            if (lexedTokens.Length == CachedTokenArraySize)
            {
                // Clear all written indexes in lexedTokens before releasing back to the pool
                Array.Clear(lexedTokens, 0, _maxWrittenLexedTokenIndex + 1);

                s_lexedTokensPool.Free(lexedTokens);
            }
        }

        protected SyntaxToken PeekToken(int n)
        {
            Debug.Assert(n >= 0);
            while (_tokenOffset + n >= _tokenCount)
            {
                this.AddNewToken();
            }

            if (_blendedTokens != null)
            {
                return _blendedTokens[_tokenOffset + n].Token;
            }
            else
            {
                return _lexedTokens[_tokenOffset + n];
            }
        }

        //this method is called very frequently
        //we should keep it simple so that it can be inlined.
        protected SyntaxToken EatToken()
        {
            var ct = this.CurrentToken;
            MoveToNextToken();
            return ct;
        }

        /// <summary>
        /// Returns and consumes the current token if it has the requested <paramref name="kind"/>.
        /// Otherwise, returns <see langword="null"/>.
        /// </summary>
        protected SyntaxToken TryEatToken(SyntaxKind kind)
            => this.CurrentToken.Kind == kind ? this.EatToken() : null;

        private void MoveToNextToken()
        {
            _prevTokenTrailingTrivia = _currentToken.GetTrailingTrivia();

            _currentToken = null;

            if (_blendedTokens != null)
            {
                _currentNode = default(BlendedNode);
            }

            _tokenOffset++;
        }

        protected void ForceEndOfFile()
        {
            _currentToken = SyntaxFactory.Token(SyntaxKind.EndOfFileToken);
        }

        //this method is called very frequently
        //we should keep it simple so that it can be inlined.
        protected SyntaxToken EatToken(SyntaxKind kind)
        {
            Debug.Assert(SyntaxFacts.IsAnyToken(kind));

            var ct = this.CurrentToken;
            if (ct.Kind == kind)
            {
                MoveToNextToken();
                return ct;
            }

            //slow part of EatToken(SyntaxKind kind)
            return CreateMissingToken(kind, this.CurrentToken.Kind);
        }

        // Consume a token if it is the right kind. Otherwise skip a token and replace it with one of the correct kind.
        protected SyntaxToken EatTokenAsKind(SyntaxKind expected)
        {
            Debug.Assert(SyntaxFacts.IsAnyToken(expected));

            var ct = this.CurrentToken;
            if (ct.Kind == expected)
            {
                MoveToNextToken();
                return ct;
            }

            var replacement = CreateMissingToken(expected, this.CurrentToken.Kind);
            return AddTrailingSkippedSyntax(replacement, this.EatToken());
        }

        protected SyntaxToken CreateMissingToken(SyntaxKind expected, SyntaxKind actual)
        {
            var token = SyntaxFactory.MissingToken(expected);
            return WithAdditionalDiagnostics(token, this.GetExpectedMissingNodeOrTokenError(token, expected, actual));
        }

        private SyntaxToken CreateMissingToken(SyntaxKind expected, ErrorCode code, bool reportError)
        {
            // should we eat the current ParseToken's leading trivia?
            var token = SyntaxFactory.MissingToken(expected);
            if (reportError)
            {
                token = AddError(token, code);
            }

            return token;
        }

        protected SyntaxToken EatToken(SyntaxKind kind, bool reportError)
        {
            if (reportError)
            {
                return EatToken(kind);
            }

            Debug.Assert(SyntaxFacts.IsAnyToken(kind));
            if (this.CurrentToken.Kind != kind)
            {
                // should we eat the current ParseToken's leading trivia?
                return SyntaxFactory.MissingToken(kind);
            }
            else
            {
                return this.EatToken();
            }
        }

        protected SyntaxToken EatToken(SyntaxKind kind, ErrorCode code, bool reportError = true)
        {
            Debug.Assert(SyntaxFacts.IsAnyToken(kind));
            if (this.CurrentToken.Kind != kind)
            {
                return CreateMissingToken(kind, code, reportError);
            }
            else
            {
                return this.EatToken();
            }
        }

        /// <summary>
        /// Called when we need to eat a token even if its kind is different from what we're looking for.  This will
        /// place a diagnostic on the resultant token if the kind is not correct.  Note: the token's kind will
        /// <em>not</em> be the same as <paramref name="kind"/>.  As such, callers should take great care here to ensure
        /// they process the result properly in their context.  For example, adding the token as skipped syntax, or
        /// forcibly changing its kind by some other means.
        /// </summary>
        protected SyntaxToken EatTokenEvenWithIncorrectKind(SyntaxKind kind)
        {
            var token = this.CurrentToken;
            Debug.Assert(SyntaxFacts.IsAnyToken(kind));
            if (token.Kind != kind)
            {
                var (offset, width) = getDiagnosticSpan();
                token = WithAdditionalDiagnostics(token, this.GetExpectedTokenError(kind, token.Kind, offset, width));
            }

            this.MoveToNextToken();
            return token;

            (int offset, int width) getDiagnosticSpan()
            {
                // We got the wrong kind while forcefully eating this token.  If it's on the same line as the last
                // token, just squiggle it as being the wrong kind. If it's on the next line, move the squiggle back to
                // the end of the previous token and make it zero width, indicating the expected token was missed at
                // that location (even though we're still unilaterally consuming this token).

                var trivia = _prevTokenTrailingTrivia;
                var triviaList = new SyntaxList<CSharpSyntaxNode>(trivia);
                if (triviaList.Any((int)SyntaxKind.EndOfLineTrivia))
                    return (offset: -(trivia.FullWidth + token.GetLeadingTriviaWidth()), width: 0);

                return (offset: 0, token.Width);
            }
        }

        protected SyntaxToken EatTokenWithPrejudice(ErrorCode errorCode, params object[] args)
        {
            var token = this.EatToken();
            token = WithAdditionalDiagnostics(token, MakeError(offset: 0, token.Width, errorCode, args));
            return token;
        }

        protected SyntaxToken EatContextualToken(SyntaxKind kind, ErrorCode code, bool reportError = true)
        {
            Debug.Assert(SyntaxFacts.IsAnyToken(kind));

            if (this.CurrentToken.ContextualKind != kind)
            {
                return CreateMissingToken(kind, code, reportError);
            }
            else
            {
                return ConvertToKeyword(this.EatToken());
            }
        }

        protected SyntaxToken EatContextualToken(SyntaxKind kind)
        {
            Debug.Assert(SyntaxFacts.IsAnyToken(kind));

            var contextualKind = this.CurrentToken.ContextualKind;
            if (contextualKind != kind)
            {
                return CreateMissingToken(kind, contextualKind);
            }
            else
            {
                return ConvertToKeyword(this.EatToken());
            }
        }

        protected virtual SyntaxDiagnosticInfo GetExpectedTokenError(SyntaxKind expected, SyntaxKind actual, int offset, int width)
        {
            var code = GetExpectedTokenErrorCode(expected, actual);
            if (code == ErrorCode.ERR_SyntaxError)
            {
                return new SyntaxDiagnosticInfo(offset, width, code, SyntaxFacts.GetText(expected));
            }
            else if (code == ErrorCode.ERR_IdentifierExpectedKW)
            {
                return new SyntaxDiagnosticInfo(offset, width, code, /*unused*/string.Empty, SyntaxFacts.GetText(actual));
            }
            else
            {
                return new SyntaxDiagnosticInfo(offset, width, code);
            }
        }

        protected virtual SyntaxDiagnosticInfo GetExpectedMissingNodeOrTokenError(
            GreenNode missingNodeOrToken, SyntaxKind expected, SyntaxKind actual)
        {
            Debug.Assert(missingNodeOrToken.IsMissing);

            var (offset, width) = this.GetDiagnosticSpanForMissingNodeOrToken(missingNodeOrToken);
            return this.GetExpectedTokenError(expected, actual, offset, width);
        }

        private static ErrorCode GetExpectedTokenErrorCode(SyntaxKind expected, SyntaxKind actual)
        {
            switch (expected)
            {
                case SyntaxKind.IdentifierToken:
                    if (SyntaxFacts.IsReservedKeyword(actual))
                    {
                        return ErrorCode.ERR_IdentifierExpectedKW;   // A keyword -- use special message.
                    }
                    else
                    {
                        return ErrorCode.ERR_IdentifierExpected;
                    }

                case SyntaxKind.SemicolonToken:
                    return ErrorCode.ERR_SemicolonExpected;

                // case TokenKind::Colon:         iError = ERR_ColonExpected;          break;
                // case TokenKind::OpenParen:     iError = ERR_LparenExpected;         break;
                case SyntaxKind.CloseParenToken:
                    return ErrorCode.ERR_CloseParenExpected;
                case SyntaxKind.OpenBraceToken:
                    return ErrorCode.ERR_LbraceExpected;
                case SyntaxKind.CloseBraceToken:
                    return ErrorCode.ERR_RbraceExpected;

                // case TokenKind::CloseSquare:   iError = ERR_CloseSquareExpected;    break;
                default:
                    return ErrorCode.ERR_SyntaxError;
            }
        }

        protected virtual TNode WithAdditionalDiagnostics<TNode>(TNode node, params DiagnosticInfo[] diagnostics) where TNode : GreenNode
        {
            DiagnosticInfo[] existingDiags = node.GetDiagnostics();
            int existingLength = existingDiags.Length;
            if (existingLength == 0)
            {
                return node.WithDiagnosticsGreen(diagnostics);
            }
            else
            {
                DiagnosticInfo[] result = new DiagnosticInfo[existingDiags.Length + diagnostics.Length];
                existingDiags.CopyTo(result, 0);
                diagnostics.CopyTo(result, existingLength);
                return node.WithDiagnosticsGreen(result);
            }
        }

        protected TNode AddError<TNode>(TNode node, ErrorCode code) where TNode : GreenNode
        {
            return AddError(node, code, Array.Empty<object>());
        }

        protected TNode AddErrorAsWarning<TNode>(TNode node, ErrorCode code, params object[] args) where TNode : GreenNode
        {
            Debug.Assert(!node.IsMissing);
            return AddError(node, ErrorCode.WRN_ErrorOverride, MakeError(node, code, args), (int)code);
        }

        protected TNode AddError<TNode>(TNode nodeOrToken, ErrorCode code, params object[] args) where TNode : GreenNode
        {
            if (!nodeOrToken.IsMissing)
            {
                // We have a normal node or token that has actual SyntaxToken.Text within it (or the EOF token). Place
                // the diagnostic at the start (not full start) of that real node/token, with a width that encompasses
                // the entire normal width of the node or token.
                Debug.Assert(nodeOrToken.Width > 0 || nodeOrToken.RawKind is (int)SyntaxKind.EndOfFileToken);
                return WithAdditionalDiagnostics(nodeOrToken, MakeError(nodeOrToken, code, args));
            }
            else
            {
                var (offset, width) = this.GetDiagnosticSpanForMissingNodeOrToken(nodeOrToken);
                return WithAdditionalDiagnostics(nodeOrToken, MakeError(offset, width, code, args));
            }
        }

        /// <summary>
        /// Given a "missing" node or token (one where <see cref="GreenNode.IsMissing"/> must be true), determines the
        /// ideal location to place the diagnostic for it.  The intuition here is that we want to place the diagnostic
        /// on the token that "follows" this 'missing' entity if they're on the same line.  Or, place it at the end of
        /// the 'preceding' token if the following token is on the next line.
        /// </summary>
        protected (int offset, int width) GetDiagnosticSpanForMissingNodeOrToken(GreenNode missingNodeOrToken)
        {
            Debug.Assert(missingNodeOrToken.IsMissing);

            // Note: missingNodeOrToken.IsMissing means this is either a MissingToken itself, or a node comprised
            // (transitively) only from MissingTokens.  Missing tokens are guaranteed to have no text.  But they are
            // allowed to have trivia.  This is a common pattern the parser will follow when it encounters unexpected
            // tokens.  It will make a missing token of the expected kind for the current location, then attach the
            // unexpected tokens as missed tokens to it.

            // At this point, we have a node or token without real text in it.  The intuition we have here is that we
            // want to place the diagnostic on the token that "follows" this 'missing' entity.  There is a subtlety
            // here.  If the node or token contains skipped tokens, then we consider that skipped token the "following"
            // token, and we will want to place the diagnostic on it.  Otherwise, we want to place it on the true 'next
            // token' the parser is currently pointing at.

            if (!missingNodeOrToken.ContainsSkippedText)
            {
                // Simple case this node/token does not contain any skipped text.  Place the diagnostic at the start of
                // the token that follows.
                return getOffsetAndWidthBasedOnPriorAndNextTokens();
            }
            else
            {
                // Complex case.  This node or token contains skipped text.  Place the diagnostic on the skipped text.
                return getOffsetAndWidthOfSkippedToken();
            }

            (int offset, int width) getOffsetAndWidthBasedOnPriorAndNextTokens()
            {
                // If the previous token has a trailing EndOfLineTrivia, the missing token diagnostic position is moved
                // to the end of line containing the previous token and its width is set to zero. Otherwise we squiggle
                // the token following the missing token (the token we're currently pointing at).

                var trivia = _prevTokenTrailingTrivia;
                var triviaList = new SyntaxList<CSharpSyntaxNode>(trivia);
                if (triviaList.Any((int)SyntaxKind.EndOfLineTrivia))
                {
                    // We have:
                    //
                    //   [previous token][previous token trailing trivia...][missing node leading trivia...][missing node or token]
                    //                                                                                      ^
                    //                                                                                      | here
                    //
                    // Update so we report diagnostic here:
                    //
                    //   [previous token][previous token trailing trivia...][missing node leading trivia...][missing node or token]
                    //                   ^
                    //                   | here
                    return (offset: -missingNodeOrToken.GetLeadingTriviaWidth() - trivia.FullWidth, width: 0);
                }
                else
                {
                    // We have:
                    //
                    //   [missing node leading trivia...][missing node or token][missing node or token trailing trivia..][current token leading trivia ...][current token]
                    //                                   ^
                    //                                   | here
                    //
                    // Update so we report diagnostic here:
                    //
                    //   [missing node leading trivia...][missing node or token][missing node or token trailing trivia..][current token leading trivia ...][current token]
                    //                                                                                                                                     ^             ^
                    //                                                                                                                                     | --- here -- |
                    var token = this.CurrentToken;
                    return (missingNodeOrToken.Width + missingNodeOrToken.GetTrailingTriviaWidth() + token.GetLeadingTriviaWidth(), token.Width);
                }
            }

            (int offset, int width) getOffsetAndWidthOfSkippedToken()
            {
                var offset = 0;

                // Walk all the children of this nodeOrToken (including itself).  Note: this does not walk into trivia.
                // We are looking for the first token that has skipped text.  When we find that token (which must exist,
                // based on the check above), we will place the diagnostic on the skipped token within that token.
                foreach (var child in missingNodeOrToken.EnumerateNodes())
                {
                    Debug.Assert(child.IsMissing, "All children of a missing node or token should themselves be missing.");
                    if (!child.IsToken)
                        continue;

                    var childToken = (Syntax.InternalSyntax.SyntaxToken)child;
                    Debug.Assert(childToken.Text == "", "All missing tokens should have no text");
                    if (!child.ContainsSkippedText)
                    {
                        offset += child.FullWidth;
                        continue;
                    }

                    // Now, walk the trivia of this token, looking for the skipped tokens trivia.
                    var allTrivia = new SyntaxList<GreenNode>(SyntaxList.Concat(childToken.GetLeadingTrivia(), childToken.GetTrailingTrivia()));
                    Debug.Assert(allTrivia.Count > 0, "How can a token with skipped text not have trivia at all?");

                    foreach (var trivia in allTrivia)
                    {
                        if (!trivia.IsSkippedTokensTrivia)
                        {
                            offset += trivia.FullWidth;
                            continue;
                        }

                        // Found the skipped tokens trivia.  Place the diagnostic on it.
                        return (offset, trivia.Width);
                    }

                    Debug.Fail("This should not be reachable.  We should have hit a skipped token in the trivia of this token.");
                    return default;
                }

                Debug.Fail("This should not be reachable.  We should have hit a child token with skipped text within this node.");
                return default;
            }
        }

        protected TNode AddError<TNode>(TNode node, int offset, int length, ErrorCode code, params object[] args) where TNode : CSharpSyntaxNode
        {
            return WithAdditionalDiagnostics(node, MakeError(offset, length, code, args));
        }

        protected TNode AddErrorToFirstToken<TNode>(TNode node, ErrorCode code) where TNode : CSharpSyntaxNode
        {
            var firstToken = node.GetFirstToken();
            return WithAdditionalDiagnostics(node, MakeError(offset: 0, firstToken.Width, code));
        }

        protected TNode AddErrorToFirstToken<TNode>(TNode node, ErrorCode code, params object[] args) where TNode : CSharpSyntaxNode
        {
            var firstToken = node.GetFirstToken();
            return WithAdditionalDiagnostics(node, MakeError(offset: 0, firstToken.Width, code, args));
        }

        protected TNode AddErrorToLastToken<TNode>(TNode node, ErrorCode code) where TNode : CSharpSyntaxNode
        {
            int offset;
            int width;
            GetOffsetAndWidthForLastToken(node, out offset, out width);
            return WithAdditionalDiagnostics(node, MakeError(offset, width, code));
        }

        private static void GetOffsetAndWidthForLastToken<TNode>(TNode node, out int offset, out int width) where TNode : CSharpSyntaxNode
        {
            var lastToken = node.GetLastNonmissingToken();
            offset = node.Width + node.GetTrailingTriviaWidth(); //advance to end of entire node
            width = 0;
            if (lastToken != null) //will be null if all tokens are missing
            {
                offset -= lastToken.FullWidth; //rewind past last token
                offset += lastToken.GetLeadingTriviaWidth(); //advance past last token leading trivia - now at start of last token
                width = lastToken.Width;
            }
        }

        protected static SyntaxDiagnosticInfo MakeError(int offset, int width, ErrorCode code)
        {
            return new SyntaxDiagnosticInfo(offset, width, code);
        }

        protected static SyntaxDiagnosticInfo MakeError(int offset, int width, ErrorCode code, params object[] args)
        {
            return new SyntaxDiagnosticInfo(offset, width, code, args);
        }

        protected static SyntaxDiagnosticInfo MakeError(GreenNode node, ErrorCode code, params object[] args)
        {
            return new SyntaxDiagnosticInfo(offset: 0, node.Width, code, args);
        }

        protected static SyntaxDiagnosticInfo MakeError(ErrorCode code, params object[] args)
        {
            return new SyntaxDiagnosticInfo(code, args);
        }

#nullable enable

        protected TNode AddLeadingSkippedSyntax<TNode>(TNode node, GreenNode? skippedSyntax) where TNode : CSharpSyntaxNode
        {
            if (skippedSyntax is null)
                return node;

            var oldToken = node as SyntaxToken ?? node.GetFirstToken();
            var newToken = AddSkippedSyntax(oldToken, skippedSyntax, trailing: false);
            return SyntaxFirstTokenReplacer.Replace(node, oldToken, newToken, skippedSyntax.FullWidth);
        }

#nullable disable

        protected void AddTrailingSkippedSyntax(SyntaxListBuilder list, GreenNode skippedSyntax)
        {
            list[^1] = AddTrailingSkippedSyntax((CSharpSyntaxNode)list[^1], skippedSyntax);
        }

        protected void AddTrailingSkippedSyntax<TNode>(SyntaxListBuilder<TNode> list, GreenNode skippedSyntax) where TNode : CSharpSyntaxNode
        {
            list[^1] = AddTrailingSkippedSyntax(list[^1], skippedSyntax);
        }

        protected TNode AddTrailingSkippedSyntax<TNode>(TNode node, GreenNode skippedSyntax) where TNode : CSharpSyntaxNode
        {
            if (node is SyntaxToken token)
            {
                return (TNode)(object)AddSkippedSyntax(token, skippedSyntax, trailing: true);
            }
            else
            {
                var lastToken = node.GetLastToken();
                var newToken = AddSkippedSyntax(lastToken, skippedSyntax, trailing: true);
                return SyntaxLastTokenReplacer.Replace(node, newToken);
            }
        }

        /// <summary>
        /// Converts skippedSyntax node into all its constituent tokens (and their constituent trivias) and adds these
        /// all as trivia on the target token.  For example, given <c>token1-token2</c>, then target will have
        /// <c>leading_trivia1-token1-trailing_trivia1-leading_trivia2-token2-trailing_trivia2-</c> added to it.
        /// <para/>
        /// 
        /// Also adds the first node-based error, or error on a missing-token, in depth-first preorder, found in the
        /// skipped syntax tree to the target token.  This ensures that we do not lose token/node errors found in
        /// skipped syntax.
        /// 
        /// Note: This behavior could technically lead to buggy behavior.  Specifically, because we only take the first
        /// diagnostic we find, we might miss a more relevant diagnostic later in the tree.  For example, we might
        /// preserve a 'warning' while missing an error.
        /// 
        /// We should either:
        /// 
        /// 1. ensure that we copy over an error if it exists, overwriting any warnings we found along the way.
        /// 
        /// 2. just copy over everything.  This seems saner, as it means not losing anything. But it might be the case
        /// that when we recover from a big error recovery scan, we might report a ton of errors.
        ///
        /// For now, we do neither, and just take the first error/warning we find.  This can/should be revisited later
        /// if we discover it means we're losing important diagnostics.
        /// </summary>
        internal SyntaxToken AddSkippedSyntax(SyntaxToken target, GreenNode skippedSyntax, bool trailing)
        {
            var builder = new SyntaxListBuilder(4);

            int currentOffset;
            if (trailing)
            {
                // The normal offset for a node/token is its start (not full start).  So if we're placing the skipped
                // syntax at the end of the trivia, then the offset relative to the node/token start will be adjusted
                // forward by the width of the node/token plus the existing trailing trivia.
                currentOffset = target.Width + target.GetTrailingTriviaWidth();
                builder.Add(target.GetTrailingTrivia());
            }
            else
            {
                // The normal offset for a node/token is its start (not full start). So if we're placing the skipped
                // syntax at the start of the trivia, then the offset relative to the node/token start will be adjusted
                // backward by the width of the existing leading trivia plus the width of the skipped syntax we're
                // tacking on at the front.
                currentOffset = -target.GetLeadingTriviaWidth() - skippedSyntax.FullWidth;
            }

            // the error in we'll attach to the node
            SyntaxDiagnosticInfo diagnostic = null;
            int finalDiagnosticOffset = 0;

            foreach (var node in skippedSyntax.EnumerateNodes())
            {
                if (node is SyntaxToken token)
                {
                    // Strip the leading trivia of the token, and add it to the target's final trivia list.
                    builder.Add(token.GetLeadingTrivia());

                    if (token.Width > 0)
                    {
                        // Then add the token (stripped of its own trivia) to the target's final trivia list.

                        builder.Add(SyntaxFactory.SkippedTokensTrivia(
                            token.TokenWithLeadingTrivia(null).TokenWithTrailingTrivia(null)));
                    }
                    else
                    {
                        // Do not bother adding zero-width tokens to target's final trivia list.  Lots of code (like
                        // GetStructure) does not like it at all. But do keep around any diagnostics that might have
                        // been on this zero width token, and move it to the target.
                        var existing = (SyntaxDiagnosticInfo)token.GetDiagnostics().FirstOrDefault();
                        if (existing != null)
                        {
                            diagnostic = existing;
                            finalDiagnosticOffset = currentOffset + token.GetLeadingTriviaWidth() + existing.Offset;
                        }
                    }

                    // Finally strip the trailing trivia of the token, and add it to the target's final list.
                    builder.Add(token.GetTrailingTrivia());

                    currentOffset += token.FullWidth;
                }
                else if (node.ContainsDiagnostics && diagnostic == null)
                {
                    // Ensure we don't lose any diagnostics on non-token nodes that we're diving into.
                    // Only propagate the first error to reduce noise:
                    var existing = (SyntaxDiagnosticInfo)node.GetDiagnostics().FirstOrDefault();
                    if (existing != null)
                    {
                        diagnostic = existing;
                        finalDiagnosticOffset = currentOffset + node.GetLeadingTriviaWidth() + existing.Offset;
                    }
                }
            }

            // If we found a diagnostic on a node (or empty-width token) in the skipped syntax, ensure it is moved
            // over to the target.
            if (diagnostic != null)
            {
                target = WithAdditionalDiagnostics(target,
                    new SyntaxDiagnosticInfo(finalDiagnosticOffset, diagnostic.Width, (ErrorCode)diagnostic.Code, diagnostic.Arguments));
            }

            // If we were adding the skipped token as trailing trivia, then at this point we're done.  Otherwise, we
            // were adding it as leading trivia, so we need to tack on the existing leading trivia of the target.
            return trailing
                ? target.TokenWithTrailingTrivia(builder.ToListNode())
                : target.TokenWithLeadingTrivia(builder.AddRange(target.GetLeadingTrivia()).ToListNode());
        }

        protected static SyntaxToken ConvertToKeyword(SyntaxToken token)
        {
            if (token.Kind != token.ContextualKind)
            {
                var kw = token.IsMissing
                        ? SyntaxFactory.MissingToken(token.LeadingTrivia.Node, token.ContextualKind, token.TrailingTrivia.Node)
                        : SyntaxFactory.Token(token.LeadingTrivia.Node, token.ContextualKind, token.TrailingTrivia.Node);
                var d = token.GetDiagnostics();
                if (d != null && d.Length > 0)
                {
                    kw = kw.WithDiagnosticsGreen(d);
                }

                return kw;
            }

            return token;
        }

        protected static SyntaxToken ConvertToIdentifier(SyntaxToken token)
        {
            Debug.Assert(!token.IsMissing);

            var identifier = SyntaxToken.Identifier(token.Kind, token.LeadingTrivia.Node, token.Text, token.ValueText, token.TrailingTrivia.Node);
            if (token.ContainsDiagnostics)
                identifier = identifier.WithDiagnosticsGreen(token.GetDiagnostics());

            return identifier;
        }

        internal DirectiveStack Directives
        {
            get { return lexer.Directives; }
        }

#nullable enable
        /// <remarks>
        /// NOTE: we are specifically diverging from dev11 to improve the user experience.
        /// Since treating the "async" keyword as an identifier in older language
        /// versions can never result in a correct program, we instead accept it as a
        /// keyword regardless of the language version and produce an error if the version
        /// is insufficient.
        /// </remarks>
        protected TNode CheckFeatureAvailability<TNode>(TNode node, MessageID feature, bool forceWarning = false)
            where TNode : GreenNode
        {
            var info = feature.GetFeatureAvailabilityDiagnosticInfo(this.Options);
            if (info != null)
            {
                if (forceWarning)
                {
                    return AddError(node, ErrorCode.WRN_ErrorOverride, info, (int)info.Code);
                }

                return AddError(node, info.Code, info.Arguments);
            }

            return node;
        }
#nullable disable

        protected bool IsFeatureEnabled(MessageID feature)
        {
            return this.Options.IsFeatureEnabled(feature);
        }

        /// <summary>
        /// Whenever parsing in a <c>while (true)</c> loop and a bug could prevent the loop from making progress,
        /// this method can prevent the parsing from hanging.
        /// Use as:
        ///     int tokenProgress = -1;
        ///     while (IsMakingProgress(ref tokenProgress))
        /// It should be used as a guardrail, not as a crutch, so it asserts if no progress was made.
        /// </summary>
        protected bool IsMakingProgress(ref int lastTokenPosition, bool assertIfFalse = true)
        {
            var pos = CurrentTokenPosition;
            if (pos > lastTokenPosition)
            {
                lastTokenPosition = pos;
                return true;
            }

            Debug.Assert(!assertIfFalse);
            return false;
        }

        private int CurrentTokenPosition => _firstToken + _tokenOffset;
    }
}
