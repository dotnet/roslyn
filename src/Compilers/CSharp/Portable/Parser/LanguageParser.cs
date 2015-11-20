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

        // Special Name checks
        private static bool IsName(CSharpSyntaxNode node, SyntaxKind kind)
        {
            if (node.Kind == SyntaxKind.IdentifierToken)
            {
                return ((SyntaxToken)node).ContextualKind == kind;
            }
            else if (node.Kind == SyntaxKind.IdentifierName)
            {
                return ((IdentifierNameSyntax)node).Identifier.ContextualKind == kind;
            }
            else
            {
                return node.ToString() == SyntaxFacts.GetText(kind);
            }
        }

        private static bool IsNameGlobal(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.GlobalKeyword);
        }

        private static bool IsNameAssembly(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.AssemblyKeyword);
        }

        private static bool IsNameModule(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.ModuleKeyword);
        }

        private static bool IsNameType(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.TypeKeyword);
        }

        private static bool IsNameGet(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.GetKeyword);
        }

        private static bool IsNameSet(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.SetKeyword);
        }

        private static bool IsNameAdd(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.AddKeyword);
        }

        private static bool IsNameRemove(CSharpSyntaxNode node)
        {
            return IsName(node, SyntaxKind.RemoveKeyword);
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
            IsEndOfilterClause = 1 << 13,
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
                TerminatorState isolated = _termState & (TerminatorState)i;
                if (isolated != 0)
                {
                    switch (isolated)
                    {
                        case TerminatorState.IsNamespaceMemberStartOrStop:
                            if (this.IsNamespaceMemberStartOrStop())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsAttributeDeclarationTerminator:
                            if (this.IsAttributeDeclarationTerminator())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsPossibleAggregateClauseStartOrStop:
                            if (this.IsPossibleAggregateClauseStartOrStop())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsPossibleMemberStartOrStop:
                            if (this.IsPossibleMemberStartOrStop())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfReturnType:
                            if (this.IsEndOfReturnType())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfParameterList:
                            if (this.IsEndOfParameterList())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfFieldDeclaration:
                            if (this.IsEndOfFieldDeclaration())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsPossibleEndOfVariableDeclaration:
                            if (this.IsPossibleEndOfVariableDeclaration())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfTypeArgumentList:
                            if (this.IsEndOfTypeArgumentList())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsPossibleStatementStartOrStop:
                            if (this.IsPossibleStatementStartOrStop())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfFixedStatement:
                            if (this.IsEndOfFixedStatement())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfTryBlock:
                            if (this.IsEndOfTryBlock())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfCatchClause:
                            if (this.IsEndOfCatchClause())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfilterClause:
                            if (this.IsEndOfFilterClause())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfCatchBlock:
                            if (this.IsEndOfCatchBlock())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfDoWhileExpression:
                            if (this.IsEndOfDoWhileExpression())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfForStatementArgument:
                            if (this.IsEndOfForStatementArgument())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfDeclarationClause:
                            if (this.IsEndOfDeclarationClause())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsEndOfArgumentList:
                            if (this.IsEndOfArgumentList())
                            {
                                return true;
                            }

                            break;
                        case TerminatorState.IsSwitchSectionStart:
                            if (this.IsPossibleSwitchSection())
                            {
                                return true;
                            }

                            break;

                        case TerminatorState.IsEndOfTypeParameterList:
                            if (this.IsEndOfTypeParameterList())
                            {
                                return true;
                            }

                            break;

                        case TerminatorState.IsEndOfMethodSignature:
                            if (this.IsEndOfMethodSignature())
                            {
                                return true;
                            }

                            break;

                        case TerminatorState.IsEndOfNameInExplicitInterface:
                            if (this.IsEndOfNameInExplicitInterface())
                            {
                                return true;
                            }

                            break;
                    }
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
            catch (Exception ex) when (StackGuard.IsInsufficientExecutionStackException(ex))
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

        private NamespaceDeclarationSyntax ParseNamespaceDeclaration()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.NamespaceDeclaration)
            {
                return (NamespaceDeclarationSyntax)this.EatNode();
            }

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.NamespaceKeyword);
            var namespaceToken = this.EatToken(SyntaxKind.NamespaceKeyword);

            if (IsScript)
            {
                namespaceToken = this.AddError(namespaceToken, ErrorCode.ERR_NamespaceNotAllowedInScript);
            }

            var name = this.ParseQualifiedName();
            if (ContainsGeneric(name))
            {
                // We're not allowed to have generics.
                name = this.AddError(name, ErrorCode.ERR_UnexpectedGenericName);
            }

            if (ContainsAlias(name))
            {
                name = this.AddError(name, ErrorCode.ERR_UnexpectedAliasedName);
            }

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
                SyntaxToken semicolon = null;
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    semicolon = this.EatToken();
                }

                Debug.Assert(initialBadNodes == null); // init bad nodes should have been attached to open brace...
                return _syntaxFactory.NamespaceDeclaration(namespaceToken, name, openBrace, body.Externs, body.Usings, body.Members, closeBrace, semicolon);
            }
            finally
            {
                body.Free(_pool);
            }
        }

        private static bool ContainsAlias(NameSyntax name)
        {
            switch (name.Kind)
            {
                case SyntaxKind.GenericName:
                    return false;
                case SyntaxKind.AliasQualifiedName:
                    return true;
                case SyntaxKind.QualifiedName:
                    var qualifiedName = (QualifiedNameSyntax)name;
                    return ContainsAlias(qualifiedName.Left);
            }

            return false;
        }

        private static bool ContainsGeneric(NameSyntax name)
        {
            switch (name.Kind)
            {
                case SyntaxKind.GenericName:
                    return true;
                case SyntaxKind.AliasQualifiedName:
                    return ContainsGeneric(((AliasQualifiedNameSyntax)name).Name);
                case SyntaxKind.QualifiedName:
                    var qualifiedName = (QualifiedNameSyntax)name;
                    return ContainsGeneric(qualifiedName.Left) || ContainsGeneric(qualifiedName.Right);
            }

            return false;
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
                body.Members[body.Members.Count - 1] = AddTrailingSkippedSyntax(body.Members[body.Members.Count - 1], skippedSyntax);
            }
            else if (body.Attributes.Count > 0)
            {
                body.Attributes[body.Attributes.Count - 1] = AddTrailingSkippedSyntax(body.Attributes[body.Attributes.Count - 1], skippedSyntax);
            }
            else if (body.Usings.Count > 0)
            {
                body.Usings[body.Usings.Count - 1] = AddTrailingSkippedSyntax(body.Usings[body.Usings.Count - 1], skippedSyntax);
            }
            else if (body.Externs.Count > 0)
            {
                body.Externs[body.Externs.Count - 1] = AddTrailingSkippedSyntax(body.Externs[body.Externs.Count - 1], skippedSyntax);
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

                            body.Members.Add(this.ParseNamespaceDeclaration());
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
                    return IsPartialInNamespaceMemberDeclaration();
                default:
                    return IsPossibleStartOfTypeDeclaration(this.CurrentToken.Kind);
            }
        }

        private bool IsPartialInNamespaceMemberDeclaration()
        {
            if (this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword)
            {
                if (this.IsPartialType())
                {
                    return true;
                }
                else if (this.PeekToken(1).Kind == SyntaxKind.NamespaceKeyword)
                {
                    return true;
                }
            }

            return false;
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
            //   extern alias foo;
            //   extern alias foo();
            //   extern alias foo { get; }

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

        private NameEqualsSyntax ParseNameEquals(bool warnOnGlobal = false)
        {
            Debug.Assert(this.IsNamedAssignment());

            var id = this.ParseIdentifierToken();
            var equals = this.EatToken(SyntaxKind.EqualsToken);

            // Warn on "using global = X".
            if (warnOnGlobal && IsNameGlobal(id))
            {
                id = this.AddError(id, ErrorCode.WRN_GlobalAliasDefn);
            }

            return _syntaxFactory.NameEquals(_syntaxFactory.IdentifierName(id), equals);
        }

        private UsingDirectiveSyntax ParseUsingDirective()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.UsingDirective)
            {
                return (UsingDirectiveSyntax)this.EatNode();
            }

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.UsingKeyword);

            var usingToken = this.EatToken(SyntaxKind.UsingKeyword);

            var staticToken = default(SyntaxToken);
            if (this.CurrentToken.Kind == SyntaxKind.StaticKeyword)
            {
                staticToken = this.EatToken(SyntaxKind.StaticKeyword);
            }

            NameEqualsSyntax alias = null;
            if (this.IsNamedAssignment())
            {
                alias = ParseNameEquals(warnOnGlobal: true);
            }

            NameSyntax name;
            SyntaxToken semicolon;

            if (IsPossibleNamespaceMemberDeclaration())
            {
                //We're worried about the case where someone already has a correct program
                //and they've gone back to add a using directive, but have not finished the
                //new directive.  e.g.
                //
                //    using 
                //    namespace Foo {
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

        private void ParseAttributeDeclarations(SyntaxListBuilder list, bool allowAttributes = true)
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsAttributeDeclarationTerminator;
            while (this.IsPossibleAttributeDeclaration())
            {
                var section = this.ParseAttributeDeclaration();
                if (!allowAttributes)
                {
                    section = this.AddError(section, ErrorCode.ERR_AttributesNotAllowed);
                }

                list.Add(section);
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
                    // comma is optional, but if it is here it should be followed by another attribute
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
                    nodes.AddSeparator(SyntaxFactory.MissingToken(SyntaxKind.CommaToken));
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
                    bool shouldHaveName = false;
                    tryAgain:
                    if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
                    {
                        if (this.IsPossibleAttributeArgument() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                        {
                            // first argument
                            argNodes.Add(this.ParseAttributeArgument(ref shouldHaveName));

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
                                    argNodes.Add(this.ParseAttributeArgument(ref shouldHaveName));
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

        private AttributeArgumentSyntax ParseAttributeArgument(ref bool shouldHaveName)
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
                            shouldHaveName = true;
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

            var expr = this.ParseExpressionCore();

            // Not named -- give an error if it's supposed to be
            if (shouldHaveName && nameEquals == null)
            {
                expr = this.AddError(expr, ErrorCode.ERR_NamedArgumentExpected);
            }

            return _syntaxFactory.AttributeArgument(nameEquals, nameColon, expr);
        }

        [Flags]
        private enum SyntaxModifier
        {
            None = 0,
            Public = 0x0001,
            Internal = 0x0002,
            Protected = 0x0004,
            Private = 0x0008,
            Sealed = 0x0010,
            Abstract = 0x0020,
            Static = 0x0040,
            Virtual = 0x0080,
            Extern = 0x0100,
            New = 0x0200,
            Override = 0x0400,
            ReadOnly = 0x0800,
            Volatile = 0x1000,
            Unsafe = 0x2000,
            Partial = 0x4000,
            Async = 0x8000
        }

        private const SyntaxModifier AccessModifiers = SyntaxModifier.Public | SyntaxModifier.Internal | SyntaxModifier.Protected | SyntaxModifier.Private;

        private static SyntaxModifier GetModifier(SyntaxToken token)
        {
            switch (token.Kind)
            {
                case SyntaxKind.PublicKeyword:
                    return SyntaxModifier.Public;
                case SyntaxKind.InternalKeyword:
                    return SyntaxModifier.Internal;
                case SyntaxKind.ProtectedKeyword:
                    return SyntaxModifier.Protected;
                case SyntaxKind.PrivateKeyword:
                    return SyntaxModifier.Private;
                case SyntaxKind.SealedKeyword:
                    return SyntaxModifier.Sealed;
                case SyntaxKind.AbstractKeyword:
                    return SyntaxModifier.Abstract;
                case SyntaxKind.StaticKeyword:
                    return SyntaxModifier.Static;
                case SyntaxKind.VirtualKeyword:
                    return SyntaxModifier.Virtual;
                case SyntaxKind.ExternKeyword:
                    return SyntaxModifier.Extern;
                case SyntaxKind.NewKeyword:
                    return SyntaxModifier.New;
                case SyntaxKind.OverrideKeyword:
                    return SyntaxModifier.Override;
                case SyntaxKind.ReadOnlyKeyword:
                    return SyntaxModifier.ReadOnly;
                case SyntaxKind.VolatileKeyword:
                    return SyntaxModifier.Volatile;
                case SyntaxKind.UnsafeKeyword:
                    return SyntaxModifier.Unsafe;
                case SyntaxKind.IdentifierToken:
                    switch (token.ContextualKind)
                    {
                        case SyntaxKind.PartialKeyword:
                            return SyntaxModifier.Partial;
                        case SyntaxKind.AsyncKeyword:
                            return SyntaxModifier.Async;
                    }

                    goto default;
                default:
                    return SyntaxModifier.None;
            }
        }

        private static SyntaxModifier GetFieldModifier(SyntaxToken token)
        {
            switch (token.Kind)
            {
                case SyntaxKind.PublicKeyword:
                    return SyntaxModifier.Public;
                case SyntaxKind.InternalKeyword:
                    return SyntaxModifier.Internal;
                case SyntaxKind.ProtectedKeyword:
                    return SyntaxModifier.Protected;
                case SyntaxKind.PrivateKeyword:
                    return SyntaxModifier.Private;
                case SyntaxKind.StaticKeyword:
                    return SyntaxModifier.Static;
                case SyntaxKind.NewKeyword:
                    return SyntaxModifier.New;
                case SyntaxKind.ReadOnlyKeyword:
                    return SyntaxModifier.ReadOnly;
                case SyntaxKind.VolatileKeyword:
                    return SyntaxModifier.Volatile;
                default:
                    return SyntaxModifier.None;
            }
        }

        private bool IsPossibleModifier()
        {
            return IsPossibleModifier(this.CurrentToken);
        }

        private bool IsPossibleModifier(SyntaxToken token)
        {
            return GetModifier(token) != SyntaxModifier.None;
        }

        private void ParseModifiers(SyntaxListBuilder tokens)
        {
            SyntaxModifier mods = 0;
            bool seenNoDuplicates = true;
            bool seenNoAccessibilityDuplicates = true;

            while (true)
            {
                var newMod = GetModifier(this.CurrentToken);
                if (newMod == SyntaxModifier.None)
                {
                    break;
                }

                SyntaxToken modTok;
                switch (newMod)
                {
                    case SyntaxModifier.Partial:
                        {
                            var nextToken = PeekToken(1);
                            if (this.IsPartialType())
                            {
                                modTok = ConvertToKeyword(this.EatToken());
                                modTok = CheckFeatureAvailability(modTok, MessageID.IDS_FeaturePartialTypes);
                            }
                            else if (this.IsPartialMember())
                            {
                                modTok = ConvertToKeyword(this.EatToken());
                                modTok = CheckFeatureAvailability(modTok, MessageID.IDS_FeaturePartialMethod);
                            }
                            else if (nextToken.Kind == SyntaxKind.NamespaceKeyword)
                            {
                                goto default;
                            }
                            else if (nextToken.Kind == SyntaxKind.EnumKeyword || nextToken.Kind == SyntaxKind.DelegateKeyword)
                            {
                                modTok = ConvertToKeyword(this.EatToken());
                                modTok = this.AddError(modTok, ErrorCode.ERR_PartialMisplaced);
                            }
                            else if (!IsPossibleStartOfTypeDeclaration(nextToken.Kind) || GetModifier(nextToken) == SyntaxModifier.None)
                            {
                                return;
                            }
                            else
                            {
                                modTok = ConvertToKeyword(this.EatToken());
                                modTok = this.AddError(modTok, ErrorCode.ERR_PartialMisplaced);
                            }

                            break;
                        }
                    case SyntaxModifier.Async:
                        {
                            // Adapted from CParser::IsAsyncMethod.

                            var nextToken = PeekToken(1);
                            if (GetModifier(nextToken) != SyntaxModifier.None && !SyntaxFacts.IsContextualKeyword(nextToken.ContextualKind))
                            {
                                // If the next token is a (non-contextual) modifier keyword, then this token is
                                // definitely the async keyword
                                modTok = ConvertToKeyword(this.EatToken());
                                modTok = CheckFeatureAvailability(modTok, MessageID.IDS_FeatureAsync);
                                break;
                            }

                            bool isModifier = false;

                            // Some of our helpers start at the current token, so we'll have to advance for their
                            // sake and then backtrack when we're done.  Don't leave this block without releasing
                            // the reset point.
                            {
                                ResetPoint resetPoint = GetResetPoint();

                                this.EatToken(); //move past "async"

                                if (this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword)
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

                                SyntaxToken currToken = this.CurrentToken;
                                if (IsPossibleStartOfTypeDeclaration(currToken.Kind) ||
                                    currToken.Kind == SyntaxKind.EventKeyword ||
                                    ((currToken.Kind == SyntaxKind.ExplicitKeyword || currToken.Kind == SyntaxKind.ImplicitKeyword) && PeekToken(1).Kind == SyntaxKind.OperatorKeyword) ||
                                    (ScanType() != ScanTypeFlags.NotType && (this.CurrentToken.Kind == SyntaxKind.OperatorKeyword || IsPossibleMemberName())))
                                {
                                    isModifier = true;
                                }

                                this.Reset(ref resetPoint);
                                this.Release(ref resetPoint);
                            }

                            if (isModifier)
                            {
                                modTok = ConvertToKeyword(this.EatToken());
                                modTok = CheckFeatureAvailability(modTok, MessageID.IDS_FeatureAsync);
                                break;
                            }
                            else
                            {
                                return;
                            }
                        }
                    default:
                        {
                            modTok = this.EatToken();
                            break;
                        }
                }

                ReportDuplicateModifiers(ref modTok, newMod, mods, ref seenNoDuplicates, ref seenNoAccessibilityDuplicates);
                mods |= newMod;

                tokens.Add(modTok);
            }
        }

        private void ReportDuplicateModifiers(ref SyntaxToken modTok, SyntaxModifier newMod, SyntaxModifier mods, ref bool seenNoDuplicates, ref bool seenNoAccessibilityDuplicates)
        {
            if ((mods & newMod) != 0)
            {
                if (seenNoDuplicates)
                {
                    modTok = this.AddError(modTok, ErrorCode.ERR_DuplicateModifier, SyntaxFacts.GetText(modTok.Kind));
                    seenNoDuplicates = false;
                }
            }
            else
            {
                if ((mods & AccessModifiers) != 0 && (newMod & AccessModifiers) != 0)
                {
                    // Can't have two different access modifiers.
                    // Exception: "internal protected" or "protected internal" is allowed.
                    if (!(((newMod == SyntaxModifier.Protected) && (mods & SyntaxModifier.Internal) != 0) ||
                            ((newMod == SyntaxModifier.Internal) && (mods & SyntaxModifier.Protected) != 0)))
                    {
                        if (seenNoAccessibilityDuplicates)
                        {
                            modTok = this.AddError(modTok, ErrorCode.ERR_BadMemberProtection);
                        }

                        seenNoAccessibilityDuplicates = false;
                    }
                }
            }
        }

        private bool IsPartialType()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword);
            switch (this.PeekToken(1).Kind)
            {
                case SyntaxKind.StructKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.InterfaceKeyword:
                    return true;
            }

            return false;
        }

        private bool IsPartialMember()
        {
            // note(cyrusn): this could have been written like so:
            //
            //  return
            //    this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword &&
            //    this.PeekToken(1).Kind == SyntaxKind.VoidKeyword;
            //
            // However, we want to be lenient and allow the user to write 
            // 'partial' in most modifier lists.  We will then provide them with
            // a more specific message later in binding that they are doing 
            // something wrong.
            //
            // Some might argue that the simple check would suffice.
            // However, we'd like to maintain behavior with 
            // previously shipped versions, and so we're keeping this code.

            // Here we check for:
            //   partial ReturnType MemberName
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword);
            var point = this.GetResetPoint();
            try
            {
                this.EatToken(); // partial

                if (this.ScanType() == ScanTypeFlags.NotType)
                {
                    return false;
                }

                return IsPossibleMemberName();
            }
            finally
            {
                this.Reset(ref point);
                this.Release(ref point);
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

        private static bool CanReuseTypeDeclaration(CSharp.Syntax.MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                // on reuse valid type declaration (not bad namespace members)
                switch (member.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                        return true;
                }
            }

            return false;
        }

        private MemberDeclarationSyntax ParseTypeDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            cancellationToken.ThrowIfCancellationRequested();

            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ClassKeyword:
                    // report use of static class
                    for (int i = 0, n = modifiers.Count; i < n; i++)
                    {
                        if (modifiers[i].Kind == SyntaxKind.StaticKeyword)
                        {
                            modifiers[i] = CheckFeatureAvailability(modifiers[i], MessageID.IDS_FeatureStaticClasses);
                        }
                    }

                    return this.ParseClassOrStructOrInterfaceDeclaration(attributes, modifiers);

                case SyntaxKind.StructKeyword:
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


        private static bool IsMissingName(NameSyntax name)
        {
            return name.Kind == SyntaxKind.IdentifierName && ((IdentifierNameSyntax)name).Identifier.IsMissing;
        }

        private TypeDeclarationSyntax ParseClassOrStructOrInterfaceDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ClassKeyword || this.CurrentToken.Kind == SyntaxKind.StructKeyword || this.CurrentToken.Kind == SyntaxKind.InterfaceKeyword);

            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            var classOrStructOrInterface = this.EatToken();
            var saveTerm = _termState;
            _termState |= TerminatorState.IsPossibleAggregateClauseStartOrStop;
            var name = this.ParseIdentifierToken();
            var typeParameters = this.ParseTypeParameterList(allowVariance: classOrStructOrInterface.Kind == SyntaxKind.InterfaceKeyword);

            _termState = saveTerm;
            bool hasTypeParams = typeParameters != null;
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
                    this.ParseTypeParameterConstraintClauses(hasTypeParams, constraints);
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

                            var memberOrStatement = this.ParseMemberDeclarationOrStatement(kind, name.ValueText);
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

                SyntaxToken semicolon = null;
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    semicolon = this.EatToken();
                }

                switch (classOrStructOrInterface.Kind)
                {
                    case SyntaxKind.ClassKeyword:
                        return _syntaxFactory.ClassDeclaration(
                            attributes,
                            modifiers.ToTokenList(),
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
                            modifiers.ToTokenList(),
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
                            modifiers.ToTokenList(),
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
                CSharpSyntaxNode tmp = members[members.Count - 1];
                this.SkipBadMemberListTokens(ref tmp);
                members[members.Count - 1] = tmp;
            }
            else
            {
                CSharpSyntaxNode tmp = openBrace;
                this.SkipBadMemberListTokens(ref tmp);
                openBrace = (SyntaxToken)tmp;
            }
        }

        private void SkipBadMemberListTokens(ref CSharpSyntaxNode previousNode)
        {
            int curlyCount = 0;
            var tokens = _pool.Allocate();
            try
            {
                bool done = false;

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

                    var token = this.EatToken();
                    if (tokens.Count == 0)
                    {
                        token = this.AddError(token, ErrorCode.ERR_InvalidMemberDecl, token.Text);
                    }

                    tokens.Add(token);
                }

                previousNode = AddTrailingSkippedSyntax(previousNode, tokens.ToListNode());
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
                || this.IsPossibleTypeParameterConstraintClauseStart()
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken;
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
                if (this.IsPossibleTypeParameterConstraintClauseStart())
                {
                    list.Add(_syntaxFactory.SimpleBaseType(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                }
                else
                {
                    TypeSyntax firstType = this.ParseDeclarationType(isConstraint: false, parentIsParameter: false);

                    list.Add(_syntaxFactory.SimpleBaseType(firstType));

                    // any additional types
                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                            || this.IsPossibleTypeParameterConstraintClauseStart())
                        {
                            break;
                        }
                        else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleType())
                        {
                            list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            if (this.IsPossibleTypeParameterConstraintClauseStart())
                            {
                                list.Add(_syntaxFactory.SimpleBaseType(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                            }
                            else
                            {
                                list.Add(_syntaxFactory.SimpleBaseType(this.ParseDeclarationType(isConstraint: false, parentIsParameter: false)));
                            }

                            continue;
                        }
                        else if (this.SkipBadBaseListTokens(ref colon, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                        {
                            break;
                        }
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
                p => p.CurrentToken.Kind == SyntaxKind.OpenBraceToken || p.IsPossibleTypeParameterConstraintClauseStart() || p.IsTerminator(),
                expected);
        }

        private bool IsPossibleTypeParameterConstraintClauseStart()
        {
            return
                this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword &&
                this.PeekToken(1).Kind == SyntaxKind.IdentifierToken &&
                this.PeekToken(2).Kind == SyntaxKind.ColonToken;
        }

        private void ParseTypeParameterConstraintClauses(bool isAllowed, SyntaxListBuilder list)
        {
            while (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
            {
                var constraint = this.ParseTypeParameterConstraintClause();
                if (!isAllowed)
                {
                    constraint = this.AddErrorToFirstToken(constraint, ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl);
                    isAllowed = true; // silence any further errors
                }

                list.Add(constraint);
            }
        }

        private TypeParameterConstraintClauseSyntax ParseTypeParameterConstraintClause()
        {
            var where = this.EatContextualToken(SyntaxKind.WhereKeyword);
            var name = (this.IsPossibleTypeParameterConstraintClauseStart() || !IsTrueIdentifier())
                ? this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_IdentifierExpected)
                : this.ParseIdentifierName();

            var colon = this.EatToken(SyntaxKind.ColonToken);

            var bounds = _pool.AllocateSeparated<TypeParameterConstraintSyntax>();
            try
            {
                bool isStruct = false;

                // first bound
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsPossibleTypeParameterConstraintClauseStart())
                {
                    bounds.Add(_syntaxFactory.TypeConstraint(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                }
                else
                {
                    bounds.Add(this.ParseTypeParameterConstraint(true, ref isStruct));

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
                            if (this.IsPossibleTypeParameterConstraintClauseStart())
                            {
                                bounds.Add(_syntaxFactory.TypeConstraint(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                                break;
                            }
                            else
                            {
                                bounds.Add(this.ParseTypeParameterConstraint(false, ref isStruct));
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

        private TypeParameterConstraintSyntax ParseTypeParameterConstraint(bool isFirst, ref bool isStruct)
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.NewKeyword:
                    var newToken = this.EatToken();
                    if (isStruct)
                    {
                        newToken = this.AddError(newToken, ErrorCode.ERR_NewBoundWithVal);
                    }

                    var open = this.EatToken(SyntaxKind.OpenParenToken);
                    var close = this.EatToken(SyntaxKind.CloseParenToken);
                    if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        newToken = this.AddError(newToken, ErrorCode.ERR_NewBoundMustBeLast);
                    }

                    return _syntaxFactory.ConstructorConstraint(newToken, open, close);
                case SyntaxKind.StructKeyword:
                    isStruct = true;
                    goto case SyntaxKind.ClassKeyword;
                case SyntaxKind.ClassKeyword:
                    var token = this.EatToken();
                    if (!isFirst)
                    {
                        token = this.AddError(token, ErrorCode.ERR_RefValBoundMustBeFirst);
                    }

                    return _syntaxFactory.ClassOrStructConstraint(isStruct ? SyntaxKind.StructConstraint : SyntaxKind.ClassConstraint, token);
                default:
                    var type = this.ParseDeclarationType(true, false);
                    return _syntaxFactory.TypeConstraint(type);
            }
        }

        private PostSkipAction SkipBadTypeParameterConstraintTokens(SeparatedSyntaxListBuilder<TypeParameterConstraintSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                p => this.CurrentToken.Kind != SyntaxKind.CommaToken && !this.IsPossibleTypeParameterConstraint(),
                p => this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsPossibleTypeParameterConstraintClauseStart() || this.IsTerminator(),
                expected);
        }

        private TypeSyntax ParseDeclarationType(bool isConstraint, bool parentIsParameter)
        {
            var type = this.ParseType(parentIsParameter);
            if (type.Kind != SyntaxKind.PredefinedType && !SyntaxFacts.IsName(type.Kind))
            {
                if (isConstraint)
                {
                    type = this.AddError(type, ErrorCode.ERR_BadConstraintType);
                }
                else
                {
                    type = this.AddError(type, ErrorCode.ERR_BadBaseType);
                }
            }

            return type;
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
                    return true;

                default:
                    return false;
            }
        }

        private static bool CanStartTypeDeclaration(SyntaxKind kind)
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

        private static bool CanReuseMemberDeclaration(
            CSharp.Syntax.MemberDeclarationSyntax member,
            string typeName)
        {
            if (member != null)
            {
                switch (member.Kind())
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
                        return true;
                }

                var parent = GetOldParent(member);
                var originalTypeDeclaration = parent as CSharp.Syntax.TypeDeclarationSyntax;

                // originalTypeDeclaration can be null in the case of script code.  In that case
                // the member declaration can be a child of a namespace/compilation-unit instead of
                // a type.
                if (originalTypeDeclaration != null)
                {
                    switch (member.Kind())
                    {
                        case SyntaxKind.MethodDeclaration:
                            // can reuse a method as long as it *doesn't* match the type name.
                            //
                            // TODO(cyrusn): Relax this in the case of generic methods?
                            var methodDeclaration = (CSharp.Syntax.MethodDeclarationSyntax)member;
                            return methodDeclaration.Identifier.ValueText != typeName;

                        case SyntaxKind.ConstructorDeclaration: // fall through
                        case SyntaxKind.DestructorDeclaration:
                            // can reuse constructors or destructors if the name and type name still
                            // match.
                            return originalTypeDeclaration.Identifier.ValueText == typeName;
                    }
                }
            }

            return false;
        }

        // Returns null if we can't parse anything (even partially).
        private MemberDeclarationSyntax ParseMemberDeclarationOrStatement(SyntaxKind parentKind, string typeName = null)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            cancellationToken.ThrowIfCancellationRequested();

            bool isGlobalScript = parentKind == SyntaxKind.CompilationUnit && this.IsScript;
            bool acceptStatement = isGlobalScript;

            // don't reuse members if they were previously declared under a different type keyword kind
            // don't reuse existing constructors & destructors because they have to match typename errors
            // don't reuse methods whose name matches the new type name (they now match as possible constructors)
            if (this.IsIncrementalAndFactoryContextMatches)
            {
                var member = this.CurrentNode as CSharp.Syntax.MemberDeclarationSyntax;
                if (CanReuseMemberDeclaration(member, typeName) || CanReuseTypeDeclaration(member))
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
                this.ParseModifiers(modifiers);
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
                    if (!isGlobalScript && this.CurrentToken.ValueText == typeName)
                    {
                        return this.ParseConstructorDeclaration(typeName, attributes, modifiers);
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
                    return this.ParseDestructorDeclaration(typeName, attributes, modifiers);
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

                if (this.CurrentToken.Kind == SyntaxKind.NamespaceKeyword && parentKind == SyntaxKind.CompilationUnit)
                {
                    // we found a namespace with modifier or an attribute: ignore the attribute/modifier and parse as namespace
                    if (attributes.Count > 0)
                    {
                        attributes[0] = this.AddError(attributes[0], ErrorCode.ERR_BadModifiersOnNamespace);
                    }
                    else
                    {
                        // if were no attributes and no modifiers we should have parsed it already in namespace body:
                        Debug.Assert(modifiers.Count > 0);

                        modifiers[0] = this.AddError(modifiers[0], ErrorCode.ERR_BadModifiersOnNamespace);
                    }

                    var namespaceDecl = ParseNamespaceDeclaration();

                    if (modifiers.Count > 0)
                    {
                        namespaceDecl = AddLeadingSkippedSyntax(namespaceDecl, modifiers.ToListNode());
                    }

                    if (attributes.Count > 0)
                    {
                        namespaceDecl = AddLeadingSkippedSyntax(namespaceDecl, attributes.ToListNode());
                    }

                    return namespaceDecl;
                }

                // It's valid to have a type declaration here -- check for those
                if (CanStartTypeDeclaration(this.CurrentToken.Kind))
                {
                    return this.ParseTypeDeclaration(attributes, modifiers);
                }

                if (acceptStatement &&
                    this.CurrentToken.Kind != SyntaxKind.CloseBraceToken &&
                    this.CurrentToken.Kind != SyntaxKind.EndOfFileToken &&
                    this.IsPossibleStatement())
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
                // (possibly void).  Parse one.
                var type = this.ParseReturnType();

                // <UNDONE> UNDONE: should disallow non-methods with void type here</UNDONE>

                // Check for misplaced modifiers.  if we see any, then consider this member
                // terminated and restart parsing.
                if (GetModifier(this.CurrentToken) != SyntaxModifier.None &&
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

                    return _syntaxFactory.IncompleteMember(attributes, modifiers.ToTokenList(), type);
                }

                parse_member_name:;
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
                    if (attributes.Count == 0 && modifiers.Count == 0 && type.IsMissing)
                    {
                        // we haven't advanced, the caller needs to consume the tokens ahead
                        return null;
                    }

                    var incompleteMember = _syntaxFactory.IncompleteMember(attributes, modifiers.ToTokenList(), type.IsMissing ? null : type);
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
                if (identifierOrThisOpt != null &&
                    (typeParameterListOpt != null && typeParameterListOpt.ContainsDiagnostics
                      || this.CurrentToken.Kind != SyntaxKind.OpenParenToken && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken) &&
                    ReconsiderTypeAsAsyncModifier(ref modifiers, ref type, ref explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt))
                {
                    goto parse_member_name;
                }

                Debug.Assert(identifierOrThisOpt != null);

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
                _pool.Free(modifiers);
                _pool.Free(attributes);
                _termState = saveTermState;
            }
        }

        // if the modifiers do not contain async and the type is the identifier "async", then
        // add async to the modifiers and assign a new type from the identifierOrThisOpt and the
        // type parameter list
        private bool ReconsiderTypeAsAsyncModifier(
            ref SyntaxListBuilder modifiers,
            ref TypeSyntax type,
            ref ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt,
            SyntaxToken identifierOrThisOpt,
            TypeParameterListSyntax typeParameterListOpt)
        {
            if (modifiers.Any(SyntaxKind.AsyncKeyword)) return false;
            if (type.Kind != SyntaxKind.IdentifierName) return false;
            if ((((IdentifierNameSyntax)type).Identifier).ContextualKind != SyntaxKind.AsyncKeyword) return false;
            if (identifierOrThisOpt.Kind != SyntaxKind.IdentifierToken) return false;
            modifiers.Add(ConvertToKeyword(((IdentifierNameSyntax)type).Identifier));
            SimpleNameSyntax newType = typeParameterListOpt == null
                ? (SimpleNameSyntax)_syntaxFactory.IdentifierName(identifierOrThisOpt)
                : _syntaxFactory.GenericName(identifierOrThisOpt, TypeArgumentFromTypeParameters(typeParameterListOpt));
            type = (explicitInterfaceOpt == null)
                ? (TypeSyntax)newType
                : _syntaxFactory.QualifiedName(explicitInterfaceOpt.Name, explicitInterfaceOpt.DotToken, newType);
            explicitInterfaceOpt = null;
            identifierOrThisOpt = default(SyntaxToken);
            typeParameterListOpt = default(TypeParameterListSyntax);
            return true;
        }

        private TypeArgumentListSyntax TypeArgumentFromTypeParameters(TypeParameterListSyntax typeParameterList)
        {
            var types = _pool.AllocateSeparated<TypeSyntax>();
            foreach (var p in typeParameterList.Parameters.GetWithSeparators())
            {
                switch (p.Kind)
                {
                    case SyntaxKind.TypeParameter:
                        var typeParameter = (TypeParameterSyntax)p;
                        var typeArgument = _syntaxFactory.IdentifierName(typeParameter.Identifier);
                        // NOTE: reverse order of variance keyword and attributes list so they come out in the right order.
                        if (typeParameter.VarianceKeyword != null)
                        {
                            // This only happens in error scenarios, so don't bother to produce a diagnostic about
                            // having a variance keyword on a type argument.
                            typeArgument = AddLeadingSkippedSyntax(typeArgument, typeParameter.VarianceKeyword);
                        }
                        if (typeParameter.AttributeLists.Node != null)
                        {
                            // This only happens in error scenarios, so don't bother to produce a diagnostic about
                            // having an attribute on a type argument.
                            typeArgument = AddLeadingSkippedSyntax(typeArgument, typeParameter.AttributeLists.Node);
                        }
                        types.Add(typeArgument);
                        break;
                    case SyntaxKind.CommaToken:
                        types.AddSeparator((SyntaxToken)p);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(p.Kind);
                }
            }

            var result = _syntaxFactory.TypeArgumentList(typeParameterList.LessThanToken, types.ToList(), typeParameterList.GreaterThanToken);
            _pool.Free(types);
            return result;
        }

        //private bool ReconsiderTypeAsAsyncModifier(ref SyntaxListBuilder modifiers, ref type, ref identifierOrThisOpt, ref typeParameterListOpt))
        //        {
        //            goto parse_member_name;
        //        }

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
                case SyntaxKind.DotToken:                   // Foo.     explicit
                case SyntaxKind.ColonColonToken:            // Foo::    explicit
                case SyntaxKind.LessThanToken:            // Foo<     explicit or generic method
                case SyntaxKind.OpenBraceToken:        // Foo {    property
                case SyntaxKind.EqualsGreaterThanToken:     // Foo =>   property
                    return false;
                case SyntaxKind.OpenParenToken:             // Foo(     method
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

        private ConstructorDeclarationSyntax ParseConstructorDeclaration(string typeName, SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            var name = this.ParseIdentifierToken();
            Debug.Assert(name.ValueText == typeName);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;
            try
            {
                var paramList = this.ParseParenthesizedParameterList(allowThisKeyword: false, allowDefaults: true, allowAttributes: true);

                ConstructorInitializerSyntax initializer = null;
                if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                {
                    bool isStatic = modifiers != null && modifiers.Any(SyntaxKind.StaticKeyword);
                    initializer = this.ParseConstructorInitializer(name.ValueText, isStatic);
                }

                BlockSyntax body;
                SyntaxToken semicolon;
                this.ParseBodyOrSemicolon(out body, out semicolon);

                return _syntaxFactory.ConstructorDeclaration(attributes, modifiers.ToTokenList(), name, paramList, initializer, body, semicolon);
            }
            finally
            {
                _termState = saveTerm;
            }
        }

        private ConstructorInitializerSyntax ParseConstructorInitializer(string name, bool isStatic)
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

            if (isStatic)
            {
                // Static constructor can't have any base call
                token = this.AddError(token, ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, name);
            }

            return _syntaxFactory.ConstructorInitializer(kind, colon, token, argumentList);
        }

        private DestructorDeclarationSyntax ParseDestructorDeclaration(string typeName, SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.TildeToken);
            var tilde = this.EatToken(SyntaxKind.TildeToken);

            var name = this.ParseIdentifierToken();
            if (name.ValueText != typeName)
            {
                name = this.AddError(name, ErrorCode.ERR_BadDestructorName);
            }

            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            BlockSyntax body;
            SyntaxToken semicolon;
            this.ParseBodyOrSemicolon(out body, out semicolon);

            var parameterList = _syntaxFactory.ParameterList(openParen, default(SeparatedSyntaxList<ParameterSyntax>), closeParen);
            return _syntaxFactory.DestructorDeclaration(attributes, modifiers.ToTokenList(), tilde, name, parameterList, body, semicolon);
        }

        /// <summary>
        /// Parses any block or expression bodies that are present. Also parses
        /// the trailing semicolon if one is present.
        /// </summary>
        private void ParseBlockAndExpressionBodiesWithSemicolon(
            out BlockSyntax blockBody,
            out ArrowExpressionClauseSyntax expressionBody,
            out SyntaxToken semicolon)
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
                expressionBody = this.ParseArrowExpressionClause();
                expressionBody = CheckFeatureAvailability(expressionBody, MessageID.IDS_FeatureExpressionBodiedMethod);
            }

            semicolon = null;
            // Expression-bodies need semicolons and native behavior
            // expects a semicolon if there is no body
            if (expressionBody != null || blockBody == null)
            {
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            // Check for bad semicolon after block body
            else if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = this.EatTokenWithPrejudice(ErrorCode.ERR_UnexpectedSemicolon);
            }
        }

        private T CheckForBlockAndExpressionBody<T>(
            CSharpSyntaxNode block,
            CSharpSyntaxNode expression,
            T syntax)
            where T : CSharpSyntaxNode
        {
            if (block != null && expression != null)
            {
                ErrorCode code;
                if (syntax is BaseMethodDeclarationSyntax)
                {
                    code = ErrorCode.ERR_BlockBodyAndExpressionBody;
                }
                else
                {
                    Debug.Assert(syntax is BasePropertyDeclarationSyntax);
                    code = ErrorCode.ERR_AccessorListAndExpressionBody;
                }
                return AddError(syntax, code);
            }
            return syntax;
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
                // void Foo<T (
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

            if (IsPossibleTypeParameterConstraintClauseStart())
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

            var paramList = this.ParseParenthesizedParameterList(allowThisKeyword: true, allowDefaults: true, allowAttributes: true);

            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            try
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                    this.ParseTypeParameterConstraintClauses(typeParameterList != null, constraints);
                }
                else if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                {
                    // Use else if, rather than if, because if we see both a constructor initializer and a constraint clause, we're too lost to recover.
                    var colonToken = this.CurrentToken;
                    // Set isStatic to false because pretending we're in a static constructor will just result in more errors.
                    ConstructorInitializerSyntax initializer = this.ParseConstructorInitializer(identifier.ValueText, isStatic: false);
                    initializer = this.AddErrorToFirstToken(initializer, ErrorCode.ERR_UnexpectedToken, colonToken.Text);
                    paramList = AddTrailingSkippedSyntax(paramList, initializer);

                    // CONSIDER: Parsing an invalid constructor initializer could, conceivably, get us way
                    // off track.  If this becomes a problem, an alternative approach would be to generalize
                    // EatTokenWithPrejudice in such a way that we can just skip everything until we recognize
                    // our context again (perhaps an open brace).
                }

                // When a generic method overrides a generic method declared in a base
                // class, or is an explicit interface member implementation of a method in
                // a base interface, the method shall not specify any type-parameter-
                // constraints-clauses. In these cases, the type parameters of the method
                // inherit constraints from the method being overridden or implemented
                if (!constraints.IsNull && constraints.Count > 0 &&
                    ((explicitInterfaceOpt != null) || (modifiers != null && modifiers.Any(SyntaxKind.OverrideKeyword))))
                {
                    constraints[0] = this.AddErrorToFirstToken(constraints[0], ErrorCode.ERR_OverrideWithConstraints);
                }

                _termState = saveTerm;

                BlockSyntax blockBody;
                ArrowExpressionClauseSyntax expressionBody;
                SyntaxToken semicolon;

                // Method declarations cannot be nested or placed inside async lambdas, and so cannot occur in an
                // asynchronous context. Therefore the IsInAsync state of the parent scope is not saved and
                // restored, just assumed to be false and reset accordingly after parsing the method body.
                Debug.Assert(!IsInAsync);

                IsInAsync = modifiers.Any(SyntaxKind.AsyncKeyword);

                this.ParseBlockAndExpressionBodiesWithSemicolon(out blockBody, out expressionBody, out semicolon);

                IsInAsync = false;

                var decl = _syntaxFactory.MethodDeclaration(
                    attributes,
                    modifiers.ToTokenList(),
                    type,
                    explicitInterfaceOpt,
                    identifier,
                    typeParameterList,
                    paramList,
                    constraints,
                    blockBody,
                    expressionBody,
                    semicolon);

                return CheckForBlockAndExpressionBody(blockBody, expressionBody, decl);
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

            var type = this.ParseType(parentIsParameter: false);

            var paramList = this.ParseParenthesizedParameterList(allowThisKeyword: false, allowDefaults: true, allowAttributes: true);
            if (paramList.Parameters.Count != 1)
            {
                paramList = this.AddErrorToFirstToken(paramList, ErrorCode.ERR_OvlUnaryOperatorExpected);
            }

            BlockSyntax blockBody;
            ArrowExpressionClauseSyntax expressionBody;
            SyntaxToken semicolon;
            this.ParseBlockAndExpressionBodiesWithSemicolon(out blockBody, out expressionBody, out semicolon);

            var decl = _syntaxFactory.ConversionOperatorDeclaration(
                attributes,
                modifiers.ToTokenList(),
                style,
                opKeyword,
                type,
                paramList,
                blockBody,
                expressionBody,
                semicolon);

            return CheckForBlockAndExpressionBody(blockBody, expressionBody, decl);
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

            var paramList = this.ParseParenthesizedParameterList(allowThisKeyword: false, allowDefaults: true, allowAttributes: true);

            // ReportExtensionMethods(parameters, retval);
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

            //if the operator is invalid, then switch it to plus (which will work either way) so that
            //we can finish building the tree
            if (!(SyntaxFacts.IsOverloadableUnaryOperator(opKind) || SyntaxFacts.IsOverloadableBinaryOperator(opKind)))
            {
                opToken = ConvertToMissingWithTrailingTrivia(opToken, SyntaxKind.PlusToken);
            }

            var decl = _syntaxFactory.OperatorDeclaration(
                attributes,
                modifiers.ToTokenList(),
                type,
                opKeyword,
                opToken,
                paramList,
                blockBody,
                expressionBody,
                semicolon);

            return CheckForBlockAndExpressionBody(blockBody, expressionBody, decl);
        }

        private MemberDeclarationSyntax ParseIndexerDeclaration(
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
            // TODO: ReportExtensionMethods(parameters, retval);
            if (parameterList.Parameters.Count == 0)
            {
                parameterList = this.AddErrorToLastToken(parameterList, ErrorCode.ERR_IndexerNeedsParam);
            }

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

            var decl = _syntaxFactory.IndexerDeclaration(
                attributes,
                modifiers.ToTokenList(),
                type,
                explicitInterfaceOpt,
                thisKeyword,
                parameterList,
                accessorList,
                expressionBody,
                semicolon);

            return CheckForBlockAndExpressionBody(accessorList, expressionBody, decl);
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
                var value = this.ParseVariableInitializer(allowStackAlloc: false);
                initializer = _syntaxFactory.EqualsValueClause(equals, value);
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

            var decl = _syntaxFactory.PropertyDeclaration(
                attributes,
                modifiers.ToTokenList(),
                type,
                explicitInterfaceOpt,
                identifier,
                accessorList,
                expressionBody,
                initializer,
                semicolon);

            return CheckForBlockAndExpressionBody(accessorList, expressionBody, decl);
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
                    bool hasGetOrAdd = false;
                    bool hasSetOrRemove = false;

                    while (true)
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                        {
                            break;
                        }
                        else if (this.IsPossibleAccessor())
                        {
                            var acc = this.ParseAccessorDeclaration(isEvent, ref hasGetOrAdd, ref hasSetOrRemove);
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
            return _syntaxFactory.ArrowExpressionClause(arrowToken, this.ParseExpressionCore());
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
            if (IsPossibleModifier())
            {
                var peekIndex = 1;
                while (IsPossibleModifier(this.PeekToken(peekIndex)))
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
                }
            }

            return false;
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
            CSharpSyntaxNode trailingTrivia;
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
            CSharpSyntaxNode trailingTrivia;
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
            out CSharpSyntaxNode trailingTrivia)
        {
            if (list.Count == 0)
            {
                return SkipBadTokensWithExpectedKind(isNotExpectedFunction, abortFunction, expected, out trailingTrivia);
            }
            else
            {
                CSharpSyntaxNode lastItemTrailingTrivia;
                var action = SkipBadTokensWithExpectedKind(isNotExpectedFunction, abortFunction, expected, out lastItemTrailingTrivia);
                if (lastItemTrailingTrivia != null)
                {
                    list[list.Count - 1] = AddTrailingSkippedSyntax(list[list.Count - 1], lastItemTrailingTrivia);
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
            out CSharpSyntaxNode trailingTrivia) where TNode : CSharpSyntaxNode
        {
            if (list.Count == 0)
            {
                return SkipBadTokensWithErrorCode(isNotExpectedFunction, abortFunction, error, out trailingTrivia);
            }
            else
            {
                CSharpSyntaxNode lastItemTrailingTrivia;
                var action = SkipBadTokensWithErrorCode(isNotExpectedFunction, abortFunction, error, out lastItemTrailingTrivia);
                if (lastItemTrailingTrivia != null)
                {
                    list[list.Count - 1] = AddTrailingSkippedSyntax(list[list.Count - 1], lastItemTrailingTrivia);
                }
                trailingTrivia = null;
                return action;
            }
        }

        private PostSkipAction SkipBadTokensWithExpectedKind(
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            SyntaxKind expected,
            out CSharpSyntaxNode trailingTrivia)
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
            out CSharpSyntaxNode trailingTrivia)
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

        private AccessorDeclarationSyntax ParseAccessorDeclaration(
            bool isEvent,
            ref bool hasGetOrAdd,
            ref bool hasSetOrRemove)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseAccessorDeclaration(isEvent))
            {
                return (AccessorDeclarationSyntax)this.EatNode();
            }

            var accAttrs = _pool.Allocate<AttributeListSyntax>();
            var accMods = _pool.Allocate();
            try
            {
                this.ParseAttributeDeclarations(accAttrs);
                this.ParseModifiers(accMods);

                if (isEvent)
                {
                    if (accMods != null && accMods.Count > 0)
                    {
                        accMods[0] = this.AddError(accMods[0], ErrorCode.ERR_NoModifiersOnAccessor);
                    }
                }
                else
                {
                    if (accMods != null && accMods.Count > 0)
                    {
                        accMods[0] = CheckFeatureAvailability(accMods[0], MessageID.IDS_FeaturePropertyAccessorMods);
                    }
                }

                bool validAccName;
                SyntaxToken accessorName;
                SyntaxKind accessorKind;

                if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    accessorName = this.EatToken();

                    // Only convert the identifier to a keyword if it's a valid one.  Otherwise any
                    // other contextual keyword (like 'partial') will be converted into a keyword
                    // and will be invalid.
                    switch (accessorName.ContextualKind)
                    {
                        case SyntaxKind.GetKeyword:
                        case SyntaxKind.SetKeyword:
                        case SyntaxKind.AddKeyword:
                        case SyntaxKind.RemoveKeyword:
                            accessorName = ConvertToKeyword(accessorName);
                            break;
                    }

                    if (isEvent)
                    {
                        bool isAdd = IsNameAdd(accessorName);
                        bool isRemove = IsNameRemove(accessorName);
                        validAccName = isAdd || isRemove;
                        if (!validAccName)
                        {
                            accessorName = this.AddError(accessorName, ErrorCode.ERR_AddOrRemoveExpected);
                            accessorKind = SyntaxKind.UnknownAccessorDeclaration;
                        }
                        else
                        {
                            if ((isAdd && hasGetOrAdd) || (isRemove && hasSetOrRemove))
                            {
                                accessorName = this.AddError(accessorName, ErrorCode.ERR_DuplicateAccessor);
                            }

                            hasGetOrAdd |= isAdd;
                            hasSetOrRemove |= isRemove;
                            accessorKind = isRemove ? SyntaxKind.RemoveAccessorDeclaration : SyntaxKind.AddAccessorDeclaration;
                        }
                    }
                    else
                    {
                        // Regular property
                        bool isGet = IsNameGet(accessorName);
                        bool isSet = IsNameSet(accessorName);
                        validAccName = isGet || isSet;
                        if (!validAccName)
                        {
                            accessorName = this.AddError(accessorName, ErrorCode.ERR_GetOrSetExpected);
                            accessorKind = SyntaxKind.UnknownAccessorDeclaration;
                        }
                        else
                        {
                            if ((isGet && hasGetOrAdd) || (isSet && hasSetOrRemove))
                            {
                                accessorName = this.AddError(accessorName, ErrorCode.ERR_DuplicateAccessor);
                            }

                            hasGetOrAdd |= isGet;
                            hasSetOrRemove |= isSet;
                            accessorKind = isSet ? SyntaxKind.SetAccessorDeclaration : SyntaxKind.GetAccessorDeclaration;
                        }
                    }
                }
                else
                {
                    validAccName = false;
                    accessorName = SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);
                    accessorName = this.AddError(accessorName, isEvent ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected);
                    accessorKind = SyntaxKind.UnknownAccessorDeclaration;
                }

                BlockSyntax body = null;
                SyntaxToken semicolon = null;
                bool currentTokenIsSemicolon = this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || (validAccName && !currentTokenIsSemicolon && !IsTerminator()))
                {
                    body = this.ParseBlock(isMethodBody: true, isAccessorBody: true);
                }
                else if (currentTokenIsSemicolon || validAccName)
                {
                    semicolon = this.EatToken(SyntaxKind.SemicolonToken, ErrorCode.ERR_SemiOrLBraceExpected);

                    if (isEvent)
                    {
                        semicolon = this.AddError(semicolon, ErrorCode.ERR_AddRemoveMustHaveBody);
                    }
                }

                return _syntaxFactory.AccessorDeclaration(accessorKind, accAttrs, accMods.ToTokenList(), accessorName, body, semicolon);
            }
            finally
            {
                _pool.Free(accMods);
                _pool.Free(accAttrs);
            }
        }

        private bool CanReuseAccessorDeclaration(bool isEvent)
        {
            var parent = GetOldParent(this.CurrentNode);
            switch (this.CurrentNodeKind)
            {
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    if (isEvent && parent != null && parent.Kind() == SyntaxKind.EventDeclaration)
                    {
                        return true;
                    }

                    break;
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    if (!isEvent && parent != null && parent.Kind() == SyntaxKind.PropertyDeclaration)
                    {
                        return true;
                    }

                    break;
            }

            return false;
        }

        internal ParameterListSyntax ParseParenthesizedParameterList(bool allowThisKeyword, bool allowDefaults, bool allowAttributes)
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
                this.ParseParameterList(out open, parameters, out close, openKind, closeKind, allowThisKeyword, allowDefaults, allowAttributes);
                return _syntaxFactory.ParameterList(open, parameters, close);
            }
            finally
            {
                _pool.Free(parameters);
            }
        }

        internal BracketedParameterListSyntax ParseBracketedParameterList(bool allowDefaults = true)
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
                this.ParseParameterList(out open, parameters, out close, openKind, closeKind, allowThisKeyword: false, allowDefaults: allowDefaults, allowAttributes: true);
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
            SyntaxKind closeKind,
            bool allowThisKeyword,
            bool allowDefaults,
            bool allowAttributes)
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
                    int mustBeLastIndex = -1;
                    bool mustBeLastHadParams = false;
                    bool hasParams = false;
                    bool hasArgList = false;

                    if (this.IsPossibleParameter(allowThisKeyword) || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        // first parameter
                        attributes.Clear();
                        modifiers.Clear();
                        var parameter = this.ParseParameter(attributes, modifiers, allowThisKeyword, allowDefaults, allowAttributes);
                        nodes.Add(parameter);
                        hasParams = modifiers.Any(SyntaxKind.ParamsKeyword);
                        hasArgList = parameter.Identifier.Kind == SyntaxKind.ArgListKeyword;
                        bool mustBeLast = hasParams || hasArgList;
                        if (mustBeLast && mustBeLastIndex == -1)
                        {
                            mustBeLastIndex = nodes.Count - 1;
                            mustBeLastHadParams = hasParams;
                        }

                        // additional parameters
                        while (true)
                        {
                            if (this.CurrentToken.Kind == closeKind)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleParameter(allowThisKeyword))
                            {
                                nodes.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                attributes.Clear();
                                modifiers.Clear();
                                parameter = this.ParseParameter(attributes, modifiers, allowThisKeyword, allowDefaults, allowAttributes);
                                nodes.Add(parameter);
                                hasParams = modifiers.Any(SyntaxKind.ParamsKeyword);
                                hasArgList = parameter.Identifier.Kind == SyntaxKind.ArgListKeyword;
                                mustBeLast = hasParams || hasArgList;
                                if (mustBeLast && mustBeLastIndex == -1)
                                {
                                    mustBeLastIndex = nodes.Count - 1;
                                    mustBeLastHadParams = hasParams;
                                }

                                continue;
                            }
                            else if (this.SkipBadParameterListTokens(ref open, nodes, SyntaxKind.CommaToken, closeKind, allowThisKeyword) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (this.SkipBadParameterListTokens(ref open, nodes, SyntaxKind.IdentifierToken, closeKind, allowThisKeyword) == PostSkipAction.Continue)
                    {
                        goto tryAgain;
                    }

                    if (mustBeLastIndex >= 0 && mustBeLastIndex < nodes.Count - 1)
                    {
                        nodes[mustBeLastIndex] = this.AddError(nodes[mustBeLastIndex], mustBeLastHadParams ? ErrorCode.ERR_ParamsLast : ErrorCode.ERR_VarargsLast);
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

        private PostSkipAction SkipBadParameterListTokens(ref SyntaxToken open, SeparatedSyntaxListBuilder<ParameterSyntax> list, SyntaxKind expected, SyntaxKind closeKind, bool allowThisKeyword)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleParameter(allowThisKeyword),
                p => p.CurrentToken.Kind == closeKind || p.IsTerminator(),
                expected);
        }

        private bool IsPossibleParameter(bool allowThisKeyword)
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenBracketToken: // attribute
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.ParamsKeyword:
                case SyntaxKind.ArgListKeyword:
                    return true;
                case SyntaxKind.ThisKeyword:
                    return allowThisKeyword;
                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();

                default:
                    return IsPredefinedType(this.CurrentToken.Kind);
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
            SyntaxListBuilder modifiers,
            bool allowThisKeyword,
            bool allowDefaults,
            bool allowAttributes)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseParameter(this.CurrentNode as CSharp.Syntax.ParameterSyntax, attributes, modifiers))
            {
                return (ParameterSyntax)this.EatNode();
            }

            this.ParseAttributeDeclarations(attributes, allowAttributes);
            this.ParseParameterModifiers(modifiers, allowThisKeyword);

            var hasArgList = this.CurrentToken.Kind == SyntaxKind.ArgListKeyword;

            TypeSyntax type = null;
            if (!hasArgList)
            {
                type = this.ParseType(true);
            }
            else if (this.IsPossibleType())
            {
                type = this.ParseType(true);
                type = WithAdditionalDiagnostics(type, this.GetExpectedTokenError(SyntaxKind.CloseParenToken, SyntaxKind.IdentifierToken, 0, type.Width));
            }

            SyntaxToken name = null;
            if (!hasArgList)
            {
                name = this.ParseIdentifierToken();

                // When the user type "int foo[]", give them a useful error
                if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind == SyntaxKind.CloseBracketToken)
                {
                    var open = this.EatToken();
                    var close = this.EatToken();
                    open = this.AddError(open, ErrorCode.ERR_BadArraySyntax);
                    name = AddTrailingSkippedSyntax(name, SyntaxList.List(open, close));
                }
            }
            else if (this.IsPossibleName())
            {
                // Current token is an identifier token, we expected a CloseParenToken.
                // Get the expected token error for the missing token with correct diagnostic
                // span and then parse the identifier token.

                SyntaxDiagnosticInfo diag = this.GetExpectedTokenError(SyntaxKind.CloseParenToken, SyntaxKind.IdentifierToken);
                name = this.ParseIdentifierToken();
                name = WithAdditionalDiagnostics(name, diag);
            }
            else
            {
                // name is not optional on ParameterSyntax
                name = this.EatToken(SyntaxKind.ArgListKeyword);
            }

            EqualsValueClauseSyntax def = null;
            if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                var equals = this.EatToken(SyntaxKind.EqualsToken);
                var expr = this.ParseExpressionCore();
                def = _syntaxFactory.EqualsValueClause(equals, expr);

                if (!allowDefaults)
                {
                    def = this.AddError(def, equals, ErrorCode.ERR_DefaultValueNotAllowed);
                }
                else
                {
                    def = CheckFeatureAvailability(def, MessageID.IDS_FeatureOptionalParameter);
                }
            }

            return _syntaxFactory.Parameter(attributes, modifiers.ToTokenList(), type, name, def);
        }

        private static bool IsParameterModifier(SyntaxKind kind, bool allowThisKeyword)
        {
            return GetParamFlags(kind, allowThisKeyword) != ParamFlags.None;
        }

        [Flags]
        private enum ParamFlags
        {
            None = 0x00,
            This = 0x01,
            Ref = 0x02,
            Out = 0x04,
            Params = 0x08,
        }

        private static ParamFlags GetParamFlags(SyntaxKind kind, bool allowThisKeyword)
        {
            switch (kind)
            {
                case SyntaxKind.ThisKeyword:
                    // if (this.IsCSharp3Enabled)
                    return (allowThisKeyword ? ParamFlags.This : ParamFlags.None);

                // goto default;
                case SyntaxKind.RefKeyword:
                    return ParamFlags.Ref;
                case SyntaxKind.OutKeyword:
                    return ParamFlags.Out;
                case SyntaxKind.ParamsKeyword:
                    return ParamFlags.Params;
                default:
                    return ParamFlags.None;
            }
        }

        private void ParseParameterModifiers(SyntaxListBuilder modifiers, bool allowThisKeyword)
        {
            var flags = ParamFlags.None;

            while (IsParameterModifier(this.CurrentToken.Kind, allowThisKeyword))
            {
                var mod = this.EatToken();

                if (mod.Kind == SyntaxKind.ThisKeyword ||
                    mod.Kind == SyntaxKind.RefKeyword ||
                    mod.Kind == SyntaxKind.OutKeyword ||
                    mod.Kind == SyntaxKind.ParamsKeyword)
                {
                    if (mod.Kind == SyntaxKind.ThisKeyword)
                    {
                        mod = CheckFeatureAvailability(mod, MessageID.IDS_FeatureExtensionMethod);

                        if ((flags & ParamFlags.This) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_DupParamMod, SyntaxFacts.GetText(SyntaxKind.ThisKeyword));
                        }
                        else if ((flags & ParamFlags.Out) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_BadOutWithThis);
                        }
                        else if ((flags & ParamFlags.Ref) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_BadRefWithThis);
                        }
                        else if ((flags & ParamFlags.Params) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_BadParamModThis);
                        }
                        else
                        {
                            flags |= ParamFlags.This;
                        }
                    }
                    else if (mod.Kind == SyntaxKind.RefKeyword)
                    {
                        if ((flags & ParamFlags.Ref) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_DupParamMod, SyntaxFacts.GetText(SyntaxKind.RefKeyword));
                        }
                        else if ((flags & ParamFlags.This) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_BadRefWithThis);
                        }
                        else if ((flags & ParamFlags.Params) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_ParamsCantBeRefOut);
                        }
                        else if ((flags & ParamFlags.Out) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_MultiParamMod);
                        }
                        else
                        {
                            flags |= ParamFlags.Ref;
                        }
                    }
                    else if (mod.Kind == SyntaxKind.OutKeyword)
                    {
                        if ((flags & ParamFlags.Out) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_DupParamMod, SyntaxFacts.GetText(SyntaxKind.OutKeyword));
                        }
                        else if ((flags & ParamFlags.This) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_BadOutWithThis);
                        }
                        else if ((flags & ParamFlags.Params) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_ParamsCantBeRefOut);
                        }
                        else if ((flags & ParamFlags.Ref) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_MultiParamMod);
                        }
                        else
                        {
                            flags |= ParamFlags.Out;
                        }
                    }
                    else if (mod.Kind == SyntaxKind.ParamsKeyword)
                    {
                        if ((flags & ParamFlags.Params) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_DupParamMod, SyntaxFacts.GetText(SyntaxKind.ParamsKeyword));
                        }
                        else if ((flags & ParamFlags.This) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_BadParamModThis);
                        }
                        else if ((flags & (ParamFlags.Ref | ParamFlags.Out | ParamFlags.This)) != 0)
                        {
                            mod = this.AddError(mod, ErrorCode.ERR_MultiParamMod);
                        }
                        else
                        {
                            flags |= ParamFlags.Params;
                        }
                    }
                }

                modifiers.Add(mod);
            }
        }

        private MemberDeclarationSyntax ParseFixedSizeBufferDeclaration(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxKind parentKind)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.FixedKeyword);

            var fixedToken = this.EatToken();
            fixedToken = CheckFeatureAvailability(fixedToken, MessageID.IDS_FeatureFixedBuffer);
            modifiers.Add(fixedToken);

            var type = this.ParseType(parentIsParameter: false);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFieldDeclaration;
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseVariableDeclarators(type, VariableFlags.Fixed, variables, parentKind);

                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);


                return _syntaxFactory.FieldDeclaration(
                    attributes, modifiers.ToTokenList(),
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
            var type = this.ParseType(parentIsParameter: false);

            if (IsFieldDeclaration(isEvent: true))
            {
                return this.ParseEventFieldDeclaration(attributes, modifiers, eventToken, type, parentKind);
            }
            else
            {
                return this.ParseEventDeclarationWithAccessors(attributes, modifiers, eventToken, type);
            }
        }

        private MemberDeclarationSyntax ParseEventDeclarationWithAccessors(
            SyntaxListBuilder<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxToken eventToken,
            TypeSyntax type)
        {
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt;
            SyntaxToken identifierOrThisOpt;
            TypeParameterListSyntax typeParameterList;

            this.ParseMemberName(out explicitInterfaceOpt, out identifierOrThisOpt, out typeParameterList, isEvent: true);

            // If we got an explicitInterfaceOpt but not an identifier, then we're in the special
            // case for ERR_ExplicitEventFieldImpl (see ParseMemberName for details).
            if (explicitInterfaceOpt != null && identifierOrThisOpt == null)
            {
                Debug.Assert(typeParameterList == null, "Exit condition of ParseMemberName in this scenario");

                // No need for a diagnostic, ParseMemberName has already added one.
                var missingIdentifier = CreateMissingIdentifierToken();

                var missingAccessorList =
                    _syntaxFactory.AccessorList(
                        SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                        default(SyntaxList<AccessorDeclarationSyntax>),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));

                return _syntaxFactory.EventDeclaration(
                    attributes,
                    modifiers.ToTokenList(),
                    eventToken,
                    type,
                    explicitInterfaceOpt, //already has an appropriate error attached
                    missingIdentifier,
                    missingAccessorList);
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

            var accessorList = this.ParseAccessorList(isEvent: true);

            var decl = _syntaxFactory.EventDeclaration(
                attributes,
                modifiers.ToTokenList(),
                eventToken,
                type,
                explicitInterfaceOpt,
                identifier,
                accessorList);

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
                    modifiers.ToTokenList(),
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon);
            }
            finally
            {
                _termState = saveTerm;
                _pool.Free(variables);
            }
        }

        private MemberDeclarationSyntax ParseEventFieldDeclaration(
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
                    modifiers.ToTokenList(),
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

            ParseVariableDeclarators(type, flags, variables, variableDeclarationsExpected);
        }

        private void ParseVariableDeclarators(TypeSyntax type, VariableFlags flags, SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables, bool variableDeclarationsExpected)
        {
            variables.Add(this.ParseVariableDeclarator(type, flags, isFirst: true));

            while (true)
            {
                if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    break;
                }
                else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    variables.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    variables.Add(this.ParseVariableDeclarator(type, flags, isFirst: false));
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

        private VariableDeclaratorSyntax ParseVariableDeclarator(TypeSyntax parentType, VariableFlags flags, bool isFirst, bool isExpressionContext = false)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseVariableDeclarator(this.CurrentNode as CSharp.Syntax.VariableDeclaratorSyntax, flags, isFirst))
            {
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
                // A + B; // etc.
                //
                // C 
                // A ? B : D;
                var resetPoint = this.GetResetPoint();
                try
                {
                    var currentTokenKind = this.CurrentToken.Kind;
                    if (currentTokenKind == SyntaxKind.IdentifierToken && !parentType.IsMissing)
                    {
                        var isAfterNewLine = parentType.GetLastToken().TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia);
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
                                currentTokenKind == SyntaxKind.MinusGreaterThanToken ||
                                isNonEqualsBinaryToken)
                            {
                                var missingIdentifier = CreateMissingIdentifierToken();
                                missingIdentifier = this.AddError(missingIdentifier, offset, width, ErrorCode.ERR_IdentifierExpected);

                                return _syntaxFactory.VariableDeclarator(missingIdentifier, null, null);
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
                    var init = this.ParseVariableInitializer(isLocal && !isConst);
                    initializer = _syntaxFactory.EqualsValueClause(equals, init);
                    break;

                case SyntaxKind.OpenParenToken:
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
                    var specifier = this.ParseArrayRankSpecifier(isArrayCreation: false, expectSizes: flags == VariableFlags.Fixed, sawNonOmittedSize: out sawNonOmittedSize);
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

            return _syntaxFactory.VariableDeclarator(name, argumentList, initializer);
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

        private ExpressionSyntax ParseVariableInitializer(bool allowStackAlloc)
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.StackAllocKeyword:
                    StackAllocArrayCreationExpressionSyntax stackAllocExpr = this.ParseStackAllocExpression();
                    if (!allowStackAlloc)
                    {
                        // CONSIDER: this is what dev10 reports (assuming unsafe constructs are allowed at all),
                        // but we could add a more specific error code.
                        stackAllocExpr = this.AddErrorToFirstToken(stackAllocExpr, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(SyntaxKind.StackAllocKeyword));
                    }
                    return stackAllocExpr;
                case SyntaxKind.OpenBraceToken:
                    return this.ParseArrayInitializer();
                default:
                    return this.ParseElementInitializer();
            }
        }

        private bool IsPossibleVariableInitializer(bool allowStack)
        {
            return (allowStack && this.CurrentToken.Kind == SyntaxKind.StackAllocKeyword)
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                || this.IsPossibleExpression();
        }

        private FieldDeclarationSyntax ParseConstantFieldDeclaration(SyntaxListBuilder<AttributeListSyntax> attributes, SyntaxListBuilder modifiers, SyntaxKind parentKind)
        {
            var constToken = this.EatToken(SyntaxKind.ConstKeyword);
            modifiers.Add(constToken);

            var type = this.ParseType(false);

            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseVariableDeclarators(type, VariableFlags.Const, variables, parentKind);
                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.FieldDeclaration(
                    attributes,
                    modifiers.ToTokenList(),
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
            var typeParameters = this.ParseTypeParameterList(allowVariance: true);
            var parameterList = this.ParseParenthesizedParameterList(allowThisKeyword: false, allowDefaults: true, allowAttributes: true);
            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            try
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                    this.ParseTypeParameterConstraintClauses(typeParameters != null, constraints);
                }

                _termState = saveTerm;

                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.DelegateDeclaration(attributes, modifiers.ToTokenList(), delegateToken, type, name, typeParameters, parameterList, constraints, semicolon);
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
            var typeParameters = this.ParseTypeParameterList(allowVariance: true);

            if (typeParameters != null)
            {
                name = AddTrailingSkippedSyntax(name, typeParameters);
                name = this.AddError(name, ErrorCode.ERR_UnexpectedGenericName);
            }

            BaseListSyntax baseList = null;
            if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                var colon = this.EatToken(SyntaxKind.ColonToken);
                var type = this.ParseType(false);
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

            SyntaxToken semicolon = null;
            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = this.EatToken();
            }

            return _syntaxFactory.EnumDeclaration(
                attributes,
                modifiers.ToTokenList(),
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
                        value = this.CreateMissingIdentifierName(); //an identifier is a valid expression
                        value = this.AddErrorToFirstToken(value, ErrorCode.ERR_ConstantExpected);
                    }
                    else
                    {
                        value = this.ParseExpressionCore();
                    }

                    equalsValue = _syntaxFactory.EqualsValueClause(equals, value);
                }

                return _syntaxFactory.EnumMemberDeclaration(memberAttrs, memberName, equalsValue);
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
                    !IsCurrentTokenQueryKeywordInQuery())
                {
                    return true;
                }
            }

            return false;
        }

        private IdentifierNameSyntax ParseIdentifierName()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.IdentifierName)
            {
                if (!SyntaxFacts.IsContextualKeyword(((CSharp.Syntax.IdentifierNameSyntax)this.CurrentNode).Identifier.Kind()))
                {
                    return (IdentifierNameSyntax)this.EatNode();
                }
            }

            var tk = ParseIdentifierToken();
            return SyntaxFactory.IdentifierName(tk);
        }

        private SyntaxToken ParseIdentifierToken()
        {
            var ctk = this.CurrentToken.Kind;
            if (ctk == SyntaxKind.IdentifierToken)
            {
                // Error tolerance for IntelliSense. Consider the following case: [EditorBrowsable( partial class Foo {
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
                name = this.AddError(name, ErrorCode.ERR_IdentifierExpected);
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
                if (this.IsPartialType() || this.IsPartialMember())
                {
                    return true;
                }
            }

            return false;
        }

        private TypeParameterListSyntax ParseTypeParameterList(bool allowVariance)
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
                parameters.Add(this.ParseTypeParameter(allowVariance));

                // remaining parameter & commas
                while (true)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken || this.IsPossibleTypeParameterConstraintClauseStart())
                    {
                        break;
                    }
                    else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        parameters.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                        parameters.Add(this.ParseTypeParameter(allowVariance));
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

        private TypeParameterSyntax ParseTypeParameter(bool allowVariance)
        {
            if (this.IsPossibleTypeParameterConstraintClauseStart())
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
                if (this.CurrentToken.Kind == SyntaxKind.InKeyword || this.CurrentToken.Kind == SyntaxKind.OutKeyword)
                {
                    // Again, we always recognize the variance syntax, but give an error if
                    // it is not appropriate. 

                    varianceToken = this.EatToken();
                    varianceToken = CheckFeatureAvailability(varianceToken, MessageID.IDS_FeatureTypeVariance);

                    if (!allowVariance)
                    {
                        varianceToken = this.AddError(varianceToken, ErrorCode.ERR_IllegalVarianceSyntax);
                    }
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
                var kind = this.ScanTypeArgumentList((options & NameOptions.InExpression) != 0);
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

        private ScanTypeArgumentListKind ScanTypeArgumentList(bool inExpression)
        {
            if (this.CurrentToken.Kind == SyntaxKind.LessThanToken)
            {
                if (inExpression)
                {
                    // Scan for a type argument list. If we think it's a type argument list
                    // then assume it is unless we see specific tokens following it.
                    if (this.ScanPossibleTypeArgumentList())
                    {
                        var tokenID = this.CurrentToken.Kind;
                        if (tokenID != SyntaxKind.OpenParenToken &&
                            tokenID != SyntaxKind.CloseParenToken &&
                            tokenID != SyntaxKind.CloseBracketToken &&
                            tokenID != SyntaxKind.ColonToken &&
                            tokenID != SyntaxKind.SemicolonToken &&
                            tokenID != SyntaxKind.CommaToken &&
                            tokenID != SyntaxKind.DotToken &&
                            tokenID != SyntaxKind.QuestionToken &&
                            tokenID != SyntaxKind.EqualsEqualsToken &&
                            tokenID != SyntaxKind.ExclamationEqualsToken &&

                            // The preceding tokens are from 7.5.4.2 Grammar Ambiguities;
                            // the following tokens are not.
                            tokenID != SyntaxKind.AmpersandAmpersandToken &&
                            tokenID != SyntaxKind.BarBarToken &&
                            tokenID != SyntaxKind.CaretToken &&
                            tokenID != SyntaxKind.BarToken &&
                            tokenID != SyntaxKind.CloseBraceToken &&
                            tokenID != SyntaxKind.EndOfFileToken)
                        {
                            return ScanTypeArgumentListKind.PossibleTypeArgumentList;
                        }
                        else
                        {
                            return ScanTypeArgumentListKind.DefiniteTypeArgumentList;
                        }
                    }
                }
                else
                {
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;
                }
            }

            return ScanTypeArgumentListKind.NotTypeArgumentList;
        }

        private bool ScanPossibleTypeArgumentList()
        {
            SyntaxToken lastTokenOfList = null;
            return ScanPossibleTypeArgumentList(ref lastTokenOfList) != ScanTypeFlags.NotType;
        }

        private ScanTypeFlags ScanPossibleTypeArgumentList(ref SyntaxToken lastTokenOfList)
        {
            if (this.CurrentToken.Kind == SyntaxKind.LessThanToken)
            {
                ScanTypeFlags result = ScanTypeFlags.GenericTypeOrExpression;

                do
                {
                    lastTokenOfList = this.EatToken();

                    // We currently do not have the ability to scan attributes, so if this is an open square, we early out and assume it is an attribute
                    if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                    {
                        return result;
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
                if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken || this.IsPossibleTypeParameterConstraintClauseStart())
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
            if (this.IsPossibleTypeParameterConstraintClauseStart())
            {
                return this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected);
            }

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

                var result = this.ParseType(parentIsParameter: false);

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
                        typeParameterListOpt = this.ParseTypeParameterList(allowVariance: false);
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
                        typeParameterListOpt = this.ParseTypeParameterList(allowVariance: false);
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

                    if (isEvent && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken)
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
                        //   event EventDelegate Parent.E; //(or anything else where the next token isn't open brace
                        //
                        // To recover: rollback to before the name of the field was parsed (just the part after the last
                        // dot), insert a missing identifier for the field name, insert missing accessors, and then treat
                        // the event name that's actually there as the beginning of a new member. e.g.
                        //
                        //   event EventDelegate Parent./*Missing nodes here*/
                        //
                        //   E;
                        //
                        // Rationale: The identifier could be the name of a type at the beginning of an existing member
                        // declaration (above which someone has started to type an explicit event implementation).

                        explicitInterfaceOpt = _syntaxFactory.ExplicitInterfaceSpecifier(
                            explicitInterfaceName,
                            AddError(separator, ErrorCode.ERR_ExplicitEventFieldImpl));

                        Debug.Assert(beforeIdentifierPointSet);
                        Reset(ref beforeIdentifierPoint);

                        //clear fields that were populated after the reset point
                        identifierOrThisOpt = null;
                        typeParameterListOpt = null;
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
                // something like Foo.Bar::Blah. We've already made an error node for the
                // ::, so just pretend that they typed Foo.Bar.Blah and continue on.

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
            /// Definitely a type name: either a predefined type (int, string, etc.) or an array type name (ending with a bracket).
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
        }

        private bool IsPossibleType()
        {
            var tk = this.CurrentToken.Kind;
            return IsPredefinedType(tk) || this.IsTrueIdentifier();
        }

        private bool IsPossibleName()
        {
            return this.IsTrueIdentifier();
        }

        private ScanTypeFlags ScanType()
        {
            SyntaxToken lastTokenOfType;
            return ScanType(out lastTokenOfType);
        }

        private ScanTypeFlags ScanType(out SyntaxToken lastTokenOfType)
        {
            ScanTypeFlags result = this.ScanNonArrayType(out lastTokenOfType);

            if (result == ScanTypeFlags.NotType)
            {
                return result;
            }

            // Finally, check for array types and nullables.
            while (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
            {
                this.EatToken();
                if (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
                {
                    while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        this.EatToken();
                    }

                    if (this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
                    {
                        lastTokenOfType = null;
                        return ScanTypeFlags.NotType;
                    }
                }

                lastTokenOfType = this.EatToken();
                result = ScanTypeFlags.MustBeType;
            }

            return result;
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
                return this.ScanPossibleTypeArgumentList(ref lastTokenOfType);
            }
            else
            {
                return ScanTypeFlags.NonGenericTypeOrExpression;
            }
        }

        private ScanTypeFlags ScanNonArrayType()
        {
            SyntaxToken lastTokenOfType;
            return ScanNonArrayType(out lastTokenOfType);
        }

        private ScanTypeFlags ScanNonArrayType(out SyntaxToken lastTokenOfType)
        {
            ScanTypeFlags result;
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
            else
            {
                // Can't be a type!
                lastTokenOfType = null;
                return ScanTypeFlags.NotType;
            }

            if (this.CurrentToken.Kind == SyntaxKind.QuestionToken)
            {
                lastTokenOfType = this.EatToken();
                result = ScanTypeFlags.NullableType;
            }

            // Now check for pointer type(s)
            while (this.CurrentToken.Kind == SyntaxKind.AsteriskToken)
            {
                lastTokenOfType = this.EatToken();
                if (result == ScanTypeFlags.GenericTypeOrExpression || result == ScanTypeFlags.NonGenericTypeOrExpression)
                {
                    result = ScanTypeFlags.PointerOrMultiplication;
                }
                else if (result == ScanTypeFlags.GenericTypeOrMethod)
                {
                    result = ScanTypeFlags.MustBeType;
                }
            }

            return result;
        }

        private static bool IsPredefinedType(SyntaxKind keyword)
        {
            return SyntaxFacts.IsPredefinedType(keyword);
        }

        public TypeSyntax ParseTypeName()
        {
            return ParseType(parentIsParameter: false);
        }

        private TypeSyntax ParseTypeOrVoid()
        {
            if (this.CurrentToken.Kind == SyntaxKind.VoidKeyword && this.PeekToken(1).Kind != SyntaxKind.AsteriskToken)
            {
                // Must be 'void' type, so create such a type node and return it.
                return _syntaxFactory.PredefinedType(this.EatToken());
            }

            return this.ParseType(parentIsParameter: false);
        }

        private TypeSyntax ParseType(bool parentIsParameter)
        {
            return ParseTypeCore(parentIsParameter, isOrAs: false, expectSizes: false, isArrayCreation: false);
        }

        private bool IsTerm()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ArgListKeyword:
                case SyntaxKind.MakeRefKeyword:
                case SyntaxKind.RefTypeKeyword:
                case SyntaxKind.RefValueKeyword:
                case SyntaxKind.BaseKeyword:
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.DefaultKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.NullKeyword:
                case SyntaxKind.SizeOfKeyword:
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.TypeOfKeyword:
                case SyntaxKind.UncheckedKeyword:
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.StringKeyword:
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.EqualsGreaterThanToken:
                case SyntaxKind.InterpolatedStringToken:
                case SyntaxKind.InterpolatedStringStartToken:
                    return true;
                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();
                default:
                    return false;
            }
        }

        private TypeSyntax ParseTypeCore(
            bool parentIsParameter,
            bool isOrAs,
            bool expectSizes,
            bool isArrayCreation)
        {
            var type = this.ParseUnderlyingType(parentIsParameter);

            if (this.CurrentToken.Kind == SyntaxKind.QuestionToken)
            {
                var resetPoint = this.GetResetPoint();
                try
                {
                    var question = this.EatToken();

                    if (isOrAs && (IsTerm() || IsPredefinedType(this.CurrentToken.Kind) || SyntaxFacts.IsAnyUnaryExpression(this.CurrentToken.Kind)))
                    {
                        this.Reset(ref resetPoint);

                        Debug.Assert(type != null);
                        return type;
                    }

                    question = CheckFeatureAvailability(question, MessageID.IDS_FeatureNullable);
                    type = _syntaxFactory.NullableType(type, question);
                }
                finally
                {
                    this.Release(ref resetPoint);
                }
            }

            // Check for pointer types (only if pType is NOT an array type)
            type = this.ParsePointerTypeMods(type);

            // Now check for arrays.
            if (this.IsPossibleRankAndDimensionSpecifier())
            {
                var ranks = _pool.Allocate<ArrayRankSpecifierSyntax>();
                try
                {
                    while (this.IsPossibleRankAndDimensionSpecifier())
                    {
                        bool unused;
                        var rank = this.ParseArrayRankSpecifier(isArrayCreation, expectSizes, out unused);
                        ranks.Add(rank);
                        expectSizes = false;
                    }

                    type = _syntaxFactory.ArrayType(type, ranks);
                }
                finally
                {
                    _pool.Free(ranks);
                }
            }

            Debug.Assert(type != null);
            return type;
        }

        private bool IsPossibleRankAndDimensionSpecifier()
        {
            if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
            {
                // When specifying rank and dimension, only commas and close square
                // brackets are valid after an open square bracket. However, we accept
                // numbers as well as the user might (mistakenly) try to specify the
                // array size here. This way, when the parser actually consumes these
                // tokens it will be able to specify an appropriate error message.
                /*
                SyntaxKind k = this.PeekToken(1).Kind;
                if (k == SyntaxKind.Comma ||
                    k == SyntaxKind.CloseBracket ||
                    k == SyntaxKind.NumericLiteral)
                {
                    return true;
                }
                 */
                return true;
            }

            return false;
        }

        private ArrayRankSpecifierSyntax ParseArrayRankSpecifier(bool isArrayCreation, bool expectSizes, out bool sawNonOmittedSize)
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
                        if (!expectSizes)
                        {
                            size = this.AddError(size, isArrayCreation ? ErrorCode.ERR_InvalidArray : ErrorCode.ERR_ArraySizeInDeclaration);
                        }

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
                        if (list[i].Kind == SyntaxKind.OmittedArraySizeExpression)
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

        private PostSkipAction SkipBadArrayRankSpecifierTokens(ref SyntaxToken openBracket, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openBracket, list,
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBracketToken || p.IsTerminator(),
                expected);
        }

        private TypeSyntax ParseUnderlyingType(bool parentIsParameter)
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
            else if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                return this.ParseQualifiedName();
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
                ParseStatementCore,
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

                return ParsePossibleBadAwaitStatement();
            }
            finally
            {
                _recursionDepth--;
            }
        }

        private StatementSyntax ParsePossibleBadAwaitStatement()
        {
            ResetPoint resetPointBeforeStatement = this.GetResetPoint();
            StatementSyntax result = ParsePossibleBadAwaitStatement(ref resetPointBeforeStatement);
            this.Release(ref resetPointBeforeStatement);
            return result;
        }

        private StatementSyntax ParsePossibleBadAwaitStatement(ref ResetPoint resetPointBeforeStatement)
        {
            // Precondition: We have already attempted to parse the statement as a non-declaration and failed.
            //
            // That means that we are in one of the following cases:
            //
            // 1) This is a perfectly mundane and correct local declaration statement like "int x;"
            // 2) This is a perfectly mundane but erroneous local declaration statement, like "int X();"
            // 3) We are in the rare case of the code containing "await x;" and the intention is that
            //    "await" is the type of "x".  This only works in a non-async method.
            // 4) We have what would be a legal await statement, like "await X();", but we are not in
            //    an async method, so the parse failed. (Had we been in an async method then the parse
            //    attempt done by our caller would have succeeded.)
            // 5) The statement begins with "await" but is not a legal local declaration and not a legal
            //    await expression regardless of whether the method is marked as "async".

            bool beginsWithAwait = this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword;
            StatementSyntax result = ParseLocalDeclarationStatement();

            // Cases (1), (2) and (3):
            if (!beginsWithAwait || !result.ContainsDiagnostics)
            {
                return result;
            }

            // The statement begins with "await" and could not be parsed as a legal declaration statement.
            // We know from our precondition that it is not a legal "await X();" statement, though it is
            // possible that it was only not legal because we were not in an async context.

            Debug.Assert(!IsInAsync);

            // Let's see if we're in case (4). Pretend that we're in an async method and see if parsing
            // a non-declaration statement would have succeeded.

            this.Reset(ref resetPointBeforeStatement);
            IsInAsync = true;
            result = ParseStatementNoDeclaration(allowAnyExpression: false);
            IsInAsync = false;

            if (!result.ContainsDiagnostics)
            {
                // We are in case (4). We do not report that we have an "await" expression in a non-async
                // method at parse time; rather we do that in BindAwait(), during the initial round of
                // semantic analysis.
                return result;
            }

            // We are in case (5); we can't figure out what is going on here. Our best guess is that it is
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
                case SyntaxKind.ForEachKeyword:
                    return this.ParseForOrForEachStatement();
                case SyntaxKind.GotoKeyword:
                    return this.ParseGotoStatement();
                case SyntaxKind.IfKeyword:
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
                    return this.ParseUnsafeStatement();
                case SyntaxKind.UsingKeyword:
                    return this.ParseUsingStatement();
                case SyntaxKind.WhileKeyword:
                    return this.ParseWhileStatement();
                case SyntaxKind.OpenBraceToken:
                    return this.ParseBlock();
                case SyntaxKind.SemicolonToken:
                    return _syntaxFactory.EmptyStatement(this.EatToken());
                case SyntaxKind.IdentifierToken:
                    if (this.IsPossibleLabeledStatement())
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
                    else
                    {
                        goto default;
                    }

                default:
                    if (this.IsPossibleLocalDeclarationStatement(allowAnyExpression))
                    {
                        return null;
                    }
                    else
                    {
                        return this.ParseExpressionStatement();
                    }
            }
        }

        private bool IsPossibleLabeledStatement()
        {
            return this.PeekToken(1).Kind == SyntaxKind.ColonToken && this.IsTrueIdentifier();
        }

        private bool IsPossibleYieldStatement()
        {
            return this.CurrentToken.ContextualKind == SyntaxKind.YieldKeyword && (this.PeekToken(1).Kind == SyntaxKind.ReturnKeyword || this.PeekToken(1).Kind == SyntaxKind.BreakKeyword);
        }

        private bool IsPossibleLocalDeclarationStatement(bool allowAnyExpression)
        {
            // This method decides whether to parse a statement as a
            // declaration or as an expression statement. In the old
            // compiler it would simple call IsLocalDeclaration.

            var tk = this.CurrentToken.Kind;
            if ((SyntaxFacts.IsPredefinedType(tk) && this.PeekToken(1).Kind != SyntaxKind.DotToken) || IsDeclarationModifier(tk))
            {
                return true;
            }

            bool? typedIdentifier = IsPossibleTypedIdentifierStart(this.CurrentToken, this.PeekToken(1), allowThisKeyword: false);
            if (typedIdentifier != null)
            {
                return typedIdentifier.Value;
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                ScanTypeFlags st = this.ScanType();

                // We could always return true for st == AliasQualName in addition to MustBeType on the first line, however, we want it to return false in the case where
                // CurrentToken.Kind != SyntaxKind.Identifier so that error cases, like: A::N(), are not parsed as variable declarations and instead are parsed as A.N() where we can give
                // a better error message saying "did you meant to use a '.'?"
                if (st == ScanTypeFlags.MustBeType && this.CurrentToken.Kind != SyntaxKind.DotToken)
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
                this.ParseVariableInitializer(allowStackAlloc: false);
                _termState = saveTerm;
            }

            return this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private bool IsPossibleMethodDeclarationFollowingNullableType()
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;

            var paramList = this.ParseParenthesizedParameterList(allowThisKeyword: true, allowDefaults: true, allowAttributes: true);

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

            SyntaxModifier modifier = GetModifier(nextToken);
            if (modifier == SyntaxModifier.Partial)
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
            else if (modifier != SyntaxModifier.None)
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
        private static bool? IsPossibleTypedIdentifierStart(SyntaxToken current, SyntaxToken next, bool allowThisKeyword)
        {
            if (current.Kind == SyntaxKind.IdentifierToken)
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

                    case SyntaxKind.IdentifierToken:
                        return true;

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
            // This makes logical sense, but isn't actually required.
            Debug.Assert(!isAccessorBody || isMethodBody, "An accessor body is a method body.");

            // Check again for incremental re-use, since ParseBlock is called from a bunch of places
            // other than ParseStatementCore()
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.Block)
            {
                return (BlockSyntax)this.EatNode();
            }

            // There's a special error code for a missing token after an accessor keyword
            var openBrace = isAccessorBody && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken
                ? this.AddError(SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken), ErrorCode.ERR_SemiOrLBraceExpected)
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
                if (this.IsPossibleStatement())
                {
                    var statement = this.ParseStatementCore();
                    statements.Add(statement);
                }
                else
                {
                    CSharpSyntaxNode trailingTrivia;
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
            }

            _termState = saveTerm;
        }

        private bool IsPossibleStatementStartOrStop()
        {
            return this.CurrentToken.Kind == SyntaxKind.SemicolonToken
                || this.IsPossibleStatement();
        }

        private PostSkipAction SkipBadStatementListTokens(SyntaxListBuilder<StatementSyntax> statements, SyntaxKind expected, out CSharpSyntaxNode trailingTrivia)
        {
            return this.SkipBadListTokensWithExpectedKindHelper(
                statements,
                p => !p.IsPossibleStatement(),
                p => p.CurrentToken.Kind == SyntaxKind.CloseBraceToken || p.IsTerminator(),
                expected,
                out trailingTrivia
            );
        }

        private bool IsPossibleStatement()
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
                    return true;
                case SyntaxKind.IdentifierToken:
                    return IsTrueIdentifier();
                case SyntaxKind.CatchKeyword:
                case SyntaxKind.FinallyKeyword:
                    return !_isInTry;
                default:
                    return IsPredefinedType(tk)
                       || IsPossibleExpression();
            }
        }

        private FixedStatementSyntax ParseFixedStatement()
        {
            var @fixed = this.EatToken(SyntaxKind.FixedKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            TypeSyntax type;
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                var saveTerm = _termState;
                _termState |= TerminatorState.IsEndOfFixedStatement;
                this.ParseDeclaration(false, out type, variables);
                _termState = saveTerm;
                var decl = _syntaxFactory.VariableDeclaration(type, variables);
                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                StatementSyntax statement = this.ParseEmbeddedStatement(false);
                return _syntaxFactory.FixedStatement(@fixed, openParen, decl, closeParen, statement);
            }
            finally
            {
                _pool.Free(variables);
            }
        }

        private bool IsEndOfFixedStatement()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseParenToken
                || this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                || this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private StatementSyntax ParseEmbeddedStatement(bool complexCheck)
        {
            StatementSyntax statement;

            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken && (!complexCheck || this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken))
            {
                statement = this.ParseStatementCore();
                statement = this.AddError(statement, ErrorCode.WRN_PossibleMistakenNullStatement);
            }
            else
            {
                statement = this.ParseStatementCore();
            }

            // An "embedded" statement is simply a statement that is not a labelled
            // statement or a declaration statement.  Parse a normal statement and post-
            // check for the error case.
            if (statement != null && (statement.Kind == SyntaxKind.LabeledStatement || statement.Kind == SyntaxKind.LocalDeclarationStatement))
            {
                statement = this.AddError(statement, ErrorCode.ERR_BadEmbeddedStmt);
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
                bool hasCatchAll = false;

                if (this.CurrentToken.Kind == SyntaxKind.CatchKeyword)
                {
                    hasEnd = true;
                    catches = _pool.Allocate<CatchClauseSyntax>();
                    while (this.CurrentToken.Kind == SyntaxKind.CatchKeyword)
                    {
                        var clause = this.ParseCatchClause(hasCatchAll);
                        hasCatchAll |= clause.Declaration == null && clause.Filter == null;
                        catches.Add(clause);
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

        private CatchClauseSyntax ParseCatchClause(bool hasCatchAll)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.CatchKeyword);

            var @catch = this.EatToken();

            // Check for the error of catch clause following empty catch here.
            if (hasCatchAll)
            {
                @catch = this.AddError(@catch, ErrorCode.ERR_TooManyCatches);
            }

            CatchDeclarationSyntax decl = null;
            var saveTerm = _termState;

            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var openParen = this.EatToken();
                _termState |= TerminatorState.IsEndOfCatchClause;
                var type = this.ParseClassType();
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
                _termState |= TerminatorState.IsEndOfilterClause;
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

        private TypeSyntax ParseClassType()
        {
            var type = this.ParseType(false);
            switch (type.Kind)
            {
                case SyntaxKind.PredefinedType:
                    var kt = ((PredefinedTypeSyntax)type).Keyword.Kind;
                    if (kt != SyntaxKind.ObjectKeyword && kt != SyntaxKind.StringKeyword)
                    {
                        goto default;
                    }

                    break;
                default:
                    if (!SyntaxFacts.IsName(type.Kind))
                    {
                        type = this.AddError(type, ErrorCode.ERR_ClassTypeExpected);
                    }

                    break;
            }

            return type;
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
            var statement = this.ParseEmbeddedStatement(false);
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
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForKeyword || this.CurrentToken.Kind == SyntaxKind.ForEachKeyword);

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
                if (this.CurrentToken.Kind == SyntaxKind.ForKeyword)
                {
                    this.EatToken();
                    if (this.EatToken().Kind == SyntaxKind.OpenParenToken &&
                        this.ScanType() != ScanTypeFlags.NotType &&
                        this.EatToken().Kind == SyntaxKind.IdentifierToken &&
                        this.EatToken().Kind == SyntaxKind.InKeyword)
                    {
                        // Looks like a foreach statement.  Parse it that way instead
                        this.Reset(ref resetPoint);
                        return this.ParseForEachStatement();
                    }
                    else
                    {
                        // Normal for statement.
                        this.Reset(ref resetPoint);
                        return this.ParseForStatement();
                    }
                }
                else
                {
                    return this.ParseForEachStatement();
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

            var @for = this.EatToken(SyntaxKind.ForKeyword);
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
                ScanTypeFlags st;
                if (this.IsQueryExpression(mayBeVariableDeclaration: true, mayBeMemberDeclaration: false))
                {
                    st = ScanTypeFlags.NotType;
                }
                else
                {
                    st = this.ScanType();
                }

                VariableDeclarationSyntax decl = null;

                if (st != ScanTypeFlags.NotType && this.IsTrueIdentifier())
                {
                    this.Reset(ref resetPoint);
                    TypeSyntax type;
                    var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
                    this.ParseDeclaration(false, out type, variables);
                    decl = _syntaxFactory.VariableDeclaration(type, variables);
                    _pool.Free(variables);
                }
                else
                {
                    // Not a type followed by an identifier, so it must be an expression list.
                    this.Reset(ref resetPoint);
                    if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                    {
                        this.ParseForStatementExpressionList(ref openParen, initializers);
                    }
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
                var statement = ParseEmbeddedStatement(true);

                return _syntaxFactory.ForStatement(@for, openParen, decl, initializers, semi, condition, semi2, incrementors, closeParen, statement);
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

        private ForEachStatementSyntax ParseForEachStatement()
        {
            // Can be a 'for' keyword if the user typed: 'for (SomeType t in'
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForEachKeyword || this.CurrentToken.Kind == SyntaxKind.ForKeyword);

            // Syntax for foreach is:
            //  foreach ( <type> <identifier> in <expr> ) <embedded-statement>

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
            var type = this.ParseType(false);
            SyntaxToken name;
            if (this.CurrentToken.Kind == SyntaxKind.InKeyword)
            {
                name = this.ParseIdentifierToken();
                name = this.AddError(name, ErrorCode.ERR_BadForeachDecl);
            }
            else
            {
                name = this.ParseIdentifierToken();
            }

            var @in = this.EatToken(SyntaxKind.InKeyword, ErrorCode.ERR_InExpected);
            var expression = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement(true);

            return _syntaxFactory.ForEachStatement(@foreach, openParen, type, name, @in, expression, closeParen, statement);
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
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IfKeyword);
            var @if = this.EatToken(SyntaxKind.IfKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var condition = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement(false);
            ElseClauseSyntax @else = null;
            if (this.CurrentToken.Kind == SyntaxKind.ElseKeyword)
            {
                var elseToken = this.EatToken(SyntaxKind.ElseKeyword);
                var elseStatement = this.ParseEmbeddedStatement(false);
                @else = _syntaxFactory.ElseClause(elseToken, elseStatement);
            }

            return _syntaxFactory.IfStatement(@if, openParen, condition, closeParen, statement, @else);
        }

        private LockStatementSyntax ParseLockStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LockKeyword);
            var @lock = this.EatToken(SyntaxKind.LockKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expression = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement(false);
            return _syntaxFactory.LockStatement(@lock, openParen, expression, closeParen, statement);
        }

        private ReturnStatementSyntax ParseReturnStatement()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ReturnKeyword);
            var @return = this.EatToken(SyntaxKind.ReturnKeyword);
            ExpressionSyntax arg = null;
            if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
            {
                arg = this.ParseExpressionCore();
            }

            var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            return _syntaxFactory.ReturnStatement(@return, arg, semicolon);
        }

        private YieldStatementSyntax ParseYieldStatement()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.YieldKeyword);

            var yieldToken = ConvertToKeyword(this.EatToken());
            SyntaxToken returnOrBreak = null;
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
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expression = this.ParseExpressionCore();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
            {
                openBrace = this.AddError(openBrace, ErrorCode.WRN_EmptySwitch);
            }

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
                            expression = this.CreateMissingIdentifierName();
                            expression = this.AddError(expression, ErrorCode.ERR_ConstantExpected);
                        }
                        else
                        {
                            expression = this.ParseExpressionCore();
                        }
                        colon = this.EatToken(SyntaxKind.ColonToken);
                        label = _syntaxFactory.CaseSwitchLabel(specifier, expression, colon);
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

        private UsingStatementSyntax ParseUsingStatement()
        {
            var @using = this.EatToken(SyntaxKind.UsingKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            VariableDeclarationSyntax declaration = null;
            ExpressionSyntax expression = null;

            var resetPoint = this.GetResetPoint();
            ParseUsingExpression(ref declaration, ref expression, ref resetPoint);
            this.Release(ref resetPoint);

            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
            var statement = this.ParseEmbeddedStatement(false);

            return _syntaxFactory.UsingStatement(@using, openParen, declaration, expression, closeParen, statement);
        }

        private void ParseUsingExpression(ref VariableDeclarationSyntax declaration, ref ExpressionSyntax expression, ref ResetPoint resetPoint)
        {
            if (this.IsAwaitExpression())
            {
                expression = this.ParseExpressionCore();
                return;
            }

            TypeSyntax type;

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
                    SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables;

                    switch (this.PeekToken(1).Kind)
                    {
                        default:
                            this.Reset(ref resetPoint);
                            expression = this.ParseExpressionCore();
                            break;

                        case SyntaxKind.CommaToken:
                        case SyntaxKind.CloseParenToken:
                            this.Reset(ref resetPoint);
                            variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
                            this.ParseDeclaration(false, out type, variables);
                            declaration = _syntaxFactory.VariableDeclaration(type, variables.ToList());
                            _pool.Free(variables);
                            break;

                        case SyntaxKind.EqualsToken:
                            // Parse it as a decl. If the next token is a : and only one variable was parsed,
                            // convert the whole thing to ?: expression.
                            this.Reset(ref resetPoint);
                            variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
                            this.ParseDeclaration(false, out type, variables);

                            // We may have non-nullable types in error scenarios.
                            if (this.CurrentToken.Kind == SyntaxKind.ColonToken &&
                                type.Kind == SyntaxKind.NullableType &&
                                SyntaxFacts.IsName(((NullableTypeSyntax)type).ElementType.Kind) &&
                                variables.Count == 1)
                            {
                                // We have "name? id = expr :" so need to convert to a ?: expression.
                                this.Reset(ref resetPoint);
                                expression = this.ParseExpressionCore();
                            }
                            else
                            {
                                declaration = _syntaxFactory.VariableDeclaration(type, variables.ToList());
                            }

                            _pool.Free(variables);
                            break;
                    }
                }
            }
            else if (IsUsingStatementVariableDeclaration(st))
            {
                this.Reset(ref resetPoint);
                var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
                this.ParseDeclaration(false, out type, variables);
                declaration = _syntaxFactory.VariableDeclaration(type, variables);
                _pool.Free(variables);
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
            var statement = this.ParseEmbeddedStatement(true);
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
            var statement = this.ParseStatementCore();
            return _syntaxFactory.LabeledStatement(label, colon, statement);
        }

        private LocalDeclarationStatementSyntax ParseLocalDeclarationStatement()
        {
            TypeSyntax type;
            var mods = _pool.Allocate();
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                this.ParseDeclarationModifiers(mods);
                this.ParseDeclaration(mods.Any(SyntaxKind.ConstKeyword), out type, variables);
                var semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return _syntaxFactory.LocalDeclarationStatement(
                    mods.ToTokenList(),
                    _syntaxFactory.VariableDeclaration(type, variables),
                    semicolon);
            }
            finally
            {
                _pool.Free(variables);
                _pool.Free(mods);
            }
        }

        private void ParseDeclaration(bool isConst, out TypeSyntax type, SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables)
        {
            type = this.ParseType(false);

            VariableFlags flags = VariableFlags.Local;
            if (isConst)
            {
                flags |= VariableFlags.Const;
            }

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfDeclarationClause;
            this.ParseVariableDeclarators(type, flags, variables, variableDeclarationsExpected: true);
            _termState = saveTerm;
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
            while (IsDeclarationModifier(k = this.CurrentToken.Kind))
            {
                var mod = this.EatToken();
                if (k == SyntaxKind.StaticKeyword || k == SyntaxKind.ReadOnlyKeyword || k == SyntaxKind.VolatileKeyword)
                {
                    mod = this.AddError(mod, ErrorCode.ERR_BadMemberFlag, mod.Text);
                }
                else if (list.Any(mod.Kind))
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
            return this.ParseSubExpression(0);
        }

        private bool IsPossibleExpression()
        {
            var tk = this.CurrentToken.Kind;
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
                    return true;
                case SyntaxKind.IdentifierToken:
                    // Specifically allow the from contextual keyword, because it can always be the start of an
                    // expression (whether it is used as an identifier or a keyword).
                    return this.IsTrueIdentifier() || (this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword);
                default:
                    return IsExpectedPrefixUnaryOperator(tk)
                        || (IsPredefinedType(tk) && tk != SyntaxKind.VoidKeyword)
                        || SyntaxFacts.IsAnyUnaryExpression(tk)
                        || SyntaxFacts.IsBinaryExpression(tk)
                        || SyntaxFacts.IsAssignmentExpressionOperatorToken(tk);
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
                case SyntaxKind.LockKeyword:
                case SyntaxKind.ReturnKeyword:
                case SyntaxKind.SwitchKeyword:
                case SyntaxKind.ThrowKeyword:
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
                case SyntaxKind.CoalesceExpression:
                    return true;
                default:
                    return false;
            }
        }

        private static uint GetPrecedence(SyntaxKind op)
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
                    return 1;
                case SyntaxKind.CoalesceExpression:
                    return 2;
                case SyntaxKind.LogicalOrExpression:
                    return 3;
                case SyntaxKind.LogicalAndExpression:
                    return 4;
                case SyntaxKind.BitwiseOrExpression:
                    return 5;
                case SyntaxKind.ExclusiveOrExpression:
                    return 6;
                case SyntaxKind.BitwiseAndExpression:
                    return 7;
                case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
                    return 8;
                case SyntaxKind.LessThanExpression:
                case SyntaxKind.LessThanOrEqualExpression:
                case SyntaxKind.GreaterThanExpression:
                case SyntaxKind.GreaterThanOrEqualExpression:
                case SyntaxKind.IsExpression:
                case SyntaxKind.AsExpression:
                    return 9;
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                    return 10;
                case SyntaxKind.AddExpression:
                case SyntaxKind.SubtractExpression:
                    return 11;
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                    return 12;
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
                    return 13;
                case SyntaxKind.CastExpression:
                    return 14;
                case SyntaxKind.PointerIndirectionExpression:
                    return 15;
                case SyntaxKind.AddressOfExpression:
                    return 16;
                default:
                    return 0;
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

        private ExpressionSyntax ParseSubExpression(uint precedence)
        {
            _recursionDepth++;

            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

            var result = ParseSubExpressionCore(precedence);

            _recursionDepth--;
            return result;
        }


        private ExpressionSyntax ParseSubExpressionCore(uint precedence)
        {
            ExpressionSyntax leftOperand = null;
            uint newPrecedence = 0;
            SyntaxKind opKind = SyntaxKind.None;

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

            // No left operand, so we need to parse one -- possibly preceded by a
            // unary operator.
            if (IsExpectedPrefixUnaryOperator(tk))
            {
                opKind = SyntaxFacts.GetPrefixUnaryExpression(tk);
                newPrecedence = GetPrecedence(opKind);
                var opToken = this.EatToken();
                var operand = this.ParseSubExpression(newPrecedence);
                leftOperand = _syntaxFactory.PrefixUnaryExpression(opKind, opToken, operand);
            }
            else if (IsAwaitExpression())
            {
                opKind = SyntaxKind.AwaitExpression;
                newPrecedence = GetPrecedence(opKind);
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
            else
            {
                // Not a unary operator - get a primary expression.
                leftOperand = this.ParseTerm(precedence);
            }

            while (true)
            {
                // We either have a binary or assignment operator here, or we're finished.
                tk = this.CurrentToken.Kind;

                bool isAssignmentOperator = false;
                if (IsExpectedBinaryOperator(tk))
                {
                    opKind = SyntaxFacts.GetBinaryExpression(tk);
                }
                else if (IsExpectedAssignmentOperator(tk))
                {
                    opKind = SyntaxFacts.GetAssignmentExpression(tk);
                    isAssignmentOperator = true;
                }
                else
                {
                    break;
                }

                newPrecedence = GetPrecedence(opKind);

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
                var opToken = this.EatToken();
                if (doubleOp)
                {
                    // combine tokens into a single token
                    var opToken2 = this.EatToken();
                    var kind = opToken2.Kind == SyntaxKind.GreaterThanToken ? SyntaxKind.GreaterThanGreaterThanToken : SyntaxKind.GreaterThanGreaterThanEqualsToken;
                    opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), kind, opToken2.GetTrailingTrivia());
                }

                if (opKind == SyntaxKind.IsExpression || opKind == SyntaxKind.AsExpression)
                {
                    leftOperand = _syntaxFactory.BinaryExpression(opKind, leftOperand, opToken,
                        this.ParseTypeCore(parentIsParameter: false, isOrAs: true, expectSizes: false, isArrayCreation: false));
                }
                else
                {
                    var rightOperand = this.ParseSubExpression(newPrecedence);
                    if (isAssignmentOperator)
                    {
                        leftOperand = _syntaxFactory.AssignmentExpression(opKind, leftOperand, opToken, rightOperand);
                    }
                    else
                    {
                        leftOperand = _syntaxFactory.BinaryExpression(opKind, leftOperand, opToken, rightOperand);
                    }
                }
            }

            // From the language spec:
            //
            // conditional-expression:
            //  null-coalescing-expression
            //  null-coalescing-expression   ?   expression   :   expression
            //
            // Only take the ternary if we're at a precedence less than the null coalescing
            // expression.

            var nullCoalescingPrecedence = GetPrecedence(SyntaxKind.CoalesceExpression);
            if (tk == SyntaxKind.QuestionToken && precedence < nullCoalescingPrecedence)
            {
                var questionToken = this.EatToken();

                var colonLeft = this.ParseExpressionCore();
                var colon = this.EatToken(SyntaxKind.ColonToken);

                var colonRight = this.ParseExpressionCore();
                leftOperand = _syntaxFactory.ConditionalExpression(leftOperand, questionToken, colonLeft, colon, colonRight);
            }

            return leftOperand;
        }

        private ExpressionSyntax ParseTerm(uint precedence)
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
                        if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword && this.PeekToken(1).Kind == SyntaxKind.DelegateKeyword)
                        {
                            expr = this.ParseAnonymousMethodExpression();
                        }
                        else if (this.IsPossibleLambdaExpression(precedence))
                        {
                            expr = this.ParseLambdaExpression();
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
                    expr = _syntaxFactory.BaseExpression(this.EatToken());
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
                    expr = this.ParseCastOrParenExpressionOrLambda(precedence);
                    break;
                case SyntaxKind.NewKeyword:
                    expr = this.ParseNewExpression();
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

        private bool IsPossibleLambdaExpression(uint precedence)
        {
            if (precedence <= LambdaPrecedence && this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken)
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
                    default:
                        return expr;
                }
            }
        }

        private bool CanStartConsequenceExpression(SyntaxKind kind)
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

            SyntaxToken openToken, closeToken;
            SeparatedSyntaxList<ArgumentSyntax> arguments;
            ParseArgumentList(out openToken, out arguments, out closeToken, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken);

            return _syntaxFactory.ArgumentList(openToken, arguments, closeToken);
        }

        internal BracketedArgumentListSyntax ParseBracketedArgumentList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.BracketedArgumentList)
            {
                return (BracketedArgumentListSyntax)this.EatNode();
            }

            SyntaxToken openToken, closeToken;
            SeparatedSyntaxList<ArgumentSyntax> arguments;
            ParseArgumentList(out openToken, out arguments, out closeToken, SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken);

            return _syntaxFactory.BracketedArgumentList(openToken, arguments, closeToken);
        }

        private void ParseArgumentList(
            out SyntaxToken openToken,
            out SeparatedSyntaxList<ArgumentSyntax> arguments,
            out SyntaxToken closeToken,
            SyntaxKind openKind,
            SyntaxKind closeKind)
        {
            bool isIndexer = openKind == SyntaxKind.OpenBracketToken;
            var open = this.EatToken(openKind);
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
                            if (this.CurrentToken.Kind == closeKind || this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleArgumentExpression())
                            {
                                list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                list.Add(this.ParseArgumentExpression(isIndexer));
                                continue;
                            }
                            else if (this.SkipBadArgumentListTokens(ref open, list, SyntaxKind.CommaToken, closeKind) == PostSkipAction.Abort)
                            {
                                break;
                            }
                        }
                    }
                    else if (this.SkipBadArgumentListTokens(ref open, list, SyntaxKind.IdentifierToken, closeKind) == PostSkipAction.Continue)
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

                openToken = open;
                closeToken = this.EatToken(closeKind);
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
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                    return true;
                default:
                    return this.IsPossibleExpression();
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

            SyntaxToken refOrOutKeyword = null;
            if (this.CurrentToken.Kind == SyntaxKind.RefKeyword || this.CurrentToken.Kind == SyntaxKind.OutKeyword)
            {
                refOrOutKeyword = this.EatToken();
            }

            ExpressionSyntax expression;

            if (isIndexer && (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.CurrentToken.Kind == SyntaxKind.CloseBracketToken))
            {
                expression = this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_ValueExpected);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                expression = this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_MissingArgument);
            }
            else
            {
                expression = this.ParseSubExpression(0);
            }

            return _syntaxFactory.Argument(nameColon, refOrOutKeyword, expression);
        }

        private TypeOfExpressionSyntax ParseTypeOfExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var type = this.ParseTypeOrVoid();
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.TypeOfExpression(keyword, openParen, type, closeParen);
        }

        private DefaultExpressionSyntax ParseDefaultExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var type = this.ParseType(false);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            keyword = CheckFeatureAvailability(keyword, MessageID.IDS_FeatureDefault);

            return _syntaxFactory.DefaultExpression(keyword, openParen, type, closeParen);
        }

        private SizeOfExpressionSyntax ParseSizeOfExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var type = this.ParseType(false);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.SizeOfExpression(keyword, openParen, type, closeParen);
        }

        private MakeRefExpressionSyntax ParseMakeRefExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(0);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.MakeRefExpression(keyword, openParen, expr, closeParen);
        }

        private RefTypeExpressionSyntax ParseRefTypeExpression()
        {
            var keyword = this.EatToken();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(0);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.RefTypeExpression(keyword, openParen, expr, closeParen);
        }

        private CheckedExpressionSyntax ParseCheckedOrUncheckedExpression()
        {
            var checkedOrUnchecked = this.EatToken();
            Debug.Assert(checkedOrUnchecked.Kind == SyntaxKind.CheckedKeyword || checkedOrUnchecked.Kind == SyntaxKind.UncheckedKeyword);
            var kind = (checkedOrUnchecked.Kind == SyntaxKind.CheckedKeyword) ? SyntaxKind.CheckedExpression : SyntaxKind.UncheckedExpression;

            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(0);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.CheckedExpression(kind, checkedOrUnchecked, openParen, expr, closeParen);
        }

        private RefValueExpressionSyntax ParseRefValueExpression()
        {
            var @refvalue = this.EatToken(SyntaxKind.RefValueKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expr = this.ParseSubExpression(0);
            var comma = this.EatToken(SyntaxKind.CommaToken);
            var type = this.ParseType(false);
            var closeParen = this.EatToken(SyntaxKind.CloseParenToken);

            return _syntaxFactory.RefValueExpression(@refvalue, openParen, expr, comma, type, closeParen);
        }

        private bool ScanParenthesizedImplicitlyTypedLambda(uint precedence)
        {
            if (!(precedence <= LambdaPrecedence))
            {
                return false;
            }

            //  case 1:  ( x ,
            if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken
                && (!this.IsInQuery || !IsTokenQueryContextualKeyword(this.PeekToken(1)))
                && this.PeekToken(2).Kind == SyntaxKind.CommaToken)
            {
                return true;
            }

            //  case 2:  ( x ) =>
            if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken
                && (!this.IsInQuery || !IsTokenQueryContextualKeyword(this.PeekToken(1)))
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

        private bool ScanExplicitlyTypedLambda(uint precedence)
        {
            if (!(precedence <= LambdaPrecedence))
            {
                return false;
            }

            var resetPoint = this.GetResetPoint();
            try
            {
                // do we have the following:
                //   case 1: ( T x ,
                //   case 2: ( T x ) =>
                //   case 3: ( out T x,
                //   case 4: ( ref T x,
                //   case 5: ( out T x ) =>
                //   case 6: ( ref T x ) =>
                //
                // if so then parse it as a lambda

                // Advance past the open paren.
                this.EatToken();

                // Eat 'out' or 'ref' for cases [3, 6]
                if (this.CurrentToken.Kind == SyntaxKind.RefKeyword || this.CurrentToken.Kind == SyntaxKind.OutKeyword)
                {
                    this.EatToken();
                }

                // NOTE: if we see "out" or ref" and part of cases 3,4,5,6 followed by EOF, we'll parse as a lambda.
                if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken)
                {
                    return true;
                }

                // NOTE: advances CurrentToken
                if (this.ScanType() == ScanTypeFlags.NotType)
                {
                    return false;
                }

                if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken)
                {
                    return true;
                }

                if (!this.IsTrueIdentifier())
                {
                    return false;
                }

                switch (this.PeekToken(1).Kind)
                {
                    case SyntaxKind.EndOfFileToken:
                    case SyntaxKind.CommaToken:
                        return true;

                    case SyntaxKind.CloseParenToken:
                        switch (this.PeekToken(2).Kind)
                        {
                            case SyntaxKind.EndOfFileToken:
                            case SyntaxKind.EqualsGreaterThanToken:
                                return true;

                            default:
                                return false;
                        }
                    default:
                        return false;
                }
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
            }
        }

        private ExpressionSyntax ParseCastOrParenExpressionOrLambda(uint precedence)
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
                        var type = this.ParseType(false);
                        var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                        var expr = this.ParseSubExpression(GetPrecedence(SyntaxKind.CastExpression));
                        return _syntaxFactory.CastExpression(openParen, type, closeParen, expr);
                    }
                }

                this.Reset(ref resetPoint);
                if (this.ScanExplicitlyTypedLambda(precedence))
                {
                    return this.ParseLambdaExpression();
                }

                // Doesn't look like a cast, so parse this as a parenthesized expression.
                {
                    this.Reset(ref resetPoint);
                    var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                    var expression = this.ParseSubExpression(0);
                    var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                    return _syntaxFactory.ParenthesizedExpression(openParen, expression, closeParen);
                }
            }
            finally
            {
                this.Release(ref resetPoint);
            }
        }

        private bool ScanCast()
        {
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            this.EatToken();

            var type = this.ScanType();
            if (type == ScanTypeFlags.NotType)
            {
                return false;
            }

            if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
            {
                return false;
            }

            // If we have any of the following, we know it must be a cast:
            // 1) (Foo*)bar;
            // 2) (Foo?)bar;
            // 3) "(int)bar" or "(int[])bar"
            // 4) (G::Foo)bar
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
            return (type == ScanTypeFlags.GenericTypeOrMethod || type == ScanTypeFlags.GenericTypeOrExpression || type == ScanTypeFlags.NonGenericTypeOrExpression) && CanFollowCast(this.CurrentToken.Kind);
        }

        private bool ScanAsyncLambda(uint precedence)
        {
            // Adapted from CParser::ScanAsyncLambda

            // Precedence must not exceed that of lambdas
            if (precedence > LambdaPrecedence)
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
            bool isNamedAssignment = this.IsNamedAssignment();

            NameEqualsSyntax nameEquals = null;
            if (isNamedAssignment)
            {
                nameEquals = ParseNameEquals();
            }

            var expression = this.ParseExpressionCore();
            if (!isNamedAssignment && !IsAnonymousTypeMemberExpression(expression))
            {
                expression = this.AddError(expression, ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator);
            }

            return _syntaxFactory.AnonymousObjectMemberDeclarator(nameEquals, expression);
        }

        private bool IsAnonymousTypeMemberExpression(ExpressionSyntax expr)
        {
            while (true)
            {
                switch (expr.Kind)
                {
                    case SyntaxKind.QualifiedName:
                        expr = ((QualifiedNameSyntax)expr).Right;
                        continue;
                    case SyntaxKind.ConditionalAccessExpression:
                        expr = ((ConditionalAccessExpressionSyntax)expr).WhenNotNull;
                        if (expr.Kind == SyntaxKind.MemberBindingExpression)
                        {
                            return true;
                        }

                        continue;
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.SimpleMemberAccessExpression:
                        return true;
                    default:
                        return false;
                }
            }
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
            bool isPossibleArrayCreation = this.IsPossibleArrayCreationExpression();
            var type = this.ParseTypeCore(parentIsParameter: false, isOrAs: false, expectSizes: isPossibleArrayCreation, isArrayCreation: isPossibleArrayCreation);

            if (type.Kind == SyntaxKind.ArrayType)
            {
                // Check for an initializer.
                InitializerExpressionSyntax initializer = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    initializer = this.ParseArrayInitializer();
                }
                else if (type.Kind == SyntaxKind.ArrayType)
                {
                    var rankSpec = ((ArrayTypeSyntax)type).RankSpecifiers[0];
                    if (GetNumberOfNonOmittedArraySizes(rankSpec) == 0)
                    {
                        type = this.AddError(type, rankSpec, ErrorCode.ERR_MissingArraySize);
                    }
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
                        this.AddError(SyntaxFactory.MissingToken(SyntaxKind.OpenParenToken), ErrorCode.ERR_BadNewExpr),
                        default(SeparatedSyntaxList<ArgumentSyntax>),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken));
                }

                return _syntaxFactory.ObjectCreationExpression(@new, type, argumentList, initializer);
            }
        }

        private static int GetNumberOfNonOmittedArraySizes(ArrayRankSpecifierSyntax rankSpec)
        {
            int count = rankSpec.Sizes.Count;
            int result = 0;
            for (int i = 0; i < count; i++)
            {
                if (rankSpec.Sizes[i].Kind != SyntaxKind.OmittedArraySizeExpression)
                {
                    result++;
                }
            }
            return result;
        }

        private bool IsPossibleArrayCreationExpression()
        {
            // previous token should be NewKeyword

            var resetPoint = this.GetResetPoint();
            try
            {
                ScanTypeFlags isType = this.ScanNonArrayType();
                return isType != ScanTypeFlags.NotType && this.CurrentToken.Kind == SyntaxKind.OpenBracketToken;
            }
            finally
            {
                this.Reset(ref resetPoint);
                this.Release(ref resetPoint);
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
            ExpressionSyntax expression;
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                expression = this.ParseObjectOrCollectionInitializer();
            }
            else
            {
                expression = this.ParseExpressionCore();
            }

            var elementAccess = _syntaxFactory.ImplicitElementAccess(arguments);
            return _syntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, elementAccess, equal, expression);
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

        private ExpressionSyntax ParseElementInitializer()
        {
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                return this.ParseComplexElementInitializer();
            }
            else
            {
                return this.ParseExpressionCore();
            }
        }

        private bool IsImplicitlyTypedArray()
        {
            return this.CurrentToken.Kind == SyntaxKind.NewKeyword && this.PeekToken(1).Kind == SyntaxKind.OpenBracketToken;
        }

        private ImplicitArrayCreationExpressionSyntax ParseImplicitlyTypedArrayCreation()
        {
            var @new = this.EatToken(SyntaxKind.NewKeyword);
            @new = CheckFeatureAvailability(@new, MessageID.IDS_FeatureImplicitArray);
            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);

            var commas = _pool.Allocate();
            try
            {
                while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    commas.Add(this.EatToken());
                }

                var closeBracket = this.EatToken(SyntaxKind.CloseBracketToken);

                var initializer = this.ParseArrayInitializer();

                return _syntaxFactory.ImplicitArrayCreationExpression(@new, openBracket, commas.ToTokenList(), closeBracket, initializer);
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
                    if (this.IsPossibleVariableInitializer(false) || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        list.Add(this.ParseVariableInitializer(false));

                        while (true)
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                            {
                                break;
                            }
                            else if (this.IsPossibleVariableInitializer(false) || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                            {
                                list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));

                                // check for exit case after legal trailing comma
                                if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                                {
                                    break;
                                }
                                else if (!this.IsPossibleVariableInitializer(false))
                                {
                                    goto tryAgain;
                                }

                                list.Add(this.ParseVariableInitializer(false));
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
                p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleVariableInitializer(false),
                p => this.CurrentToken.Kind == SyntaxKind.CloseBraceToken || this.IsTerminator(),
                expected);
        }

        private StackAllocArrayCreationExpressionSyntax ParseStackAllocExpression()
        {
            var stackAlloc = this.EatToken(SyntaxKind.StackAllocKeyword);
            var elementType = this.ParseTypeCore(parentIsParameter: false, isOrAs: false, expectSizes: true, isArrayCreation: false);
            if (elementType.Kind != SyntaxKind.ArrayType)
            {
                elementType = this.AddError(elementType, ErrorCode.ERR_BadStackAllocExpr);
            }

            return _syntaxFactory.StackAllocArrayCreationExpression(stackAlloc, elementType);
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
                parameterList = this.ParseParenthesizedParameterList(allowThisKeyword: false, allowDefaults: false, allowAttributes: false);
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
                        default(SyntaxList<StatementSyntax>),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken)));
            }

            var body = this.ParseBlock();
            IsInAsync = parentScopeIsInAsync;
            return _syntaxFactory.AnonymousMethodExpression(asyncToken, @delegate, parameterList, body);
        }

        private const int LambdaPrecedence = 1;

        private ExpressionSyntax ParseLambdaExpression()
        {
            bool parentScopeIsInAsync = IsInAsync;
            SyntaxToken asyncToken = null;
            if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword && PeekToken(1).Kind != SyntaxKind.EqualsGreaterThanToken)
            {
                asyncToken = this.EatContextualToken(SyntaxKind.AsyncKeyword);
                asyncToken = CheckFeatureAvailability(asyncToken, MessageID.IDS_FeatureAsync);
                IsInAsync = true;
            }

            ExpressionSyntax result;
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var paramList = this.ParseLambdaParameterList();
                var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
                arrow = CheckFeatureAvailability(arrow, MessageID.IDS_FeatureLambda);
                CSharpSyntaxNode body;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    body = this.ParseBlock();
                }
                else
                {
                    body = this.ParseExpressionCore();
                }

                result = _syntaxFactory.ParenthesizedLambdaExpression(asyncToken, paramList, arrow, body);
            }
            else
            {
                var name = this.ParseIdentifierToken();
                var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
                arrow = CheckFeatureAvailability(arrow, MessageID.IDS_FeatureLambda);
                CSharpSyntaxNode body = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                {
                    body = this.ParseBlock();
                }
                else
                {
                    body = this.ParseExpressionCore();
                }

                result = _syntaxFactory.SimpleLambdaExpression(
                    asyncToken,
                    _syntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxList<SyntaxToken>), type: null, identifier: name, @default: null),
                    arrow,
                    body);
            }

            IsInAsync = parentScopeIsInAsync;
            return result;
        }

        private ParameterListSyntax ParseLambdaParameterList()
        {
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfParameterList;

            var nodes = _pool.AllocateSeparated<ParameterSyntax>();
            try
            {
                bool hasTypes = false;

                if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
                {
                tryAgain:
                    if (this.IsPossibleLambdaParameter() || this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    {
                        // first parameter
                        var parameter = this.ParseLambdaParameter(isFirst: true, hasTypes: ref hasTypes);
                        nodes.Add(parameter);

                        // additional parameters
                        while (true)
                        {
                            if (this.CurrentToken.Kind == SyntaxKind.CloseParenToken)
                            {
                                break;
                            }
                            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleLambdaParameter())
                            {
                                nodes.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                                parameter = this.ParseLambdaParameter(false, ref hasTypes);
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

        private ParameterSyntax ParseLambdaParameter(bool isFirst, ref bool hasTypes)
        {
            TypeSyntax paramType = null;
            SyntaxToken paramName = null;
            SyntaxToken refOrOutOrParams = null;

            // Params are actually illegal in a lambda, but we'll allow it for error recovery purposes and
            // give the "params unexpected" error at semantic analysis time.
            bool isRefOrOutOrParams = this.CurrentToken.Kind == SyntaxKind.RefKeyword || this.CurrentToken.Kind == SyntaxKind.OutKeyword || this.CurrentToken.Kind == SyntaxKind.ParamsKeyword;
            var pk = this.PeekToken(1).Kind;
            if (isRefOrOutOrParams
                || (pk != SyntaxKind.CommaToken && pk != SyntaxKind.CloseParenToken && (hasTypes || isFirst))
                || IsPredefinedType(this.CurrentToken.Kind))
            {
                if (isRefOrOutOrParams)
                {
                    refOrOutOrParams = this.EatToken();
                }

                paramType = this.ParseType(true);
            }

            paramName = this.ParseIdentifierToken();

            if (isFirst)
            {
                hasTypes = paramType != null;
            }
            else if (paramType != null && !hasTypes && !paramName.IsMissing)
            {
                paramType = this.AddError(paramType, ErrorCode.ERR_InconsistentLambdaParameterUsage);
            }
            else if (paramType == null && hasTypes && !paramName.IsMissing)
            {
                paramName = this.AddError(paramName, ErrorCode.ERR_InconsistentLambdaParameterUsage);
            }

            return _syntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), refOrOutOrParams, paramType, paramName, null);
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

        private QueryExpressionSyntax ParseQueryExpression(uint precedence)
        {
            this.EnterQuery();
            var fc = this.ParseFromClause();
            fc = CheckFeatureAvailability(fc, MessageID.IDS_FeatureQueryExpression);
            if (precedence > 1 && IsStrict)
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
                        selectOrGroupBy = this.AddError(_syntaxFactory.SelectClause(SyntaxFactory.MissingToken(SyntaxKind.SelectKeyword), this.CreateMissingIdentifierName()), ErrorCode.ERR_ExpectedSelectOrGroup);
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
                type = this.ParseType(false);
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
                type = this.ParseType(false);
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
            return new ResetPoint(base.GetResetPoint(), _termState, _isInTry, _syntaxFactoryContext.IsInAsync, _syntaxFactoryContext.QueryDepth);
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
