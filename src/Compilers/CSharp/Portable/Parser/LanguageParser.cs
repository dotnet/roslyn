// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal partial class LanguageParser : SyntaxParser
    {
        // list pools - allocators for lists that are used to build sequences of nodes. The lists
        // can be reused (hence pooled) since the syntax factory methods don't keep references to
        // them

        private readonly SyntaxListPool _pool = new SyntaxListPool(); // Don't need to reset this.

        private readonly SyntaxFactoryContext _syntaxFactoryContext; // Fields are resettable.
        private readonly ContextAwareSyntax _syntaxFactory; // Has context, the fields of which are resettable.

        private int _recursionDepth;
        private TerminatorState _termState; // Resettable
        private bool _isInTry; // Resettable

        // NOTE: If you add new state, you should probably add it to ResetPoint as well.

        internal LanguageParser(
            Lexer lexer,
            CSharp.CSharpSyntaxNode oldTree,
            IEnumerable<TextChangeRange> changes,
            LexerMode lexerMode = LexerMode.Syntax,
            CancellationToken cancellationToken = default(CancellationToken))
            : base(lexer, lexerMode, oldTree, changes, allowModeReset: false,
                preLexIfNotIncremental: true, cancellationToken: cancellationToken)
        {
            _syntaxFactoryContext = new SyntaxFactoryContext();
            _syntaxFactory = new ContextAwareSyntax(_syntaxFactoryContext);
        }

        private static bool IsSomeWord(SyntaxKind kind)
        {
            return kind == SyntaxKind.IdentifierToken || SyntaxFacts.IsKeywordKind(kind);
        }

        // Parsing rule terminating conditions.  This is how we know if it is 
        // okay to abort the current parsing rule when unexpected tokens occur.

        [Flags]
        internal enum TerminatorState
        {
            EndOfFile = 0,
            IsNamespaceMemberStartOrStop = 1 << 0,
            IsAttributeDeclarationTerminator = 1 << 1,
            IsPossibleAggregateClauseStartOrStop = 1 << 2,
            IsPossibleMemberStartOrStop = 1 << 3,
            IsEndOfReturnType = 1 << 4,
            IsEndOfParameterList = 1 << 5,
            IsEndOfFieldDeclaration = 1 << 6,
            IsPossibleEndOfVariableDeclaration = 1 << 7,
            IsEndOfTypeArgumentList = 1 << 8,
            IsPossibleStatementStartOrStop = 1 << 9,
            IsEndOfFixedStatement = 1 << 10,
            IsEndOfTryBlock = 1 << 11,
            IsEndOfCatchClause = 1 << 12,
            IsEndOfFilterClause = 1 << 13,
            IsEndOfCatchBlock = 1 << 14,
            IsEndOfDoWhileExpression = 1 << 15,
            IsEndOfForStatementArgument = 1 << 16,
            IsEndOfDeclarationClause = 1 << 17,
            IsEndOfArgumentList = 1 << 18,
            IsSwitchSectionStart = 1 << 19,
            IsEndOfTypeParameterList = 1 << 20,
            IsEndOfMethodSignature = 1 << 21,
            IsEndOfNameInExplicitInterface = 1 << 22,
        }

        private const int LastTerminatorState = (int)TerminatorState.IsEndOfNameInExplicitInterface;

        private bool IsTerminator()
        {
            if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken)
            {
                return true;
            }

            for (int i = 1; i <= LastTerminatorState; i <<= 1)
            {
                switch (_termState & (TerminatorState)i)
                {
                    case TerminatorState.IsNamespaceMemberStartOrStop when this.IsNamespaceMemberStartOrStop():
                    case TerminatorState.IsAttributeDeclarationTerminator when this.IsAttributeDeclarationTerminator():
                    case TerminatorState.IsPossibleAggregateClauseStartOrStop when this.IsPossibleAggregateClauseStartOrStop():
                    case TerminatorState.IsPossibleMemberStartOrStop when this.IsPossibleMemberStartOrStop():
                    case TerminatorState.IsEndOfReturnType when this.IsEndOfReturnType():
                    case TerminatorState.IsEndOfParameterList when this.IsEndOfParameterList():
                    case TerminatorState.IsEndOfFieldDeclaration when this.IsEndOfFieldDeclaration():
                    case TerminatorState.IsPossibleEndOfVariableDeclaration when this.IsPossibleEndOfVariableDeclaration():
                    case TerminatorState.IsEndOfTypeArgumentList when this.IsEndOfTypeArgumentList():
                    case TerminatorState.IsPossibleStatementStartOrStop when this.IsPossibleStatementStartOrStop():
                    case TerminatorState.IsEndOfFixedStatement when this.IsEndOfFixedStatement():
                    case TerminatorState.IsEndOfTryBlock when this.IsEndOfTryBlock():
                    case TerminatorState.IsEndOfCatchClause when this.IsEndOfCatchClause():
                    case TerminatorState.IsEndOfFilterClause when this.IsEndOfFilterClause():
                    case TerminatorState.IsEndOfCatchBlock when this.IsEndOfCatchBlock():
                    case TerminatorState.IsEndOfDoWhileExpression when this.IsEndOfDoWhileExpression():
                    case TerminatorState.IsEndOfForStatementArgument when this.IsEndOfForStatementArgument():
                    case TerminatorState.IsEndOfDeclarationClause when this.IsEndOfDeclarationClause():
                    case TerminatorState.IsEndOfArgumentList when this.IsEndOfArgumentList():
                    case TerminatorState.IsSwitchSectionStart when this.IsPossibleSwitchSection():
                    case TerminatorState.IsEndOfTypeParameterList when this.IsEndOfTypeParameterList():
                    case TerminatorState.IsEndOfMethodSignature when this.IsEndOfMethodSignature():
                    case TerminatorState.IsEndOfNameInExplicitInterface when this.IsEndOfNameInExplicitInterface():
                        return true;
                }
            }

            return false;
        }

        private static CSharp.CSharpSyntaxNode GetOldParent(CSharp.CSharpSyntaxNode node)
        {
            return node != null ? node.Parent : null;
        }

        private struct NamespaceBodyBuilder
        {
            public SyntaxListBuilder<ExternAliasDirectiveSyntax> Externs;
            public SyntaxListBuilder<UsingDirectiveSyntax> Usings;
            public SyntaxListBuilder<AttributeListSyntax> Attributes;
            public SyntaxListBuilder<MemberDeclarationSyntax> Members;

            public NamespaceBodyBuilder(SyntaxListPool pool)
            {
                Externs = pool.Allocate<ExternAliasDirectiveSyntax>();
                Usings = pool.Allocate<UsingDirectiveSyntax>();
                Attributes = pool.Allocate<AttributeListSyntax>();
                Members = pool.Allocate<MemberDeclarationSyntax>();
            }

            internal void Free(SyntaxListPool pool)
            {
                pool.Free(Members);
                pool.Free(Attributes);
                pool.Free(Usings);
                pool.Free(Externs);
            }
        }

        internal CompilationUnitSyntax ParseCompilationUnit()
        {
            return ParseWithStackGuard(
                ParseCompilationUnitCore,
                () => SyntaxFactory.CompilationUnit(
                        new SyntaxList<ExternAliasDirectiveSyntax>(),
                        new SyntaxList<UsingDirectiveSyntax>(),
                        new SyntaxList<AttributeListSyntax>(),
                        new SyntaxList<MemberDeclarationSyntax>(),
                        SyntaxFactory.Token(SyntaxKind.EndOfFileToken)));
        }

        internal CompilationUnitSyntax ParseCompilationUnitCore()
        {
            SyntaxToken tmp = null;
            SyntaxListBuilder initialBadNodes = null;
            var body = new NamespaceBodyBuilder(_pool);
            try
            {
                this.ParseNamespaceBody(ref tmp, ref body, ref initialBadNodes, SyntaxKind.CompilationUnit);

                var eof = this.EatToken(SyntaxKind.EndOfFileToken);
                var result = _syntaxFactory.CompilationUnit(body.Externs, body.Usings, body.Attributes, body.Members, eof);

                if (initialBadNodes != null)
                {
                    // attach initial bad nodes as leading trivia on first token
                    result = AddLeadingSkippedSyntax(result, initialBadNodes.ToListNode());
                    _pool.Free(initialBadNodes);
                }

                return result;
            }
            finally
            {
                body.Free(_pool);
            }
        }

        internal TNode ParseWithStackGuard<TNode>(Func<TNode> parseFunc, Func<TNode> createEmptyNodeFunc) where TNode : CSharpSyntaxNode
        {
            // If this value is non-zero then we are nesting calls to ParseWithStackGuard which should not be 
            // happening.  It's not a bug but it's inefficient and should be changed.
            Debug.Assert(_recursionDepth == 0);

            try
            {
                return parseFunc();
            }
            catch (InsufficientExecutionStackException)
            {
                return CreateForGlobalFailure(lexer.TextWindow.Position, createEmptyNodeFunc());
            }
        }

        private TNode CreateForGlobalFailure<TNode>(int position, TNode node) where TNode : CSharpSyntaxNode
        {
            // Turn the complete input into a single skipped token. This avoids running the lexer, and therefore
            // the preprocessor directive parser, which may itself run into the same problem that caused the
            // original failure.
            var builder = new SyntaxListBuilder(1);
            builder.Add(SyntaxFactory.BadToken(null, lexer.TextWindow.Text.ToString(), null));
            var fileAsTrivia = _syntaxFactory.SkippedTokensTrivia(builder.ToList<SyntaxToken>());
            node = AddLeadingSkippedSyntax(node, fileAsTrivia);
            ForceEndOfFile(); // force the scanner to report that it is at the end of the input.
            return AddError(node, position, 0, ErrorCode.ERR_InsufficientStack);
        }

        private NamespaceDeclarationSyntax ParseNamespaceDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributeLists,
            SyntaxListBuilder modifiers)
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            var result = ParseNamespaceDeclarationCore(attributeLists, modifiers);
            _recursionDepth--;
            return result;
        }

        private NamespaceDeclarationSyntax ParseNamespaceDeclarationCore(
            SyntaxListBuilder<AttributeListSyntax> attributeLists,
            SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.NamespaceKeyword);
            var namespaceToken = this.EatToken(SyntaxKind.NamespaceKeyword);

            if (IsScript)
            {
                namespaceToken = this.AddError(namespaceToken, ErrorCode.ERR_NamespaceNotAllowedInScript);
            }

            var name = this.ParseQualifiedName();

            SyntaxToken openBrace;
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || IsPossibleNamespaceMemberDeclaration())
            {
                //either we see the brace we expect here or we see something that could come after a brace
                //so we insert a missing one
                openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            }
            else
            {
                //the next character is neither the brace we expect, nor a token that could follow the expected
                //brace so we assume it's a mistake and replace it with a missing brace 
                openBrace = this.EatTokenWithPrejudice(SyntaxKind.OpenBraceToken);
                openBrace = this.ConvertToMissingWithTrailingTrivia(openBrace, SyntaxKind.OpenBraceToken);
            }

            var body = new NamespaceBodyBuilder(_pool);
            SyntaxListBuilder initialBadNodes = null;
            try
            {
                this.ParseNamespaceBody(ref openBrace, ref body, ref initialBadNodes, SyntaxKind.NamespaceDeclaration);

                var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
                var semicolon = this.TryEatToken(SyntaxKind.SemicolonToken);

                Debug.Assert(initialBadNodes == null); // init bad nodes should have been attached to open brace...
                return _syntaxFactory.NamespaceDeclaration(
                    attributeLists, modifiers.ToList(),
                    namespaceToken, name, openBrace, body.Externs, body.Usings, body.Members, closeBrace, semicolon);
            }
            finally
            {
                body.Free(_pool);
            }
        }

        private static bool IsPossibleStartOfTypeDeclaration(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.EnumKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.StructKeyword:
                case SyntaxKind.AbstractKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.SealedKeyword:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.UnsafeKeyword:
                case SyntaxKind.OpenBracketToken:
                    return true;
                default:
                    return false;
            }
        }

        private void AddSkippedNamespaceText(
            ref SyntaxToken openBrace,
            ref NamespaceBodyBuilder body,
            ref SyntaxListBuilder initialBadNodes,
            CSharpSyntaxNode skippedSyntax)
        {
            if (body.Members.Count > 0)
            {
                AddTrailingSkippedSyntax(body.Members, skippedSyntax);
            }
            else if (body.Attributes.Count > 0)
            {
                AddTrailingSkippedSyntax(body.Attributes, skippedSyntax);
            }
            else if (body.Usings.Count > 0)
            {
                AddTrailingSkippedSyntax(body.Usings, skippedSyntax);
            }
            else if (body.Externs.Count > 0)
            {
                AddTrailingSkippedSyntax(body.Externs, skippedSyntax);
            }
            else if (openBrace != null)
            {
                openBrace = AddTrailingSkippedSyntax(openBrace, skippedSyntax);
            }
            else
            {
                if (initialBadNodes == null)
                {
                    initialBadNodes = _pool.Allocate();
                }

                initialBadNodes.AddRange(skippedSyntax);
            }
        }

        // Parts of a namespace declaration in the order they can be defined.
        private enum NamespaceParts
        {
            None = 0,
            ExternAliases = 1,
            Usings = 2,
            GlobalAttributes = 3,
            MembersAndStatements = 4,
        }

        private void ParseNamespaceBody(ref SyntaxToken openBrace, ref NamespaceBodyBuilder body, ref SyntaxListBuilder initialBadNodes, SyntaxKind parentKind)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            bool isGlobal = openBrace == null;
            bool isGlobalScript = isGlobal && this.IsScript;

            var saveTerm = _termState;
            _termState |= TerminatorState.IsNamespaceMemberStartOrStop;
            NamespaceParts seen = NamespaceParts.None;
            var pendingIncompleteMembers = _pool.Allocate<MemberDeclarationSyntax>();
            bool reportUnexpectedToken = true;

            try
            {
                while (true)
                {
                    switch (this.CurrentToken.Kind)
                    {
                        case SyntaxKind.NamespaceKeyword:
                            // incomplete members must be processed before we add any nodes to the body:
                            AddIncompleteMembers(ref pendingIncompleteMembers, ref body);

                            var attributeLists = _pool.Allocate<AttributeListSyntax>();
                            var modifiers = _pool.Allocate();

                            body.Members.Add(this.ParseNamespaceDeclaration(attributeLists, modifiers));

                            _pool.Free(attributeLists);
                            _pool.Free(modifiers);

                            seen = NamespaceParts.MembersAndStatements;
                            reportUnexpectedToken = true;
                            break;

                        case SyntaxKind.CloseBraceToken:
                            // A very common user error is to type an additional } 
                            // somewhere in the file.  This will cause us to stop parsing
                            // the root (global) namespace too early and will make the 
                            // rest of the file unparseable and unusable by intellisense.
                            // We detect that case here and we skip the close curly and
                            // continue parsing as if we did not see the }
                            if (isGlobal)
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBrace, ref body, ref initialBadNodes);

                                var token = this.EatToken();
                                token = this.AddError(token,
                                    IsScript ? ErrorCode.ERR_GlobalDefinitionOrStatementExpected : ErrorCode.ERR_EOFExpected);

                                this.AddSkippedNamespaceText(ref openBrace, ref body, ref initialBadNodes, token);
                                reportUnexpectedToken = true;
                                break;
                            }
                            else
                            {
                                // This token marks the end of a namespace body
                                return;
                            }

                        case SyntaxKind.EndOfFileToken:
                            // This token marks the end of a namespace body
                            return;

                        case SyntaxKind.ExternKeyword:
                            if (isGlobalScript && !ScanExternAliasDirective())
                            {
                                // extern member
                                goto default;
                            }
                            else
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBrace, ref body, ref initialBadNodes);

                                var @extern = ParseExternAliasDirective();
                                if (seen > NamespaceParts.ExternAliases)
                                {
                                    @extern = this.AddErrorToFirstToken(@extern, ErrorCode.ERR_ExternAfterElements);
                                    this.AddSkippedNamespaceText(ref openBrace, ref body, ref initialBadNodes, @extern);
                                }
                                else
                                {
                                    body.Externs.Add(@extern);
                                    seen = NamespaceParts.ExternAliases;
                                }

                                reportUnexpectedToken = true;
                                break;
                            }

                        case SyntaxKind.UsingKeyword:
                            if (isGlobalScript && this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                AddIncompleteMembers(ref pendingIncompleteMembers, ref body);

                                body.Members.Add(_syntaxFactory.GlobalStatement(ParseUsingStatement()));
                                seen = NamespaceParts.MembersAndStatements;
                            }
                            else
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBrace, ref body, ref initialBadNodes);

                                var @using = this.ParseUsingDirective();
                                if (seen > NamespaceParts.Usings)
                                {
                                    @using = this.AddError(@using, ErrorCode.ERR_UsingAfterElements);
                                    this.AddSkippedNamespaceText(ref openBrace, ref body, ref initialBadNodes, @using);
                                }
                                else
                                {
                                    body.Usings.Add(@using);
                                    seen = NamespaceParts.Usings;
                                }
                            }

                            reportUnexpectedToken = true;
                            break;

                        case SyntaxKind.OpenBracketToken:
                            if (this.IsPossibleGlobalAttributeDeclaration())
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBrace, ref body, ref initialBadNodes);

                                var attribute = this.ParseAttributeDeclaration();
                                if (!isGlobal || seen > NamespaceParts.GlobalAttributes)
                                {
                                    attribute = this.AddError(attribute, attribute.Target.Identifier, ErrorCode.ERR_GlobalAttributesNotFirst);
                                    this.AddSkippedNamespaceText(ref openBrace, ref body, ref initialBadNodes, attribute);
                                }
                                else
                                {
                                    body.Attributes.Add(attribute);
                                    seen = NamespaceParts.GlobalAttributes;
                                }

                                reportUnexpectedToken = true;
                                break;
                            }

                            goto default;

                        default:
                            var memberOrStatement = this.ParseMemberDeclarationOrStatement(parentKind);
                            if (memberOrStatement == null)
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBrace, ref body, ref initialBadNodes);

                                // eat one token and try to parse declaration or statement again:
                                var skippedToken = EatToken();
                                if (reportUnexpectedToken && !skippedToken.ContainsDiagnostics)
                                {
                                    skippedToken = this.AddError(skippedToken,
                                        IsScript ? ErrorCode.ERR_GlobalDefinitionOrStatementExpected : ErrorCode.ERR_EOFExpected);

                                    // do not report the error multiple times for subsequent tokens:
                                    reportUnexpectedToken = false;
                                }

                                this.AddSkippedNamespaceText(ref openBrace, ref body, ref initialBadNodes, skippedToken);
                            }
                            else if (memberOrStatement.Kind == SyntaxKind.IncompleteMember && seen < NamespaceParts.MembersAndStatements)
                            {
                                pendingIncompleteMembers.Add(memberOrStatement);
                                reportUnexpectedToken = true;
                            }
                            else
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                AddIncompleteMembers(ref pendingIncompleteMembers, ref body);

                                body.Members.Add(memberOrStatement);
                                seen = NamespaceParts.MembersAndStatements;
                                reportUnexpectedToken = true;
                            }
                            break;
                    }
                }
            }
            finally
            {
                _termState = saveTerm;

                // adds pending incomplete nodes:
                AddIncompleteMembers(ref pendingIncompleteMembers, ref body);
                _pool.Free(pendingIncompleteMembers);
            }
        }

        private static void AddIncompleteMembers(ref SyntaxListBuilder<MemberDeclarationSyntax> incompleteMembers, ref NamespaceBodyBuilder body)
        {
            if (incompleteMembers.Count > 0)
            {
                body.Members.AddRange(incompleteMembers);
                incompleteMembers.Clear();
            }
        }

        private void ReduceIncompleteMembers(ref SyntaxListBuilder<MemberDeclarationSyntax> incompleteMembers,
            ref SyntaxToken openBrace, ref NamespaceBodyBuilder body, ref SyntaxListBuilder initialBadNodes)
        {
            for (int i = 0; i < incompleteMembers.Count; i++)
            {
                this.AddSkippedNamespaceText(ref openBrace, ref body, ref initialBadNodes, incompleteMembers[i]);
            }
            incompleteMembers.Clear();
        }

        private bool IsPossibleNamespaceMemberDeclaration()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ExternKeyword:
                case SyntaxKind.UsingKeyword:
                case SyntaxKind.NamespaceKeyword:
                    return true;
                case SyntaxKind.IdentifierToken:
                    return IsCurrentTokenPartialKeywordOfPartialMethodOrType();
                default:
                    return IsPossibleStartOfTypeDeclaration(this.CurrentToken.Kind);
            }
        }

        public bool IsEndOfNamespace()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseBraceToken;
        }

        public bool IsGobalAttributesTerminator()
        {
            return this.IsEndOfNamespace()
                || this.IsPossibleNamespaceMemberDeclaration();
        }

        private bool IsNamespaceMemberStartOrStop()
        {
            return this.IsEndOfNamespace()
                || this.IsPossibleNamespaceMemberDeclaration();
        }

        /// <summary>
        /// Returns true if the lookahead tokens compose extern alias directive.
        /// </summary>
        private bool ScanExternAliasDirective()
        {
            // The check also includes the ending semicolon so that we can disambiguate among:
            //   extern alias goo;
            //   extern alias goo();
            //   extern alias goo { get; }

            return this.CurrentToken.Kind == SyntaxKind.ExternKeyword
                && this.PeekToken(1).Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).ContextualKind == SyntaxKind.AliasKeyword
                && this.PeekToken(2).Kind == SyntaxKind.IdentifierToken
                && this.PeekToken(3).Kind == SyntaxKind.SemicolonToken;
        }

        private ExternAliasDirectiveSyntax ParseExternAliasDirective()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.ExternAliasDirective)
            {
                return (ExternAliasDirectiveSyntax)this.EatNode();
            }

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ExternKeyword);

            var externToken = this.EatToken(SyntaxKind.ExternKeyword);
            var aliasToken = this.EatContextualToken(SyntaxKind.AliasKeyword);
            externToken = CheckFeatureAvailability(externToken, MessageID.IDS_FeatureExternAlias);

            var name = this.ParseIdentifierToken();

            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);

            return _syntaxFactory.ExternAliasDirective(externToken, aliasToken, name, semicolon);
        }

        private NameEqualsSyntax ParseNameEquals()
        {
            Debug.Assert(this.IsNamedAssignment());
            return _syntaxFactory.NameEquals(
                _syntaxFactory.IdentifierName(this.ParseIdentifierToken()),
                this.EatToken(SyntaxKind.EqualsToken));
        }

        private UsingDirectiveSyntax ParseUsingDirective()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.UsingDirective)
            {
                return (UsingDirectiveSyntax)this.EatNode();
            }

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.UsingKeyword);

            var usingToken = this.EatToken(SyntaxKind.UsingKeyword);
            var staticToken = this.TryEatToken(SyntaxKind.StaticKeyword);

            var alias = this.IsNamedAssignment() ? ParseNameEquals() : null;

            NameSyntax name;
            SyntaxToken semicolon;

            if (IsPossibleNamespaceMemberDeclaration())
            {
                //We're worried about the case where someone already has a correct program
                //and they've gone back to add a using directive, but have not finished the
                //new directive.  e.g.
                //
                //    using 
                //    namespace Goo {
                //        //...
                //    }
                //
                //If the token we see after "using" could be its own top-level construct, then
                //we just want to insert a missing identifier and semicolon and then return to
                //parsing at the top-level.
                //
                //NB: there's no way this could be true for a set of tokens that form a valid 
                //using directive, so there's no danger in checking the error case first.

                name = WithAdditionalDiagnostics(CreateMissingIdentifierName(), GetExpectedTokenError(SyntaxKind.IdentifierToken, this.CurrentToken.Kind));
                semicolon = SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken);
            }
            else
            {
                name = this.ParseQualifiedName();
                if (name.IsMissing && this.PeekToken(1).Kind == SyntaxKind.SemicolonToken)
                {
                    //if we can see a semicolon ahead, then the current token was
                    //probably supposed to be an identifier
                    name = AddTrailingSkippedSyntax(name, this.EatToken());
                }
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }

            var usingDirective = _syntaxFactory.UsingDirective(usingToken, staticToken, alias, name, semicolon);
            if (staticToken != default(SyntaxToken))
            {
                usingDirective = CheckFeatureAvailability(usingDirective, MessageID.IDS_FeatureUsingStatic);
            }

            return usingDirective;
        }

        private bool IsPossibleGlobalAttributeDeclaration()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken
                && IsGlobalAttributeTarget(this.PeekToken(1))
                && this.PeekToken(2).Kind == SyntaxKind.ColonToken;
        }

        private static bool IsGlobalAttributeTarget(SyntaxToken token)
        {
            switch (token.ToAttributeLocation())
            {
                case AttributeLocation.Assembly:
                case AttributeLocation.Module:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsPossibleAttributeDeclaration()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken;
        }

        private void ParseAttributeDeclarations(SyntaxListBuilder list)
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsAttributeDeclarationTerminator;

            while (this.IsPossibleAttributeDeclaration())
            {
                list.Add(this.ParseAttributeDeclaration());
            }

            _termState = saveTerm;
        }

        private bool IsAttributeDeclarationTerminator()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseBracketToken
                || this.IsPossibleAttributeDeclaration(); // start of a new one...
        }

        private AttributeListSyntax ParseAttributeDeclaration()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.AttributeList)
            {
                return (AttributeListSyntax)this.EatNode();
            }

            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);

            // Check for optional location :
            AttributeTargetSpecifierSyntax attrLocation = null;
            if (IsSomeWord(this.CurrentToken.Kind) && this.PeekToken(1).Kind == SyntaxKind.ColonToken)
            {
                var id = ConvertToKeyword(this.EatToken());
                var colon = this.EatToken(SyntaxKind.ColonToken);
                attrLocation = _syntaxFactory.AttributeTargetSpecifier(id, colon);
            }

            var attributes = _pool.AllocateSeparated<AttributeSyntax>();
            try
            {
                if (attrLocation != null && attrLocation.Identifier.ToAttributeLocation() == AttributeLocation.Module)
                {
                    attrLocation = CheckFeatureAvailability(attrLocation, MessageID.IDS_FeatureModuleAttrLoc);
                }

                this.ParseAttributes(attributes);
                var closeBracket = this.EatToken(SyntaxKind.CloseBracketToken);
                var declaration = _syntaxFactory.AttributeList(openBracket, attrLocation, attributes, closeBracket);

                return declaration;
            }
            finally
            {
                _pool.Free(attributes);
            }
        }

        private void ParseAttributes(SeparatedSyntaxListBuilder<AttributeSyntax> nodes)
        {
            // always expect at least one attribute
            nodes.Add(this.ParseAttribute());

            // remaining attributes
            while (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
            {
                if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // comma is optional, but if it is present it may be followed by another attribute
                    nodes.AddSeparator(this.EatToken());

                    // check for legal trailing comma
                    if (this.CurrentToken.Kind == SyntaxKind.CloseBracketToken)
                    {
                        break;
                    }

                    nodes.Add(this.ParseAttribute());
                }
                else if (this.IsPossibleAttribute())
                {
                    // report missing comma
                    nodes.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    nodes.Add(this.ParseAttribute());
                }
                else if (this.SkipBadAttributeListTokens(nodes, SyntaxKind.IdentifierToken) == PostSkipAction.Abort)
                {
                    break;
                }
            }
        }

        private PostSkipAction SkipBadAttributeListTokens(SeparatedSyntaxListBuilder<AttributeSyntax> list, SyntaxKind expected)
        {
            Debug.Assert(list.Count > 0);
            SyntaxToken tmp = null;
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleAttribute(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBracketToken || p.IsTerminator(),
                expected);
        }

        private bool IsPossibleAttribute()
        {
            return this.IsTrueIdentifier();
        }

        private AttributeSyntax ParseAttribute()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.Attribute)
            {
                return (AttributeSyntax)this.EatNode();
            }

            var name = this.ParseQualifiedName();

            var argList = this.ParseAttributeArgumentList();
            return _syntaxFactory.Attribute(name, argList);
        }

        internal AttributeArgumentListSyntax ParseAttributeArgumentList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.AttributeArgumentList)
            {
                return (AttributeArgumentListSyntax)this.EatNode();
            }

            AttributeArgumentListSyntax argList = null;
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                var argNodes = _pool.AllocateSeparated<AttributeArgumentSyntax>();
                try
                {
tryAgain:
                    if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
                    {
                        if (this.IsPossibleAttributeArgument() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                        {
                            // first argument
                            argNodes.Add(this.ParseAttributeArgument());

                            // comma + argument or end?
                            while (true)
                            {
                                if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken)
                                {
                                    break;
                                }
                                else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleAttributeArgument())
                                {
                                    argNodes.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                    argNodes.Add(this.ParseAttributeArgument());
                                }
                                else if (this.SkipBadAttributeArgumentTokens(ref openParen, argNodes, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                                {
                                    break;
                                }
                            }
                        }
                        else if (this.SkipBadAttributeArgumentTokens(ref openParen, argNodes, SyntaxKind.IdentifierToken) == PostSkipAction.Continue)
                        {
                            goto tryAgain;
                        }
                    }

                    var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                    argList = _syntaxFactory.AttributeArgumentList(openParen, argNodes, closeParen);
                }
                finally
                {
                    _pool.Free(argNodes);
                }
            }

            return argList;
        }

        private PostSkipAction SkipBadAttributeArgumentTokens(ref SyntaxToken openParen, SeparatedSyntaxListBuilder<AttributeArgumentSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openParen, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleAttributeArgument(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseParenToken || p.IsTerminator(),
                expected);
        }

        private bool IsPossibleAttributeArgument()
        {
            return this.IsPossibleExpression();
        }

        private AttributeArgumentSyntax ParseAttributeArgument()
        {
            // Need to parse both "real" named arguments and attribute-style named arguments.
            // We track attribute-style named arguments only with fShouldHaveName.

            NameEqualsSyntax nameEquals = null;
            NameColonSyntax nameColon = null;
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                SyntaxKind nextTokenKind = this.PeekToken(1).Kind;
                switch (nextTokenKind)
                {
                    case SyntaxKind.EqualsToken:
                        {
                            var name = this.ParseIdentifierToken();
                            var equals = this.EatToken(SyntaxKind.EqualsToken);
                            nameEquals = _syntaxFactory.NameEquals(_syntaxFactory.IdentifierName(name), equals);
                        }

                        break;
                    case SyntaxKind.ColonToken:
                        {
                            var name = this.ParseIdentifierName();
                            var colonToken = this.EatToken(SyntaxKind.ColonToken);
                            nameColon = _syntaxFactory.NameColon(name, colonToken);
                            nameColon = CheckFeatureAvailability(nameColon, MessageID.IDS_FeatureNamedArgument);
                        }
                        break;
                }
            }

            return _syntaxFactory.AttributeArgument(
                nameEquals, nameColon, this.ParseExpressionCore());
        }

        private static DeclarationModifiers GetModifier(SyntaxToken token)
            => GetModifier(token.Kind, token.ContextualKind);

        internal static DeclarationModifiers GetModifier(SyntaxKind kind, SyntaxKind contextualKind)
        {
            switch (kind)
            {
                case SyntaxKind.PublicKeyword:
                    return DeclarationModifiers.Public;
                case SyntaxKind.InternalKeyword:
                    return DeclarationModifiers.Internal;
                case SyntaxKind.ProtectedKeyword:
                    return DeclarationModifiers.Protected;
                case SyntaxKind.PrivateKeyword:
                    return DeclarationModifiers.Private;
                case SyntaxKind.SealedKeyword:
                    return DeclarationModifiers.Sealed;
                case SyntaxKind.AbstractKeyword:
                    return DeclarationModifiers.Abstract;
                case SyntaxKind.StaticKeyword:
                    return DeclarationModifiers.Static;
                case SyntaxKind.VirtualKeyword:
                    return DeclarationModifiers.Virtual;
                case SyntaxKind.ExternKeyword:
                    return DeclarationModifiers.Extern;
                case SyntaxKind.NewKeyword:
                    return DeclarationModifiers.New;
                case SyntaxKind.OverrideKeyword:
                    return DeclarationModifiers.Override;
                case SyntaxKind.ReadOnlyKeyword:
                    return DeclarationModifiers.ReadOnly;
                case SyntaxKind.VolatileKeyword:
                    return DeclarationModifiers.Volatile;
                case SyntaxKind.UnsafeKeyword:
                    return DeclarationModifiers.Unsafe;
                case SyntaxKind.PartialKeyword:
                    return DeclarationModifiers.Partial;
                case SyntaxKind.AsyncKeyword:
                    return DeclarationModifiers.Async;
                case SyntaxKind.RefKeyword:
                    return DeclarationModifiers.Ref;
                case SyntaxKind.IdentifierToken:
                    switch (contextualKind)
                    {
                        case SyntaxKind.PartialKeyword:
                            return DeclarationModifiers.Partial;
                        case SyntaxKind.AsyncKeyword:
                            return DeclarationModifiers.Async;
                    }

                    goto default;
                default:
                    return DeclarationModifiers.None;
            }
        }

        private void ParseModifiers(SyntaxListBuilder tokens, bool forAccessors)
        {
            while (true)
            {
                SyntaxToken modTok;
                switch (GetModifier(this.CurrentToken))
                {
                    case DeclarationModifiers.Partial:
                        {
                            var flags = ScanPartialTypeOrMember();
                            if (flags == ScanPartialFlags.NotModifier)
                            {
                                // This can't be a modifier.
                                return;
                            }

                            modTok = ConvertToKeyword(this.EatToken());
                            if (flags == ScanPartialFlags.TreatAsModifier)
                            {
                                // Not a partial type or member but we will parse it
                                // to report better diagnostics later in binding
                                break;
                            }

                            modTok = CheckFeatureAvailability(modTok,
                                flags switch
                                {
                                    ScanPartialFlags.PartialType => MessageID.IDS_FeatureRefPartialModOrdering,
                                    ScanPartialFlags.PartialTypeV8 => MessageID.IDS_FeaturePartialTypes,
                                    ScanPartialFlags.PartialMember => MessageID.IDS_FeatureRefPartialModOrdering,
                                    ScanPartialFlags.PartialMemberV8 => MessageID.IDS_FeaturePartialMethod,
                                    _ => throw ExceptionUtilities.UnexpectedValue(flags)
                                });

                            break;
                        }

                    case DeclarationModifiers.Ref:
                        {
                            var flags = ScanRefStruct(forAccessors);
                            if (flags == ScanRefStructFlags.NotModifier)
                            {
                                return;
                            }

                            modTok = this.EatToken();
                            if (flags == ScanRefStructFlags.TreatAsModifier)
                            {
                                // Not a ref struct but we will parse it
                                // to report better diagnostics later in binding
                                break;
                            }

                            modTok = CheckFeatureAvailability(modTok,
                                flags switch
                                {
                                    ScanRefStructFlags.RefStruct => MessageID.IDS_FeatureRefPartialModOrdering,
                                    ScanRefStructFlags.RefStructV8 => MessageID.IDS_FeatureRefStructs,
                                    _ => throw ExceptionUtilities.UnexpectedValue(flags)
                                });

                            break;
                        }

                    case DeclarationModifiers.Async:
                        if (!ShouldAsyncBeTreatedAsModifier(parsingStatementNotDeclaration: false))
                        {
                            return;
                        }

                        modTok = ConvertToKeyword(this.EatToken());
                        modTok = CheckFeatureAvailability(modTok, MessageID.IDS_FeatureAsync);
                        break;

                    case DeclarationModifiers.None:
                        return;

                    default:
                        modTok = this.EatToken();
                        break;
                }

                tokens.Add(modTok);
            }
        }

        private static bool IsContextualModifier(SyntaxToken token)
        {
            switch (token.ContextualKind)
            {
                case SyntaxKind.PartialKeyword:
                case SyntaxKind.AsyncKeyword:
                    return true;
            }

            return false;
        }

        // Returns true if the current token is probably a modifier.
        // To avoid further lookahead, caller is responsible to disambiguate some edge cases.
        // For instance, we return true for both of these:
        //
        //    partial partial<T>()
        //    partial partail<T> partial()
        //
        // While, in fact, 'partial' is only a modifier on the second method.
        private bool IsPossibleModifier()
        {
            if (!IsAnyModifier(this.CurrentToken))
            {
                return false;
            }

            if (IsContextualModifier(this.CurrentToken))
            {
                var tk = this.PeekToken(1);
                return tk.Kind == SyntaxKind.IdentifierToken ||
                    IsPredefinedType(tk.Kind) ||
                    IsTypeDeclarationStart(tk.Kind) || 
                    IsAnyModifier(tk);
            }

            return true;
        }

        private enum ScanRefStructFlags
        {
            /// <summary>
            /// Definitly a ref struct.
            /// </summary>
            RefStruct,
            /// <summary>
            /// Definitly a ref struct (C# 8.0)
            /// </summary>
            RefStructV8,
            /// <summary>
            /// Treat as modifier for better diagnostics to be reported during binding.
            /// </summary>
            TreatAsModifier,
            /// <summary>
            /// Not a modifier.
            /// </summary>
            NotModifier,
        }

        private ScanRefStructFlags ScanRefStruct(bool forAccessors)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.RefKeyword);
            for (int peekIndex = 1; ; peekIndex++)
            {
                var currentToken = this.PeekToken(peekIndex);
                if (!IsAnyModifier(currentToken))
                {
                    SyntaxToken prevToken;
                    return currentToken.Kind == SyntaxKind.StructKeyword
                        ? (prevToken = this.PeekToken(peekIndex - 1)).Kind == SyntaxKind.RefKeyword ||
                           (this.PeekToken(peekIndex - 2).Kind == SyntaxKind.RefKeyword && prevToken.ContextualKind == SyntaxKind.PartialKeyword)
                            ? ScanRefStructFlags.RefStructV8
                            : ScanRefStructFlags.RefStruct
                        : IsPossibleStartOfTypeDeclaration(currentToken.Kind) || (forAccessors && this.IsPossibleAccessorModifier())
                            ? ScanRefStructFlags.TreatAsModifier
                            : ScanRefStructFlags.NotModifier;
                }
            }
        }

        /// <summary>
        ///  Possible results from scanning for a partial member or type.
        /// </summary>
        private enum ScanPartialFlags
        {
            /// <summary>
            /// Definitly a partial type.
            /// </summary>
            PartialType,
            /// <summary>
            /// Definitly a partial type (C# 8.0)
            /// </summary>
            PartialTypeV8,
            /// <summary>
            /// Definitly a partial member.
            /// </summary>
            PartialMember,
            /// <summary>
            /// Definitly a partial member (C# 8.0)
            /// </summary>
            PartialMemberV8,
            /// <summary>
            /// Treat as modifier for better diagnostics to be reported during binding.
            /// </summary>
            TreatAsModifier,
            /// <summary>
            /// Not a modifier.
            /// </summary>
            NotModifier
        }

        private ScanPartialFlags ScanPartialTypeOrMember()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword);
            var point = this.GetResetPoint();
            try
            {
                SyntaxToken lastMod = EatToken();
                bool anyAdditionalModifiers = false;
                // Skip over additional modifiers
                if (IsPossibleModifier())
                {
                    anyAdditionalModifiers = true;
                    do
                    {
                        lastMod = EatToken();
                    }
                    while (IsPossibleModifier());
                }

                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.NamespaceKeyword:
                    case SyntaxKind.DelegateKeyword:
                    case SyntaxKind.EnumKeyword:
                        // Just treat as a modifier in erroneous cases.
                        // We'll report an error later during binding.
                        return ScanPartialFlags.TreatAsModifier;
                    case SyntaxKind.ClassKeyword:
                    case SyntaxKind.InterfaceKeyword:
                    case SyntaxKind.StructKeyword:
                        return lastMod.ContextualKind == SyntaxKind.PartialKeyword
                            ? ScanPartialFlags.PartialTypeV8
                            : ScanPartialFlags.PartialType;
                }

                // Scan for the return type or possibly the member name. See the next comment.
                if (this.ScanType() != ScanTypeFlags.NotType)
                {
                    // If the last additional modifier was a contextual keyword, it could actually
                    // be the return type of the member, in which case we have scanned for the member
                    // name above, and therefore IsPossiblePartialMemberStart returns false.
                    if (IsPossiblePartialMemberStart() || (anyAdditionalModifiers && IsContextualModifier(lastMod)))
                    {
                        return lastMod.ContextualKind == SyntaxKind.PartialKeyword
                            ? ScanPartialFlags.PartialMemberV8
                            : ScanPartialFlags.PartialMember;
                    }
                }

                return ScanPartialFlags.NotModifier;
            }
            finally
            {
                this.Reset(ref point);
                this.Release(ref point);
            }
        }

        private bool IsPossiblePartialMemberStart()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.ImplicitKeyword:
                case SyntaxKind.ExplicitKeyword:
                case SyntaxKind.OperatorKeyword:
                case SyntaxKind.EventKeyword:
                    return true;
            }

            return false;
        }

        private bool ShouldAsyncBeTreatedAsModifier(bool parsingStatementNotDeclaration)
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword);

            // Adapted from CParser::IsAsyncMethod.

            if (IsNonContextualModifier(PeekToken(1)))
            {
                // If the next token is a (non-contextual) modifier keyword, then this token is
                // definitely the async keyword
                return true;
            }

            // Some of our helpers start at the current token, so we'll have to advance for their
            // sake and then backtrack when we're done.  Don't leave this block without releasing
            // the reset point.
            ResetPoint resetPoint = GetResetPoint();

            try
            {
                this.EatToken(); //move past contextual 'async'

                if (!parsingStatementNotDeclaration &&
                    (this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword))
                {
                    this.EatToken(); // "partial" doesn't affect our decision, so look past it.
                }

                // Comment directly from CParser::IsAsyncMethod.
                // ... 'async' [partial] <typedecl> ...
                // ... 'async' [partial] <event> ...
                // ... 'async' [partial] <implicit> <operator> ...
                // ... 'async' [partial] <explicit> <operator> ...
                // ... 'async' [partial] <typename> <operator> ...
                // ... 'async' [partial] <typename> <membername> ...
                // DEVNOTE: Although we parse async user defined conversions, operators, etc. here,
                // anything other than async methods are detected as erroneous later, during the define phase

                if (!parsingStatementNotDeclaration)
                {
                    var ctk = this.CurrentToken.Kind;
                    if (IsPossibleStartOfTypeDeclaration(ctk) ||
                        ctk == SyntaxKind.EventKeyword ||
                        ((ctk == SyntaxKind.ExplicitKeyword || ctk == SyntaxKind.ImplicitKeyword) && PeekToken(1).Kind == SyntaxKind.OperatorKeyword))
                    {
                        return true;
                    }
                }

                if (ScanType() != ScanTypeFlags.NotType)
                {
                    // We've seen "async TypeName".  Now we have to determine if we should we treat 
                    // 'async' as a modifier.  Or is the user actually writing something like 
                    // "public async Goo" where 'async' is actually the return type.

                    if (IsPossibleMemberName())
                    {
                        // we have: "async Type X" or "async Type this", 'async' is definitely a 
                        // modifier here.
                        return true;
                    }

                    var currentTokenKind = this.CurrentToken.Kind;

                    // The file ends with "async TypeName", it's not legal code, and it's much 
                    // more likely that this is meant to be a modifier.
                    if (currentTokenKind == SyntaxKind.EndOfFileToken)
                    {
                        return true;
                    }

                    // "async TypeName }".  In this case, we just have an incomplete member, and 
                    // we should definitely default to 'async' being considered a return type here.
                    if (currentTokenKind == SyntaxKind.CloseBraceToken)
                    {
                        return true;
                    }

                    // "async TypeName void". In this case, we just have an incomplete member before
                    // an existing member.  Treat this 'async' as a keyword.
                    if (SyntaxFacts.IsPredefinedType(this.CurrentToken.Kind))
                    {
                        return true;
                    }

                    // "async TypeName public".  In this case, we just have an incomplete member before
                    // an existing member.  Treat this 'async' as a keyword.
                    if (IsNonContextualModifier(this.CurrentToken))
                    {
                        return true;
                    }

                    // "async TypeName class". In this case, we just have an incomplete member before
                    // an existing type declaration.  Treat this 'async' as a keyword.
                    if (IsTypeDeclarationStart())
                    {
                        return true;
                    }

                    // "async TypeName namespace". In this case, we just have an incomplete member before
                    // an existing namespace declaration.  Treat this 'async' as a keyword.
                    if (currentTokenKind == SyntaxKind.NamespaceKeyword)
                    {
                        return true;
                    }

                    if (!parsingStatementNotDeclaration && currentTokenKind == SyntaxKind.OperatorKeyword)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }

            return false;
        }

        private static bool IsNonContextualModifier(SyntaxToken nextToken)
        {
            return GetModifier(nextToken.Kind, contextualKind: SyntaxKind.None) != DeclarationModifiers.None;
        }

        private static bool IsAnyModifier(SyntaxToken nextToken)
        {
            return GetModifier(nextToken) != DeclarationModifiers.None;
        }

        private bool IsTypeDeclaration(out SyntaxToken typeDeclarationStart)
        {
            for (var peekIndex = 1; ; peekIndex++)
            {
                var currentToken = this.PeekToken(peekIndex);
                if (!IsAnyModifier(currentToken))
                {
                    typeDeclarationStart = currentToken;
                    return IsTypeDeclarationStart(typeDeclarationStart.Kind);
                }
            }
        }

        private bool IsPossibleMemberName()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.ThisKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private MemberDeclarationSyntax ParseTypeDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            cancellationToken.ThrowIfCancellationRequested();

            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ClassKeyword:
                    // report use of "static class" if feature is unsupported 
                    CheckForVersionSpecificModifiers(modifiers, SyntaxKind.StaticKeyword, MessageID.IDS_FeatureStaticClasses);
                    return this.ParseClassOrStructOrInterfaceDeclaration(attributes, modifiers);

                case SyntaxKind.StructKeyword:
                    // report use of "readonly struct" if feature is unsupported
                    CheckForVersionSpecificModifiers(modifiers, SyntaxKind.ReadOnlyKeyword, MessageID.IDS_FeatureReadOnlyStructs);
                    return this.ParseClassOrStructOrInterfaceDeclaration(attributes, modifiers);

                case SyntaxKind.InterfaceKeyword:
                    return this.ParseClassOrStructOrInterfaceDeclaration(attributes, modifiers);

                case SyntaxKind.DelegateKeyword:
                    return this.ParseDelegateDeclaration(attributes, modifiers);

                case SyntaxKind.EnumKeyword:
                    return this.ParseEnumDeclaration(attributes, modifiers);

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.CurrentToken.Kind);
            }
        }

        /// <summary>
        /// checks for modifiers whose feature is not available
        /// </summary>
        private void CheckForVersionSpecificModifiers(SyntaxListBuilder modifiers, SyntaxKind kind, MessageID feature)
        {
            for (int i = 0, n = modifiers.Count; i < n; i++)
            {
                if (modifiers[i].RawKind == (int)kind)
                {
                    modifiers[i] = CheckFeatureAvailability(modifiers[i], feature);
                }
            }
        }

        private TypeDeclarationSyntax ParseClassOrStructOrInterfaceDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ClassKeyword ||
                this.CurrentToken.Kind == SyntaxKind.StructKeyword ||
                this.CurrentToken.Kind == SyntaxKind.InterfaceKeyword);

            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            var classOrStructOrInterface = this.EatToken();
            var saveTerm = _termState;
            _termState |= TerminatorState.IsPossibleAggregateClauseStartOrStop;
            var name = this.ParseIdentifierToken();
            var typeParameters = this.ParseTypeParameterList();

            _termState = saveTerm;
            var baseList = this.ParseBaseList();

            // Parse class body
            bool parseMembers = true;
            SyntaxListBuilder<MemberDeclarationSyntax> members = default(SyntaxListBuilder<MemberDeclarationSyntax>);
            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            try
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                    this.ParseTypeParameterConstraintClauses(constraints);
                }

                var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

                // ignore members if missing type name or missing open curly
                if (name.IsMissing || openBrace.IsMissing)
                {
                    parseMembers = false;
                }

                // even if we saw a { or think we should parse members bail out early since
                // we know namespaces can't be nested inside types
                if (parseMembers)
                {
                    members = _pool.Allocate<MemberDeclarationSyntax>();

                    while (true)
                    {
                        SyntaxKind kind = this.CurrentToken.Kind;

                        if (CanStartMember(kind))
                        {
                            // This token can start a member -- go parse it
                            var saveTerm2 = _termState;
                            _termState |= TerminatorState.IsPossibleMemberStartOrStop;

                            var memberOrStatement = this.ParseMemberDeclarationOrStatement(classOrStructOrInterface.Kind);
                            if (memberOrStatement != null)
                            {
                                // statements are accepted here, a semantic error will be reported later
                                members.Add(memberOrStatement);
                            }
                            else
                            {
                                // we get here if we couldn't parse the lookahead as a statement or a declaration (we haven't consumed any tokens):
                                this.SkipBadMemberListTokens(ref openBrace, members);
                            }

                            _termState = saveTerm2;
                        }
                        else if (kind == SyntaxKind.CloseBraceToken || kind == SyntaxKind.EndOfFileToken || this.IsTerminator())
                        {
                            // This marks the end of members of this class
                            break;
                        }
                        else
                        {
                            // Error -- try to sync up with intended reality
                            this.SkipBadMemberListTokens(ref openBrace, members);
                        }
                    }
                }

                SyntaxToken closeBrace;
                if (openBrace.IsMissing)
                {
                    closeBrace = SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken);
                    closeBrace = WithAdditionalDiagnostics(closeBrace, this.GetExpectedTokenError(SyntaxKind.CloseBraceToken, this.CurrentToken.Kind));
                }
                else
                {
                    closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
                }

                var semicolon = TryEatToken(SyntaxKind.SemicolonToken);
                switch (classOrStructOrInterface.Kind)
                {
                    case SyntaxKind.ClassKeyword:
                        return _syntaxFactory.ClassDeclaration(
                            attributes,
                            modifiers.ToList(),
                            classOrStructOrInterface,
                            name,
                            typeParameters,
                            baseList,
                            constraints,
                            openBrace,
                            members,
                            closeBrace,
                            semicolon);

                    case SyntaxKind.StructKeyword:
                        return _syntaxFactory.StructDeclaration(
                            attributes,
                            modifiers.ToList(),
                            classOrStructOrInterface,
                            name,
                            typeParameters,
                            baseList,
                            constraints,
                            openBrace,
                            members,
                            closeBrace,
                            semicolon);

                    case SyntaxKind.InterfaceKeyword:
                        return _syntaxFactory.InterfaceDeclaration(
                            attributes,
                            modifiers.ToList(),
                            classOrStructOrInterface,
                            name,
                            typeParameters,
                            baseList,
                            constraints,
                            openBrace,
                            members,
                            closeBrace,
                            semicolon);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(classOrStructOrInterface.Kind);
                }
            }
            finally
            {
                if (!members.IsNull)
                {
                    _pool.Free(members);
                }

                if (!constraints.IsNull)
                {
                    _pool.Free(constraints);
                }
            }
        }

        private void SkipBadMemberListTokens(ref SyntaxToken openBrace, SyntaxListBuilder members)
        {
            if (members.Count > 0)
            {
                var tmp = members[members.Count - 1];
                this.SkipBadMemberListTokens(ref tmp);
                members[members.Count - 1] = tmp;
            }
            else
            {
                GreenNode tmp = openBrace;
                this.SkipBadMemberListTokens(ref tmp);
                openBrace = (SyntaxToken)tmp;
            }
        }

        private void SkipBadMemberListTokens(ref GreenNode previousNode)
        {
            int curlyCount = 0;
            var tokens = _pool.Allocate();
            try
            {
                bool done = false;

                // always consume at least one token.
                var token = this.EatToken();
                token = this.AddError(token, ErrorCode.ERR_InvalidMemberDecl, token.Text);
                tokens.Add(token);

                while (!done)
                {
                    SyntaxKind kind = this.CurrentToken.Kind;

                    // If this token can start a member, we're done
                    if (CanStartMember(kind) &&
                        !(kind == SyntaxKind.DelegateKeyword && (this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken || this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)))
                    {
                        done = true;
                        continue;
                    }

                    // <UNDONE>  UNDONE: Seems like this makes sense, 
                    // but if this token can start a namespace element, but not a member, then
                    // perhaps we should bail back up to parsing a namespace body somehow...</UNDONE>

                    // Watch curlies and look for end of file/close curly
                    switch (kind)
                    {
                        case SyntaxKind.OpenBraceToken:
                            curlyCount++;
                            break;

                        case SyntaxKind.CloseBraceToken:
                            if (curlyCount-- == 0)
                            {
                                done = true;
                                continue;
                            }

                            break;

                        case SyntaxKind.EndOfFileToken:
                            done = true;
                            continue;

                        default:
                            break;
                    }

                    tokens.Add(this.EatToken());
                }

                previousNode = AddTrailingSkippedSyntax((CSharpSyntaxNode)previousNode, tokens.ToListNode());
            }
            finally
            {
                _pool.Free(tokens);
            }
        }

        private bool IsPossibleMemberStartOrStop()
        {
            return this.IsPossibleMemberStart() || this.CurrentToken.Kind == SyntaxKind.CloseBraceToken;
        }

        private bool IsPossibleAggregateClauseStartOrStop()
        {
            return this.CurrentToken.Kind == SyntaxKind.ColonToken
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                || this.IsCurrentTokenWhereOfConstraintClause();
        }

        private BaseListSyntax ParseBaseList()
        {
            if (this.CurrentToken.Kind != SyntaxKind.ColonToken)
            {
                return null;
            }

            var colon = this.EatToken();
            var list = _pool.AllocateSeparated<BaseTypeSyntax>();
            try
            {
                // first type
                TypeSyntax firstType = this.ParseType();
                list.Add(_syntaxFactory.SimpleBaseType(firstType));

                // any additional types
                while (true)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken ||
                        this.IsCurrentTokenWhereOfConstraintClause())
                    {
                        break;
                    }
                    else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleType())
                    {
                        list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                        list.Add(_syntaxFactory.SimpleBaseType(this.ParseType()));
                        continue;
                    }
                    else if (this.SkipBadBaseListTokens(ref colon, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                    {
                        break;
                    }
                }

                return _syntaxFactory.BaseList(colon, list);
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private PostSkipAction SkipBadBaseListTokens(ref SyntaxToken colon, SeparatedSyntaxListBuilder<BaseTypeSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref colon, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleAttribute(),
                p => p.CurrentToken.Kind == SyntaxKind.OpenBraceToken || p.IsCurrentTokenWhereOfConstraintClause() || p.IsTerminator(),
                expected);
        }

        private bool IsCurrentTokenWhereOfConstraintClause()
        {
            return
                this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword &&
                this.PeekToken(1).Kind == SyntaxKind.IdentifierToken &&
                this.PeekToken(2).Kind == SyntaxKind.ColonToken;
        }

        private void ParseTypeParameterConstraintClauses(SyntaxListBuilder list)
        {
            while (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
            {
                list.Add(this.ParseTypeParameterConstraintClause());
            }
        }

        private TypeParameterConstraintClauseSyntax ParseTypeParameterConstraintClause()
        {
            var where = this.EatContextualToken(SyntaxKind.WhereKeyword);
            var name = !IsTrueIdentifier()
                ? this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_IdentifierExpected)
                : this.ParseIdentifierName();

            var colon = this.EatToken(SyntaxKind.ColonToken);

            var bounds = _pool.AllocateSeparated<TypeParameterConstraintSyntax>();
            try
            {
                // first bound
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsCurrentTokenWhereOfConstraintClause())
                {
                    bounds.Add(_syntaxFactory.TypeConstraint(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                }
                else
                {
                    bounds.Add(this.ParseTypeParameterConstraint());

                    // remaining bounds
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                            || this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken
                            || this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleTypeParameterConstraint())
                        {
                            bounds.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            if (this.IsCurrentTokenWhereOfConstraintClause())
                            {
                                bounds.Add(_syntaxFactory.TypeConstraint(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                                break;
                            }
                            else
                            {
                                bounds.Add(this.ParseTypeParameterConstraint());
                            }
                        }
                        else if (this.SkipBadTypeParameterConstraintTokens(bounds, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }

                return _syntaxFactory.TypeParameterConstraintClause(where, name, colon, bounds);
            }
            finally
            {
                _pool.Free(bounds);
            }
        }

        private bool IsPossibleTypeParameterConstraint()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.NewKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.StructKeyword:
                    return true;
                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();
                default:
                    return IsPredefinedType(this.CurrentToken.Kind);
            }
        }

        private TypeParameterConstraintSyntax ParseTypeParameterConstraint()
        {
            SyntaxToken questionToken = null;
            var syntaxKind = this.CurrentToken.Kind;

            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.NewKeyword:
                    var newToken = this.EatToken();
                    var open = this.EatToken(SyntaxKind.OpenParenToken);
                    var close = this.EatToken(SyntaxKind.CloseParenToken);
                    return _syntaxFactory.ConstructorConstraint(newToken, open, close);
                case SyntaxKind.StructKeyword:
                    var structToken = this.EatToken();

                    if (this.CurrentToken.Kind == SyntaxKind.QuestionToken)
                    {
                        questionToken = this.EatToken();
                        questionToken = this.AddError(questionToken, ErrorCode.ERR_UnexpectedToken, questionToken.Text);
                    }

                    return _syntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint, structToken, questionToken);
                case SyntaxKind.ClassKeyword:
                    var classToken = this.EatToken();
                    questionToken = this.TryEatToken(SyntaxKind.QuestionToken);

                    return _syntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint, classToken, questionToken);
                default:
                    var type = this.ParseType();
                    return _syntaxFactory.TypeConstraint(type);
            }
        }

        private PostSkipAction SkipBadTypeParameterConstraintTokens(SeparatedSyntaxListBuilder<TypeParameterConstraintSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => this.CurrentToken.Kind != SyntaxKind.CommaToken && !this.IsPossibleTypeParameterConstraint(),
                p => this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsCurrentTokenWhereOfConstraintClause() || this.IsTerminator(),
                expected);
        }

        private bool IsPossibleMemberStart()
        {
            return CanStartMember(this.CurrentToken.Kind);
        }

        private static bool CanStartMember(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AbstractKeyword:
                case SyntaxKind.BoolKeyword:
                case SyntaxKind.ByteKeyword:
                case SyntaxKind.CharKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.DecimalKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.DoubleKeyword:
                case SyntaxKind.EnumKeyword:
                case SyntaxKind.EventKeyword:
                case SyntaxKind.ExternKeyword:
                case SyntaxKind.FixedKeyword:
                case SyntaxKind.FloatKeyword:
                case SyntaxKind.IntKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.LongKeyword:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.ObjectKeyword:
                case SyntaxKind.OverrideKeyword:
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                case SyntaxKind.SByteKeyword:
                case SyntaxKind.SealedKeyword:
                case SyntaxKind.ShortKeyword:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.StringKeyword:
                case SyntaxKind.StructKeyword:
                case SyntaxKind.UIntKeyword:
                case SyntaxKind.ULongKeyword:
                case SyntaxKind.UnsafeKeyword:
                case SyntaxKind.UShortKeyword:
                case SyntaxKind.VirtualKeyword:
                case SyntaxKind.VoidKeyword:
                case SyntaxKind.VolatileKeyword:
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.TildeToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.ImplicitKeyword:
                case SyntaxKind.ExplicitKeyword:
                case SyntaxKind.OpenParenToken:    //tuple
                case SyntaxKind.RefKeyword:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsTypeDeclarationStart(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.EnumKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.StructKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsTypeDeclarationStart()
        {
            return IsTypeDeclarationStart(this.CurrentToken.Kind);
        }

        private static bool CanReuseMemberDeclaration(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.NamespaceDeclaration:
                    return true;
                default:
                    return false;
            }
        }

        public MemberDeclarationSyntax ParseMemberDeclaration()
        {
            // Use a parent kind that causes inclusion of only member declarations that could appear in a struct
            // e.g. including fixed member declarations, but not statements.
            const SyntaxKind parentKind = SyntaxKind.StructDeclaration;
            return ParseWithStackGuard(
                () => this.ParseMemberDeclarationOrStatement(parentKind),
                () => createEmptyNodeFunc());

            // Creates a dummy declaration node to which we can attach a stack overflow message
            MemberDeclarationSyntax createEmptyNodeFunc()
            {
                return _syntaxFactory.IncompleteMember(
                    new SyntaxList<AttributeListSyntax>(),
                    new SyntaxList<SyntaxToken>(),
                    CreateMissingIdentifierName()
                    );
            }
        }

        // Returns null if we can't parse anything (even partially).
        internal MemberDeclarationSyntax ParseMemberDeclarationOrStatement(SyntaxKind parentKind)
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            var result = ParseMemberDeclarationOrStatementCore(parentKind);
            _recursionDepth--;
            return result;
        }

        // Returns null if we can't parse anything (even partially).
        private MemberDeclarationSyntax ParseMemberDeclarationOrStatementCore(SyntaxKind parentKind)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            cancellationToken.ThrowIfCancellationRequested();

            bool isGlobalScript = parentKind == SyntaxKind.CompilationUnit && this.IsScript;
            bool acceptStatement = isGlobalScript;

            // don't reuse members if they were previously declared under a different type keyword kind
            if (this.IsIncrementalAndFactoryContextMatches)
            {
                if (CanReuseMemberDeclaration(CurrentNodeKind))
                {
                    return (MemberDeclarationSyntax)this.EatNode();
                }
            }

            var attributes = _pool.Allocate<AttributeListSyntax>();
            var modifiers = _pool.Allocate();

            var saveTermState = _termState;

            try
            {
                this.ParseAttributeDeclarations(attributes);

                if (attributes.Count > 0)
                {
                    acceptStatement = false;
                }

                //
                // Check for the following cases to disambiguate between member declarations and expressions.
                // Doing this before parsing modifiers simplifies further analysis since some of these keywords can act as modifiers as well.
                //
                // unsafe { ... }
                // fixed (...) { ... } 
                // delegate (...) { ... }
                // delegate { ... }
                // new { ... }
                // new[] { ... }
                // new T (...)
                // new T [...]
                //
                if (acceptStatement)
                {
                    switch (this.CurrentToken.Kind)
                    {
                        case SyntaxKind.UnsafeKeyword:
                            if (this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken)
                            {
                                return _syntaxFactory.GlobalStatement(ParseUnsafeStatement());
                            }
                            break;

                        case SyntaxKind.FixedKeyword:
                            if (this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
                            {
                                return _syntaxFactory.GlobalStatement(ParseFixedStatement());
                            }
                            break;

                        case SyntaxKind.DelegateKeyword:
                            switch (this.PeekToken(1).Kind)
                            {
                                case SyntaxKind.OpenParenToken:
                                case SyntaxKind.OpenBraceToken:
                                    return _syntaxFactory.GlobalStatement(ParseExpressionStatement());
                            }
                            break;

                        case SyntaxKind.NewKeyword:
                            if (IsPossibleNewExpression())
                            {
                                return _syntaxFactory.GlobalStatement(ParseExpressionStatement());
                            }
                            break;
                    }
                }

                // All modifiers that might start an expression are processed above.
                this.ParseModifiers(modifiers, forAccessors: false);
                if (modifiers.Count > 0)
                {
                    acceptStatement = false;
                }

                // Check for constructor form
                if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
                {
                    // Script: 
                    // Constructor definitions are not allowed. We parse them as method calls with semicolon missing error:
                    //
                    // Script(...) { ... } 
                    //            ^
                    //            missing ';'
                    if (!isGlobalScript)
                    {
                        return this.ParseConstructorDeclaration(attributes, modifiers);
                    }

                    // Script: 
                    // Unless there modifiers or attributes are present this is more likely to be a method call than a method definition.
                    if (!acceptStatement)
                    {
                        var token = SyntaxFactory.MissingToken(SyntaxKind.VoidKeyword);
                        token = this.AddError(token, ErrorCode.ERR_MemberNeedsType);
                        var voidType = _syntaxFactory.PredefinedType(token);

                        var identifier = this.EatToken();

                        return this.ParseMethodDeclaration(attributes, modifiers, voidType, explicitInterfaceOpt: null, identifier: identifier, typeParameterList: null);
                    }
                }

                // Check for destructor form
                // TODO: better error messages for script
                if (!isGlobalScript && this.CurrentToken.Kind == SyntaxKind.TildeToken)
                {
                    return this.ParseDestructorDeclaration(attributes, modifiers);
                }

                // Check for constant (prefers const field over const local variable decl)
                if (this.CurrentToken.Kind == SyntaxKind.ConstKeyword)
                {
                    return this.ParseConstantFieldDeclaration(attributes, modifiers, parentKind);
                }

                // Check for event.
                if (this.CurrentToken.Kind == SyntaxKind.EventKeyword)
                {
                    return this.ParseEventDeclaration(attributes, modifiers, parentKind);
                }

                // check for fixed size buffers.
                if (this.CurrentToken.Kind == SyntaxKind.FixedKeyword)
                {
                    return this.ParseFixedSizeBufferDeclaration(attributes, modifiers, parentKind);
                }

                // Check for conversion operators (implicit/explicit)
                if (this.CurrentToken.Kind == SyntaxKind.ExplicitKeyword ||
                    this.CurrentToken.Kind == SyntaxKind.ImplicitKeyword ||
                        (this.CurrentToken.Kind == SyntaxKind.OperatorKeyword && !SyntaxFacts.IsAnyOverloadableOperator(this.PeekToken(1).Kind)))
                {
                    return this.ParseConversionOperatorDeclaration(attributes, modifiers);
                }

                if (this.CurrentToken.Kind == SyntaxKind.NamespaceKeyword &&
                    parentKind == SyntaxKind.CompilationUnit)
                {
                    return ParseNamespaceDeclaration(attributes, modifiers);
                }

                // It's valid to have a type declaration here -- check for those
                if (IsTypeDeclarationStart())
                {
                    return this.ParseTypeDeclaration(attributes, modifiers);
                }

                if (acceptStatement &&
                    this.CurrentToken.Kind != SyntaxKind.CloseBraceToken &&
                    this.CurrentToken.Kind != SyntaxKind.EndOfFileToken &&
                    this.IsPossibleStatement(acceptAccessibilityMods: true))
                {
                    var saveTerm = _termState;
                    _termState |= TerminatorState.IsPossibleStatementStartOrStop; // partial statements can abort if a new statement starts

                    // Any expression is allowed, not just expression statements:
                    var statement = this.ParseStatementNoDeclaration(allowAnyExpression: true);

                    _termState = saveTerm;
                    if (statement != null)
                    {
                        return _syntaxFactory.GlobalStatement(statement);
                    }
                }

                // Everything that's left -- methods, fields, properties, 
                // indexers, and non-conversion operators -- starts with a type 
                // (possibly void). Parse that.
                TypeSyntax type = ParseReturnType();

                var afterTypeResetPoint = this.GetResetPoint();

                try
                {
                    var sawRef = type.Kind == SyntaxKind.RefType;

                    // Check for misplaced modifiers.  if we see any, then consider this member
                    // terminated and restart parsing.
                    if (GetModifier(this.CurrentToken) != DeclarationModifiers.None &&
                        this.CurrentToken.ContextualKind != SyntaxKind.PartialKeyword &&
                        this.CurrentToken.ContextualKind != SyntaxKind.AsyncKeyword &&
                        IsComplete(type))
                    {
                        var misplacedModifier = this.CurrentToken;
                        type = this.AddError(
                            type,
                            type.FullWidth + misplacedModifier.GetLeadingTriviaWidth(),
                            misplacedModifier.Width,
                            ErrorCode.ERR_BadModifierLocation,
                            misplacedModifier.Text);

                        return _syntaxFactory.IncompleteMember(attributes, modifiers.ToList(), type);
                    }

parse_member_name:;
                    // If we've seen the ref keyword, we know we must have an indexer, method, or property.
                    if (!sawRef)
                    {
                        // Check here for operators
                        // Allow old-style implicit/explicit casting operator syntax, just so we can give a better error
                        if (IsOperatorKeyword())
                        {
                            return this.ParseOperatorDeclaration(attributes, modifiers, type);
                        }

                        if (IsFieldDeclaration(isEvent: false))
                        {
                            if (acceptStatement)
                            {
                                // if we are script at top-level then statements can occur
                                _termState |= TerminatorState.IsPossibleStatementStartOrStop;
                            }

                            return this.ParseNormalFieldDeclaration(attributes, modifiers, type, parentKind);
                        }
                    }

                    // At this point we can either have indexers, methods, or 
                    // properties (or something unknown).  Try to break apart
                    // the following name and determine what to do from there.
                    ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt;
                    SyntaxToken identifierOrThisOpt;
                    TypeParameterListSyntax typeParameterListOpt;
                    this.ParseMemberName(out explicitInterfaceOpt, out identifierOrThisOpt, out typeParameterListOpt, isEvent: false);

                    // First, check if we got absolutely nothing.  If so, then 
                    // We need to consume a bad member and try again.
                    if (explicitInterfaceOpt == null && identifierOrThisOpt == null && typeParameterListOpt == null)
                    {
                        if (attributes.Count == 0 && modifiers.Count == 0 && type.IsMissing && !sawRef)
                        {
                            // we haven't advanced, the caller needs to consume the tokens ahead
                            return null;
                        }

                        var incompleteMember = _syntaxFactory.IncompleteMember(attributes, modifiers.ToList(), type.IsMissing ? null : type);
                        if (incompleteMember.ContainsDiagnostics)
                        {
                            return incompleteMember;
                        }
                        else if (parentKind == SyntaxKind.NamespaceDeclaration ||
                                 parentKind == SyntaxKind.CompilationUnit && !IsScript)
                        {
                            return this.AddErrorToLastToken(incompleteMember, ErrorCode.ERR_NamespaceUnexpected);
                        }
                        else
                        {
                            //the error position should indicate CurrentToken
                            return this.AddError(
                                incompleteMember,
                                incompleteMember.FullWidth + this.CurrentToken.GetLeadingTriviaWidth(),
                                this.CurrentToken.Width,
                                ErrorCode.ERR_InvalidMemberDecl,
                                this.CurrentToken.Text);
                        }
                    }

                    // If the modifiers did not include "async", and the type we got was "async", and there was an
                    // error in the identifier or its type parameters, then the user is probably in the midst of typing
                    // an async method.  In that case we reconsider "async" to be a modifier, and treat the identifier
                    // (with the type parameters) as the type (with type arguments).  Then we go back to looking for
                    // the member name again.
                    // For example, if we get
                    //     async Task<
                    // then we want async to be a modifier and Task<MISSING> to be a type.
                    if (!sawRef &&
                        identifierOrThisOpt != null &&
                        (typeParameterListOpt != null && typeParameterListOpt.ContainsDiagnostics
                          || this.CurrentToken.Kind != SyntaxKind.OpenParenToken && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken && this.CurrentToken.Kind != SyntaxKind.EqualsGreaterThanToken) &&
                        ReconsiderTypeAsAsyncModifier(ref modifiers, type, identifierOrThisOpt))
                    {
                        this.Reset(ref afterTypeResetPoint);
                        explicitInterfaceOpt = null;
                        identifierOrThisOpt = default;
                        typeParameterListOpt = null;
                        type = ParseReturnType();
                        goto parse_member_name;
                    }

                    Debug.Assert(identifierOrThisOpt != null);

                    // check availability of readonly members feature for indexers, properties and methods
                    CheckForVersionSpecificModifiers(modifiers, SyntaxKind.ReadOnlyKeyword, MessageID.IDS_FeatureReadOnlyMembers);

                    if (identifierOrThisOpt.Kind == SyntaxKind.ThisKeyword)
                    {
                        return this.ParseIndexerDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);
                    }
                    else
                    {
                        switch (this.CurrentToken.Kind)
                        {
                            case SyntaxKind.OpenBraceToken:
                            case SyntaxKind.EqualsGreaterThanToken:
                                return this.ParsePropertyDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);

                            default:
                                // treat anything else as a method.
                                return this.ParseMethodDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);
                        }
                    }
                }
                finally
                {
                    this.Release(ref afterTypeResetPoint);
                }
            }
            finally
            {
                _pool.Free(modifiers);
                _pool.Free(attributes);
                _termState = saveTermState;
            }
        }

        // if the modifiers do not contain async or replace and the type is the identifier "async" or "replace", then
        // add that identifier to the modifiers and assign a new type from the identifierOrThisOpt and the
        // type parameter list
        private bool ReconsiderTypeAsAsyncModifier(
            ref SyntaxListBuilder modifiers,
            TypeSyntax type,
            SyntaxToken identifierOrThisOpt)
        {
            if (type.Kind != SyntaxKind.IdentifierName) return false;
            if (identifierOrThisOpt.Kind != SyntaxKind.IdentifierToken) return false;

            var identifier = ((IdentifierNameSyntax)type).Identifier;
            var contextualKind = identifier.ContextualKind;
            if (contextualKind != SyntaxKind.AsyncKeyword ||
                modifiers.Any((int)contextualKind))
            {
                return false;
            }

            modifiers.Add(ConvertToKeyword(identifier));
            return true;
        }

        private bool IsFieldDeclaration(bool isEvent)
        {
            if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
            {
                return false;
            }

            // Treat this as a field, unless we have anything following that
            // makes us:
            //   a) explicit
            //   b) generic
            //   c) a property
            //   d) a method (unless we already know we're parsing an event)
            var kind = this.PeekToken(1).Kind;
            switch (kind)
            {
                case SyntaxKind.DotToken:                   // Goo.     explicit
                case SyntaxKind.ColonColonToken:            // Goo::    explicit
                case SyntaxKind.LessThanToken:            // Goo<     explicit or generic method
                case SyntaxKind.OpenBraceToken:        // Goo {    property
                case SyntaxKind.EqualsGreaterThanToken:     // Goo =>   property
                    return false;
                case SyntaxKind.OpenParenToken:             // Goo(     method
                    return isEvent;
                default:
                    return true;
            }
        }

        private bool IsOperatorKeyword()
        {
            return
                this.CurrentToken.Kind == SyntaxKind.ImplicitKeyword ||
                this.CurrentToken.Kind == SyntaxKind.ExplicitKeyword ||
                this.CurrentToken.Kind == SyntaxKind.OperatorKeyword;
        }

        public static bool IsComplete(CSharpSyntaxNode node)
        {
            if (node == null)
            {
                return false;
            }

            foreach (var child in node.ChildNodesAndTokens().Reverse())
            {
                var token = child as SyntaxToken;
                if (token == null)
                {
                    return IsComplete((CSharpSyntaxNode)child);
                }

                if (token.IsMissing)
                {
                    return false;
                }

                if (token.Kind != SyntaxKind.None)
                {
                    return true;
                }

                // if token was optional, consider the next one..
            }

            return true;
        }

        private ConstructorDeclarationSyntax ParseConstructorDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            var name = this.ParseIdentifierToken();
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;
            try
            {
                var paramList = this.ParseParenthesizedParameterList();

                ConstructorInitializerSyntax initializer = this.CurrentToken.Kind == SyntaxKind.ColonToken
                    ? this.ParseConstructorInitializer()
                    : null;

                this.ParseBlockAndExpressionBodiesWithSemicolon(
                    out BlockSyntax body, out ArrowExpressionClauseSyntax expressionBody, out SyntaxToken semicolon,
                    requestedExpressionBodyFeature: MessageID.IDS_FeatureExpressionBodiedDeOrConstructor);

                return _syntaxFactory.ConstructorDeclaration(attributes, modifiers.ToList(), name, paramList, initializer, body, expressionBody, semicolon);
            }
            finally
            {
                _termState = saveTerm;
            }
        }

        private ConstructorInitializerSyntax ParseConstructorInitializer()
        {
            var colon = this.EatToken(SyntaxKind.ColonToken);

            var reportError = true;
            var kind = this.CurrentToken.Kind == SyntaxKind.BaseKeyword
                ? SyntaxKind.BaseConstructorInitializer
                : SyntaxKind.ThisConstructorInitializer;

            SyntaxToken token;
            if (this.CurrentToken.Kind == SyntaxKind.BaseKeyword || this.CurrentToken.Kind == SyntaxKind.ThisKeyword)
            {
                token = this.EatToken();
            }
            else
            {
                token = this.EatToken(SyntaxKind.ThisKeyword, ErrorCode.ERR_ThisOrBaseExpected);

                // No need to report further errors at this point:
                reportError = false;
            }

            ArgumentListSyntax argumentList;
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                argumentList = this.ParseParenthesizedArgumentList();
            }
            else
            {
                var openToken = this.EatToken(SyntaxKind.OpenParenToken, reportError);
                var closeToken = this.EatToken(SyntaxKind.CloseParenToken, reportError);
                argumentList = _syntaxFactory.ArgumentList(openToken, default(SeparatedSyntaxList<ArgumentSyntax>), closeToken);
            }

            return _syntaxFactory.ConstructorInitializer(kind, colon, token, argumentList);
        }

        private DestructorDeclarationSyntax ParseDestructorDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.TildeToken);
            var tilde = this.EatToken(SyntaxKind.TildeToken);

            var name = this.ParseIdentifierToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            this.ParseBlockAndExpressionBodiesWithSemicolon(
                out BlockSyntax body, out ArrowExpressionClauseSyntax expressionBody, out SyntaxToken semicolon,
                requestedExpressionBodyFeature: MessageID.IDS_FeatureExpressionBodiedDeOrConstructor);

            var parameterList = _syntaxFactory.ParameterList(openParen, default(SeparatedSyntaxList<ParameterSyntax>), closeParen);

            return _syntaxFactory.DestructorDeclaration(attributes, modifiers.ToList(), tilde, name, parameterList, body, expressionBody, semicolon);
        }

        /// <summary>
        /// Parses any block or expression bodies that are present. Also parses
        /// the trailing semicolon if one is present.
        /// </summary>
        private void ParseBlockAndExpressionBodiesWithSemicolon(
            out BlockSyntax blockBody,
            out ArrowExpressionClauseSyntax expressionBody,
            out SyntaxToken semicolon,
            bool parseSemicolonAfterBlock = true,
            MessageID requestedExpressionBodyFeature = MessageID.IDS_FeatureExpressionBodiedMethod)
        {
            // Check for 'forward' declarations with no block of any kind
            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                blockBody = null;
                expressionBody = null;
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return;
            }

            blockBody = null;
            expressionBody = null;

            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                blockBody = this.ParseBlock(isMethodBody: true);
            }

            if (this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                Debug.Assert(requestedExpressionBodyFeature == MessageID.IDS_FeatureExpressionBodiedMethod
                                || requestedExpressionBodyFeature == MessageID.IDS_FeatureExpressionBodiedAccessor
                                || requestedExpressionBodyFeature == MessageID.IDS_FeatureExpressionBodiedDeOrConstructor,
                                "Only IDS_FeatureExpressionBodiedMethod, IDS_FeatureExpressionBodiedAccessor or IDS_FeatureExpressionBodiedDeOrConstructor can be requested");
                expressionBody = this.ParseArrowExpressionClause();
                expressionBody = CheckFeatureAvailability(expressionBody, requestedExpressionBodyFeature);
            }

            semicolon = null;
            // Expression-bodies need semicolons and native behavior
            // expects a semicolon if there is no body
            if (expressionBody != null || blockBody == null)
            {
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            // Check for bad semicolon after block body
            else if (parseSemicolonAfterBlock && this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = this.EatTokenWithPrejudice(ErrorCode.ERR_UnexpectedSemicolon);
            }
        }

        private void ParseBodyOrSemicolon(out BlockSyntax body, out SyntaxToken semicolon)
        {
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                body = this.ParseBlock(isMethodBody: true);

                semicolon = null;
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    semicolon = this.EatTokenWithPrejudice(ErrorCode.ERR_UnexpectedSemicolon);
                }
            }
            else
            {
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                body = null;
            }
        }

        private bool IsEndOfTypeParameterList()
        {
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                // void Goo<T (
                return true;
            }

            if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                // class C<T :
                return true;
            }

            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                // class C<T {
                return true;
            }

            if (IsCurrentTokenWhereOfConstraintClause())
            {
                // class C<T where T :
                return true;
            }

            return false;
        }

        private bool IsEndOfMethodSignature()
        {
            return this.CurrentToken.Kind == SyntaxKind.SemicolonToken || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;
        }

        private bool IsEndOfNameInExplicitInterface()
        {
            return this.CurrentToken.Kind == SyntaxKind.DotToken || this.CurrentToken.Kind == SyntaxKind.ColonColonToken;
        }

        private MethodDeclarationSyntax ParseMethodDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList)
        {
            // Parse the name (it could be qualified)
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;

            var paramList = this.ParseParenthesizedParameterList();

            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            try
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                    this.ParseTypeParameterConstraintClauses(constraints);
                }
                else if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                {
                    // Use else if, rather than if, because if we see both a constructor initializer and a constraint clause, we're too lost to recover.
                    var colonToken = this.CurrentToken;

                    ConstructorInitializerSyntax initializer = this.ParseConstructorInitializer();
                    initializer = this.AddErrorToFirstToken(initializer, ErrorCode.ERR_UnexpectedToken, colonToken.Text);
                    paramList = AddTrailingSkippedSyntax(paramList, initializer);

                    // CONSIDER: Parsing an invalid constructor initializer could, conceivably, get us way
                    // off track.  If this becomes a problem, an alternative approach would be to generalize
                    // EatTokenWithPrejudice in such a way that we can just skip everything until we recognize
                    // our context again (perhaps an open brace).
                }

                _termState = saveTerm;

                BlockSyntax blockBody;
                ArrowExpressionClauseSyntax expressionBody;
                SyntaxToken semicolon;

                // Method declarations cannot be nested or placed inside async lambdas, and so cannot occur in an
                // asynchronous context. Therefore the IsInAsync state of the parent scope is not saved and
                // restored, just assumed to be false and reset accordingly after parsing the method body.
                Debug.Assert(!IsInAsync);

                IsInAsync = modifiers.Any((int)SyntaxKind.AsyncKeyword);

                this.ParseBlockAndExpressionBodiesWithSemicolon(out blockBody, out expressionBody, out semicolon);

                IsInAsync = false;

                return _syntaxFactory.MethodDeclaration(
                    attributes,
                    modifiers.ToList(),
                    type,
                    explicitInterfaceOpt,
                    identifier,
                    typeParameterList,
                    paramList,
                    constraints,
                    blockBody,
                    expressionBody,
                    semicolon);
            }
            finally
            {
                if (!constraints.IsNull)
                {
                    _pool.Free(constraints);
                }
            }
        }

        private TypeSyntax ParseReturnType()
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfReturnType;
            var type = this.ParseTypeOrVoid();
            _termState = saveTerm;
            return type;
        }

        private bool IsEndOfReturnType()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.SemicolonToken:
                    return true;
                default:
                    return false;
            }
        }

        private ConversionOperatorDeclarationSyntax ParseConversionOperatorDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            SyntaxToken style;
            if (this.CurrentToken.Kind == SyntaxKind.ImplicitKeyword || this.CurrentToken.Kind == SyntaxKind.ExplicitKeyword)
            {
                style = this.EatToken();
            }
            else
            {
                style = this.EatToken(SyntaxKind.ExplicitKeyword);
            }

            SyntaxToken opKeyword = this.EatToken(SyntaxKind.OperatorKeyword);

            var type = this.ParseType();

            var paramList = this.ParseParenthesizedParameterList();

            BlockSyntax blockBody;
            ArrowExpressionClauseSyntax expressionBody;
            SyntaxToken semicolon;
            this.ParseBlockAndExpressionBodiesWithSemicolon(out blockBody, out expressionBody, out semicolon);

            return _syntaxFactory.ConversionOperatorDeclaration(
                attributes,
                modifiers.ToList(),
                style,
                opKeyword,
                type,
                paramList,
                blockBody,
                expressionBody,
                semicolon);
        }

        private OperatorDeclarationSyntax ParseOperatorDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type)
        {
            var opKeyword = this.EatToken(SyntaxKind.OperatorKeyword);
            SyntaxToken opToken;
            int opTokenErrorOffset;
            int opTokenErrorWidth;

            if (SyntaxFacts.IsAnyOverloadableOperator(this.CurrentToken.Kind))
            {
                opToken = this.EatToken();
                Debug.Assert(!opToken.IsMissing);
                opTokenErrorOffset = opToken.GetLeadingTriviaWidth();
                opTokenErrorWidth = opToken.Width;
            }
            else
            {
                if (this.CurrentToken.Kind == SyntaxKind.ImplicitKeyword || this.CurrentToken.Kind == SyntaxKind.ExplicitKeyword)
                {
                    // Grab the offset and width before we consume the invalid keyword and change our position.
                    GetDiagnosticSpanForMissingToken(out opTokenErrorOffset, out opTokenErrorWidth);
                    opToken = this.ConvertToMissingWithTrailingTrivia(this.EatToken(), SyntaxKind.PlusToken);
                    Debug.Assert(opToken.IsMissing); //Which is why we used GetDiagnosticSpanForMissingToken above.

                    Debug.Assert(type != null); // How could it be?  The only caller got it from ParseReturnType.

                    if (type.IsMissing)
                    {
                        SyntaxDiagnosticInfo diagInfo = MakeError(opTokenErrorOffset, opTokenErrorWidth, ErrorCode.ERR_BadOperatorSyntax, SyntaxFacts.GetText(SyntaxKind.PlusToken));
                        opToken = WithAdditionalDiagnostics(opToken, diagInfo);
                    }
                    else
                    {
                        // Dev10 puts this error on the type (if there is one).
                        type = this.AddError(type, ErrorCode.ERR_BadOperatorSyntax, SyntaxFacts.GetText(SyntaxKind.PlusToken));
                    }
                }
                else
                {
                    //Consume whatever follows the operator keyword as the operator token.  If it is not
                    //we'll add an error below (when we can guess the arity).
                    opToken = EatToken();
                    Debug.Assert(!opToken.IsMissing);
                    opTokenErrorOffset = opToken.GetLeadingTriviaWidth();
                    opTokenErrorWidth = opToken.Width;
                }
            }

            // check for >>
            var opKind = opToken.Kind;
            var tk = this.CurrentToken;
            if (opToken.Kind == SyntaxKind.GreaterThanToken && tk.Kind == SyntaxKind.GreaterThanToken)
            {
                // no trailing trivia and no leading trivia
                if (opToken.GetTrailingTriviaWidth() == 0 && tk.GetLeadingTriviaWidth() == 0)
                {
                    var opToken2 = this.EatToken();
                    opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), SyntaxKind.GreaterThanGreaterThanToken, opToken2.GetTrailingTrivia());
                }
            }

            var paramList = this.ParseParenthesizedParameterList();

            switch (paramList.Parameters.Count)
            {
                case 1:
                    if (opToken.IsMissing || !SyntaxFacts.IsOverloadableUnaryOperator(opKind))
                    {
                        SyntaxDiagnosticInfo diagInfo = MakeError(opTokenErrorOffset, opTokenErrorWidth, ErrorCode.ERR_OvlUnaryOperatorExpected);
                        opToken = WithAdditionalDiagnostics(opToken, diagInfo);
                    }

                    break;
                case 2:
                    if (opToken.IsMissing || !SyntaxFacts.IsOverloadableBinaryOperator(opKind))
                    {
                        SyntaxDiagnosticInfo diagInfo = MakeError(opTokenErrorOffset, opTokenErrorWidth, ErrorCode.ERR_OvlBinaryOperatorExpected);
                        opToken = WithAdditionalDiagnostics(opToken, diagInfo);
                    }

                    break;
                default:
                    if (opToken.IsMissing)
                    {
                        SyntaxDiagnosticInfo diagInfo = MakeError(opTokenErrorOffset, opTokenErrorWidth, ErrorCode.ERR_OvlOperatorExpected);
                        opToken = WithAdditionalDiagnostics(opToken, diagInfo);
                    }
                    else if (SyntaxFacts.IsOverloadableBinaryOperator(opKind))
                    {
                        opToken = this.AddError(opToken, ErrorCode.ERR_BadBinOpArgs, SyntaxFacts.GetText(opKind));
                    }
                    else if (SyntaxFacts.IsOverloadableUnaryOperator(opKind))
                    {
                        opToken = this.AddError(opToken, ErrorCode.ERR_BadUnOpArgs, SyntaxFacts.GetText(opKind));
                    }
                    else
                    {
                        opToken = this.AddError(opToken, ErrorCode.ERR_OvlOperatorExpected);
                    }

                    break;
            }

            BlockSyntax blockBody;
            ArrowExpressionClauseSyntax expressionBody;
            SyntaxToken semicolon;
            this.ParseBlockAndExpressionBodiesWithSemicolon(out blockBody, out expressionBody, out semicolon);

            // if the operator is invalid, then switch it to plus (which will work either way) so that
            // we can finish building the tree
            if (!(opKind == SyntaxKind.IsKeyword ||
                  SyntaxFacts.IsOverloadableUnaryOperator(opKind) ||
                  SyntaxFacts.IsOverloadableBinaryOperator(opKind)))
            {
                opToken = ConvertToMissingWithTrailingTrivia(opToken, SyntaxKind.PlusToken);
            }

            return _syntaxFactory.OperatorDeclaration(
                attributes,
                modifiers.ToList(),
                type,
                opKeyword,
                opToken,
                paramList,
                blockBody,
                expressionBody,
                semicolon);
        }

        private IndexerDeclarationSyntax ParseIndexerDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt,
            SyntaxToken thisKeyword,
            TypeParameterListSyntax typeParameterList)
        {
            Debug.Assert(thisKeyword.Kind == SyntaxKind.ThisKeyword);

            // check to see if the user tried to create a generic indexer.
            if (typeParameterList != null)
            {
                thisKeyword = AddTrailingSkippedSyntax(thisKeyword, typeParameterList);
                thisKeyword = this.AddError(thisKeyword, ErrorCode.ERR_UnexpectedGenericName);
            }

            var parameterList = this.ParseBracketedParameterList();

            AccessorListSyntax accessorList = null;
            ArrowExpressionClauseSyntax expressionBody = null;
            SyntaxToken semicolon = null;
            // Try to parse accessor list unless there is an expression
            // body and no accessor list
            if (this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                expressionBody = this.ParseArrowExpressionClause();
                expressionBody = CheckFeatureAvailability(expressionBody, MessageID.IDS_FeatureExpressionBodiedIndexer);
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            else
            {
                accessorList = this.ParseAccessorList(isEvent: false);
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    semicolon = this.EatTokenWithPrejudice(ErrorCode.ERR_UnexpectedSemicolon);
                }
            }

            // If the user has erroneously provided both an accessor list
            // and an expression body, but no semicolon, we want to parse
            // the expression body and report the error (which is done later)
            if (this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken
                && semicolon == null)
            {
                expressionBody = this.ParseArrowExpressionClause();
                expressionBody = CheckFeatureAvailability(expressionBody, MessageID.IDS_FeatureExpressionBodiedIndexer);
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }

            return _syntaxFactory.IndexerDeclaration(
                attributes,
                modifiers.ToList(),
                type,
                explicitInterfaceOpt,
                thisKeyword,
                parameterList,
                accessorList,
                expressionBody,
                semicolon);
        }

        private PropertyDeclarationSyntax ParsePropertyDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList)
        {
            // check to see if the user tried to create a generic property.
            if (typeParameterList != null)
            {
                identifier = AddTrailingSkippedSyntax(identifier, typeParameterList);
                identifier = this.AddError(identifier, ErrorCode.ERR_UnexpectedGenericName);
            }

            // We know we are parsing a property because we have seen either an
            // open brace or an arrow token
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken ||
                         this.CurrentToken.Kind == SyntaxKind.OpenBraceToken);

            AccessorListSyntax accessorList = null;
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                accessorList = this.ParseAccessorList(isEvent: false);
            }

            ArrowExpressionClauseSyntax expressionBody = null;
            EqualsValueClauseSyntax initializer = null;

            // Check for expression body
            if (this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                expressionBody = this.ParseArrowExpressionClause();
                expressionBody = CheckFeatureAvailability(expressionBody, MessageID.IDS_FeatureExpressionBodiedProperty);
            }
            // Check if we have an initializer
            else if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                var equals = this.EatToken(SyntaxKind.EqualsToken);
                var value = this.ParseVariableInitializer();
                initializer = _syntaxFactory.EqualsValueClause(equals, value: value);
                initializer = CheckFeatureAvailability(initializer, MessageID.IDS_FeatureAutoPropertyInitializer);
            }

            SyntaxToken semicolon = null;
            if (expressionBody != null || initializer != null)
            {
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = this.EatTokenWithPrejudice(ErrorCode.ERR_UnexpectedSemicolon);
            }

            return _syntaxFactory.PropertyDeclaration(
                attributes,
                modifiers.ToList(),
                type,
                explicitInterfaceOpt,
                identifier,
                accessorList,
                expressionBody,
                initializer,
                semicolon);
        }

        private AccessorListSyntax ParseAccessorList(bool isEvent)
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var accessors = default(SyntaxList<AccessorDeclarationSyntax>);

            if (!openBrace.IsMissing || !this.IsTerminator())
            {
                // parse property accessors
                var builder = _pool.Allocate<AccessorDeclarationSyntax>();
                try
                {
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.IsPossibleAccessor())
                        {
                            var acc = this.ParseAccessorDeclaration(isEvent);
                            builder.Add(acc);
                        }
                        else if (this.SkipBadAccessorListTokens(ref openBrace, builder,
                            isEvent ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }

                    accessors = builder.ToList();
                }
                finally
                {
                    _pool.Free(builder);
                }
            }

            var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
            return _syntaxFactory.AccessorList(openBrace, accessors, closeBrace);
        }

        private ArrowExpressionClauseSyntax ParseArrowExpressionClause()
        {
            var arrowToken = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
            return _syntaxFactory.ArrowExpressionClause(arrowToken, ParsePossibleRefExpression());
        }

        private ExpressionSyntax ParsePossibleRefExpression()
        {
            var refKeyword = default(SyntaxToken);
            if (this.CurrentToken.Kind == SyntaxKind.RefKeyword)
            {
                refKeyword = this.EatToken();
                refKeyword = CheckFeatureAvailability(refKeyword, MessageID.IDS_FeatureRefLocalsReturns);
            }

            var expression = this.ParseExpressionCore();
            if (refKeyword != default(SyntaxToken))
            {
                expression = _syntaxFactory.RefExpression(refKeyword, expression);
            }

            return expression;
        }

        private PostSkipAction SkipBadAccessorListTokens(ref SyntaxToken openBrace, SyntaxListBuilder<AccessorDeclarationSyntax> list, ErrorCode error)
        {
            return this.SkipBadListTokensWithErrorCode(ref openBrace, list,
                p => p.CurrentToken.Kind != SyntaxKind.CloseBraceToken && !p.IsPossibleAccessor(),
                p => p.IsTerminator(),
                error);
        }

        private bool IsPossibleAccessor()
        {
            return this.CurrentToken.Kind == SyntaxKind.IdentifierToken
                || IsPossibleAttributeDeclaration()
                || SyntaxFacts.GetAccessorDeclarationKind(this.CurrentToken.ContextualKind) != SyntaxKind.None
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken  // for accessor blocks w/ missing keyword
                || this.CurrentToken.Kind == SyntaxKind.SemicolonToken // for empty body accessors w/ missing keyword
                || IsPossibleAccessorModifier();
        }

        private bool IsPossibleAccessorModifier()
        {
            // We only want to accept a modifier as the start of an accessor if the modifiers are
            // actually followed by "get/set/add/remove".  Otherwise, we might thing think we're 
            // starting an accessor when we're actually starting a normal class member.  For example:
            //
            //      class C {
            //          public int Prop { get { this.
            //          private DateTime x;
            //
            // We don't want to think of the "private" in "private DateTime x" as starting an accessor
            // here.  If we do, we'll get totally thrown off in parsing the remainder and that will
            // throw off the rest of the features that depend on a good syntax tree.
            // 
            // Note: we allow all modifiers here.  That's because we want to parse things like
            // "abstract get" as an accessor.  This way we can provide a good error message
            // to the user that this is not allowed.

            if (GetModifier(this.CurrentToken) == DeclarationModifiers.None)
            {
                return false;
            }

            var peekIndex = 1;
            while (IsAnyModifier(this.PeekToken(peekIndex)))
            {
                peekIndex++;
            }

            var token = this.PeekToken(peekIndex);
            if (token.Kind == SyntaxKind.CloseBraceToken || token.Kind == SyntaxKind.EndOfFileToken)
            {
                // If we see "{ get { } public }
                // then we will think that "public" likely starts an accessor.
                return true;
            }

            switch (token.ContextualKind)
            {
                case SyntaxKind.GetKeyword:
                case SyntaxKind.SetKeyword:
                case SyntaxKind.AddKeyword:
                case SyntaxKind.RemoveKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private enum PostSkipAction
        {
            Continue,
            Abort
        }

        private PostSkipAction SkipBadSeparatedListTokensWithExpectedKind<T, TNode>(
            ref T startToken,
            SeparatedSyntaxListBuilder<TNode> list,
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            SyntaxKind expected)
            where T : CSharpSyntaxNode
            where TNode : CSharpSyntaxNode
        {
            // We're going to cheat here and pass the underlying SyntaxListBuilder of "list" to the helper method so that
            // it can append skipped trivia to the last element, regardless of whether that element is a node or a token.
            GreenNode trailingTrivia;
            var action = this.SkipBadListTokensWithExpectedKindHelper(list.UnderlyingBuilder, isNotExpectedFunction, abortFunction, expected, out trailingTrivia);
            if (trailingTrivia != null)
            {
                startToken = AddTrailingSkippedSyntax(startToken, trailingTrivia);
            }
            return action;
        }

        private PostSkipAction SkipBadListTokensWithErrorCode<T, TNode>(
            ref T startToken,
            SyntaxListBuilder<TNode> list,
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            ErrorCode error)
            where T : CSharpSyntaxNode
            where TNode : CSharpSyntaxNode
        {
            GreenNode trailingTrivia;
            var action = this.SkipBadListTokensWithErrorCodeHelper(list, isNotExpectedFunction, abortFunction, error, out trailingTrivia);
            if (trailingTrivia != null)
            {
                startToken = AddTrailingSkippedSyntax(startToken, trailingTrivia);
            }
            return action;
        }

        /// <remarks>
        /// WARNING: it is possible that "list" is really the underlying builder of a SeparateSyntaxListBuilder,
        /// so it is important that we not add anything to the list.
        /// </remarks>
        private PostSkipAction SkipBadListTokensWithExpectedKindHelper(
            SyntaxListBuilder list,
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            SyntaxKind expected,
            out GreenNode trailingTrivia)
        {
            if (list.Count == 0)
            {
                return SkipBadTokensWithExpectedKind(isNotExpectedFunction, abortFunction, expected, out trailingTrivia);
            }
            else
            {
                GreenNode lastItemTrailingTrivia;
                var action = SkipBadTokensWithExpectedKind(isNotExpectedFunction, abortFunction, expected, out lastItemTrailingTrivia);
                if (lastItemTrailingTrivia != null)
                {
                    AddTrailingSkippedSyntax(list, lastItemTrailingTrivia);
                }
                trailingTrivia = null;
                return action;
            }
        }

        private PostSkipAction SkipBadListTokensWithErrorCodeHelper<TNode>(
            SyntaxListBuilder<TNode> list,
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            ErrorCode error,
            out GreenNode trailingTrivia) where TNode : CSharpSyntaxNode
        {
            if (list.Count == 0)
            {
                return SkipBadTokensWithErrorCode(isNotExpectedFunction, abortFunction, error, out trailingTrivia);
            }
            else
            {
                GreenNode lastItemTrailingTrivia;
                var action = SkipBadTokensWithErrorCode(isNotExpectedFunction, abortFunction, error, out lastItemTrailingTrivia);
                if (lastItemTrailingTrivia != null)
                {
                    AddTrailingSkippedSyntax(list, lastItemTrailingTrivia);
                }
                trailingTrivia = null;
                return action;
            }
        }

        private PostSkipAction SkipBadTokensWithExpectedKind(
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            SyntaxKind expected,
            out GreenNode trailingTrivia)
        {
            var nodes = _pool.Allocate();
            try
            {
                bool first = true;
                var action = PostSkipAction.Continue;
                while (isNotExpectedFunction(this))
                {
                    if (abortFunction(this))
                    {
                        action = PostSkipAction.Abort;
                        break;
                    }

                    var token = (first && !this.CurrentToken.ContainsDiagnostics) ? this.EatTokenWithPrejudice(expected) : this.EatToken();
                    first = false;
                    nodes.Add(token);
                }

                trailingTrivia = (nodes.Count > 0) ? nodes.ToListNode() : null;
                return action;
            }
            finally
            {
                _pool.Free(nodes);
            }
        }

        private PostSkipAction SkipBadTokensWithErrorCode(
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            ErrorCode errorCode,
            out GreenNode trailingTrivia)
        {
            var nodes = _pool.Allocate();
            try
            {
                bool first = true;
                var action = PostSkipAction.Continue;
                while (isNotExpectedFunction(this))
                {
                    if (abortFunction(this))
                    {
                        action = PostSkipAction.Abort;
                        break;
                    }

                    var token = (first && !this.CurrentToken.ContainsDiagnostics) ? this.EatTokenWithPrejudice(errorCode) : this.EatToken();
                    first = false;
                    nodes.Add(token);
                }

                trailingTrivia = (nodes.Count > 0) ? nodes.ToListNode() : null;
                return action;
            }
            finally
            {
                _pool.Free(nodes);
            }
        }

        private AccessorDeclarationSyntax ParseAccessorDeclaration(bool isEvent)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseAccessorDeclaration())
            {
                return (AccessorDeclarationSyntax)this.EatNode();
            }

            var accAttrs = _pool.Allocate<AttributeListSyntax>();
            var accMods = _pool.Allocate();
            try
            {
                this.ParseAttributeDeclarations(accAttrs);
                this.ParseModifiers(accMods, forAccessors: true);

                // check availability of readonly members feature for accessors
                CheckForVersionSpecificModifiers(accMods, SyntaxKind.ReadOnlyKeyword, MessageID.IDS_FeatureReadOnlyMembers);

                if (!isEvent)
                {
                    if (accMods != null && accMods.Count > 0)
                    {
                        accMods[0] = CheckFeatureAvailability(accMods[0], MessageID.IDS_FeaturePropertyAccessorMods);
                    }
                }

                var accessorName = this.EatToken(SyntaxKind.IdentifierToken,
                    isEvent ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected);
                var accessorKind = GetAccessorKind(accessorName);

                // Only convert the identifier to a keyword if it's a valid one.  Otherwise any
                // other contextual keyword (like 'partial') will be converted into a keyword
                // and will be invalid.
                if (accessorKind == SyntaxKind.UnknownAccessorDeclaration)
                {
                    // We'll have an UnknownAccessorDeclaration either because we didn't have
                    // an IdentifierToken or because we have an IdentifierToken which is not
                    // add/remove/get/set.  In the former case, we'll already have reported
                    // an error and will have a missing token.  But in the latter case we need 
                    // to report that the identifier is incorrect.
                    if (!accessorName.IsMissing)
                    {
                        accessorName = this.AddError(accessorName,
                            isEvent ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected);
                    }
                    else
                    {
                        Debug.Assert(accessorName.ContainsDiagnostics);
                    }
                }
                else
                {
                    accessorName = ConvertToKeyword(accessorName);
                }

                BlockSyntax blockBody = null;
                ArrowExpressionClauseSyntax expressionBody = null;
                SyntaxToken semicolon = null;

                bool currentTokenIsSemicolon = this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
                bool currentTokenIsArrow = this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken;
                bool currentTokenIsOpenBraceToken = this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;

                if (currentTokenIsOpenBraceToken || currentTokenIsArrow)
                {
                    this.ParseBlockAndExpressionBodiesWithSemicolon(
                        out blockBody, out expressionBody, out semicolon,
                        requestedExpressionBodyFeature: MessageID.IDS_FeatureExpressionBodiedAccessor);
                }
                else if (currentTokenIsSemicolon)
                {
                    semicolon = EatAccessorSemicolon();
                }
                else
                {
                    // We didn't get something we recognized.  If we got an accessor type we 
                    // recognized (i.e. get/set/add/remove) then try to parse out a block.
                    // Only do this if it doesn't seem like we're at the end of the accessor/property.
                    // for example, if we have "get set", don't actually try to parse out the 
                    // block.  Otherwise we'll consume the 'set'.  In that case, just end the
                    // current accessor with a semicolon so we can properly consume the next
                    // in the calling method's loop.
                    if (accessorKind != SyntaxKind.UnknownAccessorDeclaration)
                    {
                        if (!IsTerminator())
                        {
                            blockBody = this.ParseBlock(isMethodBody: true, isAccessorBody: true);
                        }
                        else
                        {
                            semicolon = EatAccessorSemicolon();
                        }
                    }
                    else
                    {
                        // Don't bother eating anything if we didn't even have a valid accessor.
                        // It will just lead to more errors.  Note: we will have already produced
                        // a good error by now.
                        Debug.Assert(accessorName.ContainsDiagnostics);
                    }
                }

                return _syntaxFactory.AccessorDeclaration(
                    accessorKind, accAttrs, accMods.ToList(), accessorName,
                    blockBody, expressionBody, semicolon);
            }
            finally
            {
                _pool.Free(accMods);
                _pool.Free(accAttrs);
            }
        }

        private SyntaxToken EatAccessorSemicolon()
            => this.EatToken(SyntaxKind.SemicolonToken,
                IsFeatureEnabled(MessageID.IDS_FeatureExpressionBodiedAccessor)
                    ? ErrorCode.ERR_SemiOrLBraceOrArrowExpected
                    : ErrorCode.ERR_SemiOrLBraceExpected);

        private SyntaxKind GetAccessorKind(SyntaxToken accessorName)
        {
            switch (accessorName.ContextualKind)
            {
                case SyntaxKind.GetKeyword: return SyntaxKind.GetAccessorDeclaration;
                case SyntaxKind.SetKeyword: return SyntaxKind.SetAccessorDeclaration;
                case SyntaxKind.AddKeyword: return SyntaxKind.AddAccessorDeclaration;
                case SyntaxKind.RemoveKeyword: return SyntaxKind.RemoveAccessorDeclaration;
            }

            return SyntaxKind.UnknownAccessorDeclaration;
        }

        private bool CanReuseAccessorDeclaration()
        {
            switch (this.CurrentNodeKind)
            {
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    return true;
            }

            return false;
        }

        internal ParameterListSyntax ParseParenthesizedParameterList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseParameterList(this.CurrentNode as CSharp.Syntax.ParameterListSyntax))
            {
                return (ParameterListSyntax)this.EatNode();
            }

            var parameters = _pool.AllocateSeparated<ParameterSyntax>();

            try
            {
                var openKind = SyntaxKind.OpenParenToken;
                var closeKind = SyntaxKind.CloseParenToken;

                SyntaxToken open;
                SyntaxToken close;
                this.ParseParameterList(out open, parameters, out close, openKind, closeKind);
                return _syntaxFactory.ParameterList(open, parameters, close);
            }
            finally
            {
                _pool.Free(parameters);
            }
        }

        internal BracketedParameterListSyntax ParseBracketedParameterList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseBracketedParameterList(this.CurrentNode as CSharp.Syntax.BracketedParameterListSyntax))
            {
                return (BracketedParameterListSyntax)this.EatNode();
            }

            var parameters = _pool.AllocateSeparated<ParameterSyntax>();

            try
            {
                var openKind = SyntaxKind.OpenBracketToken;
                var closeKind = SyntaxKind.CloseBracketToken;

                SyntaxToken open;
                SyntaxToken close;
                this.ParseParameterList(out open, parameters, out close, openKind, closeKind);
                return _syntaxFactory.BracketedParameterList(open, parameters, close);
            }
            finally
            {
                _pool.Free(parameters);
            }
        }

        private static bool CanReuseParameterList(CSharp.Syntax.ParameterListSyntax list)
        {
            if (list == null)
            {
                return false;
            }

            if (list.OpenParenToken.IsMissing)
            {
                return false;
            }

            if (list.CloseParenToken.IsMissing)
            {
                return false;
            }

            foreach (var parameter in list.Parameters)
            {
                if (!CanReuseParameter(parameter))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanReuseBracketedParameterList(CSharp.Syntax.BracketedParameterListSyntax list)
        {
            if (list == null)
            {
                return false;
            }

            if (list.OpenBracketToken.IsMissing)
            {
                return false;
            }

            if (list.CloseBracketToken.IsMissing)
            {
                return false;
            }

            foreach (var parameter in list.Parameters)
            {
                if (!CanReuseParameter(parameter))
                {
                    return false;
                }
            }

            return true;
        }

        private void ParseParameterList(
            out SyntaxToken open,
            SeparatedSyntaxListBuilder<ParameterSyntax> nodes,
            out SyntaxToken close,
            SyntaxKind openKind,
            SyntaxKind closeKind)
        {
            open = this.EatToken(openKind);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfParameterList;

            var attributes = _pool.Allocate<AttributeListSyntax>();
            var modifiers = _pool.Allocate();
            try
            {
                if (this.CurrentToken.Kind != closeKind)
                {
tryAgain:
                    if (this.IsPossibleParameter() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        // first parameter
                        attributes.Clear();
                        modifiers.Clear();
                        var parameter = this.ParseParameter(attributes, modifiers);
                        nodes.Add(parameter);

                        // additional parameters
                        while (true)
                        {
                            if (this.CurrentToken.Kind == closeKind)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleParameter())
                            {
                                nodes.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                attributes.Clear();
                                modifiers.Clear();
                                parameter = this.ParseParameter(attributes, modifiers);
                                if (parameter.IsMissing && this.IsPossibleParameter())
                                {
                                    // ensure we always consume tokens
                                    parameter = AddTrailingSkippedSyntax(parameter, this.EatToken());
                                }

                                nodes.Add(parameter);
                                continue;
                            }
                            else if (this.SkipBadParameterListTokens(ref open, nodes, SyntaxKind.CommaToken, closeKind) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (this.SkipBadParameterListTokens(ref open, nodes, SyntaxKind.IdentifierToken, closeKind) == PostSkipAction.Continue)
                    {
                        goto tryAgain;
                    }
                }

                _termState = saveTerm;
                close = this.EatToken(closeKind);
            }
            finally
            {
                _pool.Free(modifiers);
                _pool.Free(attributes);
            }
        }

        private bool IsEndOfParameterList()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.CloseBracketToken;
        }

        private PostSkipAction SkipBadParameterListTokens(
            ref SyntaxToken open, SeparatedSyntaxListBuilder<ParameterSyntax> list, SyntaxKind expected, SyntaxKind closeKind)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleParameter(),
                p => p.CurrentToken.Kind == closeKind || p.IsTerminator(),
                expected);
        }

        private bool IsPossibleParameter()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenBracketToken: // attribute
                case SyntaxKind.ArgListKeyword:
                case SyntaxKind.OpenParenToken:   // tuple
                    return true;

                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();

                default:
                    return IsParameterModifier(this.CurrentToken.Kind) || IsPredefinedType(this.CurrentToken.Kind);
            }
        }

        private static bool CanReuseParameter(CSharp.Syntax.ParameterSyntax parameter, SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            if (parameter == null)
            {
                return false;
            }

            // cannot reuse parameter if it had attributes.
            //
            // TODO(cyrusn): Why?  We can reuse other constructs if they have attributes.
            if (attributes.Count != 0 || parameter.AttributeLists.Count != 0)
            {
                return false;
            }

            // cannot reuse parameter if it had modifiers.
            if ((modifiers != null && modifiers.Count != 0) || parameter.Modifiers.Count != 0)
            {
                return false;
            }

            return CanReuseParameter(parameter);
        }

        private static bool CanReuseParameter(CSharp.Syntax.ParameterSyntax parameter)
        {
            // cannot reuse a node that possibly ends in an expression
            if (parameter.Default != null)
            {
                return false;
            }

            // cannot reuse lambda parameters as normal parameters (parsed with
            // different rules)
            CSharp.CSharpSyntaxNode parent = parameter.Parent;
            if (parent != null)
            {
                if (parent.Kind() == SyntaxKind.SimpleLambdaExpression)
                {
                    return false;
                }

                CSharp.CSharpSyntaxNode grandparent = parent.Parent;
                if (grandparent != null && grandparent.Kind() == SyntaxKind.ParenthesizedLambdaExpression)
                {
                    Debug.Assert(parent.Kind() == SyntaxKind.ParameterList);
                    return false;
                }
            }

            return true;
        }

        private ParameterSyntax ParseParameter(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseParameter(this.CurrentNode as CSharp.Syntax.ParameterSyntax, attributes, modifiers))
            {
                return (ParameterSyntax)this.EatNode();
            }

            this.ParseAttributeDeclarations(attributes);
            this.ParseParameterModifiers(modifiers);

            TypeSyntax type;
            SyntaxToken name;
            if (this.CurrentToken.Kind != SyntaxKind.ArgListKeyword)
            {
                type = this.ParseType(mode: ParseTypeMode.Parameter);
                name = this.ParseIdentifierToken();

                // When the user type "int goo[]", give them a useful error
                if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind == SyntaxKind.CloseBracketToken)
                {
                    var open = this.EatToken();
                    var close = this.EatToken();
                    open = this.AddError(open, ErrorCode.ERR_BadArraySyntax);
                    name = AddTrailingSkippedSyntax(name, SyntaxList.List(open, close));
                }
            }
            else
            {
                // We store an __arglist parameter as a parameter with null type and whose 
                // .Identifier has the kind ArgListKeyword.
                type = null;
                name = this.EatToken(SyntaxKind.ArgListKeyword);
            }

            EqualsValueClauseSyntax def = null;
            if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                var equals = this.EatToken(SyntaxKind.EqualsToken);
                var value = this.ParseExpressionCore();
                def = _syntaxFactory.EqualsValueClause(equals, value: value);
                def = CheckFeatureAvailability(def, MessageID.IDS_FeatureOptionalParameter);
            }

            return _syntaxFactory.Parameter(attributes, modifiers.ToList(), type, name, def);
        }

        private static bool IsParameterModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.ParamsKeyword:
                    return true;
            }

            return false;
        }

        private void ParseParameterModifiers(SyntaxListBuilder modifiers)
        {
            while (IsParameterModifier(this.CurrentToken.Kind))
            {
                var modifier = this.EatToken();

                switch (modifier.Kind)
                {
                    case SyntaxKind.ThisKeyword:
                        modifier = CheckFeatureAvailability(modifier, MessageID.IDS_FeatureExtensionMethod);
                        break;

                    case SyntaxKind.RefKeyword:
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.ThisKeyword)
                            {
                                modifier = CheckFeatureAvailability(modifier, MessageID.IDS_FeatureRefExtensionMethods);
                            }

                            break;
                        }

                    case SyntaxKind.InKeyword:
                        {
                            modifier = CheckFeatureAvailability(modifier, MessageID.IDS_FeatureReadOnlyReferences);

                            if (this.CurrentToken.Kind == SyntaxKind.ThisKeyword)
                            {
                                modifier = CheckFeatureAvailability(modifier, MessageID.IDS_FeatureRefExtensionMethods);
                            }

                            break;
                        }
                }

                modifiers.Add(modifier);
            }
        }

        private FieldDeclarationSyntax ParseFixedSizeBufferDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxKind parentKind)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.FixedKeyword);

            var fixedToken = this.EatToken();
            fixedToken = CheckFeatureAvailability(fixedToken, MessageID.IDS_FeatureFixedBuffer);
            modifiers.Add(fixedToken);

            var type = this.ParseType();

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFieldDeclaration;
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseVariableDeclarators(type, VariableFlags.Fixed, variables, parentKind);

                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);

                return _syntaxFactory.FieldDeclaration(
                    attributes, modifiers.ToList(),
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon);
            }
            finally
            {
                _termState = saveTerm;
                _pool.Free(variables);
            }
        }

        private MemberDeclarationSyntax ParseEventDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxKind parentKind)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.EventKeyword);

            var eventToken = this.EatToken();
            var type = this.ParseType();

            if (IsFieldDeclaration(isEvent: true))
            {
                return this.ParseEventFieldDeclaration(attributes, modifiers, eventToken, type, parentKind);
            }
            else
            {
                return this.ParseEventDeclarationWithAccessors(attributes, modifiers, eventToken, type);
            }
        }

        private EventDeclarationSyntax ParseEventDeclarationWithAccessors(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxToken eventToken,
            TypeSyntax type)
        {
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt;
            SyntaxToken identifierOrThisOpt;
            TypeParameterListSyntax typeParameterList;

            this.ParseMemberName(out explicitInterfaceOpt, out identifierOrThisOpt, out typeParameterList, isEvent: true);

            // check availability of readonly members feature for custom events
            CheckForVersionSpecificModifiers(modifiers, SyntaxKind.ReadOnlyKeyword, MessageID.IDS_FeatureReadOnlyMembers);

            // If we got an explicitInterfaceOpt but not an identifier, then we're in the special
            // case for ERR_ExplicitEventFieldImpl (see ParseMemberName for details).
            if (explicitInterfaceOpt != null && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
            {
                Debug.Assert(typeParameterList == null, "Exit condition of ParseMemberName in this scenario");

                // No need for a diagnostic, ParseMemberName has already added one.
                var missingIdentifier = (identifierOrThisOpt == null) ? CreateMissingIdentifierToken() : identifierOrThisOpt;

                var missingAccessorList =
                    _syntaxFactory.AccessorList(
                        SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                        default(SyntaxList<AccessorDeclarationSyntax>),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));

                return _syntaxFactory.EventDeclaration(
                    attributes,
                    modifiers.ToList(),
                    eventToken,
                    type,
                    explicitInterfaceOpt, //already has an appropriate error attached
                    missingIdentifier,
                    missingAccessorList,
                    semicolonToken: null);
            }

            SyntaxToken identifier;

            if (identifierOrThisOpt == null)
            {
                identifier = CreateMissingIdentifierToken();
            }
            else if (identifierOrThisOpt.Kind != SyntaxKind.IdentifierToken)
            {
                Debug.Assert(identifierOrThisOpt.Kind == SyntaxKind.ThisKeyword);
                identifier = ConvertToMissingWithTrailingTrivia(identifierOrThisOpt, SyntaxKind.IdentifierToken);
            }
            else
            {
                identifier = identifierOrThisOpt;
            }

            Debug.Assert(identifier != null);
            Debug.Assert(identifier.Kind == SyntaxKind.IdentifierToken);

            if (identifier.IsMissing && !type.IsMissing)
            {
                identifier = this.AddError(identifier, ErrorCode.ERR_IdentifierExpected);
            }

            if (typeParameterList != null) // check to see if the user tried to create a generic event.
            {
                identifier = AddTrailingSkippedSyntax(identifier, typeParameterList);
                identifier = this.AddError(identifier, ErrorCode.ERR_UnexpectedGenericName);
            }

            AccessorListSyntax accessorList = null;
            SyntaxToken semicolon = null;

            if (explicitInterfaceOpt != null && this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            else
            {
                accessorList = this.ParseAccessorList(isEvent: true);
            }

            var decl = _syntaxFactory.EventDeclaration(
                attributes,
                modifiers.ToList(),
                eventToken,
                type,
                explicitInterfaceOpt,
                identifier,
                accessorList,
                semicolon);

            decl = EatUnexpectedTrailingSemicolon(decl);

            return decl;
        }

        private TNode EatUnexpectedTrailingSemicolon<TNode>(TNode decl) where TNode : CSharpSyntaxNode
        {
            // allow for case of one unexpected semicolon...
            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                var semi = this.EatToken();
                semi = this.AddError(semi, ErrorCode.ERR_UnexpectedSemicolon);
                decl = AddTrailingSkippedSyntax(decl, semi);
            }

            return decl;
        }

        private FieldDeclarationSyntax ParseNormalFieldDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            SyntaxKind parentKind)
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFieldDeclaration;
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseVariableDeclarators(type, flags: 0, variables: variables, parentKind: parentKind);

                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.FieldDeclaration(
                    attributes,
                    modifiers.ToList(),
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon);
            }
            finally
            {
                _termState = saveTerm;
                _pool.Free(variables);
            }
        }

        private EventFieldDeclarationSyntax ParseEventFieldDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxToken eventToken,
            TypeSyntax type,
            SyntaxKind parentKind)
        {
            // An attribute specified on an event declaration that omits event accessors can apply
            // to the event being declared, to the associated field (if the event is not abstract),
            // or to the associated add and remove methods. In the absence of an
            // attribute-target-specifier, the attribute applies to the event. The presence of the
            // event attribute-target-specifier indicates that the attribute applies to the event;
            // the presence of the field attribute-target-specifier indicates that the attribute
            // applies to the field; and the presence of the method attribute-target-specifier
            // indicates that the attribute applies to the methods.
            //
            // NOTE(cyrusn): We allow more than the above here.  Specifically, even if the event is
            // abstract, we allow the attribute to specify that it belongs to a field.  Later, in the
            // semantic pass, we will disallow this.

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFieldDeclaration;
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseVariableDeclarators(type, flags: 0, variables: variables, parentKind: parentKind);

                if (this.CurrentToken.Kind == SyntaxKind.DotToken)
                {
                    eventToken = this.AddError(eventToken, ErrorCode.ERR_ExplicitEventFieldImpl);  // Better error message for confusing event situation.
                }

                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.EventFieldDeclaration(
                    attributes,
                    modifiers.ToList(),
                    eventToken,
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon);
            }
            finally
            {
                _termState = saveTerm;
                _pool.Free(variables);
            }
        }

        private bool IsEndOfFieldDeclaration()
        {
            return this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private void ParseVariableDeclarators(TypeSyntax type, VariableFlags flags, SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables, SyntaxKind parentKind)
        {
            // Although we try parse variable declarations in contexts where they are not allowed (non-interactive top-level or a namespace) 
            // the reported errors should take into consideration whether or not one expects them in the current context.
            bool variableDeclarationsExpected =
                parentKind != SyntaxKind.NamespaceDeclaration &&
                (parentKind != SyntaxKind.CompilationUnit || IsScript);

            LocalFunctionStatementSyntax localFunction;
            ParseVariableDeclarators(
                type: type,
                flags: flags,
                variables: variables,
                variableDeclarationsExpected: variableDeclarationsExpected,
                allowLocalFunctions: false,
                mods: default(SyntaxList<SyntaxToken>),
                localFunction: out localFunction);

            Debug.Assert(localFunction == null);
        }

        private void ParseVariableDeclarators(
            TypeSyntax type,
            VariableFlags flags,
            SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables,
            bool variableDeclarationsExpected,
            bool allowLocalFunctions,
            SyntaxList<SyntaxToken> mods,
            out LocalFunctionStatementSyntax localFunction)
        {
            variables.Add(
                this.ParseVariableDeclarator(
                    type,
                    flags,
                    isFirst: true,
                    allowLocalFunctions: allowLocalFunctions,
                    mods: mods,
                    localFunction: out localFunction));

            if (localFunction != null)
            {
                // ParseVariableDeclarator returns null, so it is not added to variables
                Debug.Assert(variables.Count == 0);
                return;
            }

            while (true)
            {
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    break;
                }
                else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    variables.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    variables.Add(
                        this.ParseVariableDeclarator(
                            type,
                            flags,
                            isFirst: false,
                            allowLocalFunctions: false,
                            mods: mods,
                            localFunction: out localFunction));
                }
                else if (!variableDeclarationsExpected || this.SkipBadVariableListTokens(variables, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                {
                    break;
                }
            }
        }

        private PostSkipAction SkipBadVariableListTokens(SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => this.CurrentToken.Kind != SyntaxKind.CommaToken,
                p => this.CurrentToken.Kind == SyntaxKind.SemicolonToken || this.IsTerminator(),
                expected);
        }

        [Flags]
        private enum VariableFlags
        {
            Fixed = 0x01,
            Const = 0x02,
            Local = 0x04
        }

        private static SyntaxTokenList GetOriginalModifiers(CSharp.CSharpSyntaxNode decl)
        {
            if (decl != null)
            {
                switch (decl.Kind())
                {
                    case SyntaxKind.FieldDeclaration:
                        return ((CSharp.Syntax.FieldDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.MethodDeclaration:
                        return ((CSharp.Syntax.MethodDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.ConstructorDeclaration:
                        return ((CSharp.Syntax.ConstructorDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.DestructorDeclaration:
                        return ((CSharp.Syntax.DestructorDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.PropertyDeclaration:
                        return ((CSharp.Syntax.PropertyDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.EventFieldDeclaration:
                        return ((CSharp.Syntax.EventFieldDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                        return ((CSharp.Syntax.AccessorDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                        return ((CSharp.Syntax.TypeDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.DelegateDeclaration:
                        return ((CSharp.Syntax.DelegateDeclarationSyntax)decl).Modifiers;
                }
            }

            return default(SyntaxTokenList);
        }

        private static bool WasFirstVariable(CSharp.Syntax.VariableDeclaratorSyntax variable)
        {
            var parent = GetOldParent(variable) as CSharp.Syntax.VariableDeclarationSyntax;
            if (parent != null)
            {
                return parent.Variables[0] == variable;
            }

            return false;
        }

        private static VariableFlags GetOriginalVariableFlags(CSharp.Syntax.VariableDeclaratorSyntax old)
        {
            var parent = GetOldParent(old);
            var mods = GetOriginalModifiers(parent);
            VariableFlags flags = default(VariableFlags);
            if (mods.Any(SyntaxKind.FixedKeyword))
            {
                flags |= VariableFlags.Fixed;
            }

            if (mods.Any(SyntaxKind.ConstKeyword))
            {
                flags |= VariableFlags.Const;
            }

            if (parent != null && (parent.Kind() == SyntaxKind.VariableDeclaration || parent.Kind() == SyntaxKind.LocalDeclarationStatement))
            {
                flags |= VariableFlags.Local;
            }

            return flags;
        }

        private static bool CanReuseVariableDeclarator(CSharp.Syntax.VariableDeclaratorSyntax old, VariableFlags flags, bool isFirst)
        {
            if (old == null)
            {
                return false;
            }

            SyntaxKind oldKind;

            return (flags == GetOriginalVariableFlags(old))
                && (isFirst == WasFirstVariable(old))
                && old.Initializer == null  // can't reuse node that possibly ends in an expression
                && (oldKind = GetOldParent(old).Kind()) != SyntaxKind.VariableDeclaration // or in a method body
                && oldKind != SyntaxKind.LocalDeclarationStatement;
        }

        private VariableDeclaratorSyntax ParseVariableDeclarator(
            TypeSyntax parentType,
            VariableFlags flags,
            bool isFirst,
            bool allowLocalFunctions,
            SyntaxList<SyntaxToken> mods,
            out LocalFunctionStatementSyntax localFunction,
            bool isExpressionContext = false)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseVariableDeclarator(this.CurrentNode as CSharp.Syntax.VariableDeclaratorSyntax, flags, isFirst))
            {
                localFunction = null;
                return (VariableDeclaratorSyntax)this.EatNode();
            }

            if (!isExpressionContext)
            {
                // Check for the common pattern of:
                //
                // C                    //<-- here
                // Console.WriteLine();
                //
                // Standard greedy parsing will assume that this should be parsed as a variable
                // declaration: "C Console".  We want to avoid that as it can confused parts of the
                // system further up.  So, if we see certain things following the identifier, then we can
                // assume it's not the actual name.  
                // 
                // So, if we're after a newline and we see a name followed by the list below, then we
                // assume that we're accidentally consuming too far into the next statement.
                //
                // <dot>, <arrow>, any binary operator (except =), <question>.  None of these characters
                // are allowed in a normal variable declaration.  This also provides a more useful error
                // message to the user.  Instead of telling them that a semicolon is expected after the
                // following token, then instead get a useful message about an identifier being missing.
                // The above list prevents:
                //
                // C                    //<-- here
                // Console.WriteLine();
                //
                // C                    //<-- here 
                // Console->WriteLine();
                //
                // C 
                // A + B;
                //
                // C 
                // A ? B : D;
                //
                // C 
                // A()
                var resetPoint = this.GetResetPoint();
                try
                {
                    var currentTokenKind = this.CurrentToken.Kind;
                    if (currentTokenKind == SyntaxKind.IdentifierToken && !parentType.IsMissing)
                    {
                        var isAfterNewLine = parentType.GetLastToken().TrailingTrivia.Any((int)SyntaxKind.EndOfLineTrivia);
                        if (isAfterNewLine)
                        {
                            int offset, width;
                            this.GetDiagnosticSpanForMissingToken(out offset, out width);

                            this.EatToken();
                            currentTokenKind = this.CurrentToken.Kind;

                            var isNonEqualsBinaryToken =
                                currentTokenKind != SyntaxKind.EqualsToken &&
                                SyntaxFacts.IsBinaryExpressionOperatorToken(currentTokenKind);

                            if (currentTokenKind == SyntaxKind.DotToken ||
                                currentTokenKind == SyntaxKind.OpenParenToken ||
                                currentTokenKind == SyntaxKind.MinusGreaterThanToken ||
                                isNonEqualsBinaryToken)
                            {
                                var isPossibleLocalFunctionToken =
                                    currentTokenKind == SyntaxKind.OpenParenToken ||
                                    currentTokenKind == SyntaxKind.LessThanToken;

                                // Make sure this isn't a local function
                                if (!isPossibleLocalFunctionToken || !IsLocalFunctionAfterIdentifier())
                                {
                                    var missingIdentifier = CreateMissingIdentifierToken();
                                    missingIdentifier = this.AddError(missingIdentifier, offset, width, ErrorCode.ERR_IdentifierExpected);

                                    localFunction = null;
                                    return _syntaxFactory.VariableDeclarator(missingIdentifier, null, null);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    this.Reset(ref resetPoint);
                    this.Release(ref resetPoint);
                }
            }

            // NOTE: Diverges from Dev10.
            //
            // When we see parse an identifier and we see the partial contextual keyword, we check
            // to see whether it is already attached to a partial class or partial method
            // declaration.  However, in the specific case of variable declarators, Dev10
            // specifically treats it as a variable name, even if it could be interpreted as a
            // keyword.
            var name = this.ParseIdentifierToken();
            BracketedArgumentListSyntax argumentList = null;
            EqualsValueClauseSyntax initializer = null;
            TerminatorState saveTerm = _termState;
            bool isFixed = (flags & VariableFlags.Fixed) != 0;
            bool isConst = (flags & VariableFlags.Const) != 0;
            bool isLocal = (flags & VariableFlags.Local) != 0;

            // Give better error message in the case where the user did something like:
            //
            // X x = 1, Y y = 2; 
            // using (X x = expr1, Y y = expr2) ...
            //
            // The superfluous type name is treated as variable (it is an identifier) and a missing ',' is injected after it.
            if (!isFirst && this.IsTrueIdentifier())
            {
                name = this.AddError(name, ErrorCode.ERR_MultiTypeInDeclaration);
            }

            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.EqualsToken:
                    if (isFixed)
                    {
                        goto default;
                    }

                    var equals = this.EatToken();

                    SyntaxToken refKeyword = null;
                    if (isLocal && !isConst &&
                        this.CurrentToken.Kind == SyntaxKind.RefKeyword)
                    {
                        refKeyword = this.EatToken();
                        refKeyword = CheckFeatureAvailability(refKeyword, MessageID.IDS_FeatureRefLocalsReturns);
                    }

                    var init = this.ParseVariableInitializer();
                    if (refKeyword != null)
                    {
                        init = _syntaxFactory.RefExpression(refKeyword, init);
                    }

                    initializer = _syntaxFactory.EqualsValueClause(equals, init);
                    break;

                case SyntaxKind.LessThanToken:
                    if (allowLocalFunctions && isFirst)
                    {
                        localFunction = TryParseLocalFunctionStatementBody(mods, parentType, name);
                        if (localFunction != null)
                        {
                            return null;
                        }
                    }
                    goto default;

                case SyntaxKind.OpenParenToken:
                    if (allowLocalFunctions && isFirst)
                    {
                        localFunction = TryParseLocalFunctionStatementBody(mods, parentType, name);
                        if (localFunction != null)
                        {
                            return null;
                        }
                    }

                    // Special case for accidental use of C-style constructors
                    // Fake up something to hold the arguments.
                    _termState |= TerminatorState.IsPossibleEndOfVariableDeclaration;
                    argumentList = this.ParseBracketedArgumentList();
                    _termState = saveTerm;
                    argumentList = this.AddError(argumentList, ErrorCode.ERR_BadVarDecl);
                    break;

                case SyntaxKind.OpenBracketToken:
                    bool sawNonOmittedSize;
                    _termState |= TerminatorState.IsPossibleEndOfVariableDeclaration;
                    var specifier = this.ParseArrayRankSpecifier(sawNonOmittedSize: out sawNonOmittedSize);
                    _termState = saveTerm;
                    var open = specifier.OpenBracketToken;
                    var sizes = specifier.Sizes;
                    var close = specifier.CloseBracketToken;
                    if (isFixed && !sawNonOmittedSize)
                    {
                        close = this.AddError(close, ErrorCode.ERR_ValueExpected);
                    }

                    var args = _pool.AllocateSeparated<ArgumentSyntax>();
                    try
                    {
                        var withSeps = sizes.GetWithSeparators();
                        foreach (var item in withSeps)
                        {
                            var expression = item as ExpressionSyntax;
                            if (expression != null)
                            {
                                bool isOmitted = expression.Kind == SyntaxKind.OmittedArraySizeExpression;
                                if (!isFixed && !isOmitted)
                                {
                                    expression = this.AddError(expression, ErrorCode.ERR_ArraySizeInDeclaration);
                                }

                                args.Add(_syntaxFactory.Argument(null, default(SyntaxToken), expression));
                            }
                            else
                            {
                                args.AddSeparator((SyntaxToken)item);
                            }
                        }

                        argumentList = _syntaxFactory.BracketedArgumentList(open, args, close);
                        if (!isFixed)
                        {
                            argumentList = this.AddError(argumentList, ErrorCode.ERR_CStyleArray);
                            // If we have "int x[] = new int[10];" then parse the initializer.
                            if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
                            {
                                goto case SyntaxKind.EqualsToken;
                            }
                        }
                    }
                    finally
                    {
                        _pool.Free(args);
                    }

                    break;

                default:
                    if (isConst)
                    {
                        name = this.AddError(name, ErrorCode.ERR_ConstValueRequired);  // Error here for missing constant initializers
                    }
                    else if (isFixed)
                    {
                        if (parentType.Kind == SyntaxKind.ArrayType)
                        {
                            // They accidentally put the array before the identifier
                            name = this.AddError(name, ErrorCode.ERR_FixedDimsRequired);
                        }
                        else
                        {
                            goto case SyntaxKind.OpenBracketToken;
                        }
                    }

                    break;
            }

            localFunction = null;
            return _syntaxFactory.VariableDeclarator(name, argumentList, initializer);
        }

        // Is there a local function after an eaten identifier?
        private bool IsLocalFunctionAfterIdentifier()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenParenToken ||
                         this.CurrentToken.Kind == SyntaxKind.LessThanToken);
            var resetPoint = this.GetResetPoint();

            try
            {
                var typeParameterListOpt = this.ParseTypeParameterList();
                var paramList = ParseParenthesizedParameterList();

                if (!paramList.IsMissing &&
                     (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken ||
                      this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken ||
                      this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword))
                {
                    return true;
                }

                return false;
            }
            finally
            {
                Reset(ref resetPoint);
                Release(ref resetPoint);
            }
        }

        private bool IsPossibleEndOfVariableDeclaration()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.CommaToken:
                case SyntaxKind.SemicolonToken:
                    return true;
                default:
                    return false;
            }
        }

        private ExpressionSyntax ParseVariableInitializer()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                    return this.ParseArrayInitializer();
                default:
                    return this.ParseExpressionCore();
            }
        }

        private bool IsPossibleVariableInitializer()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsPossibleExpression();
        }

        private FieldDeclarationSyntax ParseConstantFieldDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers, SyntaxKind parentKind)
        {
            var constToken = this.EatToken(SyntaxKind.ConstKeyword);
            modifiers.Add(constToken);

            var type = this.ParseType();

            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseVariableDeclarators(type, VariableFlags.Const, variables, parentKind);
                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.FieldDeclaration(
                    attributes,
                    modifiers.ToList(),
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon);
            }
            finally
            {
                _pool.Free(variables);
            }
        }

        private DelegateDeclarationSyntax ParseDelegateDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.DelegateKeyword);

            var delegateToken = this.EatToken(SyntaxKind.DelegateKeyword);
            var type = this.ParseReturnType();
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;
            var name = this.ParseIdentifierToken();
            var typeParameters = this.ParseTypeParameterList();
            var parameterList = this.ParseParenthesizedParameterList();
            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            try
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                    this.ParseTypeParameterConstraintClauses(constraints);
                }

                _termState = saveTerm;

                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.DelegateDeclaration(attributes, modifiers.ToList(), delegateToken, type, name, typeParameters, parameterList, constraints, semicolon);
            }
            finally
            {
                if (!constraints.IsNull)
                {
                    _pool.Free(constraints);
                }
            }
        }

        private EnumDeclarationSyntax ParseEnumDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.EnumKeyword);

            var enumToken = this.EatToken(SyntaxKind.EnumKeyword);
            var name = this.ParseIdentifierToken();

            // check to see if the user tried to create a generic enum.
            var typeParameters = this.ParseTypeParameterList();

            if (typeParameters != null)
            {
                name = AddTrailingSkippedSyntax(name, typeParameters);
                name = this.AddError(name, ErrorCode.ERR_UnexpectedGenericName);
            }

            BaseListSyntax baseList = null;
            if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                var colon = this.EatToken(SyntaxKind.ColonToken);
                var type = this.ParseType();
                var tmpList = _pool.AllocateSeparated<BaseTypeSyntax>();
                tmpList.Add(_syntaxFactory.SimpleBaseType(type));
                baseList = _syntaxFactory.BaseList(colon, tmpList);
                _pool.Free(tmpList);
            }

            var members = default(SeparatedSyntaxList<EnumMemberDeclarationSyntax>);
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            if (!openBrace.IsMissing)
            {
                var builder = _pool.AllocateSeparated<EnumMemberDeclarationSyntax>();
                try
                {
                    this.ParseEnumMemberDeclarations(ref openBrace, builder);
                    members = builder.ToList();
                }
                finally
                {
                    _pool.Free(builder);
                }
            }

            var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
            var semicolon = TryEatToken(SyntaxKind.SemicolonToken);

            return _syntaxFactory.EnumDeclaration(
                attributes,
                modifiers.ToList(),
                enumToken,
                name,
                baseList,
                openBrace,
                members,
                closeBrace,
                semicolon);
        }

        private void ParseEnumMemberDeclarations(
            ref SyntaxToken openBrace,
            SeparatedSyntaxListBuilder<EnumMemberDeclarationSyntax> members)
        {
            if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
            {
tryAgain:

                if (this.IsPossibleEnumMemberDeclaration() || this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    // first member
                    members.Add(this.ParseEnumMemberDeclaration());

                    // additional members
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.SemicolonToken || this.IsPossibleEnumMemberDeclaration())
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                            {
                                // semicolon instead of comma.. consume it with error and act as if it were a comma.
                                members.AddSeparator(this.EatTokenWithPrejudice(SyntaxKind.CommaToken));
                            }
                            else
                            {
                                members.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            }

                            // check for exit case after legal trailing comma
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }
                            else if (!this.IsPossibleEnumMemberDeclaration())
                            {
                                goto tryAgain;
                            }

                            members.Add(this.ParseEnumMemberDeclaration());
                            continue;
                        }
                        else if (this.SkipBadEnumMemberListTokens(ref openBrace, members, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadEnumMemberListTokens(ref openBrace, members, SyntaxKind.IdentifierToken) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }
            }
        }

        private PostSkipAction SkipBadEnumMemberListTokens(ref SyntaxToken openBrace, SeparatedSyntaxListBuilder<EnumMemberDeclarationSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openBrace, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && p.CurrentToken.Kind != SyntaxKind.SemicolonToken && !p.IsPossibleEnumMemberDeclaration(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBraceToken || p.IsTerminator(),
                expected);
        }

        private EnumMemberDeclarationSyntax ParseEnumMemberDeclaration()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.EnumMemberDeclaration)
            {
                return (EnumMemberDeclarationSyntax)this.EatNode();
            }

            var memberAttrs = _pool.Allocate<AttributeListSyntax>();
            try
            {
                this.ParseAttributeDeclarations(memberAttrs);
                var memberName = this.ParseIdentifierToken();
                EqualsValueClauseSyntax equalsValue = null;
                if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
                {
                    var equals = this.EatToken(SyntaxKind.EqualsToken);
                    ExpressionSyntax value;
                    if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                    {
                        //an identifier is a valid expression
                        value = this.ParseIdentifierName(ErrorCode.ERR_ConstantExpected);
                    }
                    else
                    {
                        value = this.ParseExpressionCore();
                    }

                    equalsValue = _syntaxFactory.EqualsValueClause(equals, value: value);
                }

                return _syntaxFactory.EnumMemberDeclaration(memberAttrs, modifiers: default, memberName, equalsValue);
            }
            finally
            {
                _pool.Free(memberAttrs);
            }
        }

        private bool IsPossibleEnumMemberDeclaration()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken || this.IsTrueIdentifier();
        }

        private bool IsDotOrColonColon()
        {
            return this.CurrentToken.Kind == SyntaxKind.DotToken || this.CurrentToken.Kind == SyntaxKind.ColonColonToken;
        }

        // This is public and parses open types. You probably don't want to use it.
        public NameSyntax ParseName()
        {
            return this.ParseQualifiedName();
        }

        private IdentifierNameSyntax CreateMissingIdentifierName()
        {
            return _syntaxFactory.IdentifierName(CreateMissingIdentifierToken());
        }

        private static SyntaxToken CreateMissingIdentifierToken()
        {
            return SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);
        }

        [Flags]
        private enum NameOptions
        {
            None = 0,
            InExpression = 1 << 0, // Used to influence parser ambiguity around "<" and generics vs. expressions. Used in ParseSimpleName.
            InTypeList = 1 << 1, // Allows attributes to appear within the generic type argument list. Used during ParseInstantiation.
            PossiblePattern = 1 << 2, // Used to influence parser ambiguity around "<" and generics vs. expressions on the right of 'is'
            AfterIs = 1 << 3,
            DefinitePattern = 1 << 4,
            AfterOut = 1 << 5,
            AfterTupleComma = 1 << 6,
            FirstElementOfPossibleTupleLiteral = 1 << 7,
        }

        /// <summary>
        /// True if current identifier token is not really some contextual keyword
        /// </summary>
        /// <returns></returns>
        private bool IsTrueIdentifier()
        {
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                if (!IsCurrentTokenPartialKeywordOfPartialMethodOrType() &&
                    !IsCurrentTokenQueryKeywordInQuery() &&
                    !IsCurrentTokenWhereOfConstraintClause())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True if the given token is not really some contextual keyword.
        /// This method is for use in executable code, as it treats <c>partial</c> as an identifier.
        /// </summary>
        private bool IsTrueIdentifier(SyntaxToken token)
        {
            return
                token.Kind == SyntaxKind.IdentifierToken &&
                !(this.IsInQuery && IsTokenQueryContextualKeyword(token));
        }

        private IdentifierNameSyntax ParseIdentifierName(ErrorCode code = ErrorCode.ERR_IdentifierExpected)
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.IdentifierName)
            {
                if (!SyntaxFacts.IsContextualKeyword(((CSharp.Syntax.IdentifierNameSyntax)this.CurrentNode).Identifier.Kind()))
                {
                    return (IdentifierNameSyntax)this.EatNode();
                }
            }

            var tk = ParseIdentifierToken(code);
            return SyntaxFactory.IdentifierName(tk);
        }

        private SyntaxToken ParseIdentifierToken(ErrorCode code = ErrorCode.ERR_IdentifierExpected)
        {
            var ctk = this.CurrentToken.Kind;
            if (ctk == SyntaxKind.IdentifierToken)
            {
                // Error tolerance for IntelliSense. Consider the following case: [EditorBrowsable( partial class Goo {
                // } Because we're parsing an attribute argument we'll end up consuming the "partial" identifier and
                // we'll eventually end up in an pretty confused state.  Because of that it becomes very difficult to
                // show the correct parameter help in this case.  So, when we see "partial" we check if it's being used
                // as an identifier or as a contextual keyword.  If it's the latter then we bail out.  See
                // Bug: vswhidbey/542125
                if (IsCurrentTokenPartialKeywordOfPartialMethodOrType() || IsCurrentTokenQueryKeywordInQuery())
                {
                    var result = CreateMissingIdentifierToken();
                    result = this.AddError(result, ErrorCode.ERR_InvalidExprTerm, this.CurrentToken.Text);
                    return result;
                }

                SyntaxToken identifierToken = this.EatToken();

                if (this.IsInAsync && identifierToken.ContextualKind == SyntaxKind.AwaitKeyword)
                {
                    identifierToken = this.AddError(identifierToken, ErrorCode.ERR_BadAwaitAsIdentifier);
                }

                return identifierToken;
            }
            else
            {
                var name = CreateMissingIdentifierToken();
                name = this.AddError(name, code);
                return name;
            }
        }

        private bool IsCurrentTokenQueryKeywordInQuery()
        {
            return this.IsInQuery && this.IsCurrentTokenQueryContextualKeyword;
        }

        private bool IsCurrentTokenPartialKeywordOfPartialMethodOrType()
        {
            if (this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword)
            {
                if (ScanPartialTypeOrMember() != ScanPartialFlags.NotModifier)
                {
                    return true;
                }
            }

            return false;
        }

        private TypeParameterListSyntax ParseTypeParameterList()
        {
            if (this.CurrentToken.Kind != SyntaxKind.LessThanToken)
            {
                return null;
            }

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfTypeParameterList;
            try
            {
                var parameters = _pool.AllocateSeparated<TypeParameterSyntax>();
                var open = this.EatToken(SyntaxKind.LessThanToken);
                open = CheckFeatureAvailability(open, MessageID.IDS_FeatureGenerics);

                // first parameter
                parameters.Add(this.ParseTypeParameter());

                // remaining parameter & commas
                while (true)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken || this.IsCurrentTokenWhereOfConstraintClause())
                    {
                        break;
                    }
                    else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        parameters.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                        parameters.Add(this.ParseTypeParameter());
                    }
                    else if (this.SkipBadTypeParameterListTokens(parameters, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                    {
                        break;
                    }
                }

                var close = this.EatToken(SyntaxKind.GreaterThanToken);

                return _syntaxFactory.TypeParameterList(open, parameters, close);
            }
            finally
            {
                _termState = saveTerm;
            }
        }

        private PostSkipAction SkipBadTypeParameterListTokens(SeparatedSyntaxListBuilder<TypeParameterSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => this.CurrentToken.Kind != SyntaxKind.CommaToken,
                p => this.CurrentToken.Kind == SyntaxKind.GreaterThanToken || this.IsTerminator(),
                expected);
        }

        private TypeParameterSyntax ParseTypeParameter()
        {
            if (this.IsCurrentTokenWhereOfConstraintClause())
            {
                return _syntaxFactory.TypeParameter(
                    default(SyntaxList<AttributeListSyntax>),
                    default(SyntaxToken),
                    this.AddError(CreateMissingIdentifierToken(), ErrorCode.ERR_IdentifierExpected));
            }

            var attrs = _pool.Allocate<AttributeListSyntax>();
            try
            {
                if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind != SyntaxKind.CloseBracketToken)
                {
                    var saveTerm = _termState;
                    _termState = TerminatorState.IsEndOfTypeArgumentList;
                    this.ParseAttributeDeclarations(attrs);
                    _termState = saveTerm;
                }

                SyntaxToken varianceToken = null;
                if (this.CurrentToken.Kind == SyntaxKind.InKeyword ||
                    this.CurrentToken.Kind == SyntaxKind.OutKeyword)
                {
                    varianceToken = CheckFeatureAvailability(this.EatToken(), MessageID.IDS_FeatureTypeVariance);
                }

                return _syntaxFactory.TypeParameter(attrs, varianceToken, this.ParseIdentifierToken());
            }
            finally
            {
                _pool.Free(attrs);
            }
        }

        // Parses the parts of the names between Dots and ColonColons.
        private SimpleNameSyntax ParseSimpleName(NameOptions options = NameOptions.None)
        {
            var id = this.ParseIdentifierName();
            if (id.Identifier.IsMissing)
            {
                return id;
            }

            // You can pass ignore generics if you don't even want the parser to consider generics at all.
            // The name parsing will then stop at the first "<". It doesn't make sense to pass both Generic and IgnoreGeneric.

            SimpleNameSyntax name = id;
            if (this.CurrentToken.Kind == SyntaxKind.LessThanToken)
            {
                var pt = this.GetResetPoint();
                var kind = this.ScanTypeArgumentList(options);
                this.Reset(ref pt);
                this.Release(ref pt);

                if (kind == ScanTypeArgumentListKind.DefiniteTypeArgumentList || (kind == ScanTypeArgumentListKind.PossibleTypeArgumentList && (options & NameOptions.InTypeList) != 0))
                {
                    Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);
                    SyntaxToken open;
                    var types = _pool.AllocateSeparated<TypeSyntax>();
                    SyntaxToken close;
                    this.ParseTypeArgumentList(out open, types, out close);
                    name = _syntaxFactory.GenericName(id.Identifier,
                        _syntaxFactory.TypeArgumentList(open, types, close));
                    _pool.Free(types);
                }
            }

            return name;
        }

        private enum ScanTypeArgumentListKind
        {
            NotTypeArgumentList,
            PossibleTypeArgumentList,
            DefiniteTypeArgumentList
        }

        private ScanTypeArgumentListKind ScanTypeArgumentList(NameOptions options)
        {
            if (this.CurrentToken.Kind != SyntaxKind.LessThanToken)
            {
                return ScanTypeArgumentListKind.NotTypeArgumentList;
            }

            if ((options & NameOptions.InExpression) == 0)
            {
                return ScanTypeArgumentListKind.DefiniteTypeArgumentList;
            }

            // We're in an expression context, and we have a < token.  This could be a 
            // type argument list, or it could just be a relational expression.  
            //
            // Scan just the type argument list portion (i.e. the part from < to > ) to
            // see what we think it could be.  This will give us one of three possibilities:
            //
            //      result == ScanTypeFlags.NotType.
            //
            // This is absolutely not a type-argument-list.  Just return that result immediately.
            //
            //      result != ScanTypeFlags.NotType && isDefinitelyTypeArgumentList.
            //
            // This is absolutely a type-argument-list.  Just return that result immediately
            // 
            //      result != ScanTypeFlags.NotType && !isDefinitelyTypeArgumentList.
            //
            // This could be a type-argument list, or it could be an expression.  Need to see
            // what came after the last '>' to find out which it is.

            // Scan for a type argument list. If we think it's a type argument list
            // then assume it is unless we see specific tokens following it.
            SyntaxToken lastTokenOfList = null;
            ScanTypeFlags possibleTypeArgumentFlags = ScanPossibleTypeArgumentList(
                ref lastTokenOfList, out bool isDefinitelyTypeArgumentList);

            if (possibleTypeArgumentFlags == ScanTypeFlags.NotType)
            {
                return ScanTypeArgumentListKind.NotTypeArgumentList;
            }

            if (isDefinitelyTypeArgumentList)
            {
                return ScanTypeArgumentListKind.DefiniteTypeArgumentList;
            }

            // If we did not definitively determine from immediate syntax that it was or
            // was not a type argument list, we must have scanned the entire thing up through
            // the closing greater-than token. In that case we will disambiguate based on the
            // token that follows it.
            Debug.Assert(lastTokenOfList.Kind == SyntaxKind.GreaterThanToken);

            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.ColonToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CommaToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.QuestionToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsToken:
                case SyntaxKind.BarToken:
                case SyntaxKind.CaretToken:
                    // These tokens are from 7.5.4.2 Grammar Ambiguities
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                case SyntaxKind.AmpersandAmpersandToken: // e.g. `e is A<B> && e`
                case SyntaxKind.BarBarToken:             // e.g. `e is A<B> || e`
                case SyntaxKind.AmpersandToken:          // e.g. `e is A<B> & e`
                case SyntaxKind.OpenBracketToken:        // e.g. `e is A<B>[]`
                case SyntaxKind.LessThanToken:           // e.g. `e is A<B> < C`
                case SyntaxKind.LessThanEqualsToken:     // e.g. `e is A<B> <= C`
                case SyntaxKind.GreaterThanEqualsToken:  // e.g. `e is A<B> >= C`
                case SyntaxKind.IsKeyword:               // e.g. `e is A<B> is bool`
                case SyntaxKind.AsKeyword:               // e.g. `e is A<B> as bool`
                    // These tokens were added to 7.5.4.2 Grammar Ambiguities in C# 7.0
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                case SyntaxKind.OpenBraceToken: // e.g. `e is A<B> {}`
                    // This token was added to 7.5.4.2 Grammar Ambiguities in C# 8.0
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                case SyntaxKind.GreaterThanToken when ((options & NameOptions.AfterIs) != 0) && this.PeekToken(1).Kind != SyntaxKind.GreaterThanToken:
                    // This token is added to 7.5.4.2 Grammar Ambiguities in C#7 for the special case in which
                    // the possible generic is following an `is` keyword, e.g. `e is A<B> > C`.
                    // We test one further token ahead because a right-shift operator `>>` looks like a pair of greater-than
                    // tokens at this stage, but we don't intend to be handling the right-shift operator.
                    // The upshot is that we retain compatibility with the two previous behaviors:
                    // `(x is A<B>>C)` is parsed as `(x is A<B>) > C`
                    // `A<B>>C` elsewhere is parsed as `A < (B >> C)`
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                case SyntaxKind.IdentifierToken:
                    // C#7: In certain contexts, we treat *identifier* as a disambiguating token. Those
                    // contexts are where the sequence of tokens being disambiguated is immediately preceded by one
                    // of the keywords is, case, or out, or arises while parsing the first element of a tuple literal
                    // (in which case the tokens are preceded by `(` and the identifier is followed by a `,`) or a
                    // subsequent element of a tuple literal (in which case the tokens are preceded by `,` and the
                    // identifier is followed by a `,` or `)`).
                    // In C#8 (or whenever recursive patterns are introduced) we also treat an identifier as a
                    // disambiguating token if we're parsing the type of a pattern.
                    // Note that we treat query contextual keywords (which appear here as identifiers) as disambiguating tokens as well.
                    if ((options & (NameOptions.AfterIs | NameOptions.DefinitePattern | NameOptions.AfterOut)) != 0 ||
                        (options & NameOptions.AfterTupleComma) != 0 && (this.PeekToken(1).Kind == SyntaxKind.CommaToken || this.PeekToken(1).Kind == SyntaxKind.CloseParenToken) ||
                        (options & NameOptions.FirstElementOfPossibleTupleLiteral) != 0 && this.PeekToken(1).Kind == SyntaxKind.CommaToken
                        )
                    {
                        // we allow 'G<T,U> x' as a pattern-matching operation and a declaration expression in a tuple.
                        return ScanTypeArgumentListKind.DefiniteTypeArgumentList;
                    }

                    return ScanTypeArgumentListKind.PossibleTypeArgumentList;

                case SyntaxKind.EndOfFileToken:          // e.g. `e is A<B>`
                    // This is useful for parsing expressions in isolation
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                default:
                    return ScanTypeArgumentListKind.PossibleTypeArgumentList;
            }
        }

        private ScanTypeFlags ScanPossibleTypeArgumentList(
            ref SyntaxToken lastTokenOfList, out bool isDefinitelyTypeArgumentList)
        {
            isDefinitelyTypeArgumentList = false;

            if (this.CurrentToken.Kind == SyntaxKind.LessThanToken)
            {
                ScanTypeFlags result = ScanTypeFlags.GenericTypeOrExpression;

                do
                {
                    lastTokenOfList = this.EatToken();

                    // Type arguments cannot contain attributes, so if this is an open square, we early out and assume it is not a type argument
                    if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                    {
                        lastTokenOfList = null;
                        return ScanTypeFlags.NotType;
                    }

                    if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                    {
                        lastTokenOfList = EatToken();
                        return result;
                    }

                    switch (this.ScanType(out lastTokenOfList))
                    {
                        case ScanTypeFlags.NotType:
                            lastTokenOfList = null;
                            return ScanTypeFlags.NotType;

                        case ScanTypeFlags.MustBeType:
                            // We're currently scanning a possible type-argument list.  But we're
                            // not sure if this is actually a type argument list, or is maybe some
                            // complex relational expression with <'s and >'s.  One thing we can
                            // tell though is that if we have a predefined type (like 'int' or 'string')
                            // before a comma or > then this is definitely a type argument list. i.e.
                            // if you have:
                            // 
                            //      var v = ImmutableDictionary<int,
                            //
                            // then there's no legal interpretation of this as an expression (since a
                            // standalone predefined type is not a valid simple term.  Contrast that
                            // with :
                            //
                            //  var v = ImmutableDictionary<Int32,
                            //
                            // Here this might actually be a relational expression and the comma is meant
                            // to separate out the variable declarator 'v' from the next variable.
                            //
                            // Note: we check if we got 'MustBeType' which triggers for predefined types,
                            // (int, string, etc.), or array types (Goo[], A<T>[][] etc.), or pointer types
                            // of things that must be types (int*, void**, etc.).
                            isDefinitelyTypeArgumentList = DetermineIfDefinitelyTypeArgumentList(isDefinitelyTypeArgumentList);
                            result = ScanTypeFlags.GenericTypeOrMethod;
                            break;

                        // case ScanTypeFlags.TupleType:
                        // It would be nice if we saw a tuple to state that we definitely had a 
                        // type argument list.  However, there are cases where this would not be
                        // true.  For example:
                        //
                        // public class C
                        // {
                        //     public static void Main()
                        //     {
                        //         XX X = default;
                        //         int a = 1, b = 2;
                        //         bool z = X < (a, b), w = false;
                        //     }
                        // }
                        //
                        // struct XX
                        // {
                        //     public static bool operator <(XX x, (int a, int b) arg) => true;
                        //     public static bool operator >(XX x, (int a, int b) arg) => false;
                        // }

                        case ScanTypeFlags.NullableType:
                            // See above.  If we have X<Y?,  or X<Y?>, then this is definitely a type argument list.
                            isDefinitelyTypeArgumentList = DetermineIfDefinitelyTypeArgumentList(isDefinitelyTypeArgumentList);
                            if (isDefinitelyTypeArgumentList)
                            {
                                result = ScanTypeFlags.GenericTypeOrMethod;
                            }

                            // Note: we intentionally fall out without setting 'result'. 
                            // Seeing a nullable type (not followed by a , or > ) is not enough 
                            // information for us to determine what this is yet.  i.e. the user may have:
                            //
                            //      X < Y ? Z : W
                            //
                            // We'd see a nullable type here, but htis is definitely not a type arg list.

                            break;

                        case ScanTypeFlags.GenericTypeOrExpression:
                            // See above.  If we have  X<Y<Z>,  then this would definitely be a type argument list.
                            // However, if we have  X<Y<Z>> then this might not be type argument list.  This could just
                            // be some sort of expression where we're comparing, and then shifting values.
                            if (!isDefinitelyTypeArgumentList)
                            {
                                isDefinitelyTypeArgumentList = this.CurrentToken.Kind == SyntaxKind.CommaToken;
                                result = ScanTypeFlags.GenericTypeOrMethod;
                            }
                            break;

                        case ScanTypeFlags.GenericTypeOrMethod:
                            result = ScanTypeFlags.GenericTypeOrMethod;
                            break;
                    }
                }
                while (this.CurrentToken.Kind == SyntaxKind.CommaToken);

                if (this.CurrentToken.Kind != SyntaxKind.GreaterThanToken)
                {
                    lastTokenOfList = null;
                    return ScanTypeFlags.NotType;
                }

                lastTokenOfList = this.EatToken();
                return result;
            }

            return ScanTypeFlags.NonGenericTypeOrExpression;
        }

        private bool DetermineIfDefinitelyTypeArgumentList(bool isDefinitelyTypeArgumentList)
        {
            if (!isDefinitelyTypeArgumentList)
            {
                isDefinitelyTypeArgumentList =
                    this.CurrentToken.Kind == SyntaxKind.CommaToken ||
                    this.CurrentToken.Kind == SyntaxKind.GreaterThanToken;
            }

            return isDefinitelyTypeArgumentList;
        }

        // ParseInstantiation: Parses the generic argument/parameter parts of the name.
        private void ParseTypeArgumentList(out SyntaxToken open, SeparatedSyntaxListBuilder<TypeSyntax> types, out SyntaxToken close)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);
            open = this.EatToken(SyntaxKind.LessThanToken);
            open = CheckFeatureAvailability(open, MessageID.IDS_FeatureGenerics);

            if (this.IsOpenName())
            {
                // NOTE: trivia will be attached to comma, not omitted type argument
                var omittedTypeArgumentInstance = _syntaxFactory.OmittedTypeArgument(SyntaxFactory.Token(SyntaxKind.OmittedTypeArgumentToken));
                types.Add(omittedTypeArgumentInstance);
                while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    types.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    types.Add(omittedTypeArgumentInstance);
                }

                close = this.EatToken(SyntaxKind.GreaterThanToken);

                return;
            }

            // first type
            types.Add(this.ParseTypeArgument());

            // remaining types & commas
            while (true)
            {
                if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                {
                    break;
                }
                else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleType())
                {
                    types.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    types.Add(this.ParseTypeArgument());
                }
                else if (this.SkipBadTypeArgumentListTokens(types, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                {
                    break;
                }
            }

            close = this.EatToken(SyntaxKind.GreaterThanToken);
        }

        private PostSkipAction SkipBadTypeArgumentListTokens(SeparatedSyntaxListBuilder<TypeSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => this.CurrentToken.Kind != SyntaxKind.CommaToken && !this.IsPossibleType(),
                p => this.CurrentToken.Kind == SyntaxKind.GreaterThanToken || this.IsTerminator(),
                expected);
        }

        // Parses the individual generic parameter/arguments in a name.
        private TypeSyntax ParseTypeArgument()
        {
            var attrs = _pool.Allocate<AttributeListSyntax>();
            try
            {
                if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind != SyntaxKind.CloseBracketToken)
                {
                    // Here, if we see a "[" that looks like it has something in it, we parse
                    // it as an attribute and then later put an error on the whole type if
                    // it turns out that attributes are not allowed. 
                    // TODO: should there be another flag that controls this behavior? we have
                    // "allowAttrs" but should there also be a "recognizeAttrs" that we can
                    // set to false in an expression context?

                    var saveTerm = _termState;
                    _termState = TerminatorState.IsEndOfTypeArgumentList;
                    this.ParseAttributeDeclarations(attrs);
                    _termState = saveTerm;
                }

                SyntaxToken varianceToken = null;
                if (this.CurrentToken.Kind == SyntaxKind.InKeyword || this.CurrentToken.Kind == SyntaxKind.OutKeyword)
                {
                    // Recognize the variance syntax, but give an error as it's
                    // only appropriate in a type parameter list.
                    varianceToken = this.EatToken();
                    varianceToken = CheckFeatureAvailability(varianceToken, MessageID.IDS_FeatureTypeVariance);
                    varianceToken = this.AddError(varianceToken, ErrorCode.ERR_IllegalVarianceSyntax);
                }

                var result = this.ParseType();

                // Consider the case where someone supplies an invalid type argument
                // Such as Action<0> or Action<static>.  In this case we generate a missing 
                // identifier in ParseType, but if we continue as is we'll immediately start to 
                // interpret 0 as the start of a new expression when we can tell it's most likely
                // meant to be part of the type list.  
                //
                // To solve this we check if the current token is not comma or greater than and 
                // the next token is a comma or greater than. If so we assume that the found 
                // token is part of this expression and we attempt to recover. This does open 
                // the door for cases where we have an  incomplete line to be interpretted as 
                // a single expression.  For example:
                //
                // Action< // Incomplete line
                // a>b;
                //
                // However, this only happens when the following expression is of the form a>... 
                // or a,... which  means this case should happen less frequently than what we're 
                // trying to solve here so we err on the side of better error messages
                // for the majority of cases.
                SyntaxKind nextTokenKind = SyntaxKind.None;

                if (result.IsMissing &&
                    (this.CurrentToken.Kind != SyntaxKind.CommaToken && this.CurrentToken.Kind != SyntaxKind.GreaterThanToken) &&
                    ((nextTokenKind = this.PeekToken(1).Kind) == SyntaxKind.CommaToken || nextTokenKind == SyntaxKind.GreaterThanToken))
                {
                    // Eat the current token and add it as skipped so we recover
                    result = AddTrailingSkippedSyntax(result, this.EatToken());
                }

                if (varianceToken != null)
                {
                    result = AddLeadingSkippedSyntax(result, varianceToken);
                }

                if (attrs.Count > 0)
                {
                    result = AddLeadingSkippedSyntax(result, attrs.ToListNode());
                    result = this.AddError(result, ErrorCode.ERR_TypeExpected);
                }

                return result;
            }
            finally
            {
                _pool.Free(attrs);
            }
        }

        private bool IsEndOfTypeArgumentList()
        {
            return this.CurrentToken.Kind == SyntaxKind.GreaterThanToken;
        }

        private bool IsOpenName()
        {
            bool isOpen = true;
            int n = 0;
            while (this.PeekToken(n).Kind == SyntaxKind.CommaToken)
            {
                n++;
            }

            if (this.PeekToken(n).Kind != SyntaxKind.GreaterThanToken)
            {
                isOpen = false;
            }

            return isOpen;
        }

        private void ParseMemberName(
            out ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt,
            out SyntaxToken identifierOrThisOpt,
            out TypeParameterListSyntax typeParameterListOpt,
            bool isEvent)
        {
            identifierOrThisOpt = null;
            explicitInterfaceOpt = null;
            typeParameterListOpt = null;

            if (!IsPossibleMemberName())
            {
                // No clue what this is.  Just bail.  Our caller will have to
                // move forward and try again.
                return;
            }

            NameSyntax explicitInterfaceName = null;
            SyntaxToken separator = null;

            ResetPoint beforeIdentifierPoint = default(ResetPoint);
            bool beforeIdentifierPointSet = false;

            try
            {
                while (true)
                {
                    // Check if we got 'this'.  If so, then we have an indexer.
                    // Note: we parse out type parameters here as well so that
                    // we can give a useful error about illegal generic indexers.
                    if (this.CurrentToken.Kind == SyntaxKind.ThisKeyword)
                    {
                        beforeIdentifierPoint = GetResetPoint();
                        beforeIdentifierPointSet = true;
                        identifierOrThisOpt = this.EatToken();
                        typeParameterListOpt = this.ParseTypeParameterList();
                        break;
                    }

                    // now, scan past the next name.  if it's followed by a dot then
                    // it's part of the explicit name we're building up.  Otherwise,
                    // it's the name of the member.
                    var point = GetResetPoint();
                    bool isMemberName;
                    try
                    {
                        ScanNamedTypePart();
                        isMemberName = !IsDotOrColonColon();
                    }
                    finally
                    {
                        this.Reset(ref point);
                        this.Release(ref point);
                    }

                    if (isMemberName)
                    {
                        // We're past any explicit interface portion and We've 
                        // gotten to the member name.  
                        beforeIdentifierPoint = GetResetPoint();
                        beforeIdentifierPointSet = true;

                        if (separator != null && separator.Kind == SyntaxKind.ColonColonToken)
                        {
                            separator = this.AddError(separator, ErrorCode.ERR_AliasQualAsExpression);
                            separator = this.ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                        }

                        identifierOrThisOpt = this.ParseIdentifierToken();
                        typeParameterListOpt = this.ParseTypeParameterList();
                        break;
                    }
                    else
                    {
                        // If we saw a . or :: then we must have something explicit.
                        // first parse the upcoming name portion.

                        var saveTerm = _termState;
                        _termState |= TerminatorState.IsEndOfNameInExplicitInterface;

                        if (explicitInterfaceName == null)
                        {
                            // If this is the first time, then just get the next simple
                            // name and store it as the explicit interface name.
                            explicitInterfaceName = this.ParseSimpleName(NameOptions.InTypeList);

                            // Now, get the next separator.
                            separator = this.CurrentToken.Kind == SyntaxKind.ColonColonToken
                                ? this.EatToken() // fine after the first identifier
                                : this.EatToken(SyntaxKind.DotToken);
                        }
                        else
                        {
                            // Parse out the next part and combine it with the 
                            // current explicit name to form the new explicit name.
                            var tmp = this.ParseQualifiedNameRight(NameOptions.InTypeList, explicitInterfaceName, separator);
                            Debug.Assert(!ReferenceEquals(tmp, explicitInterfaceName), "We should have consumed something and updated explicitInterfaceName");
                            explicitInterfaceName = tmp;

                            // Now, get the next separator.
                            separator = this.CurrentToken.Kind == SyntaxKind.ColonColonToken
                                ? this.ConvertToMissingWithTrailingTrivia(this.EatToken(), SyntaxKind.DotToken)
                                : this.EatToken(SyntaxKind.DotToken);
                        }

                        _termState = saveTerm;
                    }
                }

                if (explicitInterfaceName != null)
                {
                    if (separator.Kind != SyntaxKind.DotToken)
                    {
                        separator = WithAdditionalDiagnostics(separator, GetExpectedTokenError(SyntaxKind.DotToken, separator.Kind, separator.GetLeadingTriviaWidth(), separator.Width));
                        separator = ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                    }

                    if (isEvent && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                    {
                        // CS0071: If you're explicitly implementing an event field, you have to use the accessor form
                        //
                        // Good:
                        //   event EventDelegate Parent.E
                        //   {
                        //      add { ... }
                        //      remove { ... }
                        //   }
                        //
                        // Bad:
                        //   event EventDelegate Parent.
                        //   E( //(or anything that is not the semicolon
                        //
                        // To recover: rollback to before the name of the field was parsed (just the part after the last
                        // dot), insert a missing identifier for the field name, insert missing accessors, and then treat
                        // the event name that's actually there as the beginning of a new member. e.g.
                        //
                        //   event EventDelegate Parent./*Missing nodes here*/
                        //
                        //   E(
                        //
                        // Rationale: The identifier could be the name of a type at the beginning of an existing member
                        // declaration (above which someone has started to type an explicit event implementation).
                        //
                        // In case the dot doesn't follow with an end line or E ends with a semicolon, the error recovery
                        // is skipped. In that case the rationale above does not fit very well.

                        explicitInterfaceOpt = _syntaxFactory.ExplicitInterfaceSpecifier(
                            explicitInterfaceName,
                            AddError(separator, ErrorCode.ERR_ExplicitEventFieldImpl));

                        if (separator.TrailingTrivia.Any((int)SyntaxKind.EndOfLineTrivia))
                        {
                            Debug.Assert(beforeIdentifierPointSet);
                            Reset(ref beforeIdentifierPoint);
                            //clear fields that were populated after the reset point
                            identifierOrThisOpt = null;
                            typeParameterListOpt = null;
                        }
                    }
                    else
                    {
                        explicitInterfaceOpt = _syntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceName, separator);
                    }
                }
            }
            finally
            {
                if (beforeIdentifierPointSet)
                {
                    Release(ref beforeIdentifierPoint);
                }
            }
        }

        private NameSyntax ParseAliasQualifiedName(NameOptions allowedParts = NameOptions.None)
        {
            NameSyntax name = this.ParseSimpleName(allowedParts);
            if (this.CurrentToken.Kind == SyntaxKind.ColonColonToken)
            {
                var token = this.EatToken();

                name = ParseQualifiedNameRight(allowedParts, name, token);
            }
            return name;
        }

        private NameSyntax ParseQualifiedName(NameOptions options = NameOptions.None)
        {
            NameSyntax name = this.ParseAliasQualifiedName(options);

            while (this.IsDotOrColonColon())
            {
                if (this.PeekToken(1).Kind == SyntaxKind.ThisKeyword)
                {
                    break;
                }

                var separator = this.EatToken();
                name = ParseQualifiedNameRight(options, name, separator);
            }

            return name;
        }

        private NameSyntax ParseQualifiedNameRight(
            NameOptions options,
            NameSyntax left,
            SyntaxToken separator)
        {
            var right = this.ParseSimpleName(options);

            if (separator.Kind == SyntaxKind.DotToken)
            {
                return _syntaxFactory.QualifiedName(left, separator, right);
            }
            else if (separator.Kind == SyntaxKind.ColonColonToken)
            {
                if (left.Kind != SyntaxKind.IdentifierName)
                {
                    separator = this.AddError(separator, ErrorCode.ERR_UnexpectedAliasedName, separator.ToString());
                }

                // If the left hand side is not an identifier name then the user has done
                // something like Goo.Bar::Blah. We've already made an error node for the
                // ::, so just pretend that they typed Goo.Bar.Blah and continue on.

                var identifierLeft = left as IdentifierNameSyntax;
                if (identifierLeft == null)
                {
                    separator = this.ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                    return _syntaxFactory.QualifiedName(left, separator, right);
                }
                else
                {
                    if (identifierLeft.Identifier.ContextualKind == SyntaxKind.GlobalKeyword)
                    {
                        identifierLeft = _syntaxFactory.IdentifierName(ConvertToKeyword(identifierLeft.Identifier));
                    }

                    identifierLeft = CheckFeatureAvailability(identifierLeft, MessageID.IDS_FeatureGlobalNamespace);

                    // If the name on the right had errors or warnings then we need to preserve
                    // them in the tree.
                    return WithAdditionalDiagnostics(_syntaxFactory.AliasQualifiedName(identifierLeft, separator, right), left.GetDiagnostics());
                }
            }
            else
            {
                return left;
            }
        }

        private SyntaxToken ConvertToMissingWithTrailingTrivia(SyntaxToken token, SyntaxKind expectedKind)
        {
            var newToken = SyntaxFactory.MissingToken(expectedKind);
            newToken = AddTrailingSkippedSyntax(newToken, token);
            return newToken;
        }

        private enum ScanTypeFlags
        {
            /// <summary>
            /// Definitely not a type name.
            /// </summary>
            NotType,

            /// <summary>
            /// Definitely a type name: either a predefined type (int, string, etc.) or an array
            /// type (ending with a [] brackets), or a pointer type (ending with *s).
            /// </summary>
            MustBeType,

            /// <summary>
            /// Might be a generic (qualified) type name or a method name.
            /// </summary>
            GenericTypeOrMethod,

            /// <summary>
            /// Might be a generic (qualified) type name or an expression or a method name.
            /// </summary>
            GenericTypeOrExpression,

            /// <summary>
            /// Might be a non-generic (qualified) type name or an expression.
            /// </summary>
            NonGenericTypeOrExpression,

            /// <summary>
            /// A type name with alias prefix (Alias::Name)
            /// </summary>
            AliasQualifiedName,

            /// <summary>
            /// Nullable type (ending with ?).
            /// </summary>
            NullableType,

            /// <summary>
            /// Might be a pointer type or a multiplication.
            /// </summary>
            PointerOrMultiplication,

            /// <summary>
            /// Might be a tuple type.
            /// </summary>
            TupleType,
        }

        private bool IsPossibleType()
        {
            var tk = this.CurrentToken.Kind;
            return IsPredefinedType(tk) || this.IsTrueIdentifier();
        }

        private ScanTypeFlags ScanType(bool forPattern = false)
        {
            return ScanType(out _, forPattern);
        }

        private ScanTypeFlags ScanType(out SyntaxToken lastTokenOfType, bool forPattern = false)
        {
            return ScanType(forPattern ? ParseTypeMode.DefinitePattern : ParseTypeMode.Normal, out lastTokenOfType);
        }

        private void ScanNamedTypePart()
        {
            SyntaxToken lastTokenOfType;
            ScanNamedTypePart(out lastTokenOfType);
        }

        private ScanTypeFlags ScanNamedTypePart(out SyntaxToken lastTokenOfType)
        {
            if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken || !this.IsTrueIdentifier())
            {
                lastTokenOfType = null;
                return ScanTypeFlags.NotType;
            }

            lastTokenOfType = this.EatToken();
            if (this.CurrentToken.Kind == SyntaxKind.LessThanToken)
            {
                return this.ScanPossibleTypeArgumentList(ref lastTokenOfType, out _);
            }
            else
            {
                return ScanTypeFlags.NonGenericTypeOrExpression;
            }
        }

        private ScanTypeFlags ScanType(ParseTypeMode mode, out SyntaxToken lastTokenOfType)
        {
            Debug.Assert(mode != ParseTypeMode.NewExpression);
            ScanTypeFlags result;

            if (this.CurrentToken.Kind == SyntaxKind.RefKeyword)
            {
                // in a ref local or ref return, we treat "ref" and "ref readonly" as part of the type
                this.EatToken();

                if (this.CurrentToken.Kind == SyntaxKind.ReadOnlyKeyword)
                {
                    this.EatToken();
                }
            }

            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                result = this.ScanNamedTypePart(out lastTokenOfType);
                if (result == ScanTypeFlags.NotType)
                {
                    return ScanTypeFlags.NotType;
                }

                bool isAlias = this.CurrentToken.Kind == SyntaxKind.ColonColonToken;

                // Scan a name
                for (bool firstLoop = true; IsDotOrColonColon(); firstLoop = false)
                {
                    if (!firstLoop && isAlias)
                    {
                        isAlias = false;
                    }

                    lastTokenOfType = this.EatToken();

                    result = this.ScanNamedTypePart(out lastTokenOfType);
                    if (result == ScanTypeFlags.NotType)
                    {
                        return ScanTypeFlags.NotType;
                    }
                }

                if (isAlias)
                {
                    result = ScanTypeFlags.AliasQualifiedName;
                }
            }
            else if (IsPredefinedType(this.CurrentToken.Kind))
            {
                // Simple type...
                lastTokenOfType = this.EatToken();
                result = ScanTypeFlags.MustBeType;
            }
            else if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                lastTokenOfType = this.EatToken();

                result = this.ScanTupleType(out lastTokenOfType);
                if (result == ScanTypeFlags.NotType || mode == ParseTypeMode.DefinitePattern && this.CurrentToken.Kind != SyntaxKind.OpenBracketToken)
                {
                    // A tuple type can appear in a pattern only if it is the element type of an array type.
                    return ScanTypeFlags.NotType;
                }
            }
            else
            {
                // Can't be a type!
                lastTokenOfType = null;
                return ScanTypeFlags.NotType;
            }

            int lastTokenPosition = -1;
            while (IsMakingProgress(ref lastTokenPosition))
            {
                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.QuestionToken
                            when lastTokenOfType.Kind != SyntaxKind.QuestionToken && // don't allow `Type??`
                                 lastTokenOfType.Kind != SyntaxKind.AsteriskToken: // don't allow `Type*?`
                        lastTokenOfType = this.EatToken();
                        result = ScanTypeFlags.NullableType;
                        break;
                    case SyntaxKind.AsteriskToken
                            when lastTokenOfType.Kind != SyntaxKind.CloseBracketToken: // don't allow `Type[]*`
                        // Check for pointer type(s)
                        switch (mode)
                        {
                            case ParseTypeMode.FirstElementOfPossibleTupleLiteral:
                            case ParseTypeMode.AfterTupleComma:
                                // We are parsing the type for a declaration expression in a tuple, which does
                                // not permit pointer types except as an element type of an array type.
                                // In that context a `*` is parsed as a multiplication.
                                if (PointerTypeModsFollowedByRankAndDimensionSpecifier())
                                {
                                    goto default;
                                }
                                goto done;
                            case ParseTypeMode.DefinitePattern:
                                // pointer type syntax is not supported in patterns.
                                goto done;
                            default:
                                lastTokenOfType = this.EatToken();
                                if (result == ScanTypeFlags.GenericTypeOrExpression || result == ScanTypeFlags.NonGenericTypeOrExpression)
                                {
                                    result = ScanTypeFlags.PointerOrMultiplication;
                                }
                                else if (result == ScanTypeFlags.GenericTypeOrMethod)
                                {
                                    result = ScanTypeFlags.MustBeType;
                                }
                                break;
                        }
                        break;
                    case SyntaxKind.OpenBracketToken:
                        // Check for array types.
                        this.EatToken();
                        while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                        {
                            this.EatToken();
                        }

                        if (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
                        {
                            lastTokenOfType = null;
                            return ScanTypeFlags.NotType;
                        }

                        lastTokenOfType = this.EatToken();
                        result = ScanTypeFlags.MustBeType;
                        break;
                    default:
                        goto done;
                }
            }

done:
            return result;
        }

        /// <summary>
        /// Returns TupleType when a possible tuple type is found.
        /// Note that this is not MustBeType, so that the caller can consider deconstruction syntaxes.
        /// The caller is expected to have consumed the opening paren.
        /// </summary>
        private ScanTypeFlags ScanTupleType(out SyntaxToken lastTokenOfType)
        {
            var tupleElementType = ScanType(out lastTokenOfType);
            if (tupleElementType != ScanTypeFlags.NotType)
            {
                if (IsTrueIdentifier())
                {
                    lastTokenOfType = this.EatToken();
                }

                if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    do
                    {
                        lastTokenOfType = this.EatToken();
                        tupleElementType = ScanType(out lastTokenOfType);

                        if (tupleElementType == ScanTypeFlags.NotType)
                        {
                            lastTokenOfType = this.EatToken();
                            return ScanTypeFlags.NotType;
                        }

                        if (IsTrueIdentifier())
                        {
                            lastTokenOfType = this.EatToken();
                        }
                    }
                    while (this.CurrentToken.Kind == SyntaxKind.CommaToken);

                    if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken)
                    {
                        lastTokenOfType = this.EatToken();
                        return ScanTypeFlags.TupleType;
                    }
                }
            }

            // Can't be a type!
            lastTokenOfType = null;
            return ScanTypeFlags.NotType;
        }

        private static bool IsPredefinedType(SyntaxKind keyword)
        {
            return SyntaxFacts.IsPredefinedType(keyword);
        }

        public TypeSyntax ParseTypeName()
        {
            return ParseType();
        }

        private TypeSyntax ParseTypeOrVoid()
        {
            if (this.CurrentToken.Kind == SyntaxKind.VoidKeyword && this.PeekToken(1).Kind != SyntaxKind.AsteriskToken)
            {
                // Must be 'void' type, so create such a type node and return it.
                return _syntaxFactory.PredefinedType(this.EatToken());
            }

            return this.ParseType();
        }

        private enum ParseTypeMode
        {
            Normal,
            Parameter,
            AfterIs,
            DefinitePattern,
            AfterOut,
            AfterTupleComma,
            AsExpression,
            NewExpression,
            FirstElementOfPossibleTupleLiteral
        }

        private TypeSyntax ParseType(ParseTypeMode mode = ParseTypeMode.Normal)
        {
            if (this.CurrentToken.Kind == SyntaxKind.RefKeyword)
            {
                var refKeyword = this.EatToken();
                refKeyword = this.CheckFeatureAvailability(refKeyword, MessageID.IDS_FeatureRefLocalsReturns);

                SyntaxToken readonlyKeyword = null;
                if (this.CurrentToken.Kind == SyntaxKind.ReadOnlyKeyword)
                {
                    readonlyKeyword = this.EatToken();
                    readonlyKeyword = this.CheckFeatureAvailability(readonlyKeyword, MessageID.IDS_FeatureReadOnlyReferences);
                }

                var type = ParseTypeCore(mode);
                return _syntaxFactory.RefType(refKeyword, readonlyKeyword, type);
            }

            return ParseTypeCore(mode);
        }

        private TypeSyntax ParseTypeCore(ParseTypeMode mode)
        {
            NameOptions nameOptions;
            switch (mode)
            {
                case ParseTypeMode.AfterIs:
                    nameOptions = NameOptions.InExpression | NameOptions.AfterIs | NameOptions.PossiblePattern;
                    break;
                case ParseTypeMode.DefinitePattern:
                    nameOptions = NameOptions.InExpression | NameOptions.DefinitePattern | NameOptions.PossiblePattern;
                    break;
                case ParseTypeMode.AfterOut:
                    nameOptions = NameOptions.InExpression | NameOptions.AfterOut;
                    break;
                case ParseTypeMode.AfterTupleComma:
                    nameOptions = NameOptions.InExpression | NameOptions.AfterTupleComma;
                    break;
                case ParseTypeMode.FirstElementOfPossibleTupleLiteral:
                    nameOptions = NameOptions.InExpression | NameOptions.FirstElementOfPossibleTupleLiteral;
                    break;
                case ParseTypeMode.NewExpression:
                case ParseTypeMode.AsExpression:
                case ParseTypeMode.Normal:
                case ParseTypeMode.Parameter:
                    nameOptions = NameOptions.None;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(mode);
            }

            var type = this.ParseUnderlyingType(parentIsParameter: mode == ParseTypeMode.Parameter, options: nameOptions);
            Debug.Assert(type != null);

            int lastTokenPosition = -1;
            while (IsMakingProgress(ref lastTokenPosition))
            {
                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.QuestionToken when canBeNullableType():
                        {
                            var question = EatNullableQualifierIfApplicable(mode);
                            if (question != null)
                            {
                                type = _syntaxFactory.NullableType(type, question);
                                continue;
                            }
                            goto done; // token not consumed
                        }
                        bool canBeNullableType()
                        {
                            // These are the fast tests for (in)applicability.
                            // More expensive tests are in `EatNullableQualifierIfApplicable`
                            if (type.Kind == SyntaxKind.NullableType || type.Kind == SyntaxKind.PointerType)
                                return false;
                            if (this.PeekToken(1).Kind == SyntaxKind.OpenBracketToken)
                                return true;
                            if (mode == ParseTypeMode.DefinitePattern)
                                return false;
                            if (mode == ParseTypeMode.NewExpression && type.Kind == SyntaxKind.TupleType &&
                                this.PeekToken(1).Kind != SyntaxKind.OpenParenToken && this.PeekToken(1).Kind != SyntaxKind.OpenBraceToken)
                                return false; // Permit `new (int, int)?(t)` (creation) and `new (int, int) ? x : y` (conditional)
                            return true;
                        }
                    case SyntaxKind.AsteriskToken when type.Kind != SyntaxKind.ArrayType:
                        switch (mode)
                        {
                            case ParseTypeMode.AfterIs:
                            case ParseTypeMode.DefinitePattern:
                            case ParseTypeMode.AfterTupleComma:
                            case ParseTypeMode.FirstElementOfPossibleTupleLiteral:
                                // these contexts do not permit a pointer type except as an element type of an array.
                                if (PointerTypeModsFollowedByRankAndDimensionSpecifier())
                                {
                                    type = this.ParsePointerTypeMods(type);
                                    continue;
                                }
                                break;
                            case ParseTypeMode.Normal:
                            case ParseTypeMode.Parameter:
                            case ParseTypeMode.AfterOut:
                            case ParseTypeMode.AsExpression:
                            case ParseTypeMode.NewExpression:
                                type = this.ParsePointerTypeMods(type);
                                continue;
                        }
                        goto done; // token not consumed
                    case SyntaxKind.OpenBracketToken:
                        // Now check for arrays.
                        {
                            var ranks = _pool.Allocate<ArrayRankSpecifierSyntax>();
                            try
                            {
                                while (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                                {
                                    var rank = this.ParseArrayRankSpecifier(out _);
                                    ranks.Add(rank);
                                }

                                type = _syntaxFactory.ArrayType(type, ranks);
                            }
                            finally
                            {
                                _pool.Free(ranks);
                            }
                            continue;
                        }
                    default:
                        goto done; // token not consumed
                }
            }
done:;

            Debug.Assert(type != null);
            return type;
        }

        private SyntaxToken EatNullableQualifierIfApplicable(ParseTypeMode mode)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.QuestionToken);
            var resetPoint = this.GetResetPoint();
            try
            {
                var questionToken = this.EatToken();
                if (!canFollowNullableType())
                {
                    // Restore current token index
                    this.Reset(ref resetPoint);
                    return null;
                }

                return CheckFeatureAvailability(questionToken, MessageID.IDS_FeatureNullable);

                bool canFollowNullableType()
                {
                    switch (mode)
                    {
                        case ParseTypeMode.AfterIs:
                        case ParseTypeMode.DefinitePattern:
                        case ParseTypeMode.AsExpression:
                            // These contexts might be a type that is at the end of an expression.
                            // In these contexts we only permit the nullable qualifier if it is followed
                            // by a token that could not start an expression, because for backward
                            // compatibility we want to consider a `?` token as part of the `?:`
                            // operator if possible.
                            return !CanStartExpression();
                        case ParseTypeMode.NewExpression:
                            // A nullable qualifier is permitted as part of the type in a `new` expression.
                            // e.g. `new int?()` is allowed.  It creates a null value of type `Nullable<int>`.
                            // Similarly `new int? {}` is allowed.
                            return
                                this.CurrentToken.Kind == SyntaxKind.OpenParenToken ||   // ctor parameters
                                this.CurrentToken.Kind == SyntaxKind.OpenBracketToken ||   // array type
                                this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;   // object initializer
                        default:
                            return true;
                    }
                }
            }
            finally
            {
                this.Release(ref resetPoint);
            }
        }

        private bool PointerTypeModsFollowedByRankAndDimensionSpecifier()
        {
            // Are pointer specifiers (if any) followed by an array specifier?
            for (int i = 0; ; i++)
            {
                switch (this.PeekToken(i).Kind)
                {
                    case SyntaxKind.AsteriskToken:
                        continue;
                    case SyntaxKind.OpenBracketToken:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private ArrayRankSpecifierSyntax ParseArrayRankSpecifier(out bool sawNonOmittedSize)
        {
            sawNonOmittedSize = false;
            bool sawOmittedSize = false;
            var open = this.EatToken(SyntaxKind.OpenBracketToken);
            var list = _pool.AllocateSeparated<ExpressionSyntax>();
            try
            {
                var omittedArraySizeExpressionInstance = _syntaxFactory.OmittedArraySizeExpression(SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken));
                while (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        // NOTE: trivia will be attached to comma, not omitted array size
                        sawOmittedSize = true;
                        list.Add(omittedArraySizeExpressionInstance);
                        list.AddSeparator(this.EatToken());
                    }
                    else if (this.IsPossibleExpression())
                    {
                        var size = this.ParseExpressionCore();
                        sawNonOmittedSize = true;
                        list.Add(size);

                        if (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                        }
                    }
                    else if (this.SkipBadArrayRankSpecifierTokens(ref open, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                    {
                        break;
                    }
                }

                // Don't end on a comma.
                // If the omitted size would be the only element, then skip it unless sizes were expected.
                if (((list.Count & 1) == 0))
                {
                    sawOmittedSize = true;
                    list.Add(omittedArraySizeExpressionInstance);
                }

                // Never mix omitted and non-omitted array sizes.  If there were non-omitted array sizes,
                // then convert all of the omitted array sizes to missing identifiers.
                if (sawOmittedSize && sawNonOmittedSize)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].RawKind == (int)SyntaxKind.OmittedArraySizeExpression)
                        {
                            int width = list[i].Width;
                            int offset = list[i].GetLeadingTriviaWidth();
                            list[i] = this.AddError(this.CreateMissingIdentifierName(), offset, width, ErrorCode.ERR_ValueExpected);
                        }
                    }
                }

                // Eat the close brace and we're done.
                var close = this.EatToken(SyntaxKind.CloseBracketToken);
                return _syntaxFactory.ArrayRankSpecifier(open, list, close);
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private TupleTypeSyntax ParseTupleType()
        {
            var open = this.EatToken(SyntaxKind.OpenParenToken);
            var list = _pool.AllocateSeparated<TupleElementSyntax>();
            try
            {
                if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
                {
                    var element = ParseTupleElement();
                    list.Add(element);

                    while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        var comma = this.EatToken(SyntaxKind.CommaToken);
                        list.AddSeparator(comma);

                        element = ParseTupleElement();
                        list.Add(element);
                    }
                }

                if (list.Count < 2)
                {
                    if (list.Count < 1)
                    {
                        list.Add(_syntaxFactory.TupleElement(this.CreateMissingIdentifierName(), identifier: null));
                    }

                    list.AddSeparator(SyntaxFactory.MissingToken(SyntaxKind.CommaToken));
                    var missing = this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TupleTooFewElements);
                    list.Add(_syntaxFactory.TupleElement(missing, identifier: null));
                }

                var close = this.EatToken(SyntaxKind.CloseParenToken);
                var result = _syntaxFactory.TupleType(open, list, close);

                result = CheckFeatureAvailability(result, MessageID.IDS_FeatureTuples);

                return result;
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private TupleElementSyntax ParseTupleElement()
        {
            var type = ParseType();
            SyntaxToken name = null;

            if (IsTrueIdentifier())
            {
                name = this.ParseIdentifierToken();
            }

            return _syntaxFactory.TupleElement(type, name);
        }

        private PostSkipAction SkipBadArrayRankSpecifierTokens(ref SyntaxToken openBracket, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openBracket, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBracketToken || p.IsTerminator(),
                expected);
        }

        private TypeSyntax ParseUnderlyingType(bool parentIsParameter, NameOptions options = NameOptions.None)
        {
            if (IsPredefinedType(this.CurrentToken.Kind))
            {
                // This is a predefined type
                var token = this.EatToken();
                if (token.Kind == SyntaxKind.VoidKeyword && this.CurrentToken.Kind != SyntaxKind.AsteriskToken)
                {
                    token = this.AddError(token, parentIsParameter ? ErrorCode.ERR_NoVoidParameter : ErrorCode.ERR_NoVoidHere);
                }

                return _syntaxFactory.PredefinedType(token);
            }
            else if (IsTrueIdentifier())
            {
                return this.ParseQualifiedName(options);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                return this.ParseTupleType();
            }
            else
            {
                var name = this.CreateMissingIdentifierName();
                return this.AddError(name, ErrorCode.ERR_TypeExpected);
            }
        }

        private TypeSyntax ParsePointerTypeMods(TypeSyntax type)
        {
            // Check for pointer types
            while (this.CurrentToken.Kind == SyntaxKind.AsteriskToken)
            {
                type = _syntaxFactory.PointerType(type, this.EatToken());
            }

            return type;
        }

        public StatementSyntax ParseStatement()
        {
            return ParseWithStackGuard(
                () => ParseStatementCore() ?? ParseExpressionStatement(),
                () => SyntaxFactory.EmptyStatement(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)));
        }

        private StatementSyntax ParseStatementCore()
        {
            try
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNode is CSharp.Syntax.StatementSyntax)
                {
                    return (StatementSyntax)this.EatNode();
                }

                // First, try to parse as a non-declaration statement. If the statement is a single
                // expression then we only allow legal expression statements. (That is, "new C();",
                // "C();", "x = y;" and so on.)

                StatementSyntax result = ParseStatementNoDeclaration(allowAnyExpression: false);
                if (result != null)
                {
                    return result;
                }

                // We could not successfully parse the statement as a non-declaration. Try to parse
                // it as either a declaration or as an "await X();" statement that is in a non-async
                // method. 

                return ParsePossibleDeclarationOrBadAwaitStatement();
            }
            finally
            {
                _recursionDepth--;
            }
        }

        private StatementSyntax ParsePossibleDeclarationOrBadAwaitStatement()
        {
            ResetPoint resetPointBeforeStatement = this.GetResetPoint();
            try
            {
                StatementSyntax result = ParsePossibleDeclarationOrBadAwaitStatement(ref resetPointBeforeStatement);
                return result;
            }
            finally
            {
                this.Release(ref resetPointBeforeStatement);
            }
        }

        private StatementSyntax ParsePossibleDeclarationOrBadAwaitStatement(ref ResetPoint resetPointBeforeStatement)
        {
            // Precondition: We have already attempted to parse the statement as a non-declaration and failed.
            //
            // That means that we are in one of the following cases:
            //
            // 1) This is not a statement. This can happen if the start of the statement was an
            //    accessibility modifier, but the rest of the statement did not parse as a local
            //    function. If there was an accessibility modifier and the statement parsed as
            //    local function, that should be marked as a mistake with local function visibility.
            //    Otherwise, it's likely the user just forgot a closing brace on their method.
            // 2) This is a perfectly mundane and correct local declaration statement like "int x;"
            // 3) This is a perfectly mundane but erroneous local declaration statement, like "int X();"
            // 4) We are in the rare case of the code containing "await x;" and the intention is that
            //    "await" is the type of "x".  This only works in a non-async method.
            // 5) We have what would be a legal await statement, like "await X();", but we are not in
            //    an async method, so the parse failed. (Had we been in an async method then the parse
            //    attempt done by our caller would have succeeded.)
            // 6) The statement begins with "await" but is not a legal local declaration and not a legal
            //    await expression regardless of whether the method is marked as "async".

            bool beginsWithAwait = this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword;
            StatementSyntax result = ParseLocalDeclarationStatement();

            // Case (1)
            if (result == null)
            {
                this.Reset(ref resetPointBeforeStatement);
                return null;
            }

            // Cases (2), (3) and (4):
            if (!beginsWithAwait || !result.ContainsDiagnostics)
            {
                return result;
            }

            // The statement begins with "await" and could not be parsed as a legal declaration statement.
            // We know from our precondition that it is not a legal "await X();" statement, though it is
            // possible that it was only not legal because we were not in an async context.

            Debug.Assert(!IsInAsync);

            // Let's see if we're in case (5). Pretend that we're in an async method and see if parsing
            // a non-declaration statement would have succeeded.

            this.Reset(ref resetPointBeforeStatement);
            IsInAsync = true;
            result = ParseStatementNoDeclaration(allowAnyExpression: false);
            IsInAsync = false;

            if (!result.ContainsDiagnostics)
            {
                // We are in case (5). We do not report that we have an "await" expression in a non-async
                // method at parse time; rather we do that in BindAwait(), during the initial round of
                // semantic analysis.
                return result;
            }

            // We are in case (6); we can't figure out what is going on here. Our best guess is that it is
            // a malformed local declaration, so back up and re-parse it.

            this.Reset(ref resetPointBeforeStatement);
            result = ParseLocalDeclarationStatement();
            Debug.Assert(result.ContainsDiagnostics);

            return result;
        }

        /// <summary>
        /// Parses any statement but a declaration statement. Returns null if the lookahead looks like a declaration.
        /// </summary>
        /// <remarks>
        /// Variable declarations in global code are parsed as field declarations so we need to fallback if we encounter a declaration statement.
        /// </remarks>
        private StatementSyntax ParseStatementNoDeclaration(bool allowAnyExpression)
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.FixedKeyword:
                    return this.ParseFixedStatement();
                case SyntaxKind.BreakKeyword:
                    return this.ParseBreakStatement();
                case SyntaxKind.ContinueKeyword:
                    return this.ParseContinueStatement();
                case SyntaxKind.TryKeyword:
                case SyntaxKind.CatchKeyword:
                case SyntaxKind.FinallyKeyword:
                    return this.ParseTryStatement();
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                    return this.ParseCheckedStatement();
                case SyntaxKind.ConstKeyword:
                    return null;
                case SyntaxKind.DoKeyword:
                    return this.ParseDoStatement();
                case SyntaxKind.ForKeyword:
                    return this.ParseForOrForEachStatement();
                case SyntaxKind.ForEachKeyword:
                    return this.ParseForEachStatement(awaitTokenOpt: default);
                case SyntaxKind.GotoKeyword:
                    return this.ParseGotoStatement();
                case SyntaxKind.IfKeyword:
                case SyntaxKind.ElseKeyword: // Including 'else' keyword to handle 'else without if' error cases 
                    return this.ParseIfStatement();
                case SyntaxKind.LockKeyword:
                    return this.ParseLockStatement();
                case SyntaxKind.ReturnKeyword:
                    return this.ParseReturnStatement();
                case SyntaxKind.SwitchKeyword:
                    return this.ParseSwitchStatement();
                case SyntaxKind.ThrowKeyword:
                    return this.ParseThrowStatement();
                case SyntaxKind.UnsafeKeyword:
                    // Checking for brace to disambiguate between unsafe statement and unsafe local function
                    if (this.IsPossibleUnsafeStatement())
                    {
                        return this.ParseUnsafeStatement();
                    }
                    break;
                case SyntaxKind.UsingKeyword:
                    return PeekToken(1).Kind == SyntaxKind.OpenParenToken ? this.ParseUsingStatement() : this.ParseLocalDeclarationStatement();
                case SyntaxKind.WhileKeyword:
                    return this.ParseWhileStatement();
                case SyntaxKind.OpenBraceToken:
                    return this.ParseBlock();
                case SyntaxKind.SemicolonToken:
                    return _syntaxFactory.EmptyStatement(this.EatToken());
                case SyntaxKind.IdentifierToken:
                    if (isPossibleAwaitForEach())
                    {
                        return this.ParseForEachStatement(parseAwaitKeywordForAsyncStreams());
                    }
                    else if (isPossibleAwaitUsing())
                    {
                        if (PeekToken(2).Kind == SyntaxKind.OpenParenToken)
                        {
                            return this.ParseUsingStatement(parseAwaitKeywordForAsyncStreams());
                        }
                        else
                        {
                            return this.ParseLocalDeclarationStatement(parseAwaitKeywordForAsyncStreams());
                        }
                    }
                    else if (this.IsPossibleLabeledStatement())
                    {
                        return this.ParseLabeledStatement();
                    }
                    else if (this.IsPossibleYieldStatement())
                    {
                        return this.ParseYieldStatement();
                    }
                    else if (this.IsPossibleAwaitExpressionStatement())
                    {
                        return this.ParseExpressionStatement();
                    }
                    else if (this.IsQueryExpression(mayBeVariableDeclaration: true, mayBeMemberDeclaration: allowAnyExpression))
                    {
                        return this.ParseExpressionStatement(this.ParseQueryExpression(0));
                    }
                    break;
            }

            if (this.IsPossibleLocalDeclarationStatement(allowAnyExpression))
            {
                return null;
            }
            else
            {
                return this.ParseExpressionStatement();
            }

            bool isPossibleAwaitForEach()
            {
                return this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword &&
                    this.PeekToken(1).Kind == SyntaxKind.ForEachKeyword;
            }

            bool isPossibleAwaitUsing()
            {
                return this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword &&
                    this.PeekToken(1).Kind == SyntaxKind.UsingKeyword;
            }

            SyntaxToken parseAwaitKeywordForAsyncStreams()
            {
                Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword);
                SyntaxToken awaitToken = this.EatContextualToken(SyntaxKind.AwaitKeyword);
                return CheckFeatureAvailability(awaitToken, MessageID.IDS_FeatureAsyncStreams);
            }
        }

        private bool IsPossibleLabeledStatement()
        {
            return this.PeekToken(1).Kind == SyntaxKind.ColonToken && this.IsTrueIdentifier();
        }

        private bool IsPossibleUnsafeStatement()
        {
            return this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken;
        }

        private bool IsPossibleYieldStatement()
        {
            return this.CurrentToken.ContextualKind == SyntaxKind.YieldKeyword && (this.PeekToken(1).Kind == SyntaxKind.ReturnKeyword || this.PeekToken(1).Kind == SyntaxKind.BreakKeyword);
        }

        private bool IsPossibleLocalDeclarationStatement(bool allowAnyExpression)
        {
            // This method decides whether to parse a statement as a
            // declaration or as an expression statement. In the old
            // compiler it would simply call IsLocalDeclaration.

            var tk = this.CurrentToken.Kind;
            if (tk == SyntaxKind.RefKeyword ||
                IsDeclarationModifier(tk) || // treat `static int x = 2;` as a local variable declaration
                (SyntaxFacts.IsPredefinedType(tk) &&
                        this.PeekToken(1).Kind != SyntaxKind.DotToken && // e.g. `int.Parse()` is an expression
                        this.PeekToken(1).Kind != SyntaxKind.OpenParenToken)) // e.g. `int (x, y)` is an error decl expression
            {
                return true;
            }

            tk = this.CurrentToken.ContextualKind;
            if (IsAdditionalLocalFunctionModifier(tk) &&
                (tk != SyntaxKind.AsyncKeyword || ShouldAsyncBeTreatedAsModifier(parsingStatementNotDeclaration: true)))
            {
                return true;
            }

            bool? typedIdentifier = IsPossibleTypedIdentifierStart(this.CurrentToken, this.PeekToken(1), allowThisKeyword: false);
            if (typedIdentifier != null)
            {
                return typedIdentifier.Value;
            }

            // It's common to have code like the following:
            // 
            //      Task.
            //      await Task.Delay()
            //
            // In this case we don't want to parse this as as a local declaration like:
            //
            //      Task.await Task
            //
            // This does not represent user intent, and it causes all sorts of problems to higher 
            // layers.  This is because both the parse tree is strange, and the symbol tables have
            // entries that throw things off (like a bogus 'Task' local).
            //
            // Note that we explicitly do this check when we see that the code spreads over multiple 
            // lines.  We don't want this if the user has actually written "X.Y z"
            if (tk == SyntaxKind.IdentifierToken)
            {
                var token1 = PeekToken(1);
                if (token1.Kind == SyntaxKind.DotToken &&
                    token1.TrailingTrivia.Any((int)SyntaxKind.EndOfLineTrivia))
                {
                    if (PeekToken(2).Kind == SyntaxKind.IdentifierToken &&
                        PeekToken(3).Kind == SyntaxKind.IdentifierToken)
                    {
                        // We have something like:
                        //
                        //      X.
                        //      Y z
                        //
                        // This is only a local declaration if we have:
                        //
                        //      X.Y z;
                        //      X.Y z = ...
                        //      X.Y z, ...  
                        //      X.Y z( ...      (local function) 
                        //      X.Y z<W...      (local function)
                        //
                        var token4Kind = PeekToken(4).Kind;
                        if (token4Kind != SyntaxKind.SemicolonToken &&
                            token4Kind != SyntaxKind.EqualsToken &&
                            token4Kind != SyntaxKind.CommaToken &&
                            token4Kind != SyntaxKind.OpenParenToken &&
                            token4Kind != SyntaxKind.LessThanToken)
                        {
                            return false;
                        }
                    }
                }
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                ScanTypeFlags st = this.ScanType();

                // We could always return true for st == AliasQualName in addition to MustBeType on the first line, however, we want it to return false in the case where
                // CurrentToken.Kind != SyntaxKind.Identifier so that error cases, like: A::N(), are not parsed as variable declarations and instead are parsed as A.N() where we can give
                // a better error message saying "did you meant to use a '.'?"
                if (st == ScanTypeFlags.MustBeType && this.CurrentToken.Kind != SyntaxKind.DotToken && this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
                {
                    return true;
                }

                if (st == ScanTypeFlags.NotType || this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
                {
                    return false;
                }

                // T? and T* might start an expression, we need to parse further to disambiguate:
                if (allowAnyExpression)
                {
                    if (st == ScanTypeFlags.PointerOrMultiplication)
                    {
                        return false;
                    }
                    else if (st == ScanTypeFlags.NullableType)
                    {
                        return IsPossibleDeclarationStatementFollowingNullableType();
                    }
                }

                return true;
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        // Looks ahead for a declaration of a field, property or method declaration following a nullable type T?.
        private bool IsPossibleDeclarationStatementFollowingNullableType()
        {
            if (IsFieldDeclaration(isEvent: false))
            {
                return IsPossibleFieldDeclarationFollowingNullableType();
            }

            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt;
            SyntaxToken identifierOrThisOpt;
            TypeParameterListSyntax typeParameterListOpt;
            this.ParseMemberName(out explicitInterfaceOpt, out identifierOrThisOpt, out typeParameterListOpt, isEvent: false);

            if (explicitInterfaceOpt == null && identifierOrThisOpt == null && typeParameterListOpt == null)
            {
                return false;
            }

            // looks like a property:
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                return true;
            }

            // don't accept indexers:
            if (identifierOrThisOpt.Kind == SyntaxKind.ThisKeyword)
            {
                return false;
            }

            return IsPossibleMethodDeclarationFollowingNullableType();
        }

        // At least one variable declaration terminated by a semicolon or a comma.
        //   idf;
        //   idf,
        //   idf = <expr>;
        //   idf = <expr>, 
        private bool IsPossibleFieldDeclarationFollowingNullableType()
        {
            if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
            {
                return false;
            }

            this.EatToken();

            if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                var saveTerm = _termState;
                _termState |= TerminatorState.IsEndOfFieldDeclaration;
                this.EatToken();
                this.ParseVariableInitializer();
                _termState = saveTerm;
            }

            return this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private bool IsPossibleMethodDeclarationFollowingNullableType()
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;

            var paramList = this.ParseParenthesizedParameterList();

            _termState = saveTerm;
            var separatedParameters = paramList.Parameters.GetWithSeparators();

            // parsed full signature:
            if (!paramList.CloseParenToken.IsMissing)
            {
                // (...) {
                // (...) where
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    return true;
                }

                // disambiguates conditional expressions
                // (...) :
                if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                {
                    return false;
                }
            }

            // no parameters, just an open paren followed by a token that doesn't belong to a parameter definition:
            if (separatedParameters.Count == 0)
            {
                return false;
            }

            var parameter = (ParameterSyntax)separatedParameters[0];

            // has an attribute:
            //   ([Attr]
            if (parameter.AttributeLists.Count > 0)
            {
                return true;
            }

            // has params modifier:
            //   (params
            for (int i = 0; i < parameter.Modifiers.Count; i++)
            {
                if (parameter.Modifiers[i].Kind == SyntaxKind.ParamsKeyword)
                {
                    return true;
                }
            }

            if (parameter.Type == null)
            {
                // has arglist:
                //   (__arglist
                if (parameter.Identifier.Kind == SyntaxKind.ArgListKeyword)
                {
                    return true;
                }
            }
            else if (parameter.Type.Kind == SyntaxKind.NullableType)
            {
                // nullable type with modifiers
                //   (ref T?
                //   (out T?
                if (parameter.Modifiers.Count > 0)
                {
                    return true;
                }

                // nullable type, identifier, and separator or closing parent
                //   (T ? idf,
                //   (T ? idf)
                if (!parameter.Identifier.IsMissing &&
                    (separatedParameters.Count >= 2 && !separatedParameters[1].IsMissing ||
                     separatedParameters.Count == 1 && !paramList.CloseParenToken.IsMissing))
                {
                    return true;
                }
            }
            else if (parameter.Type.Kind == SyntaxKind.IdentifierName &&
                    ((IdentifierNameSyntax)parameter.Type).Identifier.ContextualKind == SyntaxKind.FromKeyword)
            {
                // assume that "from" is meant to be a query start ("from" bound to a type is rare):
                // (from
                return false;
            }
            else
            {
                // has a name and a non-nullable type:
                //   (T idf
                //   (ref T idf
                //   (out T idf
                if (!parameter.Identifier.IsMissing)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPossibleNewExpression()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.NewKeyword);

            // skip new
            SyntaxToken nextToken = PeekToken(1);

            // new { }
            // new [ ]
            switch (nextToken.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.OpenBracketToken:
                    return true;
            }

            //
            // Declaration with new modifier vs. new expression
            // Parse it as an expression if the type is not followed by an identifier or this keyword.
            //
            // Member declarations:
            //   new T Idf ...
            //   new T this ...
            //   new partial Idf    ("partial" as a type name)
            //   new partial this   ("partial" as a type name)
            //   new partial T Idf
            //   new partial T this
            //   new <modifier>
            //   new <class|interface|struct|enum>
            //   new partial <class|interface|struct|enum>
            //
            // New expressions:
            //   new T []
            //   new T { }
            //   new <non-type>
            //
            if (SyntaxFacts.GetBaseTypeDeclarationKind(nextToken.Kind) != SyntaxKind.None)
            {
                return false;
            }

            DeclarationModifiers modifier = GetModifier(nextToken);
            if (modifier == DeclarationModifiers.Partial)
            {
                if (SyntaxFacts.IsPredefinedType(PeekToken(2).Kind))
                {
                    return false;
                }

                // class, struct, enum, interface keywords, but also other modifiers that are not allowed after 
                // partial keyword but start class declaration, so we can assume the user just swapped them.
                if (IsPossibleStartOfTypeDeclaration(PeekToken(2).Kind))
                {
                    return false;
                }
            }
            else if (modifier != DeclarationModifiers.None)
            {
                return false;
            }

            bool? typedIdentifier = IsPossibleTypedIdentifierStart(nextToken, PeekToken(2), allowThisKeyword: true);
            if (typedIdentifier != null)
            {
                // new Idf Idf
                // new Idf .
                // new partial T
                // new partial .
                return !typedIdentifier.Value;
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                // skips new keyword
                EatToken();

                ScanTypeFlags st = this.ScanType();

                return !IsPossibleMemberName() || st == ScanTypeFlags.NotType;
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        /// <returns>
        /// true if the current token can be the first token of a typed identifier (a type name followed by an identifier),
        /// false if it definitely can't be,
        /// null if we need to scan further to find out.
        /// </returns>
        private bool? IsPossibleTypedIdentifierStart(SyntaxToken current, SyntaxToken next, bool allowThisKeyword)
        {
            if (IsTrueIdentifier(current))
            {
                switch (next.Kind)
                {
                    // tokens that can be in type names...
                    case SyntaxKind.DotToken:
                    case SyntaxKind.AsteriskToken:
                    case SyntaxKind.QuestionToken:
                    case SyntaxKind.OpenBracketToken:
                    case SyntaxKind.LessThanToken:
                    case SyntaxKind.ColonColonToken:
                        return null;

                    case SyntaxKind.OpenParenToken:
                        if (current.IsIdentifierVar())
                        {
                            // potentially either a tuple type in a local declaration (true), or
                            // a tuple lvalue in a deconstruction assignment (false).
                            return null;
                        }
                        else
                        {
                            return false;
                        }

                    case SyntaxKind.IdentifierToken:
                        return IsTrueIdentifier(next);

                    case SyntaxKind.ThisKeyword:
                        return allowThisKeyword;

                    default:
                        return false;
                }
            }

            return null;
        }

        // If "isMethodBody" is true, then this is the immediate body of a method/accessor.
        // In this case, we create a many-child list if the body is not a small single statement.
        // This then allows a "with many weak children" red node when the red node is created.
        // If "isAccessorBody" is true, then we produce a special diagnostic if the open brace is
        // missing.  Also, "isMethodBody" must be true.
        private BlockSyntax ParseBlock(bool isMethodBody = false, bool isAccessorBody = false)
        {
            // Check again for incremental re-use, since ParseBlock is called from a bunch of places
            // other than ParseStatementCore()
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.Block)
            {
                return (BlockSyntax)this.EatNode();
            }

            // There's a special error code for a missing token after an accessor keyword
            var openBrace = isAccessorBody && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken
                ? this.AddError(
                    SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                    IsFeatureEnabled(MessageID.IDS_FeatureExpressionBodiedAccessor)
                            ? ErrorCode.ERR_SemiOrLBraceOrArrowExpected
                            : ErrorCode.ERR_SemiOrLBraceExpected)
                : this.EatToken(SyntaxKind.OpenBraceToken);

            var statements = _pool.Allocate<StatementSyntax>();
            try
            {
                CSharpSyntaxNode tmp = openBrace;
                this.ParseStatements(ref tmp, statements, stopOnSwitchSections: false);
                openBrace = (SyntaxToken)tmp;
                var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);

                SyntaxList<StatementSyntax> statementList;
                if (isMethodBody && IsLargeEnoughNonEmptyStatementList(statements))
                {
                    // Force creation a many-children list, even if only 1, 2, or 3 elements in the statement list.
                    statementList = new SyntaxList<StatementSyntax>(SyntaxList.List(((SyntaxListBuilder)statements).ToArray()));
                }
                else
                {
                    statementList = statements;
                }

                return _syntaxFactory.Block(openBrace, statementList, closeBrace);
            }
            finally
            {
                _pool.Free(statements);
            }
        }

        // Is this statement list non-empty, and large enough to make using weak children beneficial?
        private static bool IsLargeEnoughNonEmptyStatementList(SyntaxListBuilder<StatementSyntax> statements)
        {
            if (statements.Count == 0)
            {
                return false;
            }
            else if (statements.Count == 1)
            {
                // If we have a single statement, it might be small, like "return null", or large,
                // like a loop or if or switch with many statements inside. Use the width as a proxy for
                // how big it is. If it's small, its better to forgo a many children list anyway, since the
                // weak reference would consume as much memory as is saved.
                return statements[0].Width > 60;
            }
            else
            {
                // For 2 or more statements, go ahead and create a many-children lists.
                return true;
            }
        }

        private void ParseStatements(ref CSharpSyntaxNode previousNode, SyntaxListBuilder<StatementSyntax> statements, bool stopOnSwitchSections)
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsPossibleStatementStartOrStop; // partial statements can abort if a new statement starts
            if (stopOnSwitchSections)
            {
                _termState |= TerminatorState.IsSwitchSectionStart;
            }

            while (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken
                && this.CurrentToken.Kind != SyntaxKind.EndOfFileToken
                && !(stopOnSwitchSections && this.IsPossibleSwitchSection()))
            {
                if (this.IsPossibleStatement(acceptAccessibilityMods: true))
                {
                    var statement = this.ParseStatementCore();
                    if (statement != null)
                    {
                        statements.Add(statement);
                        continue;
                    }
                }

                GreenNode trailingTrivia;
                var action = this.SkipBadStatementListTokens(statements, SyntaxKind.CloseBraceToken, out trailingTrivia);
                if (trailingTrivia != null)
                {
                    previousNode = AddTrailingSkippedSyntax(previousNode, trailingTrivia);
                }

                if (action == PostSkipAction.Abort)
                {
                    break;
                }
            }

            _termState = saveTerm;
        }

        private bool IsPossibleStatementStartOrStop()
        {
            return this.CurrentToken.Kind == SyntaxKind.SemicolonToken
                || this.IsPossibleStatement(acceptAccessibilityMods: true);
        }

        private PostSkipAction SkipBadStatementListTokens(SyntaxListBuilder<StatementSyntax> statements, SyntaxKind expected, out GreenNode trailingTrivia)
        {
            return this.SkipBadListTokensWithExpectedKindHelper(
                statements,
                // We know we have a bad statement, so it can't be a local
                // function, meaning we shouldn't consider accessibility
                // modifiers to be the start of a statement
                p => !p.IsPossibleStatement(acceptAccessibilityMods: false),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBraceToken || p.IsTerminator(),
                expected,
                out trailingTrivia
            );
        }

        private bool IsPossibleStatement(bool acceptAccessibilityMods)
        {
            var tk = this.CurrentToken.Kind;
            switch (tk)
            {
                case SyntaxKind.FixedKeyword:
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.TryKeyword:
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.DoKeyword:
                case SyntaxKind.ForKeyword:
                case SyntaxKind.ForEachKeyword:
                case SyntaxKind.GotoKeyword:
                case SyntaxKind.IfKeyword:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.LockKeyword:
                case SyntaxKind.ReturnKeyword:
                case SyntaxKind.SwitchKeyword:
                case SyntaxKind.ThrowKeyword:
                case SyntaxKind.UnsafeKeyword:
                case SyntaxKind.UsingKeyword:
                case SyntaxKind.WhileKeyword:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                case SyntaxKind.VolatileKeyword:
                case SyntaxKind.RefKeyword:
                    return true;

                case SyntaxKind.IdentifierToken:
                    return IsTrueIdentifier();

                case SyntaxKind.CatchKeyword:
                case SyntaxKind.FinallyKeyword:
                    return !_isInTry;

                // Accessibility modifiers are not legal in a statement,
                // but a common mistake for local functions. Parse to give a
                // better error message.
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.PrivateKeyword:
                    return acceptAccessibilityMods;

                default:
                    return IsPredefinedType(tk)
                        || IsPossibleExpression();
            }
        }

        private FixedStatementSyntax ParseFixedStatement()
        {
            var @fixed = this.EatToken(SyntaxKind.FixedKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFixedStatement;
            var decl = ParseVariableDeclaration();
            _termState = saveTerm;

            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            StatementSyntax statement = this.ParseEmbeddedStatement();
            return _syntaxFactory.FixedStatement(@fixed, openParen, decl, closeParen, statement);
        }

        private bool IsEndOfFixedStatement()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                || this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private StatementSyntax ParseEmbeddedStatement()
        {
            // The consumers of embedded statements are expecting to receive a non-null statement 
            // yet there are several error conditions that can lead ParseStatementCore to return 
            // null.  When that occurs create an error empty Statement and return it to the caller.
            StatementSyntax statement = this.ParseStatementCore() ?? SyntaxFactory.EmptyStatement(EatToken(SyntaxKind.SemicolonToken));

            switch (statement.Kind)
            {
                // In scripts, stand-alone expression statements may not be followed by semicolons.
                // ParseExpressionStatement hides the error.
                // However, embedded expression statements are required to be followed by semicolon. 
                case SyntaxKind.ExpressionStatement:
                    if (IsScript)
                    {
                        var expressionStatementSyntax = (ExpressionStatementSyntax)statement;
                        var semicolonToken = expressionStatementSyntax.SemicolonToken;

                        // Do not add a new error if the same error was already added.
                        if (semicolonToken.IsMissing &&
                            !semicolonToken.GetDiagnostics().Contains(diagnosticInfo => (ErrorCode)diagnosticInfo.Code == ErrorCode.ERR_SemicolonExpected))
                        {
                            semicolonToken = this.AddError(semicolonToken, ErrorCode.ERR_SemicolonExpected);
                            statement = expressionStatementSyntax.Update(expressionStatementSyntax.Expression, semicolonToken);
                        }
                    }

                    break;
            }

            return statement;
        }

        private BreakStatementSyntax ParseBreakStatement()
        {
            var breakKeyword = this.EatToken(SyntaxKind.BreakKeyword);
            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.BreakStatement(breakKeyword, semicolon);
        }

        private ContinueStatementSyntax ParseContinueStatement()
        {
            var continueKeyword = this.EatToken(SyntaxKind.ContinueKeyword);
            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.ContinueStatement(continueKeyword, semicolon);
        }

        private TryStatementSyntax ParseTryStatement()
        {
            var isInTry = _isInTry;
            _isInTry = true;

            var @try = this.EatToken(SyntaxKind.TryKeyword);

            BlockSyntax block;
            if (@try.IsMissing)
            {
                block = _syntaxFactory.Block(this.EatToken(SyntaxKind.OpenBraceToken), default(SyntaxList<StatementSyntax>), this.EatToken(SyntaxKind.CloseBraceToken));
            }
            else
            {
                var saveTerm = _termState;
                _termState |= TerminatorState.IsEndOfTryBlock;
                block = this.ParseBlock();
                _termState = saveTerm;
            }

            var catches = default(SyntaxListBuilder<CatchClauseSyntax>);
            FinallyClauseSyntax @finally = null;
            try
            {
                bool hasEnd = false;

                if (this.CurrentToken.Kind == SyntaxKind.CatchKeyword)
                {
                    hasEnd = true;
                    catches = _pool.Allocate<CatchClauseSyntax>();
                    while (this.CurrentToken.Kind == SyntaxKind.CatchKeyword)
                    {
                        catches.Add(this.ParseCatchClause());
                    }
                }

                if (this.CurrentToken.Kind == SyntaxKind.FinallyKeyword)
                {
                    hasEnd = true;
                    var fin = this.EatToken();
                    var finBlock = this.ParseBlock();
                    @finally = _syntaxFactory.FinallyClause(fin, finBlock);
                }

                if (!hasEnd)
                {
                    block = this.AddErrorToLastToken(block, ErrorCode.ERR_ExpectedEndTry);

                    // synthesize missing tokens for "finally { }":
                    @finally = _syntaxFactory.FinallyClause(
                        SyntaxToken.CreateMissing(SyntaxKind.FinallyKeyword, null, null),
                        _syntaxFactory.Block(
                            SyntaxToken.CreateMissing(SyntaxKind.OpenBraceToken, null, null),
                            default(SyntaxList<StatementSyntax>),
                            SyntaxToken.CreateMissing(SyntaxKind.CloseBraceToken, null, null)));
                }

                _isInTry = isInTry;

                return _syntaxFactory.TryStatement(@try, block, catches, @finally);
            }
            finally
            {
                if (!catches.IsNull)
                {
                    _pool.Free(catches);
                }
            }
        }

        private bool IsEndOfTryBlock()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseBraceToken
                || this.CurrentToken.Kind == SyntaxKind.CatchKeyword
                || this.CurrentToken.Kind == SyntaxKind.FinallyKeyword;
        }

        private CatchClauseSyntax ParseCatchClause()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.CatchKeyword);

            var @catch = this.EatToken();

            CatchDeclarationSyntax decl = null;
            var saveTerm = _termState;

            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var openParen = this.EatToken();

                _termState |= TerminatorState.IsEndOfCatchClause;
                var type = this.ParseType();
                SyntaxToken name = null;
                if (this.IsTrueIdentifier())
                {
                    name = this.ParseIdentifierToken();
                }

                _termState = saveTerm;

                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                decl = _syntaxFactory.CatchDeclaration(openParen, type, name, closeParen);
            }

            CatchFilterClauseSyntax filter = null;

            var keywordKind = this.CurrentToken.ContextualKind;
            if (keywordKind == SyntaxKind.WhenKeyword || keywordKind == SyntaxKind.IfKeyword)
            {
                var whenKeyword = this.EatContextualToken(SyntaxKind.WhenKeyword);
                if (keywordKind == SyntaxKind.IfKeyword)
                {
                    // The initial design of C# exception filters called for the use of the
                    // "if" keyword in this position.  We've since changed to "when", but 
                    // the error recovery experience for early adopters (and for old source
                    // stored in the symbol server) will be better if we consume "if" as
                    // though it were "when".
                    whenKeyword = AddTrailingSkippedSyntax(whenKeyword, EatToken());
                }
                whenKeyword = CheckFeatureAvailability(whenKeyword, MessageID.IDS_FeatureExceptionFilter);
                _termState |= TerminatorState.IsEndOfFilterClause;
                var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                var filterExpression = this.ParseExpressionCore();

                _termState = saveTerm;
                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                filter = _syntaxFactory.CatchFilterClause(whenKeyword, openParen, filterExpression, closeParen);
            }

            _termState |= TerminatorState.IsEndOfCatchBlock;
            var block = this.ParseBlock();
            _termState = saveTerm;

            return _syntaxFactory.CatchClause(@catch, decl, filter, block);
        }

        private bool IsEndOfCatchClause()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                || this.CurrentToken.Kind == SyntaxKind.CloseBraceToken
                || this.CurrentToken.Kind == SyntaxKind.CatchKeyword
                || this.CurrentToken.Kind == SyntaxKind.FinallyKeyword;
        }

        private bool IsEndOfFilterClause()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                || this.CurrentToken.Kind == SyntaxKind.CloseBraceToken
                || this.CurrentToken.Kind == SyntaxKind.CatchKeyword
                || this.CurrentToken.Kind == SyntaxKind.FinallyKeyword;
        }
        private bool IsEndOfCatchBlock()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseBraceToken
                || this.CurrentToken.Kind == SyntaxKind.CatchKeyword
                || this.CurrentToken.Kind == SyntaxKind.FinallyKeyword;
        }

        private StatementSyntax ParseCheckedStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.CheckedKeyword || this.CurrentToken.Kind == SyntaxKind.UncheckedKeyword);

            if (this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
            {
                return this.ParseExpressionStatement();
            }

            var spec = this.EatToken();
            var block = this.ParseBlock();
            return _syntaxFactory.CheckedStatement(SyntaxFacts.GetCheckStatement(spec.Kind), spec, block);
        }

        private DoStatementSyntax ParseDoStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.DoKeyword);
            var @do = this.EatToken(SyntaxKind.DoKeyword);
            var statement = this.ParseEmbeddedStatement();
            var @while = this.EatToken(SyntaxKind.WhileKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfDoWhileExpression;
            var expression = this.ParseExpressionCore();
            _termState = saveTerm;

            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.DoStatement(@do, statement, @while, openParen, expression, closeParen, semicolon);
        }

        private bool IsEndOfDoWhileExpression()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private StatementSyntax ParseForOrForEachStatement()
        {
            // Check if the user wrote the following accidentally:
            //
            // for (SomeType t in
            //
            // instead of
            //
            // foreach (SomeType t in
            //
            // In that case, parse it as a foreach, but given the appropriate message that a
            // 'foreach' keyword was expected.
            var resetPoint = this.GetResetPoint();
            try
            {
                Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForKeyword);
                this.EatToken();
                if (this.EatToken().Kind == SyntaxKind.OpenParenToken &&
                    this.ScanType() != ScanTypeFlags.NotType &&
                    this.EatToken().Kind == SyntaxKind.IdentifierToken &&
                    this.EatToken().Kind == SyntaxKind.InKeyword)
                {
                    // Looks like a foreach statement.  Parse it that way instead
                    this.Reset(ref resetPoint);
                    return this.ParseForEachStatement(awaitTokenOpt: default);
                }
                else
                {
                    // Normal for statement.
                    this.Reset(ref resetPoint);
                    return this.ParseForStatement();
                }
            }
            finally
            {
                this.Release(ref resetPoint);
            }
        }

        private ForStatementSyntax ParseForStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForKeyword);

            var forToken = this.EatToken(SyntaxKind.ForKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfForStatementArgument;

            var resetPoint = this.GetResetPoint();
            var initializers = _pool.AllocateSeparated<ExpressionSyntax>();
            var incrementors = _pool.AllocateSeparated<ExpressionSyntax>();
            try
            {
                // Here can be either a declaration or an expression statement list.  Scan
                // for a declaration first.
                VariableDeclarationSyntax decl = null;
                bool isDeclaration = false;
                if (this.CurrentToken.Kind == SyntaxKind.RefKeyword)
                {
                    isDeclaration = true;
                }
                else
                {
                    isDeclaration = !this.IsQueryExpression(mayBeVariableDeclaration: true, mayBeMemberDeclaration: false) &&
                                    this.ScanType() != ScanTypeFlags.NotType &&
                                    this.IsTrueIdentifier();

                    this.Reset(ref resetPoint);
                }

                if (isDeclaration)
                {
                    decl = ParseVariableDeclaration();
                    if (decl.Type.Kind == SyntaxKind.RefType)
                    {
                        decl = decl.Update(
                            CheckFeatureAvailability(decl.Type, MessageID.IDS_FeatureRefFor),
                            decl.Variables);
                    }
                }
                else if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                {
                    // Not a type followed by an identifier, so it must be an expression list.
                    this.ParseForStatementExpressionList(ref openParen, initializers);
                }

                var semi = this.EatToken(SyntaxKind.SemicolonToken);
                ExpressionSyntax condition = null;
                if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                {
                    condition = this.ParseExpressionCore();
                }

                var semi2 = this.EatToken(SyntaxKind.SemicolonToken);
                if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
                {
                    this.ParseForStatementExpressionList(ref semi2, incrementors);
                }

                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                var statement = ParseEmbeddedStatement();
                return _syntaxFactory.ForStatement(forToken, openParen, decl, initializers, semi, condition, semi2, incrementors, closeParen, statement);
            }
            finally
            {
                _termState = saveTerm;
                this.Release(ref resetPoint);
                _pool.Free(incrementors);
                _pool.Free(initializers);
            }
        }

        private bool IsEndOfForStatementArgument()
        {
            return this.CurrentToken.Kind == SyntaxKind.SemicolonToken
                || this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;
        }

        private void ParseForStatementExpressionList(ref SyntaxToken startToken, SeparatedSyntaxListBuilder<ExpressionSyntax> list)
        {
            if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
            {
tryAgain:
                if (this.IsPossibleExpression() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // first argument
                    list.Add(this.ParseExpressionCore());

                    // additional arguments
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken || this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleExpression())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            list.Add(this.ParseExpressionCore());
                            continue;
                        }
                        else if (this.SkipBadForStatementExpressionListTokens(ref startToken, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadForStatementExpressionListTokens(ref startToken, list, SyntaxKind.IdentifierToken) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }
            }
        }

        private PostSkipAction SkipBadForStatementExpressionListTokens(ref SyntaxToken startToken, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref startToken, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseParenToken || p.CurrentToken.Kind == SyntaxKind.SemicolonToken || p.IsTerminator(),
                expected);
        }

        private CommonForEachStatementSyntax ParseForEachStatement(SyntaxToken awaitTokenOpt)
        {
            // Can be a 'for' keyword if the user typed: 'for (SomeType t in'
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForEachKeyword || this.CurrentToken.Kind == SyntaxKind.ForKeyword);

            // Syntax for foreach is either:
            //  foreach [await] ( <type> <identifier> in <expr> ) <embedded-statement>
            // or
            //  foreach [await] ( <deconstruction-declaration> in <expr> ) <embedded-statement>

            SyntaxToken @foreach;

            // If we're at a 'for', then consume it and attach
            // it as skipped text to the missing 'foreach' token.
            if (this.CurrentToken.Kind == SyntaxKind.ForKeyword)
            {
                var skippedForToken = this.EatToken();
                skippedForToken = this.AddError(skippedForToken, ErrorCode.ERR_SyntaxError, SyntaxFacts.GetText(SyntaxKind.ForEachKeyword), SyntaxFacts.GetText(SyntaxKind.ForKeyword));
                @foreach = ConvertToMissingWithTrailingTrivia(skippedForToken, SyntaxKind.ForEachKeyword);
            }
            else
            {
                @foreach = this.EatToken(SyntaxKind.ForEachKeyword);
            }

            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var variable = ParseExpressionOrDeclaration(ParseTypeMode.Normal, feature: MessageID.IDS_FeatureTuples, permitTupleDesignation: true);
            var @in = this.EatToken(SyntaxKind.InKeyword, ErrorCode.ERR_InExpected);
            if (!IsValidForeachVariable(variable))
            {
                @in = this.AddError(@in, ErrorCode.ERR_BadForeachDecl);
            }

            var expression = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement();

            if (variable is DeclarationExpressionSyntax decl)
            {
                if (decl.Type.Kind == SyntaxKind.RefType)
                {
                    decl = decl.Update(
                        CheckFeatureAvailability(decl.Type, MessageID.IDS_FeatureRefForEach),
                        decl.Designation);
                }


                if (decl.designation.Kind != SyntaxKind.ParenthesizedVariableDesignation)
                {
                    // if we see a foreach declaration that isn't a deconstruction, we use the old form of foreach syntax node.
                    SyntaxToken identifier;
                    switch (decl.designation.Kind)
                    {
                        case SyntaxKind.SingleVariableDesignation:
                            identifier = ((SingleVariableDesignationSyntax)decl.designation).identifier;
                            break;
                        case SyntaxKind.DiscardDesignation:
                            // revert the identifier from its contextual underscore back to an identifier.
                            var discard = ((DiscardDesignationSyntax)decl.designation).underscoreToken;
                            Debug.Assert(discard.Kind == SyntaxKind.UnderscoreToken);
                            identifier = SyntaxToken.WithValue(SyntaxKind.IdentifierToken, discard.LeadingTrivia.Node, discard.Text, discard.ValueText, discard.TrailingTrivia.Node);
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(decl.designation.Kind);
                    }

                    return _syntaxFactory.ForEachStatement(awaitTokenOpt, @foreach, openParen, decl.Type, identifier, @in, expression, closeParen, statement);
                }
            }

            return _syntaxFactory.ForEachVariableStatement(awaitTokenOpt, @foreach, openParen, variable, @in, expression, closeParen, statement);
        }

        private static bool IsValidForeachVariable(ExpressionSyntax variable)
        {
            switch (variable.Kind)
            {
                case SyntaxKind.DeclarationExpression:
                    // e.g. `foreach (var (x, y) in e)`
                    return true;
                case SyntaxKind.TupleExpression:
                    // e.g. `foreach ((var x, var y) in e)`
                    return true;
                case SyntaxKind.IdentifierName:
                    // e.g. `foreach (_ in e)`
                    return ((IdentifierNameSyntax)variable).Identifier.ContextualKind == SyntaxKind.UnderscoreToken;
                default:
                    return false;
            }
        }

        private GotoStatementSyntax ParseGotoStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.GotoKeyword);

            var @goto = this.EatToken(SyntaxKind.GotoKeyword);

            SyntaxToken caseOrDefault = null;
            ExpressionSyntax arg = null;
            SyntaxKind kind;

            if (this.CurrentToken.Kind == SyntaxKind.CaseKeyword || this.CurrentToken.Kind == SyntaxKind.DefaultKeyword)
            {
                caseOrDefault = this.EatToken();
                if (caseOrDefault.Kind == SyntaxKind.CaseKeyword)
                {
                    kind = SyntaxKind.GotoCaseStatement;
                    arg = this.ParseExpressionCore();
                }
                else
                {
                    kind = SyntaxKind.GotoDefaultStatement;
                }
            }
            else
            {
                kind = SyntaxKind.GotoStatement;
                arg = this.ParseIdentifierName();
            }

            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.GotoStatement(kind, @goto, caseOrDefault, arg, semicolon);
        }

        private IfStatementSyntax ParseIfStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IfKeyword || this.CurrentToken.Kind == SyntaxKind.ElseKeyword);

            bool firstTokenIsElse = this.CurrentToken.Kind == SyntaxKind.ElseKeyword;
            var @if = firstTokenIsElse
                ? this.EatToken(SyntaxKind.IfKeyword, ErrorCode.ERR_ElseCannotStartStatement)
                : this.EatToken(SyntaxKind.IfKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var condition = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = firstTokenIsElse ? this.ParseExpressionStatement() : this.ParseEmbeddedStatement();
            var elseClause = this.ParseElseClauseOpt();

            return _syntaxFactory.IfStatement(@if, openParen, condition, closeParen, statement, elseClause);
        }

        private ElseClauseSyntax ParseElseClauseOpt()
        {
            if (this.CurrentToken.Kind != SyntaxKind.ElseKeyword)
            {
                return null;
            }

            var elseToken = this.EatToken(SyntaxKind.ElseKeyword);
            var elseStatement = this.ParseEmbeddedStatement();
            return _syntaxFactory.ElseClause(elseToken, elseStatement);
        }

        private LockStatementSyntax ParseLockStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LockKeyword);
            var @lock = this.EatToken(SyntaxKind.LockKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expression = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement();
            return _syntaxFactory.LockStatement(@lock, openParen, expression, closeParen, statement);
        }

        private ReturnStatementSyntax ParseReturnStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ReturnKeyword);
            var @return = this.EatToken(SyntaxKind.ReturnKeyword);
            ExpressionSyntax arg = null;
            if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
            {
                arg = this.ParsePossibleRefExpression();
            }

            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.ReturnStatement(@return, arg, semicolon);
        }

        private YieldStatementSyntax ParseYieldStatement()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.YieldKeyword);

            var yieldToken = ConvertToKeyword(this.EatToken());
            SyntaxToken returnOrBreak;
            ExpressionSyntax arg = null;
            SyntaxKind kind;

            yieldToken = CheckFeatureAvailability(yieldToken, MessageID.IDS_FeatureIterators);

            if (this.CurrentToken.Kind == SyntaxKind.BreakKeyword)
            {
                kind = SyntaxKind.YieldBreakStatement;
                returnOrBreak = this.EatToken();
            }
            else
            {
                kind = SyntaxKind.YieldReturnStatement;
                returnOrBreak = this.EatToken(SyntaxKind.ReturnKeyword);
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    returnOrBreak = this.AddError(returnOrBreak, ErrorCode.ERR_EmptyYield);
                }
                else
                {
                    arg = this.ParseExpressionCore();
                }
            }

            var semi = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.YieldStatement(kind, yieldToken, returnOrBreak, arg, semi);
        }

        private SwitchStatementSyntax ParseSwitchStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.SwitchKeyword);
            var @switch = this.EatToken(SyntaxKind.SwitchKeyword);
            var expression = this.ParseExpressionCore();
            SyntaxToken openParen;
            SyntaxToken closeParen;
            if (expression.Kind == SyntaxKind.ParenthesizedExpression)
            {
                var parenExpression = (ParenthesizedExpressionSyntax)expression;
                openParen = parenExpression.OpenParenToken;
                expression = parenExpression.Expression;
                closeParen = parenExpression.CloseParenToken;

                Debug.Assert(parenExpression.GetDiagnostics().Length == 0);
            }
            else if (expression.Kind == SyntaxKind.TupleExpression)
            {
                // As a special case, when a tuple literal is the governing expression of
                // a switch statement we permit the switch statement's own parentheses to be omitted.
                // LDM 2018-04-04.
                openParen = closeParen = default;
            }
            else
            {
                // Some other expression has appeared without parens. Give a syntax error.
                openParen = SyntaxFactory.MissingToken(SyntaxKind.OpenParenToken);
                expression = this.AddError(expression, ErrorCode.ERR_SwitchGoverningExpressionRequiresParens);
                closeParen = SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken);
            }

            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            var sections = _pool.Allocate<SwitchSectionSyntax>();
            try
            {
                while (this.IsPossibleSwitchSection())
                {
                    var swcase = this.ParseSwitchSection();
                    sections.Add(swcase);
                }

                var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
                return _syntaxFactory.SwitchStatement(@switch, openParen, expression, closeParen, openBrace, sections, closeBrace);
            }
            finally
            {
                _pool.Free(sections);
            }
        }

        private bool IsPossibleSwitchSection()
        {
            return (this.CurrentToken.Kind == SyntaxKind.CaseKeyword) ||
                   (this.CurrentToken.Kind == SyntaxKind.DefaultKeyword && this.PeekToken(1).Kind != SyntaxKind.OpenParenToken);
        }

        private SwitchSectionSyntax ParseSwitchSection()
        {
            Debug.Assert(this.IsPossibleSwitchSection());

            // First, parse case label(s)
            var labels = _pool.Allocate<SwitchLabelSyntax>();
            var statements = _pool.Allocate<StatementSyntax>();
            try
            {
                do
                {
                    SyntaxToken specifier;
                    SwitchLabelSyntax label;
                    SyntaxToken colon;
                    if (this.CurrentToken.Kind == SyntaxKind.CaseKeyword)
                    {
                        ExpressionSyntax expression;
                        specifier = this.EatToken();

                        if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                        {
                            expression = ParseIdentifierName(ErrorCode.ERR_ConstantExpected);
                            colon = this.EatToken(SyntaxKind.ColonToken);
                            label = _syntaxFactory.CaseSwitchLabel(specifier, expression, colon);
                        }
                        else
                        {
                            var node = CheckRecursivePatternFeature(ParseExpressionOrPattern(whenIsKeyword: true, forSwitchCase: true, precedence: Precedence.Conditional));

                            // if there is a 'when' token, we treat a case expression as a constant pattern.
                            if (this.CurrentToken.ContextualKind == SyntaxKind.WhenKeyword && node is ExpressionSyntax ex)
                                node = _syntaxFactory.ConstantPattern(ex);

                            if (node.Kind == SyntaxKind.DiscardPattern)
                                node = this.AddError(node, ErrorCode.ERR_DiscardPatternInSwitchStatement);

                            if (node is PatternSyntax pat)
                            {
                                var whenClause = ParseWhenClause(Precedence.Expression);
                                colon = this.EatToken(SyntaxKind.ColonToken);
                                label = _syntaxFactory.CasePatternSwitchLabel(specifier, pat, whenClause, colon);
                                label = CheckFeatureAvailability(label, MessageID.IDS_FeaturePatternMatching);
                            }
                            else
                            {
                                colon = this.EatToken(SyntaxKind.ColonToken);
                                label = _syntaxFactory.CaseSwitchLabel(specifier, (ExpressionSyntax)node, colon);
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(this.CurrentToken.Kind == SyntaxKind.DefaultKeyword);
                        specifier = this.EatToken(SyntaxKind.DefaultKeyword);
                        colon = this.EatToken(SyntaxKind.ColonToken);
                        label = _syntaxFactory.DefaultSwitchLabel(specifier, colon);
                    }

                    labels.Add(label);
                }
                while (IsPossibleSwitchSection());

                // Next, parse statement list stopping for new sections
                CSharpSyntaxNode tmp = labels[labels.Count - 1];
                this.ParseStatements(ref tmp, statements, true);
                labels[labels.Count - 1] = (SwitchLabelSyntax)tmp;

                return _syntaxFactory.SwitchSection(labels, statements);
            }
            finally
            {
                _pool.Free(statements);
                _pool.Free(labels);
            }
        }

        private ThrowStatementSyntax ParseThrowStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ThrowKeyword);
            var @throw = this.EatToken(SyntaxKind.ThrowKeyword);
            ExpressionSyntax arg = null;
            if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
            {
                arg = this.ParseExpressionCore();
            }

            var semi = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.ThrowStatement(@throw, arg, semi);
        }

        private UnsafeStatementSyntax ParseUnsafeStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.UnsafeKeyword);
            var @unsafe = this.EatToken(SyntaxKind.UnsafeKeyword);
            var block = this.ParseBlock();
            return _syntaxFactory.UnsafeStatement(@unsafe, block);
        }

        private UsingStatementSyntax ParseUsingStatement(SyntaxToken awaitTokenOpt = default)
        {
            var @using = this.EatToken(SyntaxKind.UsingKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            VariableDeclarationSyntax declaration = null;
            ExpressionSyntax expression = null;

            var resetPoint = this.GetResetPoint();
            ParseUsingExpression(ref declaration, ref expression, ref resetPoint);
            this.Release(ref resetPoint);

            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement();

            return _syntaxFactory.UsingStatement(awaitTokenOpt, @using, openParen, declaration, expression, closeParen, statement);
        }

        private void ParseUsingExpression(ref VariableDeclarationSyntax declaration, ref ExpressionSyntax expression, ref ResetPoint resetPoint)
        {
            if (this.IsAwaitExpression())
            {
                expression = this.ParseExpressionCore();
                return;
            }

            // Now, this can be either an expression or a decl list

            ScanTypeFlags st;

            if (this.IsQueryExpression(mayBeVariableDeclaration: true, mayBeMemberDeclaration: false))
            {
                st = ScanTypeFlags.NotType;
            }
            else
            {
                st = this.ScanType();
            }

            if (st == ScanTypeFlags.NullableType)
            {
                // We need to handle:
                // * using (f ? x = a : x = b)
                // * using (f ? x = a)
                // * using (f ? x, y)

                if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
                {
                    this.Reset(ref resetPoint);
                    expression = this.ParseExpressionCore();
                }
                else
                {
                    switch (this.PeekToken(1).Kind)
                    {
                        default:
                            this.Reset(ref resetPoint);
                            expression = this.ParseExpressionCore();
                            break;

                        case SyntaxKind.CommaToken:
                        case SyntaxKind.CloseParenToken:
                            this.Reset(ref resetPoint);
                            declaration = ParseVariableDeclaration();
                            break;

                        case SyntaxKind.EqualsToken:
                            // Parse it as a decl. If the next token is a : and only one variable was parsed,
                            // convert the whole thing to ?: expression.
                            this.Reset(ref resetPoint);
                            declaration = ParseVariableDeclaration();

                            // We may have non-nullable types in error scenarios.
                            if (this.CurrentToken.Kind == SyntaxKind.ColonToken &&
                                declaration.Type.Kind == SyntaxKind.NullableType &&
                                SyntaxFacts.IsName(((NullableTypeSyntax)declaration.Type).ElementType.Kind) &&
                                declaration.Variables.Count == 1)
                            {
                                // We have "name? id = expr :" so need to convert to a ?: expression.
                                this.Reset(ref resetPoint);
                                declaration = null;
                                expression = this.ParseExpressionCore();
                            }

                            break;
                    }
                }
            }
            else if (IsUsingStatementVariableDeclaration(st))
            {
                this.Reset(ref resetPoint);
                declaration = ParseVariableDeclaration();
            }
            else
            {
                // Must be an expression statement
                this.Reset(ref resetPoint);
                expression = this.ParseExpressionCore();
            }
        }

        private bool IsUsingStatementVariableDeclaration(ScanTypeFlags st)
        {
            Debug.Assert(st != ScanTypeFlags.NullableType);

            bool condition1 = st == ScanTypeFlags.MustBeType && this.CurrentToken.Kind != SyntaxKind.DotToken;
            bool condition2 = st != ScanTypeFlags.NotType && this.CurrentToken.Kind == SyntaxKind.IdentifierToken;
            bool condition3 = st == ScanTypeFlags.NonGenericTypeOrExpression || this.PeekToken(1).Kind == SyntaxKind.EqualsToken;

            return condition1 || (condition2 && condition3);
        }

        private WhileStatementSyntax ParseWhileStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.WhileKeyword);
            var @while = this.EatToken(SyntaxKind.WhileKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var condition = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement();
            return _syntaxFactory.WhileStatement(@while, openParen, condition, closeParen, statement);
        }

        private LabeledStatementSyntax ParseLabeledStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IdentifierToken);

            // We have an identifier followed by a colon. But if the identifier is a contextual keyword in a query context,
            // ParseIdentifier will result in a missing name and Eat(Colon) will fail. We won't make forward progress.
            Debug.Assert(this.IsTrueIdentifier());

            var label = this.ParseIdentifierToken();
            var colon = this.EatToken(SyntaxKind.ColonToken);
            Debug.Assert(!colon.IsMissing);
            var statement = this.ParseStatementCore() ?? SyntaxFactory.EmptyStatement(EatToken(SyntaxKind.SemicolonToken));
            return _syntaxFactory.LabeledStatement(label, colon, statement);
        }

        /// <summary>
        /// Parses any kind of local declaration statement: local variable or local function.
        /// </summary>
        private StatementSyntax ParseLocalDeclarationStatement(SyntaxToken awaitKeywordOpt = default)
        {
            var usingKeyword = TryEatToken(SyntaxKind.UsingKeyword);
            if (usingKeyword != null)
            {
                usingKeyword = CheckFeatureAvailability(usingKeyword, MessageID.IDS_FeatureUsingDeclarations);
            }
            bool canParseAsLocalFunction = usingKeyword == null;

            var mods = _pool.Allocate();
            this.ParseDeclarationModifiers(mods);

            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                TypeSyntax type;
                LocalFunctionStatementSyntax localFunction;
                this.ParseLocalDeclaration(variables,
                    allowLocalFunctions: canParseAsLocalFunction,
                    mods: mods.ToList(),
                    type: out type,
                    localFunction: out localFunction);

                if (localFunction != null)
                {
                    Debug.Assert(variables.Count == 0);
                    return localFunction;
                }

                // If we find an accessibility modifier but no local function it's likely
                // the user forgot a closing brace. Let's back out of statement parsing.
                if (canParseAsLocalFunction &&
                    mods.Count > 0 &&
                    IsAccessibilityModifier(((SyntaxToken)mods[0]).ContextualKind))
                {
                    return null;
                }

                for (int i = 0; i < mods.Count; i++)
                {
                    var mod = (SyntaxToken)mods[i];

                    if (IsAdditionalLocalFunctionModifier(mod.ContextualKind))
                    {
                        mods[i] = this.AddError(mod, ErrorCode.ERR_BadMemberFlag, mod.Text);
                    }
                }
                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.LocalDeclarationStatement(
                    awaitKeywordOpt,
                    usingKeyword,
                    mods.ToList(),
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon
                    );
            }
            finally
            {
                _pool.Free(variables);
                _pool.Free(mods);
            }
        }

        private VariableDesignationSyntax ParseDesignation(bool forPattern)
        {
            // the two forms of designation are
            // (1) identifier
            // (2) ( designation ... )
            // for pattern-matching, we permit the designation list to be empty
            VariableDesignationSyntax result;
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                var listOfDesignations = _pool.AllocateSeparated<VariableDesignationSyntax>();

                bool done = false;
                if (forPattern)
                {
                    done = (this.CurrentToken.Kind == SyntaxKind.CloseParenToken);
                }
                else
                {
                    listOfDesignations.Add(ParseDesignation(forPattern));
                    listOfDesignations.AddSeparator(EatToken(SyntaxKind.CommaToken));
                }

                if (!done)
                {
                    while (true)
                    {
                        listOfDesignations.Add(ParseDesignation(forPattern));
                        if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                        {
                            listOfDesignations.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                result = _syntaxFactory.ParenthesizedVariableDesignation(openParen, listOfDesignations, closeParen);
                _pool.Free(listOfDesignations);
            }
            else
            {
                result = ParseSimpleDesignation();
            }

            return result;
        }

        /// <summary>
        /// Parse a single variable designation (e.g. <c>x</c>) or a wildcard designation (e.g. <c>_</c>)
        /// </summary>
        /// <returns></returns>
        private VariableDesignationSyntax ParseSimpleDesignation()
        {
            if (CurrentToken.ContextualKind == SyntaxKind.UnderscoreToken)
            {
                var underscore = this.EatContextualToken(SyntaxKind.UnderscoreToken);
                return _syntaxFactory.DiscardDesignation(underscore);
            }
            else
            {
                var identifier = this.EatToken(SyntaxKind.IdentifierToken);
                return _syntaxFactory.SingleVariableDesignation(identifier);
            }
        }

        private WhenClauseSyntax ParseWhenClause(Precedence precedence)
        {
            if (this.CurrentToken.ContextualKind != SyntaxKind.WhenKeyword)
            {
                return null;
            }

            var when = this.EatContextualToken(SyntaxKind.WhenKeyword);
            var condition = ParseSubExpression(precedence);
            return _syntaxFactory.WhenClause(when, condition);
        }

        /// <summary>
        /// Parse a local variable declaration.
        /// </summary>
        /// <returns></returns>
        private VariableDeclarationSyntax ParseVariableDeclaration()
        {
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            TypeSyntax type;
            LocalFunctionStatementSyntax localFunction;
            ParseLocalDeclaration(variables, false, default(SyntaxList<SyntaxToken>), out type, out localFunction);
            Debug.Assert(localFunction == null);
            var result = _syntaxFactory.VariableDeclaration(type, variables);
            _pool.Free(variables);
            return result;
        }

        private void ParseLocalDeclaration(
            SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables,
            bool allowLocalFunctions,
            SyntaxList<SyntaxToken> mods,
            out TypeSyntax type,
            out LocalFunctionStatementSyntax localFunction)
        {
            type = allowLocalFunctions ? ParseReturnType() : this.ParseType();

            VariableFlags flags = VariableFlags.Local;
            if (mods.Any((int)SyntaxKind.ConstKeyword))
            {
                flags |= VariableFlags.Const;
            }

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfDeclarationClause;
            this.ParseVariableDeclarators(
                type,
                flags,
                variables,
                variableDeclarationsExpected: true,
                allowLocalFunctions: allowLocalFunctions,
                mods: mods,
                localFunction: out localFunction);
            _termState = saveTerm;

            if (allowLocalFunctions && localFunction == null && (type as PredefinedTypeSyntax)?.Keyword.Kind == SyntaxKind.VoidKeyword)
            {
                type = this.AddError(type, ErrorCode.ERR_NoVoidHere);
            }
        }

        private bool IsEndOfDeclarationClause()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.ColonToken:
                    return true;
                default:
                    return false;
            }
        }

        private void ParseDeclarationModifiers(SyntaxListBuilder list)
        {
            SyntaxKind k;
            while (IsDeclarationModifier(k = this.CurrentToken.ContextualKind) || IsAdditionalLocalFunctionModifier(k))
            {
                SyntaxToken mod;
                if (k == SyntaxKind.AsyncKeyword)
                {
                    // check for things like "async async()" where async is the type and/or the function name
                    {
                        var resetPoint = this.GetResetPoint();

                        var invalid = !IsPossibleStartOfTypeDeclaration(this.EatToken().Kind) &&
                            !IsDeclarationModifier(this.CurrentToken.Kind) && !IsAdditionalLocalFunctionModifier(this.CurrentToken.Kind) &&
                            (ScanType() == ScanTypeFlags.NotType || this.CurrentToken.Kind != SyntaxKind.IdentifierToken);

                        this.Reset(ref resetPoint);
                        this.Release(ref resetPoint);

                        if (invalid)
                        {
                            break;
                        }
                    }

                    mod = this.EatContextualToken(k);
                    if (k == SyntaxKind.AsyncKeyword)
                    {
                        mod = CheckFeatureAvailability(mod, MessageID.IDS_FeatureAsync);
                    }
                }
                else
                {
                    mod = this.EatToken();
                }

                if (k == SyntaxKind.ReadOnlyKeyword || k == SyntaxKind.VolatileKeyword)
                {
                    mod = this.AddError(mod, ErrorCode.ERR_BadMemberFlag, mod.Text);
                }
                else if (list.Any(mod.RawKind))
                {
                    // check for duplicates, can only be const
                    mod = this.AddError(mod, ErrorCode.ERR_TypeExpected, mod.Text);
                }

                list.Add(mod);
            }
        }

        private static bool IsDeclarationModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                case SyntaxKind.VolatileKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAdditionalLocalFunctionModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.AsyncKeyword:
                case SyntaxKind.UnsafeKeyword:
                // Not a valid modifier, but we should parse to give a good
                // error message
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.PrivateKeyword:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsAccessibilityModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                // Accessibility modifiers aren't legal in a local function,
                // but a common mistake. Parse to give a better error message.
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.PrivateKeyword:
                    return true;

                default:
                    return false;
            }
        }

        private LocalFunctionStatementSyntax TryParseLocalFunctionStatementBody(
            SyntaxList<SyntaxToken> modifiers,
            TypeSyntax type,
            SyntaxToken identifier)
        {
            // This may potentially be an ambiguous parse until very far into the token stream, so we may have to backtrack.
            // For example, "await x()" is ambiguous at the current point of parsing (right now we're right after the x).
            // The point at which it becomes unambiguous is after the argument list. A "=>" or "{" means its a local function
            // (with return type @await), a ";" or other expression-y token means its an await of a function call.

            // Note that we could just check if we're in an async context, but that breaks some analyzers, because
            // "await f();" would be parsed as a local function statement when really we want a parse error so we can say
            // "did you mean to make this method be an async method?" (it's invalid either way, so the spec doesn't care)
            var resetPoint = this.GetResetPoint();

            // Indicates this must be parsed as a local function, even if there's no body
            bool forceLocalFunc = true;
            if (type.Kind == SyntaxKind.IdentifierName)
            {
                var id = ((IdentifierNameSyntax)type).Identifier;
                forceLocalFunc = id.ContextualKind != SyntaxKind.AwaitKeyword;
            }

            bool parentScopeIsInAsync = IsInAsync;
            IsInAsync = false;
            SyntaxListBuilder badBuilder = null;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                switch (modifier.ContextualKind)
                {
                    case SyntaxKind.AsyncKeyword:
                        IsInAsync = true;
                        forceLocalFunc = true;
                        continue;
                    case SyntaxKind.UnsafeKeyword:
                        forceLocalFunc = true;
                        continue;
                    case SyntaxKind.ReadOnlyKeyword:
                    case SyntaxKind.VolatileKeyword:
                        continue; // already reported earlier, no need to report again
                    case SyntaxKind.StaticKeyword:
                        modifier = CheckFeatureAvailability(modifier, MessageID.IDS_FeatureStaticLocalFunctions);
                        if ((object)modifier == modifiers[i])
                        {
                            continue;
                        }
                        break;
                    default:
                        modifier = this.AddError(modifier, ErrorCode.ERR_BadMemberFlag, modifier.Text);
                        break;
                }
                if (badBuilder == null)
                {
                    badBuilder = _pool.Allocate();
                    badBuilder.AddRange(modifiers);
                }
                badBuilder[i] = modifier;
            }
            if (badBuilder != null)
            {
                modifiers = badBuilder.ToList();
                _pool.Free(badBuilder);
            }

            TypeParameterListSyntax typeParameterListOpt = this.ParseTypeParameterList();
            // "await f<T>()" still makes sense, so don't force accept a local function if there's a type parameter list.
            ParameterListSyntax paramList = this.ParseParenthesizedParameterList();
            // "await x()" is ambiguous (see note at start of this method), but we assume "await x(await y)" is meant to be a function if it's in a non-async context.
            if (!forceLocalFunc)
            {
                var paramListSyntax = paramList.Parameters;
                for (int i = 0; i < paramListSyntax.Count; i++)
                {
                    // "await x(y)" still parses as a parameter list, so check to see if it's a valid parameter (like "x(t y)")
                    forceLocalFunc |= !paramListSyntax[i].ContainsDiagnostics;
                    if (forceLocalFunc)
                        break;
                }
            }

            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
            {
                constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                this.ParseTypeParameterConstraintClauses(constraints);
                forceLocalFunc = true;
            }

            BlockSyntax blockBody;
            ArrowExpressionClauseSyntax expressionBody;
            SyntaxToken semicolon;
            this.ParseBlockAndExpressionBodiesWithSemicolon(out blockBody, out expressionBody, out semicolon, parseSemicolonAfterBlock: false);

            IsInAsync = parentScopeIsInAsync;

            if (!forceLocalFunc && blockBody == null && expressionBody == null)
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
                return null;
            }
            this.Release(ref resetPoint);

            identifier = CheckFeatureAvailability(identifier, MessageID.IDS_FeatureLocalFunctions);
            return _syntaxFactory.LocalFunctionStatement(
                modifiers,
                type,
                identifier,
                typeParameterListOpt,
                paramList,
                constraints,
                blockBody,
                expressionBody,
                semicolon);
        }

        private ExpressionStatementSyntax ParseExpressionStatement()
        {
            return ParseExpressionStatement(this.ParseExpressionCore());
        }

        private ExpressionStatementSyntax ParseExpressionStatement(ExpressionSyntax expression)
        {
            SyntaxToken semicolon;
            if (IsScript && this.CurrentToken.Kind == SyntaxKind.EndOfFileToken)
            {
                semicolon = SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken);
            }
            else
            {
                // Do not report an error if the expression is not a statement expression.
                // The error is reported in semantic analysis.
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }

            return _syntaxFactory.ExpressionStatement(expression, semicolon);
        }

        public ExpressionSyntax ParseExpression()
        {
            return ParseWithStackGuard(
                this.ParseExpressionCore,
                this.CreateMissingIdentifierName);
        }

        private ExpressionSyntax ParseExpressionCore()
        {
            return this.ParseSubExpression(Precedence.Expression);
        }

        /// <summary>
        /// Is the current token one that could start an expression?
        /// </summary>
        private bool CanStartExpression()
        {
            return IsPossibleExpression(allowBinaryExpressions: false, allowAssignmentExpressions: false);
        }

        /// <summary>
        /// Is the current token one that could be in an expression?
        /// </summary>
        private bool IsPossibleExpression()
        {
            return IsPossibleExpression(allowBinaryExpressions: true, allowAssignmentExpressions: true);
        }

        private bool IsPossibleExpression(bool allowBinaryExpressions, bool allowAssignmentExpressions)
        {
            SyntaxKind tk = this.CurrentToken.Kind;
            switch (tk)
            {
                case SyntaxKind.TypeOfKeyword:
                case SyntaxKind.DefaultKeyword:
                case SyntaxKind.SizeOfKeyword:
                case SyntaxKind.MakeRefKeyword:
                case SyntaxKind.RefTypeKeyword:
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                case SyntaxKind.RefValueKeyword:
                case SyntaxKind.ArgListKeyword:
                case SyntaxKind.BaseKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.NullKeyword:
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.InterpolatedStringStartToken:
                case SyntaxKind.InterpolatedStringToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.ColonColonToken: // bad aliased name
                case SyntaxKind.ThrowKeyword:
                case SyntaxKind.StackAllocKeyword:
                case SyntaxKind.DotDotToken:
                    return true;
                case SyntaxKind.IdentifierToken:
                    // Specifically allow the from contextual keyword, because it can always be the start of an
                    // expression (whether it is used as an identifier or a keyword).
                    return this.IsTrueIdentifier() || (this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword);
                default:
                    return (IsPredefinedType(tk) && tk != SyntaxKind.VoidKeyword)
                        || SyntaxFacts.IsAnyUnaryExpression(tk)
                        || (allowBinaryExpressions && SyntaxFacts.IsBinaryExpression(tk))
                        || (allowAssignmentExpressions && SyntaxFacts.IsAssignmentExpressionOperatorToken(tk));
            }
        }

        private static bool IsInvalidSubExpression(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.CaseKeyword:
                case SyntaxKind.CatchKeyword:
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.DoKeyword:
                case SyntaxKind.FinallyKeyword:
                case SyntaxKind.ForKeyword:
                case SyntaxKind.ForEachKeyword:
                case SyntaxKind.GotoKeyword:
                case SyntaxKind.IfKeyword:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.LockKeyword:
                case SyntaxKind.ReturnKeyword:
                case SyntaxKind.SwitchKeyword:
                case SyntaxKind.TryKeyword:
                case SyntaxKind.UsingKeyword:
                case SyntaxKind.WhileKeyword:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsRightAssociative(SyntaxKind op)
        {
            switch (op)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                case SyntaxKind.CoalesceExpression:
                    return true;
                default:
                    return false;
            }
        }

        enum Precedence : uint
        {
            Expression = 0, // Loosest possible precedence, used to accept all expressions
            Assignment,
            Lambda = Assignment, // "The => operator has the same precedence as assignment (=) and is right-associative."
            Conditional,
            Coalescing,
            ConditionalOr,
            ConditionalAnd,
            LogicalOr,
            LogicalXor,
            LogicalAnd,
            Equality,
            Relational,
            Shift,
            Additive,
            Mutiplicative,
            Switch,
            Range,
            Unary,
            Cast,
            PointerIndirection,
            AddressOf,
            Primary_UNUSED, // Primaries are parsed in an ad-hoc manner.
        }

        private static Precedence GetPrecedence(SyntaxKind op)
        {
            switch (op)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                    return Precedence.Assignment;
                case SyntaxKind.CoalesceExpression:
                    return Precedence.Coalescing;
                case SyntaxKind.LogicalOrExpression:
                    return Precedence.ConditionalOr;
                case SyntaxKind.LogicalAndExpression:
                    return Precedence.ConditionalAnd;
                case SyntaxKind.BitwiseOrExpression:
                    return Precedence.LogicalOr;
                case SyntaxKind.ExclusiveOrExpression:
                    return Precedence.LogicalXor;
                case SyntaxKind.BitwiseAndExpression:
                    return Precedence.LogicalAnd;
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                    return Precedence.Equality;
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.IsExpression:
                case SyntaxKind.AsExpression:
                case SyntaxKind.IsPatternExpression:
                    return Precedence.Relational;
                case SyntaxKind.SwitchExpression:
                    return Precedence.Switch;
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                    return Precedence.Shift;
                case SyntaxKind.AddExpression:
                case SyntaxKind.SubtractExpression:
                    return Precedence.Additive;
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                    return Precedence.Mutiplicative;
                case SyntaxKind.UnaryPlusExpression:
                case SyntaxKind.UnaryMinusExpression:
                case SyntaxKind.BitwiseNotExpression:
                case SyntaxKind.LogicalNotExpression:
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PreDecrementExpression:
                case SyntaxKind.TypeOfExpression:
                case SyntaxKind.SizeOfExpression:
                case SyntaxKind.CheckedExpression:
                case SyntaxKind.UncheckedExpression:
                case SyntaxKind.MakeRefExpression:
                case SyntaxKind.RefValueExpression:
                case SyntaxKind.RefTypeExpression:
                case SyntaxKind.AwaitExpression:
                case SyntaxKind.IndexExpression:
                    return Precedence.Unary;
                case SyntaxKind.CastExpression:
                    return Precedence.Cast;
                case SyntaxKind.PointerIndirectionExpression:
                    return Precedence.PointerIndirection;
                case SyntaxKind.AddressOfExpression:
                    return Precedence.AddressOf;
                case SyntaxKind.RangeExpression:
                    return Precedence.Range;
                case SyntaxKind.ConditionalExpression:
                    return Precedence.Expression;
                default:
                    throw ExceptionUtilities.UnexpectedValue(op);
            }
        }

        private static bool IsExpectedPrefixUnaryOperator(SyntaxKind kind)
        {
            return SyntaxFacts.IsPrefixUnaryExpression(kind) && kind != SyntaxKind.RefKeyword && kind != SyntaxKind.OutKeyword;
        }

        private static bool IsExpectedBinaryOperator(SyntaxKind kind)
        {
            return SyntaxFacts.IsBinaryExpression(kind);
        }

        private static bool IsExpectedAssignmentOperator(SyntaxKind kind)
        {
            return SyntaxFacts.IsAssignmentExpressionOperatorToken(kind);
        }

        private bool IsPossibleAwaitExpressionStatement()
        {
            return (this.IsScript || this.IsInAsync) && this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword;
        }

        private bool IsAwaitExpression()
        {
            if (this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword)
            {
                if (this.IsInAsync)
                {
                    // If we see an await in an async function, parse it as an unop.
                    return true;
                }

                // If we see an await followed by a token that cannot follow an identifier, parse await as a unop.
                // BindAwait() catches the cases where await successfully parses as a unop but is not in an async
                // function, and reports an appropriate ERR_BadAwaitWithoutAsync* error.
                switch (this.PeekToken(1).Kind)
                {
                    case SyntaxKind.IdentifierToken:

                    // Keywords
                    case SyntaxKind.NewKeyword:
                    case SyntaxKind.ThisKeyword:
                    case SyntaxKind.BaseKeyword:
                    case SyntaxKind.DelegateKeyword:
                    case SyntaxKind.TypeOfKeyword:
                    case SyntaxKind.CheckedKeyword:
                    case SyntaxKind.UncheckedKeyword:
                    case SyntaxKind.DefaultKeyword:

                    // Literals
                    case SyntaxKind.TrueKeyword:
                    case SyntaxKind.FalseKeyword:
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.InterpolatedStringStartToken:
                    case SyntaxKind.InterpolatedStringToken:
                    case SyntaxKind.NumericLiteralToken:
                    case SyntaxKind.NullKeyword:
                    case SyntaxKind.CharacterLiteralToken:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Parse a subexpression of the enclosing operator of the given precedence.
        /// </summary>
        private ExpressionSyntax ParseSubExpression(Precedence precedence)
        {
            _recursionDepth++;

            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

            var result = ParseSubExpressionCore(precedence);

            _recursionDepth--;
            return result;
        }

        private ExpressionSyntax ParseSubExpressionCore(Precedence precedence)
        {
            ExpressionSyntax leftOperand;
            Precedence newPrecedence = 0;

            // all of these are tokens that start statements and are invalid
            // to start a expression with. if we see one, then we must have
            // something like:
            //
            // return
            // if (...
            // parse out a missing name node for the expression, and keep on going
            var tk = this.CurrentToken.Kind;
            if (IsInvalidSubExpression(tk))
            {
                return this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
            }

            // Parse a left operand -- possibly preceded by a unary operator.
            if (IsExpectedPrefixUnaryOperator(tk))
            {
                var opKind = SyntaxFacts.GetPrefixUnaryExpression(tk);
                newPrecedence = GetPrecedence(opKind);
                var opToken = this.EatToken();
                var operand = this.ParseSubExpression(newPrecedence);
                leftOperand = _syntaxFactory.PrefixUnaryExpression(opKind, opToken, operand);
            }
            else if (tk == SyntaxKind.DotDotToken)
            {
                // Operator ".." here can either be a prefix unary operator or a stand alone empty range:
                var opToken = this.EatToken();
                newPrecedence = GetPrecedence(SyntaxKind.RangeExpression);

                ExpressionSyntax rightOperand;
                if (CanStartExpression())
                {
                    rightOperand = this.ParseSubExpression(newPrecedence);
                }
                else
                {
                    rightOperand = null;
                }

                leftOperand = _syntaxFactory.RangeExpression(leftOperand: null, opToken, rightOperand);
            }
            else if (IsAwaitExpression())
            {
                newPrecedence = GetPrecedence(SyntaxKind.AwaitExpression);
                var awaitToken = this.EatContextualToken(SyntaxKind.AwaitKeyword);
                awaitToken = CheckFeatureAvailability(awaitToken, MessageID.IDS_FeatureAsync);
                var operand = this.ParseSubExpression(newPrecedence);
                leftOperand = _syntaxFactory.AwaitExpression(awaitToken, operand);
            }
            else if (this.IsQueryExpression(mayBeVariableDeclaration: false, mayBeMemberDeclaration: false))
            {
                leftOperand = this.ParseQueryExpression(precedence);
            }
            else if (this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword && IsInQuery)
            {
                // If this "from" token wasn't the start of a query then it's not really an expression.
                // Consume it so that we don't try to parse it again as the next argument in an
                // argument list.
                SyntaxToken skipped = this.EatToken(); // consume but skip "from"
                skipped = this.AddError(skipped, ErrorCode.ERR_InvalidExprTerm, this.CurrentToken.Text);
                leftOperand = AddTrailingSkippedSyntax(this.CreateMissingIdentifierName(), skipped);
            }
            else if (tk == SyntaxKind.ThrowKeyword)
            {
                var result = ParseThrowExpression();
                // we parse a throw expression even at the wrong precedence for better recovery
                return (precedence <= Precedence.Coalescing) ? result :
                    this.AddError(result, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
            }
            else if (this.IsPossibleDeconstructionLeft(precedence))
            {
                leftOperand = ParseDeclarationExpression(ParseTypeMode.Normal, MessageID.IDS_FeatureTuples);
            }
            else
            {
                // Not a unary operator - get a primary expression.
                leftOperand = this.ParseTerm(precedence);
            }

            return ParseExpressionContinued(leftOperand, precedence);
        }

        private ExpressionSyntax ParseExpressionContinued(ExpressionSyntax leftOperand, Precedence precedence)
        {
            while (true)
            {
                // We either have a binary or assignment operator here, or we're finished.
                var tk = this.CurrentToken.ContextualKind;

                bool isAssignmentOperator = false;
                SyntaxKind opKind;
                if (IsExpectedBinaryOperator(tk))
                {
                    opKind = SyntaxFacts.GetBinaryExpression(tk);
                }
                else if (IsExpectedAssignmentOperator(tk))
                {
                    opKind = SyntaxFacts.GetAssignmentExpression(tk);
                    isAssignmentOperator = true;
                }
                else if (tk == SyntaxKind.DotDotToken)
                {
                    opKind = SyntaxKind.RangeExpression;
                }
                else if (tk == SyntaxKind.SwitchKeyword && this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken)
                {
                    opKind = SyntaxKind.SwitchExpression;
                }
                else
                {
                    break;
                }

                var newPrecedence = GetPrecedence(opKind);

                Debug.Assert(newPrecedence > 0);      // All binary operators must have precedence > 0!

                // check for >> or >>=
                bool doubleOp = false;
                if (tk == SyntaxKind.GreaterThanToken
                    && (this.PeekToken(1).Kind == SyntaxKind.GreaterThanToken || this.PeekToken(1).Kind == SyntaxKind.GreaterThanEqualsToken))
                {
                    // check to see if they really are adjacent
                    if (this.CurrentToken.GetTrailingTriviaWidth() == 0 && this.PeekToken(1).GetLeadingTriviaWidth() == 0)
                    {
                        if (this.PeekToken(1).Kind == SyntaxKind.GreaterThanToken)
                        {
                            opKind = SyntaxFacts.GetBinaryExpression(SyntaxKind.GreaterThanGreaterThanToken);
                        }
                        else
                        {
                            opKind = SyntaxFacts.GetAssignmentExpression(SyntaxKind.GreaterThanGreaterThanEqualsToken);
                            isAssignmentOperator = true;
                        }
                        newPrecedence = GetPrecedence(opKind);
                        doubleOp = true;
                    }
                }

                // Check the precedence to see if we should "take" this operator
                if (newPrecedence < precedence)
                {
                    break;
                }

                // Same precedence, but not right-associative -- deal with this "later"
                if ((newPrecedence == precedence) && !IsRightAssociative(opKind))
                {
                    break;
                }

                // Precedence is okay, so we'll "take" this operator.
                var opToken = this.EatContextualToken(tk);
                if (doubleOp)
                {
                    // combine tokens into a single token
                    var opToken2 = this.EatToken();
                    var kind = opToken2.Kind == SyntaxKind.GreaterThanToken ? SyntaxKind.GreaterThanGreaterThanToken : SyntaxKind.GreaterThanGreaterThanEqualsToken;
                    opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), kind, opToken2.GetTrailingTrivia());
                }

                if (opKind == SyntaxKind.AsExpression)
                {
                    var type = this.ParseType(ParseTypeMode.AsExpression);
                    leftOperand = _syntaxFactory.BinaryExpression(opKind, leftOperand, opToken, type);
                }
                else if (opKind == SyntaxKind.IsExpression)
                {
                    leftOperand = ParseIsExpression(leftOperand, opToken);
                }
                else if (isAssignmentOperator)
                {
                    ExpressionSyntax rhs = opKind == SyntaxKind.SimpleAssignmentExpression && CurrentToken.Kind == SyntaxKind.RefKeyword
                        ? rhs = CheckFeatureAvailability(ParsePossibleRefExpression(), MessageID.IDS_FeatureRefReassignment)
                        : rhs = this.ParseSubExpression(newPrecedence);

                    if (opKind == SyntaxKind.CoalesceAssignmentExpression)
                    {
                        opToken = CheckFeatureAvailability(opToken, MessageID.IDS_FeatureCoalesceAssignmentExpression);
                    }

                    leftOperand = _syntaxFactory.AssignmentExpression(opKind, leftOperand, opToken, rhs);
                }
                else if (opKind == SyntaxKind.SwitchExpression)
                {
                    leftOperand = ParseSwitchExpression(leftOperand, opToken);
                }
                else if (tk == SyntaxKind.DotDotToken)
                {
                    // Operator ".." here can either be a binary or a postfix unary operator:
                    Debug.Assert(opKind == SyntaxKind.RangeExpression);

                    ExpressionSyntax rightOperand;
                    if (CanStartExpression())
                    {
                        newPrecedence = GetPrecedence(opKind);
                        rightOperand = this.ParseSubExpression(newPrecedence);
                    }
                    else
                    {
                        rightOperand = null;
                    }

                    leftOperand = _syntaxFactory.RangeExpression(leftOperand, opToken, rightOperand);
                }
                else
                {
                    Debug.Assert(IsExpectedBinaryOperator(tk));
                    leftOperand = _syntaxFactory.BinaryExpression(opKind, leftOperand, opToken, this.ParseSubExpression(newPrecedence));
                }
            }

            // From the language spec:
            //
            // conditional-expression:
            //  null-coalescing-expression
            //  null-coalescing-expression   ?   expression   :   expression
            //
            // Only take the conditional if we're at or below its precedence.
            if (CurrentToken.Kind == SyntaxKind.QuestionToken && precedence <= Precedence.Conditional)
            {
                var questionToken = this.EatToken();
                var colonLeft = this.ParsePossibleRefExpression();
                if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken && this.lexer.InterpolationFollowedByColon)
                {
                    // We have an interpolated string with an interpolation that contains a conditional expression.
                    // Unfortunately, the precedence demands that the colon is considered to signal the start of the
                    // format string. Without this code, the compiler would complain about a missing colon, and point
                    // to the colon that is present, which would be confusing. We aim to give a better error message.
                    var colon = SyntaxFactory.MissingToken(SyntaxKind.ColonToken);
                    var colonRight = _syntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken));
                    leftOperand = _syntaxFactory.ConditionalExpression(leftOperand, questionToken, colonLeft, colon, colonRight);
                    leftOperand = this.AddError(leftOperand, ErrorCode.ERR_ConditionalInInterpolation);
                }
                else
                {
                    var colon = this.EatToken(SyntaxKind.ColonToken);
                    var colonRight = this.ParsePossibleRefExpression();
                    leftOperand = _syntaxFactory.ConditionalExpression(leftOperand, questionToken, colonLeft, colon, colonRight);
                }
            }

            return leftOperand;
        }

        private ExpressionSyntax ParseDeclarationExpression(ParseTypeMode mode, MessageID feature)
        {
            TypeSyntax type = this.ParseType(mode);
            var designation = ParseDesignation(forPattern: false);
            if (feature != MessageID.None)
            {
                designation = CheckFeatureAvailability(designation, feature);
            }

            return _syntaxFactory.DeclarationExpression(type, designation);
        }

        private ExpressionSyntax ParseThrowExpression()
        {
            var throwToken = this.EatToken(SyntaxKind.ThrowKeyword);
            var thrown = this.ParseSubExpression(Precedence.Coalescing);
            var result = _syntaxFactory.ThrowExpression(throwToken, thrown);
            return CheckFeatureAvailability(result, MessageID.IDS_FeatureThrowExpression);
        }

        private ExpressionSyntax ParseIsExpression(ExpressionSyntax leftOperand, SyntaxToken opToken)
        {
            var node = this.ParseTypeOrPatternForIsOperator();
            if (node is PatternSyntax)
            {
                var result = _syntaxFactory.IsPatternExpression(leftOperand, opToken, (PatternSyntax)node);
                return CheckFeatureAvailability(result, MessageID.IDS_FeaturePatternMatching);
            }
            else
            {
                Debug.Assert(node is TypeSyntax);
                return _syntaxFactory.BinaryExpression(SyntaxKind.IsExpression, leftOperand, opToken, (TypeSyntax)node);
            }
        }

        private ExpressionSyntax ParseTerm(Precedence precedence)
        {
            ExpressionSyntax expr = null;

            var tk = this.CurrentToken.Kind;
            switch (tk)
            {
                case SyntaxKind.TypeOfKeyword:
                    expr = this.ParseTypeOfExpression();
                    break;
                case SyntaxKind.DefaultKeyword:
                    expr = this.ParseDefaultExpression();
                    break;
                case SyntaxKind.SizeOfKeyword:
                    expr = this.ParseSizeOfExpression();
                    break;
                case SyntaxKind.MakeRefKeyword:
                    expr = this.ParseMakeRefExpression();
                    break;
                case SyntaxKind.RefTypeKeyword:
                    expr = this.ParseRefTypeExpression();
                    break;
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                    expr = this.ParseCheckedOrUncheckedExpression();
                    break;
                case SyntaxKind.RefValueKeyword:
                    expr = this.ParseRefValueExpression();
                    break;
                case SyntaxKind.ColonColonToken:
                    // misplaced ::
                    // TODO: this should not be a compound name.. (disallow dots)
                    expr = this.ParseQualifiedName(NameOptions.InExpression);
                    break;
                case SyntaxKind.IdentifierToken:
                    if (this.IsTrueIdentifier())
                    {
                        var contextualKind = this.CurrentToken.ContextualKind;
                        if (contextualKind == SyntaxKind.AsyncKeyword && this.PeekToken(1).Kind == SyntaxKind.DelegateKeyword)
                        {
                            expr = this.ParseAnonymousMethodExpression();
                        }
                        else if (this.IsPossibleLambdaExpression(precedence))
                        {
                            expr = this.ParseLambdaExpression();
                        }
                        else if (this.IsPossibleDeconstructionLeft(precedence))
                        {
                            expr = ParseDeclarationExpression(ParseTypeMode.Normal, MessageID.IDS_FeatureTuples);
                        }
                        else
                        {
                            expr = this.ParseAliasQualifiedName(NameOptions.InExpression);
                        }
                    }
                    else
                    {
                        expr = this.CreateMissingIdentifierName();
                        expr = this.AddError(expr, ErrorCode.ERR_InvalidExprTerm, this.CurrentToken.Text);
                    }

                    break;
                case SyntaxKind.ThisKeyword:
                    expr = _syntaxFactory.ThisExpression(this.EatToken());
                    break;
                case SyntaxKind.BaseKeyword:
                    expr = ParseBaseExpression();
                    break;
                case SyntaxKind.ArgListKeyword:
                case SyntaxKind.FalseKeyword:

                case SyntaxKind.TrueKeyword:
                case SyntaxKind.NullKeyword:
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                    expr = _syntaxFactory.LiteralExpression(SyntaxFacts.GetLiteralExpression(tk), this.EatToken());
                    break;
                case SyntaxKind.InterpolatedStringStartToken:
                    throw new NotImplementedException(); // this should not occur because these tokens are produced and parsed immediately
                case SyntaxKind.InterpolatedStringToken:
                    expr = this.ParseInterpolatedStringToken();
                    break;
                case SyntaxKind.OpenParenToken:
                    expr = this.ParseCastOrParenExpressionOrLambdaOrTuple(precedence);
                    break;
                case SyntaxKind.NewKeyword:
                    expr = this.ParseNewExpression();
                    break;
                case SyntaxKind.StackAllocKeyword:
                    expr = this.ParseStackAllocExpression();
                    break;
                case SyntaxKind.DelegateKeyword:
                    expr = this.ParseAnonymousMethodExpression();
                    break;
                default:
                    // check for intrinsic type followed by '.'
                    if (IsPredefinedType(tk))
                    {
                        expr = _syntaxFactory.PredefinedType(this.EatToken());

                        if (this.CurrentToken.Kind != SyntaxKind.DotToken || tk == SyntaxKind.VoidKeyword)
                        {
                            expr = this.AddError(expr, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
                        }
                    }
                    else
                    {
                        expr = this.CreateMissingIdentifierName();

                        if (tk == SyntaxKind.EndOfFileToken)
                        {
                            expr = this.AddError(expr, ErrorCode.ERR_ExpressionExpected);
                        }
                        else
                        {
                            expr = this.AddError(expr, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
                        }
                    }

                    break;
            }

            return this.ParsePostFixExpression(expr);
        }

        private ExpressionSyntax ParseBaseExpression()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.BaseKeyword);
            return _syntaxFactory.BaseExpression(this.EatToken());
        }

        /// <summary>
        /// Returns true if...
        /// 1. The precedence is less than or equal to Assignment, and
        /// 2. The current token is the identifier var or a predefined type, and
        /// 3. it is followed by (, and
        /// 4. that ( begins a valid parenthesized designation, and
        /// 5. the token following that designation is =
        /// </summary>
        private bool IsPossibleDeconstructionLeft(Precedence precedence)
        {
            if (precedence > Precedence.Assignment || !(this.CurrentToken.IsIdentifierVar() || IsPredefinedType(this.CurrentToken.Kind)))
            {
                return false;
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                this.EatToken(); // `var`
                return
                    this.CurrentToken.Kind == SyntaxKind.OpenParenToken && ScanDesignator() &&
                    this.CurrentToken.Kind == SyntaxKind.EqualsToken;
            }
            finally
            {
                // Restore current token index
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        private bool ScanDesignator()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.IdentifierToken:
                    if (!IsTrueIdentifier())
                    {
                        goto default;
                    }

                    this.EatToken(); // eat the identifier
                    return true;
                case SyntaxKind.OpenParenToken:
                    while (true)
                    {
                        this.EatToken(); // eat the open paren or comma
                        if (!ScanDesignator())
                        {
                            return false;
                        }

                        switch (this.CurrentToken.Kind)
                        {
                            case SyntaxKind.CommaToken:
                                continue;
                            case SyntaxKind.CloseParenToken:
                                this.EatToken(); // eat the close paren
                                return true;
                            default:
                                return false;
                        }
                    }
                default:
                    return false;
            }
        }

        private bool IsPossibleLambdaExpression(Precedence precedence)
        {
            if (precedence <= Precedence.Lambda && this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }

            if (ScanAsyncLambda(precedence))
            {
                return true;
            }

            return false;
        }

        private ExpressionSyntax ParsePostFixExpression(ExpressionSyntax expr)
        {
            Debug.Assert(expr != null);

            while (true)
            {
                SyntaxKind tk = this.CurrentToken.Kind;
                switch (tk)
                {
                    case SyntaxKind.OpenParenToken:
                        expr = _syntaxFactory.InvocationExpression(expr, this.ParseParenthesizedArgumentList());
                        break;

                    case SyntaxKind.OpenBracketToken:
                        expr = _syntaxFactory.ElementAccessExpression(expr, this.ParseBracketedArgumentList());
                        break;

                    case SyntaxKind.PlusPlusToken:
                    case SyntaxKind.MinusMinusToken:
                        expr = _syntaxFactory.PostfixUnaryExpression(SyntaxFacts.GetPostfixUnaryExpression(tk), expr, this.EatToken());
                        break;

                    case SyntaxKind.ColonColonToken:
                        if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken)
                        {
                            // replace :: with missing dot and annotate with skipped text "::" and error
                            var ccToken = this.EatToken();
                            ccToken = this.AddError(ccToken, ErrorCode.ERR_UnexpectedAliasedName);
                            var dotToken = this.ConvertToMissingWithTrailingTrivia(ccToken, SyntaxKind.DotToken);
                            expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, dotToken, this.ParseSimpleName(NameOptions.InExpression));
                        }
                        else
                        {
                            // just some random trailing :: ?
                            expr = AddTrailingSkippedSyntax(expr, this.EatTokenWithPrejudice(SyntaxKind.DotToken));
                        }
                        break;

                    case SyntaxKind.MinusGreaterThanToken:
                        expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.PointerMemberAccessExpression, expr, this.EatToken(), this.ParseSimpleName(NameOptions.InExpression));
                        break;
                    case SyntaxKind.DotToken:
                        // if we have the error situation:
                        //
                        //      expr.
                        //      X Y
                        //
                        // Then we don't want to parse this out as "Expr.X"
                        //
                        // It's far more likely the member access expression is simply incomplete and
                        // there is a new declaration on the next line.
                        if (this.CurrentToken.TrailingTrivia.Any((int)SyntaxKind.EndOfLineTrivia) &&
                            this.PeekToken(1).Kind == SyntaxKind.IdentifierToken &&
                            this.PeekToken(2).ContextualKind == SyntaxKind.IdentifierToken)
                        {
                            expr = _syntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression, expr, this.EatToken(),
                                this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_IdentifierExpected));

                            return expr;
                        }

                        expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, this.EatToken(), this.ParseSimpleName(NameOptions.InExpression));
                        break;

                    case SyntaxKind.QuestionToken:
                        if (CanStartConsequenceExpression(this.PeekToken(1).Kind))
                        {
                            var qToken = this.EatToken();
                            var consequence = ParseConsequenceSyntax();
                            expr = _syntaxFactory.ConditionalAccessExpression(expr, qToken, consequence);
                            expr = CheckFeatureAvailability(expr, MessageID.IDS_FeatureNullPropagatingOperator);
                            break;
                        }

                        goto default;

                    case SyntaxKind.ExclamationToken:
                        expr = _syntaxFactory.PostfixUnaryExpression(SyntaxFacts.GetPostfixUnaryExpression(tk), expr, this.EatToken());
                        expr = CheckFeatureAvailability(expr, MessageID.IDS_FeatureNullableReferenceTypes);
                        break;

                    default:
                        return expr;
                }
            }
        }

        private static bool CanStartConsequenceExpression(SyntaxKind kind)
        {
            return kind == SyntaxKind.DotToken ||
                    kind == SyntaxKind.OpenBracketToken;
        }

        internal ExpressionSyntax ParseConsequenceSyntax()
        {
            SyntaxKind tk = this.CurrentToken.Kind;
            ExpressionSyntax expr = null;
            switch (tk)
            {
                case SyntaxKind.DotToken:
                    expr = _syntaxFactory.MemberBindingExpression(this.EatToken(), this.ParseSimpleName(NameOptions.InExpression));
                    break;

                case SyntaxKind.OpenBracketToken:
                    expr = _syntaxFactory.ElementBindingExpression(this.ParseBracketedArgumentList());
                    break;
            }

            Debug.Assert(expr != null);

            while (true)
            {
                tk = this.CurrentToken.Kind;
                switch (tk)
                {
                    case SyntaxKind.OpenParenToken:
                        expr = _syntaxFactory.InvocationExpression(expr, this.ParseParenthesizedArgumentList());
                        break;

                    case SyntaxKind.OpenBracketToken:
                        expr = _syntaxFactory.ElementAccessExpression(expr, this.ParseBracketedArgumentList());
                        break;

                    case SyntaxKind.DotToken:
                        expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, this.EatToken(), this.ParseSimpleName(NameOptions.InExpression));
                        break;

                    case SyntaxKind.QuestionToken:
                        if (CanStartConsequenceExpression(this.PeekToken(1).Kind))
                        {
                            var qToken = this.EatToken();
                            var consequence = ParseConsequenceSyntax();
                            expr = _syntaxFactory.ConditionalAccessExpression(expr, qToken, consequence);
                        }
                        return expr;

                    default:
                        return expr;
                }
            }
        }

        internal ArgumentListSyntax ParseParenthesizedArgumentList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.ArgumentList)
            {
                return (ArgumentListSyntax)this.EatNode();
            }

            ParseArgumentList(
                openToken: out SyntaxToken openToken,
                arguments: out SeparatedSyntaxList<ArgumentSyntax> arguments,
                closeToken: out SyntaxToken closeToken,
                openKind: SyntaxKind.OpenParenToken,
                closeKind: SyntaxKind.CloseParenToken);
            return _syntaxFactory.ArgumentList(openToken, arguments, closeToken);
        }

        internal BracketedArgumentListSyntax ParseBracketedArgumentList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.BracketedArgumentList)
            {
                return (BracketedArgumentListSyntax)this.EatNode();
            }

            ParseArgumentList(
                openToken: out SyntaxToken openToken,
                arguments: out SeparatedSyntaxList<ArgumentSyntax> arguments,
                closeToken: out SyntaxToken closeToken,
                openKind: SyntaxKind.OpenBracketToken,
                closeKind: SyntaxKind.CloseBracketToken);
            return _syntaxFactory.BracketedArgumentList(openToken, arguments, closeToken);
        }

        private void ParseArgumentList(
            out SyntaxToken openToken,
            out SeparatedSyntaxList<ArgumentSyntax> arguments,
            out SyntaxToken closeToken,
            SyntaxKind openKind,
            SyntaxKind closeKind)
        {
            Debug.Assert(openKind == SyntaxKind.OpenParenToken || openKind == SyntaxKind.OpenBracketToken);
            Debug.Assert(closeKind == SyntaxKind.CloseParenToken || closeKind == SyntaxKind.CloseBracketToken);
            Debug.Assert((openKind == SyntaxKind.OpenParenToken) == (closeKind == SyntaxKind.CloseParenToken));
            bool isIndexer = openKind == SyntaxKind.OpenBracketToken;

            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken ||
                this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
            {
                // convert `[` into `(` or vice versa for error recovery
                openToken = this.EatTokenAsKind(openKind);
            }
            else
            {
                openToken = this.EatToken(openKind);
            }

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfArgumentList;

            SeparatedSyntaxListBuilder<ArgumentSyntax> list = default(SeparatedSyntaxListBuilder<ArgumentSyntax>);
            try
            {
                if (this.CurrentToken.Kind != closeKind && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                {
tryAgain:
                    if (list.IsNull)
                    {
                        list = _pool.AllocateSeparated<ArgumentSyntax>();
                    }

                    if (this.IsPossibleArgumentExpression() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        // first argument
                        list.Add(this.ParseArgumentExpression(isIndexer));

                        // additional arguments
                        while (true)
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken ||
                                this.CurrentToken.Kind == SyntaxKind.CloseBracketToken ||
                                this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleArgumentExpression())
                            {
                                list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                list.Add(this.ParseArgumentExpression(isIndexer));
                                continue;
                            }
                            else if (this.SkipBadArgumentListTokens(ref openToken, list, SyntaxKind.CommaToken, closeKind) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (this.SkipBadArgumentListTokens(ref openToken, list, SyntaxKind.IdentifierToken, closeKind) == PostSkipAction.Continue)
                    {
                        goto tryAgain;
                    }
                }
                else if (isIndexer && this.CurrentToken.Kind == closeKind)
                {
                    // An indexer always expects at least one value. And so we need to give an error
                    // for the case where we see only "[]". ParseArgumentExpression gives it.

                    if (list.IsNull)
                    {
                        list = _pool.AllocateSeparated<ArgumentSyntax>();
                    }

                    list.Add(this.ParseArgumentExpression(isIndexer));
                }

                _termState = saveTerm;

                if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken ||
                    this.CurrentToken.Kind == SyntaxKind.CloseBracketToken)
                {
                    // convert `]` into `)` or vice versa for error recovery
                    closeToken = this.EatTokenAsKind(closeKind);
                }
                else
                {
                    closeToken = this.EatToken(closeKind);
                }

                arguments = list.ToList();
            }
            finally
            {
                if (!list.IsNull)
                {
                    _pool.Free(list);
                }
            }
        }

        private PostSkipAction SkipBadArgumentListTokens(ref SyntaxToken open, SeparatedSyntaxListBuilder<ArgumentSyntax> list, SyntaxKind expected, SyntaxKind closeKind)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleArgumentExpression(),
                p => p.CurrentToken.Kind == closeKind || p.CurrentToken.Kind == SyntaxKind.SemicolonToken || p.IsTerminator(),
                expected);
        }

        private bool IsEndOfArgumentList()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.CloseBracketToken;
        }

        private bool IsPossibleArgumentExpression()
        {
            return IsValidArgumentRefKindKeyword(this.CurrentToken.Kind) || this.IsPossibleExpression();
        }

        private static bool IsValidArgumentRefKindKeyword(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private ArgumentSyntax ParseArgumentExpression(bool isIndexer)
        {
            NameColonSyntax nameColon = null;
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.ColonToken)
            {
                var name = this.ParseIdentifierName();
                var colon = this.EatToken(SyntaxKind.ColonToken);
                nameColon = _syntaxFactory.NameColon(name, colon);
                nameColon = CheckFeatureAvailability(nameColon, MessageID.IDS_FeatureNamedArgument);
            }

            SyntaxToken refKindKeyword = null;
            if (IsValidArgumentRefKindKeyword(this.CurrentToken.Kind))
            {
                refKindKeyword = this.EatToken();
            }

            ExpressionSyntax expression;

            if (isIndexer && (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.CloseBracketToken))
            {
                expression = this.ParseIdentifierName(ErrorCode.ERR_ValueExpected);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                expression = this.ParseIdentifierName(ErrorCode.ERR_MissingArgument);
            }
            else
            {
                if (refKindKeyword?.Kind == SyntaxKind.InKeyword)
                {
                    refKindKeyword = this.CheckFeatureAvailability(refKindKeyword, MessageID.IDS_FeatureReadOnlyReferences);
                }

                // According to Language Specification, section 7.6.7 Element access
                //      The argument-list of an element-access is not allowed to contain ref or out arguments.
                // However, we actually do support ref indexing of indexed properties in COM interop
                // scenarios, and when indexing an object of static type "dynamic". So we enforce
                // that the ref/out of the argument must match the parameter when binding the argument list.

                expression = (refKindKeyword?.Kind == SyntaxKind.OutKeyword)
                    ? ParseExpressionOrDeclaration(ParseTypeMode.Normal, feature: MessageID.IDS_FeatureOutVar, permitTupleDesignation: false)
                    : ParseSubExpression(Precedence.Expression);
            }

            return _syntaxFactory.Argument(nameColon, refKindKeyword, expression);
        }

        private TypeOfExpressionSyntax ParseTypeOfExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var type = this.ParseTypeOrVoid();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.TypeOfExpression(keyword, openParen, type, closeParen);
        }

        private ExpressionSyntax ParseDefaultExpression()
        {
            var keyword = this.EatToken();
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                var type = this.ParseType();
                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

                keyword = CheckFeatureAvailability(keyword, MessageID.IDS_FeatureDefault);
                return _syntaxFactory.DefaultExpression(keyword, openParen, type, closeParen);
            }
            else
            {
                keyword = CheckFeatureAvailability(keyword, MessageID.IDS_FeatureDefaultLiteral);
                return _syntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, keyword);
            }
        }

        private SizeOfExpressionSyntax ParseSizeOfExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var type = this.ParseType();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.SizeOfExpression(keyword, openParen, type, closeParen);
        }

        private MakeRefExpressionSyntax ParseMakeRefExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(Precedence.Expression);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.MakeRefExpression(keyword, openParen, expr, closeParen);
        }

        private RefTypeExpressionSyntax ParseRefTypeExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(Precedence.Expression);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.RefTypeExpression(keyword, openParen, expr, closeParen);
        }

        private CheckedExpressionSyntax ParseCheckedOrUncheckedExpression()
        {
            var checkedOrUnchecked = this.EatToken();
            Debug.Assert(checkedOrUnchecked.Kind == SyntaxKind.CheckedKeyword || checkedOrUnchecked.Kind == SyntaxKind.UncheckedKeyword);
            var kind = (checkedOrUnchecked.Kind == SyntaxKind.CheckedKeyword) ? SyntaxKind.CheckedExpression : SyntaxKind.UncheckedExpression;

            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(Precedence.Expression);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.CheckedExpression(kind, checkedOrUnchecked, openParen, expr, closeParen);
        }

        private RefValueExpressionSyntax ParseRefValueExpression()
        {
            var @refvalue = this.EatToken(SyntaxKind.RefValueKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(Precedence.Expression);
            var comma = this.EatToken(SyntaxKind.CommaToken);
            var type = this.ParseType();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.RefValueExpression(@refvalue, openParen, expr, comma, type, closeParen);
        }

        private bool ScanParenthesizedImplicitlyTypedLambda(Precedence precedence)
        {
            if (!(precedence <= Precedence.Lambda))
            {
                return false;
            }

            //  case 1:  ( x ,
            if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken
                && (!this.IsInQuery || !IsTokenQueryContextualKeyword(this.PeekToken(1)))
                && this.PeekToken(2).Kind == SyntaxKind.CommaToken)
            {
                // Make sure it really looks like a lambda, not just a tuple
                int curTk = 3;
                while (true)
                {
                    var tk = this.PeekToken(curTk++);

                    // skip  identifiers commas and predefined types in any combination for error recovery
                    if (tk.Kind != SyntaxKind.IdentifierToken
                        && !SyntaxFacts.IsPredefinedType(tk.Kind)
                        && tk.Kind != SyntaxKind.CommaToken
                        && (this.IsInQuery || !IsTokenQueryContextualKeyword(tk)))
                    {
                        break;
                    };
                }

                // ) =>
                return this.PeekToken(curTk - 1).Kind == SyntaxKind.CloseParenToken &&
                       this.PeekToken(curTk).Kind == SyntaxKind.EqualsGreaterThanToken;
            }

            //  case 2:  ( x ) =>
            if (IsTrueIdentifier(this.PeekToken(1))
                && this.PeekToken(2).Kind == SyntaxKind.CloseParenToken
                && this.PeekToken(3).Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }

            //  case 3:  ( ) =>
            if (this.PeekToken(1).Kind == SyntaxKind.CloseParenToken
                && this.PeekToken(2).Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }

            // case 4:  ( params
            // This case is interesting in that it is not legal; this error could be caught at parse time but we would rather
            // recover from the error and let the semantic analyzer deal with it.
            if (this.PeekToken(1).Kind == SyntaxKind.ParamsKeyword)
            {
                return true;
            }

            return false;
        }

        private bool ScanExplicitlyTypedLambda(Precedence precedence)
        {
            if (!(precedence <= Precedence.Lambda))
            {
                return false;
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                bool foundParameterModifier = false;

                // do we have the following:
                //   case 1: ( T x , ... ) =>
                //   case 2: ( T x ) =>
                //   case 3: ( out T x,
                //   case 4: ( ref T x,
                //   case 5: ( out T x ) =>
                //   case 6: ( ref T x ) =>
                //   case 7: ( in T x ) =>
                //
                // if so then parse it as a lambda

                // Note: in the first two cases, we cannot distinguish a lambda from a tuple expression
                // containing declaration expressions, so we scan forwards to the `=>` so we know for sure.

                while (true)
                {
                    // Advance past the open paren or comma.
                    this.EatToken();

                    // Eat 'out' or 'ref' for cases [3, 6]. Even though not allowed in a lambda,
                    // we treat `params` similarly for better error recovery.
                    switch (this.CurrentToken.Kind)
                    {
                        case SyntaxKind.RefKeyword:
                            this.EatToken();
                            foundParameterModifier = true;
                            if (this.CurrentToken.Kind == SyntaxKind.ReadOnlyKeyword)
                            {
                                this.EatToken();
                            }
                            break;
                        case SyntaxKind.OutKeyword:
                        case SyntaxKind.InKeyword:
                        case SyntaxKind.ParamsKeyword:
                            this.EatToken();
                            foundParameterModifier = true;
                            break;
                    }

                    if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken)
                    {
                        return foundParameterModifier;
                    }

                    // NOTE: advances CurrentToken
                    if (this.ScanType() == ScanTypeFlags.NotType)
                    {
                        return false;
                    }

                    if (this.IsTrueIdentifier())
                    {
                        // eat the identifier
                        this.EatToken();
                    }

                    switch (this.CurrentToken.Kind)
                    {
                        case SyntaxKind.EndOfFileToken:
                            return foundParameterModifier;

                        case SyntaxKind.CommaToken:
                            if (foundParameterModifier)
                            {
                                return true;
                            }

                            continue;

                        case SyntaxKind.CloseParenToken:
                            return this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken;

                        default:
                            return false;
                    }
                }
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        private ExpressionSyntax ParseCastOrParenExpressionOrLambdaOrTuple(Precedence precedence)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenParenToken);

            var resetPoint = this.GetResetPoint();
            try
            {
                if (ScanParenthesizedImplicitlyTypedLambda(precedence))
                {
                    return this.ParseLambdaExpression();
                }

                // We have a decision to make -- is this a cast, or is it a parenthesized
                // expression?  Because look-ahead is cheap with our token stream, we check
                // to see if this "looks like" a cast (without constructing any parse trees)
                // to help us make the decision.
                if (this.ScanCast())
                {
                    if (!IsCurrentTokenQueryKeywordInQuery())
                    {
                        // Looks like a cast, so parse it as one.
                        this.Reset(ref resetPoint);
                        var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                        var type = this.ParseType();
                        var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                        var expr = this.ParseSubExpression(Precedence.Cast);
                        return _syntaxFactory.CastExpression(openParen, type, closeParen, expr);
                    }
                }

                this.Reset(ref resetPoint);
                if (this.ScanExplicitlyTypedLambda(precedence))
                {
                    return this.ParseLambdaExpression();
                }

                // Doesn't look like a cast, so parse this as a parenthesized expression or tuple.
                {
                    this.Reset(ref resetPoint);
                    var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                    var expression = this.ParseExpressionOrDeclaration(ParseTypeMode.FirstElementOfPossibleTupleLiteral, feature: 0, permitTupleDesignation: true);

                    //  ( <expr>,    must be a tuple
                    if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        var firstArg = _syntaxFactory.Argument(nameColon: null, refKindKeyword: default(SyntaxToken), expression: expression);
                        return ParseTupleExpressionTail(openParen, firstArg);
                    }

                    // ( name:
                    if (expression.Kind == SyntaxKind.IdentifierName && this.CurrentToken.Kind == SyntaxKind.ColonToken)
                    {
                        var nameColon = _syntaxFactory.NameColon((IdentifierNameSyntax)expression, EatToken());
                        expression = this.ParseExpressionOrDeclaration(ParseTypeMode.FirstElementOfPossibleTupleLiteral, feature: 0, permitTupleDesignation: true);

                        var firstArg = _syntaxFactory.Argument(nameColon, refKindKeyword: default(SyntaxToken), expression: expression);
                        return ParseTupleExpressionTail(openParen, firstArg);
                    }

                    var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                    return _syntaxFactory.ParenthesizedExpression(openParen, expression, closeParen);
                }
            }
            finally
            {
                this.Release(ref resetPoint);
            }
        }

        private TupleExpressionSyntax ParseTupleExpressionTail(SyntaxToken openParen, ArgumentSyntax firstArg)
        {
            var list = _pool.AllocateSeparated<ArgumentSyntax>();
            try
            {
                list.Add(firstArg);

                while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    var comma = this.EatToken(SyntaxKind.CommaToken);
                    list.AddSeparator(comma);

                    ArgumentSyntax arg;

                    var expression = ParseExpressionOrDeclaration(ParseTypeMode.AfterTupleComma, feature: 0, permitTupleDesignation: true);
                    if (expression.Kind == SyntaxKind.IdentifierName && this.CurrentToken.Kind == SyntaxKind.ColonToken)
                    {
                        var nameColon = _syntaxFactory.NameColon((IdentifierNameSyntax)expression, EatToken());
                        expression = ParseExpressionOrDeclaration(ParseTypeMode.AfterTupleComma, feature: 0, permitTupleDesignation: true);
                        arg = _syntaxFactory.Argument(nameColon, refKindKeyword: default(SyntaxToken), expression: expression);
                    }
                    else
                    {
                        arg = _syntaxFactory.Argument(nameColon: null, refKindKeyword: default(SyntaxToken), expression: expression);
                    }

                    list.Add(arg);
                }

                if (list.Count < 2)
                {
                    list.AddSeparator(SyntaxFactory.MissingToken(SyntaxKind.CommaToken));
                    var missing = this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TupleTooFewElements);
                    list.Add(_syntaxFactory.Argument(nameColon: null, refKindKeyword: default(SyntaxToken), expression: missing));
                }

                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                var result = _syntaxFactory.TupleExpression(openParen, list, closeParen);

                result = CheckFeatureAvailability(result, MessageID.IDS_FeatureTuples);
                return result;
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private bool ScanCast(bool forPattern = false)
        {
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            this.EatToken();

            var type = this.ScanType(forPattern: forPattern);
            if (type == ScanTypeFlags.NotType)
            {
                return false;
            }

            if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
            {
                return false;
            }

            // If we have any of the following, we know it must be a cast:
            // 1) (Goo*)bar;
            // 2) (Goo?)bar;
            // 3) "(int)bar" or "(int[])bar"
            // 4) (G::Goo)bar
            if (type == ScanTypeFlags.PointerOrMultiplication ||
                type == ScanTypeFlags.NullableType ||
                type == ScanTypeFlags.MustBeType ||
                type == ScanTypeFlags.AliasQualifiedName)
            {
                return true;
            }

            this.EatToken();

            // check for ambiguous type or expression followed by disambiguating token.  i.e.
            //
            // "(A)b" is a cast.  But "(A)+b" is not a cast.  
            return (type == ScanTypeFlags.GenericTypeOrMethod || type == ScanTypeFlags.GenericTypeOrExpression || type == ScanTypeFlags.NonGenericTypeOrExpression || type == ScanTypeFlags.TupleType) && CanFollowCast(this.CurrentToken.Kind);
        }

        private bool ScanAsyncLambda(Precedence precedence)
        {
            // Adapted from CParser::ScanAsyncLambda

            // Precedence must not exceed that of lambdas
            if (precedence > Precedence.Lambda)
            {
                return false;
            }

            // Async lambda must start with 'async'
            if (this.CurrentToken.ContextualKind != SyntaxKind.AsyncKeyword)
            {
                return false;
            }

            // 'async <identifier> => ...' looks like an async simple lambda
            if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken && this.PeekToken(2).Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }

            // Non-simple async lambda must be of the form 'async (...'
            if (this.PeekToken(1).Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            {
                var resetPoint = this.GetResetPoint();

                // Skip 'async'
                EatToken(SyntaxKind.IdentifierToken);

                // Check whether looks like implicitly or explicitly typed lambda
                bool isAsync = ScanParenthesizedImplicitlyTypedLambda(precedence) || ScanExplicitlyTypedLambda(precedence);

                // Restore current token index
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);

                return isAsync;
            }
        }

        private static bool CanFollowCast(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AsKeyword:
                case SyntaxKind.IsKeyword:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.CommaToken:
                case SyntaxKind.EqualsToken:
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.AsteriskEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.PercentEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.CaretEqualsToken:
                case SyntaxKind.BarEqualsToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.QuestionToken:
                case SyntaxKind.ColonToken:
                case SyntaxKind.BarBarToken:
                case SyntaxKind.AmpersandAmpersandToken:
                case SyntaxKind.BarToken:
                case SyntaxKind.CaretToken:
                case SyntaxKind.AmpersandToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsToken:
                case SyntaxKind.LessThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.GreaterThanEqualsToken:
                case SyntaxKind.QuestionQuestionEqualsToken:
                case SyntaxKind.LessThanLessThanToken:
                case SyntaxKind.GreaterThanGreaterThanToken:
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.AsteriskToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.PercentToken:
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.MinusGreaterThanToken:
                case SyntaxKind.QuestionQuestionToken:
                case SyntaxKind.EndOfFileToken:
                case SyntaxKind.SwitchKeyword:
                case SyntaxKind.EqualsGreaterThanToken:
                case SyntaxKind.DotDotToken:
                    return false;
                default:
                    return true;
            }
        }

        private ExpressionSyntax ParseNewExpression()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.NewKeyword);

            if (this.IsAnonymousType())
            {
                return this.ParseAnonymousTypeExpression();
            }
            else if (this.IsImplicitlyTypedArray())
            {
                return this.ParseImplicitlyTypedArrayCreation();
            }
            else
            {
                // assume object creation as default case
                return this.ParseArrayOrObjectCreationExpression();
            }
        }

        private bool IsAnonymousType()
        {
            return this.CurrentToken.Kind == SyntaxKind.NewKeyword && this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken;
        }

        private AnonymousObjectCreationExpressionSyntax ParseAnonymousTypeExpression()
        {
            Debug.Assert(IsAnonymousType());
            var @new = this.EatToken(SyntaxKind.NewKeyword);
            @new = CheckFeatureAvailability(@new, MessageID.IDS_FeatureAnonymousTypes);

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenBraceToken);

            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var expressions = _pool.AllocateSeparated<AnonymousObjectMemberDeclaratorSyntax>();
            this.ParseAnonymousTypeMemberInitializers(ref openBrace, ref expressions);
            var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
            var result = _syntaxFactory.AnonymousObjectCreationExpression(@new, openBrace, expressions, closeBrace);
            _pool.Free(expressions);

            return result;
        }

        private void ParseAnonymousTypeMemberInitializers(ref SyntaxToken openBrace, ref SeparatedSyntaxListBuilder<AnonymousObjectMemberDeclaratorSyntax> list)
        {
            if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
            {
tryAgain:
                if (this.IsPossibleExpression() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // first argument
                    list.Add(this.ParseAnonymousTypeMemberInitializer());

                    // additional arguments
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleExpression())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));

                            // check for exit case after legal trailing comma
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }
                            else if (!this.IsPossibleExpression())
                            {
                                goto tryAgain;
                            }

                            list.Add(this.ParseAnonymousTypeMemberInitializer());
                            continue;
                        }
                        else if (this.SkipBadInitializerListTokens(ref openBrace, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadInitializerListTokens(ref openBrace, list, SyntaxKind.IdentifierToken) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }
            }
        }

        private AnonymousObjectMemberDeclaratorSyntax ParseAnonymousTypeMemberInitializer()
        {
            var nameEquals = this.IsNamedAssignment()
                ? ParseNameEquals()
                : null;

            var expression = this.ParseExpressionCore();
            return _syntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, expression);
        }

        private bool IsInitializerMember()
        {
            return this.IsComplexElementInitializer() ||
                this.IsNamedAssignment() ||
                this.IsDictionaryInitializer() ||
                this.IsPossibleExpression();
        }

        private bool IsComplexElementInitializer()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;
        }

        private bool IsNamedAssignment()
        {
            return IsTrueIdentifier() && this.PeekToken(1).Kind == SyntaxKind.EqualsToken;
        }

        private bool IsDictionaryInitializer()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken;
        }

        private ExpressionSyntax ParseArrayOrObjectCreationExpression()
        {
            SyntaxToken @new = this.EatToken(SyntaxKind.NewKeyword);
            var type = this.ParseType(ParseTypeMode.NewExpression);

            if (type.Kind == SyntaxKind.ArrayType)
            {
                // Check for an initializer.
                InitializerExpressionSyntax initializer = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    initializer = this.ParseArrayInitializer();
                }

                return _syntaxFactory.ArrayCreationExpression(@new, (ArrayTypeSyntax)type, initializer);
            }
            else
            {
                ArgumentListSyntax argumentList = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
                {
                    argumentList = this.ParseParenthesizedArgumentList();
                }

                InitializerExpressionSyntax initializer = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    initializer = this.ParseObjectOrCollectionInitializer();
                }

                // we need one or the other
                if (argumentList == null && initializer == null)
                {
                    argumentList = _syntaxFactory.ArgumentList(
                        this.EatToken(SyntaxKind.OpenParenToken, ErrorCode.ERR_BadNewExpr),
                        default(SeparatedSyntaxList<ArgumentSyntax>),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken));
                }

                return _syntaxFactory.ObjectCreationExpression(@new, type, argumentList, initializer);
            }
        }

        private InitializerExpressionSyntax ParseObjectOrCollectionInitializer()
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            var initializers = _pool.AllocateSeparated<ExpressionSyntax>();
            try
            {
                bool isObjectInitializer;
                this.ParseObjectOrCollectionInitializerMembers(ref openBrace, initializers, out isObjectInitializer);
                Debug.Assert(initializers.Count > 0 || isObjectInitializer);

                openBrace = CheckFeatureAvailability(openBrace, isObjectInitializer ? MessageID.IDS_FeatureObjectInitializer : MessageID.IDS_FeatureCollectionInitializer);

                var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
                return _syntaxFactory.InitializerExpression(
                    isObjectInitializer ?
                        SyntaxKind.ObjectInitializerExpression :
                        SyntaxKind.CollectionInitializerExpression,
                    openBrace,
                    initializers,
                    closeBrace);
            }
            finally
            {
                _pool.Free(initializers);
            }
        }

        private void ParseObjectOrCollectionInitializerMembers(ref SyntaxToken startToken, SeparatedSyntaxListBuilder<ExpressionSyntax> list, out bool isObjectInitializer)
        {
            // Empty initializer list must be parsed as an object initializer.
            isObjectInitializer = true;

            if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
            {
tryAgain:
                if (this.IsInitializerMember() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // We have at least one initializer expression.
                    // If at least one initializer expression is a named assignment, this is an object initializer.
                    // Otherwise, this is a collection initializer.
                    isObjectInitializer = false;

                    // first argument
                    list.Add(this.ParseObjectOrCollectionInitializerMember(ref isObjectInitializer));

                    // additional arguments
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsInitializerMember())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));

                            // check for exit case after legal trailing comma
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }

                            list.Add(this.ParseObjectOrCollectionInitializerMember(ref isObjectInitializer));
                            continue;
                        }
                        else if (this.SkipBadInitializerListTokens(ref startToken, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadInitializerListTokens(ref startToken, list, SyntaxKind.IdentifierToken) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }
            }

            // We may have invalid initializer elements. These will be reported during binding.
        }

        private ExpressionSyntax ParseObjectOrCollectionInitializerMember(ref bool isObjectInitializer)
        {
            if (this.IsComplexElementInitializer())
            {
                return this.ParseComplexElementInitializer();
            }
            else if (IsDictionaryInitializer())
            {
                isObjectInitializer = true;
                var initializer = this.ParseDictionaryInitializer();
                initializer = CheckFeatureAvailability(initializer, MessageID.IDS_FeatureDictionaryInitializer);
                return initializer;
            }
            else if (this.IsNamedAssignment())
            {
                isObjectInitializer = true;
                return this.ParseObjectInitializerNamedAssignment();
            }
            else
            {
                return this.ParseExpressionCore();
            }
        }

        private PostSkipAction SkipBadInitializerListTokens<T>(ref SyntaxToken startToken, SeparatedSyntaxListBuilder<T> list, SyntaxKind expected)
            where T : CSharpSyntaxNode
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref startToken, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBraceToken || p.IsTerminator(),
                expected);
        }

        private ExpressionSyntax ParseObjectInitializerNamedAssignment()
        {
            var identifier = this.ParseIdentifierName();
            var equal = this.EatToken(SyntaxKind.EqualsToken);
            ExpressionSyntax expression;
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                expression = this.ParseObjectOrCollectionInitializer();
            }
            else
            {
                expression = this.ParseExpressionCore();
            }

            return _syntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, identifier, equal, expression);
        }

        private ExpressionSyntax ParseDictionaryInitializer()
        {
            var arguments = this.ParseBracketedArgumentList();
            var equal = this.EatToken(SyntaxKind.EqualsToken);
            var expression = this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                ? this.ParseObjectOrCollectionInitializer()
                : this.ParseExpressionCore();

            var elementAccess = _syntaxFactory.ImplicitElementAccess(arguments);
            return _syntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression, elementAccess, equal, expression);
        }

        private InitializerExpressionSyntax ParseComplexElementInitializer()
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var initializers = _pool.AllocateSeparated<ExpressionSyntax>();
            try
            {
                DiagnosticInfo closeBraceError;
                this.ParseExpressionsForComplexElementInitializer(ref openBrace, initializers, out closeBraceError);
                var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
                if (closeBraceError != null)
                {
                    closeBrace = WithAdditionalDiagnostics(closeBrace, closeBraceError);
                }
                return _syntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression, openBrace, initializers, closeBrace);
            }
            finally
            {
                _pool.Free(initializers);
            }
        }

        private void ParseExpressionsForComplexElementInitializer(ref SyntaxToken openBrace, SeparatedSyntaxListBuilder<ExpressionSyntax> list, out DiagnosticInfo closeBraceError)
        {
            closeBraceError = null;

            if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
            {
tryAgain:
                if (this.IsPossibleExpression() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // first argument
                    list.Add(this.ParseExpressionCore());

                    // additional arguments
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleExpression())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                closeBraceError = MakeError(this.CurrentToken, ErrorCode.ERR_ExpressionExpected);
                                break;
                            }
                            list.Add(this.ParseExpressionCore());
                            continue;
                        }
                        else if (this.SkipBadInitializerListTokens(ref openBrace, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
                    }
                }
                else if (this.SkipBadInitializerListTokens(ref openBrace, list, SyntaxKind.IdentifierToken) == PostSkipAction.Continue)
                {
                    goto tryAgain;
                }
            }
        }

        private bool IsImplicitlyTypedArray()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.NewKeyword || this.CurrentToken.Kind == SyntaxKind.StackAllocKeyword);
            return this.PeekToken(1).Kind == SyntaxKind.OpenBracketToken;
        }

        private ImplicitArrayCreationExpressionSyntax ParseImplicitlyTypedArrayCreation()
        {
            var @new = this.EatToken(SyntaxKind.NewKeyword);
            @new = CheckFeatureAvailability(@new, MessageID.IDS_FeatureImplicitArray);
            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);

            var commas = _pool.Allocate();
            try
            {
                int lastTokenPosition = -1;
                while (IsMakingProgress(ref lastTokenPosition))
                {
                    if (this.IsPossibleExpression())
                    {
                        var size = this.AddError(this.ParseExpressionCore(), ErrorCode.ERR_InvalidArray);
                        if (commas.Count == 0)
                        {
                            openBracket = AddTrailingSkippedSyntax(openBracket, size);
                        }
                        else
                        {
                            AddTrailingSkippedSyntax(commas, size);
                        }
                    }

                    if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        commas.Add(this.EatToken());
                        continue;
                    }

                    break;
                }

                var closeBracket = this.EatToken(SyntaxKind.CloseBracketToken);
                var initializer = this.ParseArrayInitializer();

                return _syntaxFactory.ImplicitArrayCreationExpression(@new, openBracket, commas.ToList(), closeBracket, initializer);
            }
            finally
            {
                _pool.Free(commas);
            }
        }

        private InitializerExpressionSyntax ParseArrayInitializer()
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            // NOTE:  This loop allows " { <initexpr>, } " but not " { , } "
            var list = _pool.AllocateSeparated<ExpressionSyntax>();
            try
            {
                if (this.CurrentToken.Kind != SyntaxKind.CloseBraceToken)
                {
tryAgain:
                    if (this.IsPossibleVariableInitializer() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        list.Add(this.ParseVariableInitializer());

                        while (true)
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }
                            else if (this.IsPossibleVariableInitializer() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                            {
                                list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));

                                // check for exit case after legal trailing comma
                                if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                                {
                                    break;
                                }
                                else if (!this.IsPossibleVariableInitializer())
                                {
                                    goto tryAgain;
                                }

                                list.Add(this.ParseVariableInitializer());
                                continue;
                            }
                            else if (SkipBadArrayInitializerTokens(ref openBrace, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (SkipBadArrayInitializerTokens(ref openBrace, list, SyntaxKind.CommaToken) == PostSkipAction.Continue)
                    {
                        goto tryAgain;
                    }
                }

                var closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);

                return _syntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, openBrace, list, closeBrace);
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private PostSkipAction SkipBadArrayInitializerTokens(ref SyntaxToken openBrace, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openBrace, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleVariableInitializer(),
                p => this.CurrentToken.Kind == SyntaxKind.CloseBraceToken || this.IsTerminator(),
                expected);
        }

        private ExpressionSyntax ParseStackAllocExpression()
        {
            if (this.IsImplicitlyTypedArray())
            {
                return ParseImplicitlyTypedStackAllocExpression();
            }
            else
            {
                return ParseRegularStackAllocExpression();
            }
        }

        private ExpressionSyntax ParseImplicitlyTypedStackAllocExpression()
        {
            var @stackalloc = this.EatToken(SyntaxKind.StackAllocKeyword);
            @stackalloc = CheckFeatureAvailability(@stackalloc, MessageID.IDS_FeatureStackAllocInitializer);
            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);

            int lastTokenPosition = -1;
            while (IsMakingProgress(ref lastTokenPosition))
            {
                if (this.IsPossibleExpression())
                {
                    var size = this.AddError(this.ParseExpressionCore(), ErrorCode.ERR_InvalidStackAllocArray);
                    openBracket = AddTrailingSkippedSyntax(openBracket, size);
                }

                if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    var comma = this.AddError(this.EatToken(), ErrorCode.ERR_InvalidStackAllocArray);
                    openBracket = AddTrailingSkippedSyntax(openBracket, comma);
                    continue;
                }

                break;
            }

            var closeBracket = this.EatToken(SyntaxKind.CloseBracketToken);
            var initializer = this.ParseArrayInitializer();
            return _syntaxFactory.ImplicitStackAllocArrayCreationExpression(@stackalloc, openBracket, closeBracket, initializer);
        }

        private ExpressionSyntax ParseRegularStackAllocExpression()
        {
            var @stackalloc = this.EatToken(SyntaxKind.StackAllocKeyword);
            var elementType = this.ParseType();
            InitializerExpressionSyntax initializer = null;
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                @stackalloc = CheckFeatureAvailability(@stackalloc, MessageID.IDS_FeatureStackAllocInitializer);
                initializer = this.ParseArrayInitializer();
            }

            return _syntaxFactory.StackAllocArrayCreationExpression(@stackalloc, elementType, initializer);
        }

        private AnonymousMethodExpressionSyntax ParseAnonymousMethodExpression()
        {
            bool parentScopeIsInAsync = IsInAsync;
            IsInAsync = false;
            SyntaxToken asyncToken = null;
            if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword)
            {
                asyncToken = this.EatContextualToken(SyntaxKind.AsyncKeyword);
                asyncToken = CheckFeatureAvailability(asyncToken, MessageID.IDS_FeatureAsync);
                IsInAsync = true;
            }

            var @delegate = this.EatToken(SyntaxKind.DelegateKeyword);
            @delegate = CheckFeatureAvailability(@delegate, MessageID.IDS_FeatureAnonDelegates);

            ParameterListSyntax parameterList = null;
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                parameterList = this.ParseParenthesizedParameterList();
            }

            // In mismatched braces cases (missing a }) it is possible for delegate declarations to be
            // parsed as delegate statement expressions.  When this situation occurs all subsequent 
            // delegate declarations will also be parsed as delegate statement expressions.  In a file with
            // a sufficient number of delegates, common in generated code, it will put considerable 
            // stack pressure on the parser.  
            //
            // To help avoid this problem we don't recursively descend into a delegate expression unless 
            // { } are actually present.  This keeps the stack pressure lower in bad code scenarios.
            if (this.CurrentToken.Kind != SyntaxKind.OpenBraceToken)
            {
                // There's a special error code for a missing token after an accessor keyword
                var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
                return _syntaxFactory.AnonymousMethodExpression(
                    asyncToken,
                    @delegate,
                    parameterList,
                    _syntaxFactory.Block(
                        openBrace,
                        statements: default,
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken)),
                    expressionBody: null);
            }

            var body = this.ParseBlock();
            IsInAsync = parentScopeIsInAsync;
            return _syntaxFactory.AnonymousMethodExpression(
                asyncToken, @delegate, parameterList, body, expressionBody: null);
        }

        private LambdaExpressionSyntax ParseLambdaExpression()
        {
            bool parentScopeIsInAsync = IsInAsync;
            SyntaxToken asyncToken = null;
            if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword &&
                PeekToken(1).Kind != SyntaxKind.EqualsGreaterThanToken)
            {
                asyncToken = this.EatContextualToken(SyntaxKind.AsyncKeyword);
                asyncToken = CheckFeatureAvailability(asyncToken, MessageID.IDS_FeatureAsync);
                IsInAsync = true;
            }

            var result = ParseLambdaExpression(asyncToken);

            IsInAsync = parentScopeIsInAsync;
            return result;
        }

        private LambdaExpressionSyntax ParseLambdaExpression(SyntaxToken asyncToken)
        {
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var paramList = this.ParseLambdaParameterList();
                var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
                arrow = CheckFeatureAvailability(arrow, MessageID.IDS_FeatureLambda);
                var (block, expression) = ParseLambdaBody();

                return _syntaxFactory.ParenthesizedLambdaExpression(
                    asyncToken, paramList, arrow, block, expression);
            }
            else
            {
                var name = this.ParseIdentifierToken();
                var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
                arrow = CheckFeatureAvailability(arrow, MessageID.IDS_FeatureLambda);

                var parameter = _syntaxFactory.Parameter(
                    attributeLists: default, modifiers: default,
                    type: null, identifier: name, @default: null);
                var (block, expression) = ParseLambdaBody();

                return _syntaxFactory.SimpleLambdaExpression(
                    asyncToken, parameter, arrow, block, expression);
            }
        }

        private (BlockSyntax, ExpressionSyntax) ParseLambdaBody()
            => CurrentToken.Kind == SyntaxKind.OpenBraceToken
                ? (ParseBlock(), default(ExpressionSyntax))
                : (default(BlockSyntax), ParsePossibleRefExpression());

        private ParameterListSyntax ParseLambdaParameterList()
        {
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfParameterList;

            var nodes = _pool.AllocateSeparated<ParameterSyntax>();
            try
            {
                if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
                {
tryAgain:
                    if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleLambdaParameter())
                    {
                        // first parameter
                        var parameter = this.ParseLambdaParameter();
                        nodes.Add(parameter);

                        // additional parameters
                        int tokenProgress = -1;
                        while (IsMakingProgress(ref tokenProgress))
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleLambdaParameter())
                            {
                                nodes.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                parameter = this.ParseLambdaParameter();
                                nodes.Add(parameter);
                                continue;
                            }
                            else if (this.SkipBadLambdaParameterListTokens(ref openParen, nodes, SyntaxKind.CommaToken, SyntaxKind.CloseParenToken) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (this.SkipBadLambdaParameterListTokens(ref openParen, nodes, SyntaxKind.IdentifierToken, SyntaxKind.CloseParenToken) == PostSkipAction.Continue)
                    {
                        goto tryAgain;
                    }
                }

                _termState = saveTerm;
                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

                return _syntaxFactory.ParameterList(openParen, nodes, closeParen);
            }
            finally
            {
                _pool.Free(nodes);
            }
        }

        private bool IsPossibleLambdaParameter()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ParamsKeyword:
                // params is not actually legal in a lambda, but we allow it for error
                // recovery purposes and then give an error during semantic analysis.
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.OpenParenToken:   // tuple
                    return true;

                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();

                default:
                    return IsPredefinedType(this.CurrentToken.Kind);
            }
        }

        private PostSkipAction SkipBadLambdaParameterListTokens(ref SyntaxToken openParen, SeparatedSyntaxListBuilder<ParameterSyntax> list, SyntaxKind expected, SyntaxKind closeKind)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openParen, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleLambdaParameter(),
                p => p.CurrentToken.Kind == closeKind || p.IsTerminator(),
                expected);
        }

        private ParameterSyntax ParseLambdaParameter()
        {
            // Params are actually illegal in a lambda, but we'll allow it for error recovery purposes and
            // give the "params unexpected" error at semantic analysis time.
            bool hasModifier = IsParameterModifier(this.CurrentToken.Kind);

            TypeSyntax paramType = null;
            SyntaxListBuilder modifiers = _pool.Allocate();

            if (ShouldParseLambdaParameterType(hasModifier))
            {
                if (hasModifier)
                {
                    ParseParameterModifiers(modifiers);
                }

                paramType = ParseType(ParseTypeMode.Parameter);
            }

            SyntaxToken paramName = this.ParseIdentifierToken();
            var parameter = _syntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), modifiers.ToList(), paramType, paramName, null);
            _pool.Free(modifiers);
            return parameter;
        }

        private bool ShouldParseLambdaParameterType(bool hasModifier)
        {
            // If we have "ref/out/in/params" always try to parse out a type.
            if (hasModifier)
            {
                return true;
            }

            // If we have "int/string/etc." always parse out a type.
            if (IsPredefinedType(this.CurrentToken.Kind))
            {
                return true;
            }

            // if we have a tuple type in a lambda.
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                return true;
            }

            if (this.IsTrueIdentifier(this.CurrentToken))
            {
                // Don't parse out a type if we see:
                //
                //      (a,
                //      (a)
                //      (a =>
                //      (a {
                //
                // In all other cases, parse out a type.
                var peek1 = this.PeekToken(1);
                if (peek1.Kind != SyntaxKind.CommaToken &&
                    peek1.Kind != SyntaxKind.CloseParenToken &&
                    peek1.Kind != SyntaxKind.EqualsGreaterThanToken &&
                    peek1.Kind != SyntaxKind.OpenBraceToken)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCurrentTokenQueryContextualKeyword
        {
            get
            {
                return IsTokenQueryContextualKeyword(this.CurrentToken);
            }
        }

        private static bool IsTokenQueryContextualKeyword(SyntaxToken token)
        {
            if (IsTokenStartOfNewQueryClause(token))
            {
                return true;
            }

            switch (token.ContextualKind)
            {
                case SyntaxKind.OnKeyword:
                case SyntaxKind.EqualsKeyword:
                case SyntaxKind.AscendingKeyword:
                case SyntaxKind.DescendingKeyword:
                case SyntaxKind.ByKeyword:
                    return true;
            }

            return false;
        }

        private static bool IsTokenStartOfNewQueryClause(SyntaxToken token)
        {
            switch (token.ContextualKind)
            {
                case SyntaxKind.FromKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.IntoKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.SelectKeyword:
                case SyntaxKind.LetKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsQueryExpression(bool mayBeVariableDeclaration, bool mayBeMemberDeclaration)
        {
            if (this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword)
            {
                return this.IsQueryExpressionAfterFrom(mayBeVariableDeclaration, mayBeMemberDeclaration);
            }

            return false;
        }

        // from_clause ::= from <type>? <identifier> in expression
        private bool IsQueryExpressionAfterFrom(bool mayBeVariableDeclaration, bool mayBeMemberDeclaration)
        {
            // from x ...
            var pk1 = this.PeekToken(1).Kind;
            if (IsPredefinedType(pk1))
            {
                return true;
            }

            if (pk1 == SyntaxKind.IdentifierToken)
            {
                var pk2 = this.PeekToken(2).Kind;
                if (pk2 == SyntaxKind.InKeyword)
                {
                    return true;
                }

                if (mayBeVariableDeclaration)
                {
                    if (pk2 == SyntaxKind.SemicolonToken ||    // from x;
                        pk2 == SyntaxKind.CommaToken ||        // from x, y;
                        pk2 == SyntaxKind.EqualsToken)         // from x = null;
                    {
                        return false;
                    }
                }

                if (mayBeMemberDeclaration)
                {
                    // from idf { ...   property decl
                    // from idf(...     method decl
                    if (pk2 == SyntaxKind.OpenParenToken ||
                        pk2 == SyntaxKind.OpenBraceToken)
                    {
                        return false;
                    }

                    // otherwise we need to scan a type
                }
                else
                {
                    return true;
                }
            }

            // from T x ...
            var resetPoint = this.GetResetPoint();
            try
            {
                this.EatToken();

                ScanTypeFlags isType = this.ScanType();
                if (isType != ScanTypeFlags.NotType && (this.CurrentToken.Kind == SyntaxKind.IdentifierToken || this.CurrentToken.Kind == SyntaxKind.InKeyword))
                {
                    return true;
                }
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }

            return false;
        }

        private QueryExpressionSyntax ParseQueryExpression(Precedence precedence)
        {
            this.EnterQuery();
            var fc = this.ParseFromClause();
            fc = CheckFeatureAvailability(fc, MessageID.IDS_FeatureQueryExpression);
            if (precedence > Precedence.Assignment && IsStrict)
            {
                fc = this.AddError(fc, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(SyntaxKind.FromKeyword));
            }

            var body = this.ParseQueryBody();
            this.LeaveQuery();
            return _syntaxFactory.QueryExpression(fc, body);
        }

        private QueryBodySyntax ParseQueryBody()
        {
            var clauses = _pool.Allocate<QueryClauseSyntax>();
            try
            {
                SelectOrGroupClauseSyntax selectOrGroupBy = null;
                QueryContinuationSyntax continuation = null;

                // from, join, let, where and orderby
                while (true)
                {
                    switch (this.CurrentToken.ContextualKind)
                    {
                        case SyntaxKind.FromKeyword:
                            var fc = this.ParseFromClause();
                            clauses.Add(fc);
                            continue;
                        case SyntaxKind.JoinKeyword:
                            clauses.Add(this.ParseJoinClause());
                            continue;
                        case SyntaxKind.LetKeyword:
                            clauses.Add(this.ParseLetClause());
                            continue;
                        case SyntaxKind.WhereKeyword:
                            clauses.Add(this.ParseWhereClause());
                            continue;
                        case SyntaxKind.OrderByKeyword:
                            clauses.Add(this.ParseOrderByClause());
                            continue;
                    }

                    break;
                }

                // select or group clause
                switch (this.CurrentToken.ContextualKind)
                {
                    case SyntaxKind.SelectKeyword:
                        selectOrGroupBy = this.ParseSelectClause();
                        break;
                    case SyntaxKind.GroupKeyword:
                        selectOrGroupBy = this.ParseGroupClause();
                        break;
                    default:
                        selectOrGroupBy = _syntaxFactory.SelectClause(
                            this.EatToken(SyntaxKind.SelectKeyword, ErrorCode.ERR_ExpectedSelectOrGroup),
                            this.CreateMissingIdentifierName());
                        break;
                }

                // optional query continuation clause
                if (this.CurrentToken.ContextualKind == SyntaxKind.IntoKeyword)
                {
                    continuation = this.ParseQueryContinuation();
                }

                return _syntaxFactory.QueryBody(clauses, selectOrGroupBy, continuation);
            }
            finally
            {
                _pool.Free(clauses);
            }
        }

        private FromClauseSyntax ParseFromClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword);
            var @from = this.EatContextualToken(SyntaxKind.FromKeyword);
            @from = CheckFeatureAvailability(@from, MessageID.IDS_FeatureQueryExpression);

            TypeSyntax type = null;
            if (this.PeekToken(1).Kind != SyntaxKind.InKeyword)
            {
                type = this.ParseType();
            }

            SyntaxToken name;
            if (this.PeekToken(1).ContextualKind == SyntaxKind.InKeyword &&
                (this.CurrentToken.Kind != SyntaxKind.IdentifierToken || SyntaxFacts.IsQueryContextualKeyword(this.CurrentToken.ContextualKind)))
            {
                //if this token is a something other than an identifier (someone accidentally used a contextual
                //keyword or a literal, for example), but we can see that the "in" is in the right place, then
                //just replace whatever is here with a missing identifier
                name = this.EatToken();
                name = WithAdditionalDiagnostics(name, this.GetExpectedTokenError(SyntaxKind.IdentifierToken, name.ContextualKind, name.GetLeadingTriviaWidth(), name.Width));
                name = this.ConvertToMissingWithTrailingTrivia(name, SyntaxKind.IdentifierToken);
            }
            else
            {
                name = this.ParseIdentifierToken();
            }
            var @in = this.EatToken(SyntaxKind.InKeyword);
            var expression = this.ParseExpressionCore();
            return _syntaxFactory.FromClause(@from, type, name, @in, expression);
        }

        private JoinClauseSyntax ParseJoinClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.JoinKeyword);
            var @join = this.EatContextualToken(SyntaxKind.JoinKeyword);
            TypeSyntax type = null;
            if (this.PeekToken(1).Kind != SyntaxKind.InKeyword)
            {
                type = this.ParseType();
            }

            var name = this.ParseIdentifierToken();
            var @in = this.EatToken(SyntaxKind.InKeyword);
            var inExpression = this.ParseExpressionCore();
            var @on = this.EatContextualToken(SyntaxKind.OnKeyword, ErrorCode.ERR_ExpectedContextualKeywordOn);
            var leftExpression = this.ParseExpressionCore();
            var @equals = this.EatContextualToken(SyntaxKind.EqualsKeyword, ErrorCode.ERR_ExpectedContextualKeywordEquals);
            var rightExpression = this.ParseExpressionCore();
            JoinIntoClauseSyntax joinInto = null;
            if (this.CurrentToken.ContextualKind == SyntaxKind.IntoKeyword)
            {
                var @into = ConvertToKeyword(this.EatToken());
                var intoId = this.ParseIdentifierToken();
                joinInto = _syntaxFactory.JoinIntoClause(@into, intoId);
            }

            return _syntaxFactory.JoinClause(@join, type, name, @in, inExpression, @on, leftExpression, @equals, rightExpression, joinInto);
        }

        private LetClauseSyntax ParseLetClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.LetKeyword);
            var @let = this.EatContextualToken(SyntaxKind.LetKeyword);
            var name = this.ParseIdentifierToken();
            var equal = this.EatToken(SyntaxKind.EqualsToken);
            var expression = this.ParseExpressionCore();
            return _syntaxFactory.LetClause(@let, name, equal, expression);
        }

        private WhereClauseSyntax ParseWhereClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword);
            var @where = this.EatContextualToken(SyntaxKind.WhereKeyword);
            var condition = this.ParseExpressionCore();
            return _syntaxFactory.WhereClause(@where, condition);
        }

        private OrderByClauseSyntax ParseOrderByClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.OrderByKeyword);
            var @orderby = this.EatContextualToken(SyntaxKind.OrderByKeyword);

            var list = _pool.AllocateSeparated<OrderingSyntax>();
            try
            {
                // first argument
                list.Add(this.ParseOrdering());

                // additional arguments
                while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken || this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                    {
                        break;
                    }
                    else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                        list.Add(this.ParseOrdering());
                        continue;
                    }
                    else if (this.SkipBadOrderingListTokens(list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                    {
                        break;
                    }
                }

                return _syntaxFactory.OrderByClause(@orderby, list);
            }
            finally
            {
                _pool.Free(list);
            }
        }

        private PostSkipAction SkipBadOrderingListTokens(SeparatedSyntaxListBuilder<OrderingSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken,
                p => p.CurrentToken.Kind == SyntaxKind.CloseParenToken
                    || p.CurrentToken.Kind == SyntaxKind.SemicolonToken
                    || p.IsCurrentTokenQueryContextualKeyword
                    || p.IsTerminator(),
                expected);
        }

        private OrderingSyntax ParseOrdering()
        {
            var expression = this.ParseExpressionCore();
            SyntaxToken direction = null;
            SyntaxKind kind = SyntaxKind.AscendingOrdering;

            if (this.CurrentToken.ContextualKind == SyntaxKind.AscendingKeyword ||
                this.CurrentToken.ContextualKind == SyntaxKind.DescendingKeyword)
            {
                direction = ConvertToKeyword(this.EatToken());
                if (direction.Kind == SyntaxKind.DescendingKeyword)
                {
                    kind = SyntaxKind.DescendingOrdering;
                }
            }

            return _syntaxFactory.Ordering(kind, expression, direction);
        }

        private SelectClauseSyntax ParseSelectClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.SelectKeyword);
            var @select = this.EatContextualToken(SyntaxKind.SelectKeyword);
            var expression = this.ParseExpressionCore();
            return _syntaxFactory.SelectClause(@select, expression);
        }

        private GroupClauseSyntax ParseGroupClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.GroupKeyword);
            var @group = this.EatContextualToken(SyntaxKind.GroupKeyword);
            var groupExpression = this.ParseExpressionCore();
            var @by = this.EatContextualToken(SyntaxKind.ByKeyword, ErrorCode.ERR_ExpectedContextualKeywordBy);
            var byExpression = this.ParseExpressionCore();
            return _syntaxFactory.GroupClause(@group, groupExpression, @by, byExpression);
        }

        private QueryContinuationSyntax ParseQueryContinuation()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.IntoKeyword);
            var @into = this.EatContextualToken(SyntaxKind.IntoKeyword);
            var name = this.ParseIdentifierToken();
            var body = this.ParseQueryBody();
            return _syntaxFactory.QueryContinuation(@into, name, body);
        }

        private bool IsStrict => this.Options.Features.ContainsKey("strict");

        [Obsolete("Use IsIncrementalAndFactoryContextMatches")]
        private new bool IsIncremental
        {
            get { throw new Exception("Use IsIncrementalAndFactoryContextMatches"); }
        }

        private bool IsIncrementalAndFactoryContextMatches
        {
            get
            {
                if (!base.IsIncremental)
                {
                    return false;
                }

                CSharp.CSharpSyntaxNode current = this.CurrentNode;
                return current != null && MatchesFactoryContext(current.Green, _syntaxFactoryContext);
            }
        }

        internal static bool MatchesFactoryContext(GreenNode green, SyntaxFactoryContext context)
        {
            return context.IsInAsync == green.ParsedInAsync &&
                context.IsInQuery == green.ParsedInQuery;
        }

        private bool IsInAsync
        {
            get
            {
                return _syntaxFactoryContext.IsInAsync;
            }
            set
            {
                _syntaxFactoryContext.IsInAsync = value;
            }
        }

        private bool IsInQuery
        {
            get { return _syntaxFactoryContext.IsInQuery; }
        }

        private void EnterQuery()
        {
            _syntaxFactoryContext.QueryDepth++;
        }

        private void LeaveQuery()
        {
            Debug.Assert(_syntaxFactoryContext.QueryDepth > 0);
            _syntaxFactoryContext.QueryDepth--;
        }

        private new ResetPoint GetResetPoint()
        {
            return new ResetPoint(
                base.GetResetPoint(),
                _termState,
                _isInTry,
                _syntaxFactoryContext.IsInAsync,
                _syntaxFactoryContext.QueryDepth);
        }

        private void Reset(ref ResetPoint state)
        {
            _termState = state.TerminatorState;
            _isInTry = state.IsInTry;
            _syntaxFactoryContext.IsInAsync = state.IsInAsync;
            _syntaxFactoryContext.QueryDepth = state.QueryDepth;
            base.Reset(ref state.BaseResetPoint);
        }

        private void Release(ref ResetPoint state)
        {
            base.Release(ref state.BaseResetPoint);
        }

        private new struct ResetPoint
        {
            internal SyntaxParser.ResetPoint BaseResetPoint;
            internal readonly TerminatorState TerminatorState;
            internal readonly bool IsInTry;
            internal readonly bool IsInAsync;
            internal readonly int QueryDepth;

            internal ResetPoint(
                SyntaxParser.ResetPoint resetPoint,
                TerminatorState terminatorState,
                bool isInTry,
                bool isInAsync,
                int queryDepth)
            {
                this.BaseResetPoint = resetPoint;
                this.TerminatorState = terminatorState;
                this.IsInTry = isInTry;
                this.IsInAsync = isInAsync;
                this.QueryDepth = queryDepth;
            }
        }

        internal TNode ConsumeUnexpectedTokens<TNode>(TNode node) where TNode : CSharpSyntaxNode
        {
            if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken) return node;
            SyntaxListBuilder<SyntaxToken> b = _pool.Allocate<SyntaxToken>();
            while (this.CurrentToken.Kind != SyntaxKind.EndOfFileToken)
            {
                b.Add(this.EatToken());
            }

            var trailingTrash = b.ToList();
            _pool.Free(b);

            node = this.AddError(node, ErrorCode.ERR_UnexpectedToken, trailingTrash[0].ToString());
            node = this.AddTrailingSkippedSyntax(node, trailingTrash.Node);
            return node;
        }
    }
}
