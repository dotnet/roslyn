// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal sealed partial class LanguageParser : SyntaxParser
    {
        // list pools - allocators for lists that are used to build sequences of nodes. The lists
        // can be reused (hence pooled) since the syntax factory methods don't keep references to
        // them

        private readonly SyntaxListPool _pool = new SyntaxListPool(); // Don't need to reset this.

        private readonly SyntaxFactoryContext _syntaxFactoryContext; // Fields are resettable.
        private readonly ContextAwareSyntax _syntaxFactory; // Has context, the fields of which are resettable.

        private int _recursionDepth;
        private TerminatorState _termState; // Resettable

        // NOTE: If you add new state, you should probably add it to ResetPoint as well.

        internal LanguageParser(
            Lexer lexer,
            CSharp.CSharpSyntaxNode? oldTree,
            IEnumerable<TextChangeRange>? changes,
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
            IsEndOfFunctionPointerParameterList = 1 << 23,
            IsEndOfFunctionPointerParameterListErrored = 1 << 24,
            IsEndOfFunctionPointerCallingConvention = 1 << 25,
            IsEndOfTypeSignature = 1 << 26,
            IsExpressionOrPatternInCaseLabelOfSwitchStatement = 1 << 27,
            IsPatternInSwitchExpressionArm = 1 << 28,
        }

        private const int LastTerminatorState = (int)TerminatorState.IsPatternInSwitchExpressionArm;

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
                    case TerminatorState.IsEndOfFunctionPointerParameterList when this.IsEndOfFunctionPointerParameterList(errored: false):
                    case TerminatorState.IsEndOfFunctionPointerParameterListErrored when this.IsEndOfFunctionPointerParameterList(errored: true):
                    case TerminatorState.IsEndOfFunctionPointerCallingConvention when this.IsEndOfFunctionPointerCallingConvention():
                    case TerminatorState.IsEndOfTypeSignature when this.IsEndOfTypeSignature():
                        return true;
                }
            }

            return false;
        }

        private static CSharp.CSharpSyntaxNode? GetOldParent(CSharp.CSharpSyntaxNode node)
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
                static @this => @this.ParseCompilationUnitCore(),
                static @this => SyntaxFactory.CompilationUnit(
                    new SyntaxList<ExternAliasDirectiveSyntax>(),
                    new SyntaxList<UsingDirectiveSyntax>(),
                    new SyntaxList<AttributeListSyntax>(),
                    new SyntaxList<MemberDeclarationSyntax>(),
                    SyntaxFactory.Token(SyntaxKind.EndOfFileToken)));
        }

        internal CompilationUnitSyntax ParseCompilationUnitCore()
        {
            SyntaxToken? tmp = null;
            SyntaxListBuilder? initialBadNodes = null;
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

        internal TNode ParseWithStackGuard<TNode>(Func<LanguageParser, TNode> parseFunc, Func<LanguageParser, TNode> createEmptyNodeFunc) where TNode : CSharpSyntaxNode
        {
            // If this value is non-zero then we are nesting calls to ParseWithStackGuard which should not be 
            // happening.  It's not a bug but it's inefficient and should be changed.
            Debug.Assert(_recursionDepth == 0);

            try
            {
                return parseFunc(this);
            }
            catch (InsufficientExecutionStackException)
            {
                return CreateForGlobalFailure(lexer.TextWindow.Position, createEmptyNodeFunc(this));
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

        private BaseNamespaceDeclarationSyntax ParseNamespaceDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxListBuilder modifiers)
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            var result = ParseNamespaceDeclarationCore(attributeLists, modifiers);
            _recursionDepth--;
            return result;
        }

        private BaseNamespaceDeclarationSyntax ParseNamespaceDeclarationCore(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.NamespaceKeyword);
            var namespaceToken = this.EatToken(SyntaxKind.NamespaceKeyword);

            if (IsScript)
            {
                namespaceToken = this.AddError(namespaceToken, ErrorCode.ERR_NamespaceNotAllowedInScript);
            }

            var name = this.ParseQualifiedName();

            SyntaxToken? openBrace = null;
            SyntaxToken? semicolon = null;

            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || IsPossibleNamespaceMemberDeclaration())
            {
                //either we see the brace we expect here or we see something that could come after a brace
                //so we insert a missing one
                openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            }
            else
            {
                //the next character is neither the brace we expect, nor a token that could follow the expected
                //brace so we assume it's a mistake and replace it with a missing brace 
                openBrace = this.ConvertToMissingWithTrailingTrivia(
                    this.EatTokenEvenWithIncorrectKind(SyntaxKind.OpenBraceToken), SyntaxKind.OpenBraceToken);
            }

            Debug.Assert(semicolon != null || openBrace != null);

            var body = new NamespaceBodyBuilder(_pool);
            try
            {
                if (openBrace == null)
                {
                    Debug.Assert(semicolon != null);

                    SyntaxListBuilder? initialBadNodes = null;
                    this.ParseNamespaceBody(ref semicolon, ref body, ref initialBadNodes, SyntaxKind.FileScopedNamespaceDeclaration);
                    Debug.Assert(initialBadNodes == null); // init bad nodes should have been attached to semicolon...

                    return _syntaxFactory.FileScopedNamespaceDeclaration(
                        attributeLists,
                        modifiers.ToList(),
                        namespaceToken,
                        name,
                        semicolon,
                        body.Externs,
                        body.Usings,
                        body.Members);
                }
                else
                {
                    SyntaxListBuilder? initialBadNodes = null;
                    this.ParseNamespaceBody(ref openBrace, ref body, ref initialBadNodes, SyntaxKind.NamespaceDeclaration);
                    Debug.Assert(initialBadNodes == null); // init bad nodes should have been attached to open brace...

                    return _syntaxFactory.NamespaceDeclaration(
                        attributeLists,
                        modifiers.ToList(),
                        namespaceToken,
                        name,
                        openBrace,
                        body.Externs,
                        body.Usings,
                        body.Members,
                        this.EatToken(SyntaxKind.CloseBraceToken),
                        this.TryEatToken(SyntaxKind.SemicolonToken));
                }
            }
            finally
            {
                body.Free(_pool);
            }
        }

        /// <summary>Are we possibly at the start of an attribute list, or at a modifier which is valid on a type, or on a keyword of a type declaration?</summary>
        private static bool IsPossibleStartOfTypeDeclaration(SyntaxKind kind)
        {
            return IsTypeModifierOrTypeKeyword(kind) || kind == SyntaxKind.OpenBracketToken;
        }

        /// <summary>Are we at a modifier which is valid on a type declaration or at a type keyword?</summary>
        private static bool IsTypeModifierOrTypeKeyword(SyntaxKind kind)
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
                    return true;
                default:
                    return false;
            }
        }

        private void AddSkippedNamespaceText(
            ref SyntaxToken? openBraceOrSemicolon,
            ref NamespaceBodyBuilder body,
            ref SyntaxListBuilder? initialBadNodes,
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
            else if (openBraceOrSemicolon != null)
            {
                openBraceOrSemicolon = AddTrailingSkippedSyntax(openBraceOrSemicolon, skippedSyntax);
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
            TypesAndNamespaces = 5,
            TopLevelStatementsAfterTypesAndNamespaces = 6,
        }

        private void ParseNamespaceBody(
            [NotNullIfNotNull(nameof(openBraceOrSemicolon))] ref SyntaxToken? openBraceOrSemicolon,
            ref NamespaceBodyBuilder body,
            ref SyntaxListBuilder? initialBadNodes,
            SyntaxKind parentKind)
        {
            ParseNamespaceBodyWorker(
                ref openBraceOrSemicolon, ref body, ref initialBadNodes, parentKind, out var sawMemberDeclarationOnlyValidWithinTypeDeclaration);

            // In the common case, we will not see errant type-only member declarations in the namespace itself.  In
            // that case, we have no extra work to do and can return immediately.
            //
            // If we do see errant type-only members (like a method/property/constructor/etc.), then see if they follow
            // some normal type declaration.  If so, it's likely there was a misplaced close curly that preemptively
            // ended the type declaration, and the member declaration was supposed to go in it instead.
            if (!sawMemberDeclarationOnlyValidWithinTypeDeclaration)
                return;

            // In a script file, it can be ok to have these members at the top level.  For example, a field is actually
            // ok to parse out at the top level as it will become a field on the script global object.
            if (IsScript && parentKind == SyntaxKind.CompilationUnit)
                return;

            var finalMembers = _pool.Allocate<MemberDeclarationSyntax>();

            // Do a single linear sweep, examining each type declaration we run into within the namespace.
            for (var currentBodyMemberIndex = 0; currentBodyMemberIndex < body.Members.Count;)
            {
                var currentMember = body.Members[currentBodyMemberIndex];

                // If we have a suitable type declaration that ended without problem (has a real close curly and no
                // trailing semicolon),  then see if there are any type-only members following it that should be moved
                // into it.
                if (currentMember is TypeDeclarationSyntax
                    {
                        SemicolonToken: null,
                        CloseBraceToken: { IsMissing: false, ContainsDiagnostics: false }
                    } currentTypeDeclaration)
                {
                    var siblingsToMoveIntoType = determineSiblingsToMoveIntoType(typeDeclarationIndex: currentBodyMemberIndex, body);
                    if (siblingsToMoveIntoType is (var firstSiblingToMoveInclusive, var lastSiblingToMoveExclusive))
                    {
                        // We found sibling type-only members.  Move them into the preceding type declaration.
                        var finalTypeDeclaration = moveSiblingMembersIntoPrecedingType(
                            currentTypeDeclaration, body, firstSiblingToMoveInclusive, lastSiblingToMoveExclusive);
                        finalMembers.Add(finalTypeDeclaration);

                        // We moved a sequence of type-only-members into the preceding type declaration.  We need to
                        // continue processing from the end of that sequence.
                        currentBodyMemberIndex = lastSiblingToMoveExclusive;
                        continue;
                    }
                }

                // Simple case.  A normal namespace member we don't need to do anything with.
                finalMembers.Add(currentMember);
                currentBodyMemberIndex++;
            }

            _pool.Free(body.Members);
            body.Members = finalMembers;

            return;

            (int firstSiblingToMoveInclusive, int lastSiblingToMoveExclusive)? determineSiblingsToMoveIntoType(
                int typeDeclarationIndex,
                in NamespaceBodyBuilder body)
            {
                var startInclusive = typeDeclarationIndex + 1;
                if (startInclusive < body.Members.Count &&
                    IsMemberDeclarationOnlyValidWithinTypeDeclaration(body.Members[startInclusive]))
                {
                    var endExclusive = startInclusive + 1;

                    while (endExclusive < body.Members.Count &&
                           IsMemberDeclarationOnlyValidWithinTypeDeclaration(body.Members[endExclusive]))
                    {
                        endExclusive++;
                    }

                    return (startInclusive, endExclusive);
                }

                return null;
            }

            TypeDeclarationSyntax moveSiblingMembersIntoPrecedingType(
                TypeDeclarationSyntax typeDeclaration,
                in NamespaceBodyBuilder body,
                int firstSiblingToMoveInclusive,
                int lastSiblingToMoveExclusive)
            {
                var finalTypeDeclarationMembers = _pool.Allocate<MemberDeclarationSyntax>();
                finalTypeDeclarationMembers.AddRange(typeDeclaration.Members);

                for (var memberToMoveIndex = firstSiblingToMoveInclusive; memberToMoveIndex < lastSiblingToMoveExclusive; memberToMoveIndex++)
                {
                    var currentSibling = body.Members[memberToMoveIndex];
                    if (memberToMoveIndex == firstSiblingToMoveInclusive)
                    {
                        // Move the existing close brace token to the first member as a skipped token, with a
                        // diagnostic saying that it was unexpected.
                        currentSibling = AddLeadingSkippedSyntax(
                            currentSibling,
                            AddError(typeDeclaration.CloseBraceToken, ErrorCode.ERR_InvalidMemberDecl, "}"));
                    }

                    finalTypeDeclarationMembers.Add(currentSibling);
                }

                var isLast = lastSiblingToMoveExclusive == body.Members.Count;

                // The existing close brace token is moved to the first member as a skipped token, with a diagnostic saying
                // it was unexpected.  The type decl will then get a missing close brace token if there are still members
                // following.  If not, we'll try to eat an actual close brace token.
                var finalCloseBraceToken = isLast
                    ? EatToken(SyntaxKind.CloseBraceToken)
                    : AddError(
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken), ErrorCode.ERR_RbraceExpected);
                var newMembers = _pool.ToListAndFree(finalTypeDeclarationMembers);

                return typeDeclaration.UpdateCore(
                    typeDeclaration.AttributeLists,
                    typeDeclaration.Modifiers,
                    typeDeclaration.Keyword,
                    typeDeclaration.Identifier,
                    typeDeclaration.TypeParameterList,
                    typeDeclaration.ParameterList,
                    typeDeclaration.BaseList,
                    typeDeclaration.ConstraintClauses,
                    typeDeclaration.OpenBraceToken,
                    newMembers,
                    finalCloseBraceToken,
                    typeDeclaration.SemicolonToken);
            }
        }

        private static bool IsMemberDeclarationOnlyValidWithinTypeDeclaration(MemberDeclarationSyntax? memberDeclaration)
        {
            return memberDeclaration?.Kind
                is SyntaxKind.ConstructorDeclaration
                or SyntaxKind.ConversionOperatorDeclaration
                or SyntaxKind.DestructorDeclaration
                or SyntaxKind.EventDeclaration
                or SyntaxKind.EventFieldDeclaration
                or SyntaxKind.FieldDeclaration
                or SyntaxKind.IndexerDeclaration
                or SyntaxKind.MethodDeclaration
                or SyntaxKind.OperatorDeclaration
                or SyntaxKind.PropertyDeclaration;
        }

        private void ParseNamespaceBodyWorker(
            [NotNullIfNotNull(nameof(openBraceOrSemicolon))] ref SyntaxToken? openBraceOrSemicolon,
            ref NamespaceBodyBuilder body,
            ref SyntaxListBuilder? initialBadNodes,
            SyntaxKind parentKind,
            out bool sawMemberDeclarationOnlyValidWithinTypeDeclaration)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            bool isGlobal = openBraceOrSemicolon == null;

            var saveTerm = _termState;
            _termState |= TerminatorState.IsNamespaceMemberStartOrStop;
            NamespaceParts seen = NamespaceParts.None;
            var pendingIncompleteMembers = _pool.Allocate<MemberDeclarationSyntax>();
            bool reportUnexpectedToken = true;

            sawMemberDeclarationOnlyValidWithinTypeDeclaration = false;

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

                            body.Members.Add(adjustStateAndReportStatementOutOfOrder(ref seen, this.ParseNamespaceDeclaration(attributeLists, modifiers)));

                            _pool.Free(attributeLists);
                            _pool.Free(modifiers);

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
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBraceOrSemicolon, ref body, ref initialBadNodes);

                                var token = this.EatToken();
                                token = this.AddError(token,
                                    IsScript ? ErrorCode.ERR_GlobalDefinitionOrStatementExpected : ErrorCode.ERR_EOFExpected);

                                this.AddSkippedNamespaceText(ref openBraceOrSemicolon, ref body, ref initialBadNodes, token);
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
                            if (isGlobal && !ScanExternAliasDirective())
                            {
                                // extern member or a local function
                                goto default;
                            }
                            else
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBraceOrSemicolon, ref body, ref initialBadNodes);

                                var @extern = ParseExternAliasDirective();
                                if (seen > NamespaceParts.ExternAliases)
                                {
                                    @extern = this.AddErrorToFirstToken(@extern, ErrorCode.ERR_ExternAfterElements);
                                    this.AddSkippedNamespaceText(ref openBraceOrSemicolon, ref body, ref initialBadNodes, @extern);
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
                            if (isGlobal && (this.PeekToken(1).Kind == SyntaxKind.OpenParenToken || (!IsScript && IsPossibleTopLevelUsingLocalDeclarationStatement())))
                            {
                                // Top-level using statement or using local declaration
                                goto default;
                            }
                            else
                            {
                                parseUsingDirective(ref openBraceOrSemicolon, ref body, ref initialBadNodes, ref seen, ref pendingIncompleteMembers);
                            }

                            reportUnexpectedToken = true;
                            break;

                        case SyntaxKind.IdentifierToken:
                            if (this.CurrentToken.ContextualKind != SyntaxKind.GlobalKeyword || this.PeekToken(1).Kind != SyntaxKind.UsingKeyword)
                            {
                                goto default;
                            }
                            else
                            {
                                parseUsingDirective(ref openBraceOrSemicolon, ref body, ref initialBadNodes, ref seen, ref pendingIncompleteMembers);
                            }

                            reportUnexpectedToken = true;
                            break;

                        case SyntaxKind.OpenBracketToken:
                            if (this.IsPossibleGlobalAttributeDeclaration())
                            {
                                // Could be an attribute, or it could be a collection expression at the top level.  e.g.
                                // `[assembly: 1].XYZ();`. While this is definitely odd code, it is totally legal (as
                                // `assembly` is just an identifier).
                                var attribute = this.TryParseAttributeDeclaration(inExpressionContext: parentKind == SyntaxKind.CompilationUnit);
                                if (attribute != null)
                                {
                                    // incomplete members must be processed before we add any nodes to the body:
                                    ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBraceOrSemicolon, ref body, ref initialBadNodes);

                                    if (!isGlobal || seen > NamespaceParts.GlobalAttributes)
                                    {
                                        RoslynDebug.Assert(attribute.Target != null, "Must have a target as IsPossibleGlobalAttributeDeclaration checks for that");

                                        attribute = attribute.Update(
                                            attribute.OpenBracketToken,
                                            attribute.Target.Update(
                                                this.AddError(attribute.Target.Identifier, ErrorCode.ERR_GlobalAttributesNotFirst),
                                                attribute.Target.ColonToken),
                                            attribute.Attributes,
                                            attribute.CloseBracketToken);

                                        this.AddSkippedNamespaceText(ref openBraceOrSemicolon, ref body, ref initialBadNodes, attribute);
                                    }
                                    else
                                    {
                                        body.Attributes.Add(attribute);
                                        seen = NamespaceParts.GlobalAttributes;
                                    }

                                    reportUnexpectedToken = true;
                                    break;
                                }
                            }

                            goto default;

                        default:
                            var memberOrStatement = isGlobal
                                ? this.ParseMemberDeclarationOrStatement(parentKind)
                                : this.ParseMemberDeclaration(parentKind);

                            sawMemberDeclarationOnlyValidWithinTypeDeclaration |= IsMemberDeclarationOnlyValidWithinTypeDeclaration(memberOrStatement);
                            if (memberOrStatement == null)
                            {
                                // incomplete members must be processed before we add any nodes to the body:
                                ReduceIncompleteMembers(ref pendingIncompleteMembers, ref openBraceOrSemicolon, ref body, ref initialBadNodes);

                                // eat one token and try to parse declaration or statement again:
                                var skippedToken = EatToken();
                                if (reportUnexpectedToken && !skippedToken.ContainsDiagnostics)
                                {
                                    skippedToken = this.AddError(skippedToken,
                                        IsScript ? ErrorCode.ERR_GlobalDefinitionOrStatementExpected : ErrorCode.ERR_EOFExpected);

                                    // do not report the error multiple times for subsequent tokens:
                                    reportUnexpectedToken = false;
                                }

                                this.AddSkippedNamespaceText(ref openBraceOrSemicolon, ref body, ref initialBadNodes, skippedToken);
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

                                body.Members.Add(adjustStateAndReportStatementOutOfOrder(ref seen, memberOrStatement));
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

            MemberDeclarationSyntax adjustStateAndReportStatementOutOfOrder(ref NamespaceParts seen, MemberDeclarationSyntax memberOrStatement)
            {
                switch (memberOrStatement.Kind)
                {
                    case SyntaxKind.GlobalStatement:
                        if (seen < NamespaceParts.MembersAndStatements)
                        {
                            seen = NamespaceParts.MembersAndStatements;
                        }
                        else if (seen == NamespaceParts.TypesAndNamespaces)
                        {
                            seen = NamespaceParts.TopLevelStatementsAfterTypesAndNamespaces;

                            if (!IsScript)
                            {
                                memberOrStatement = this.AddError(memberOrStatement, ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType);
                            }
                        }

                        break;

                    case SyntaxKind.NamespaceDeclaration:
                    case SyntaxKind.FileScopedNamespaceDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        if (seen < NamespaceParts.TypesAndNamespaces)
                        {
                            seen = NamespaceParts.TypesAndNamespaces;
                        }
                        break;

                    default:
                        if (seen < NamespaceParts.MembersAndStatements)
                        {
                            seen = NamespaceParts.MembersAndStatements;
                        }
                        break;
                }

                return memberOrStatement;
            }

            void parseUsingDirective(
                ref SyntaxToken? openBrace,
                ref NamespaceBodyBuilder body,
                ref SyntaxListBuilder? initialBadNodes,
                ref NamespaceParts seen,
                ref SyntaxListBuilder<MemberDeclarationSyntax> pendingIncompleteMembers)
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
        }

        private static void AddIncompleteMembers(ref SyntaxListBuilder<MemberDeclarationSyntax> incompleteMembers, ref NamespaceBodyBuilder body)
        {
            if (incompleteMembers.Count > 0)
            {
                body.Members.AddRange(incompleteMembers);
                incompleteMembers.Clear();
            }
        }

        private void ReduceIncompleteMembers(
            ref SyntaxListBuilder<MemberDeclarationSyntax> incompleteMembers,
            ref SyntaxToken? openBraceOrSemicolon,
            ref NamespaceBodyBuilder body,
            ref SyntaxListBuilder? initialBadNodes)
        {
            for (int i = 0; i < incompleteMembers.Count; i++)
                this.AddSkippedNamespaceText(ref openBraceOrSemicolon, ref body, ref initialBadNodes, incompleteMembers[i]);

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
            //   extern alias goo;
            //   extern alias goo();
            //   extern alias goo { get; }

            return this.CurrentToken.Kind == SyntaxKind.ExternKeyword
                && this.PeekToken(1) is { Kind: SyntaxKind.IdentifierToken, ContextualKind: SyntaxKind.AliasKeyword }
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

            return _syntaxFactory.ExternAliasDirective(
                this.EatToken(SyntaxKind.ExternKeyword),
                this.EatContextualToken(SyntaxKind.AliasKeyword),
                this.ParseIdentifierToken(),
                this.EatToken(SyntaxKind.SemicolonToken));
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

            var globalToken = this.CurrentToken.ContextualKind == SyntaxKind.GlobalKeyword
                ? ConvertToKeyword(this.EatToken())
                : null;

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.UsingKeyword);

            var usingToken = this.EatToken(SyntaxKind.UsingKeyword);
            var staticToken = this.TryEatToken(SyntaxKind.StaticKeyword);
            var unsafeToken = this.TryEatToken(SyntaxKind.UnsafeKeyword);

            // if the user wrote `using unsafe static` skip the `static` and tell them it needs to be `using static unsafe`.
            if (staticToken is null && unsafeToken != null && this.CurrentToken.Kind == SyntaxKind.StaticKeyword)
            {
                // create a missing 'static' token so that later binding does recognize what the user wanted.
                staticToken = SyntaxFactory.MissingToken(SyntaxKind.StaticKeyword);
                unsafeToken = AddTrailingSkippedSyntax(unsafeToken, AddError(this.EatToken(), ErrorCode.ERR_BadStaticAfterUnsafe));
            }

            var alias = this.IsNamedAssignment() ? ParseNameEquals() : null;

            TypeSyntax type;
            SyntaxToken semicolon;

            var isAliasToFunctionPointer = alias != null && this.CurrentToken.Kind == SyntaxKind.DelegateKeyword;
            if (!isAliasToFunctionPointer && IsPossibleNamespaceMemberDeclaration())
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

                type = _syntaxFactory.IdentifierName(CreateMissingToken(SyntaxKind.IdentifierToken, this.CurrentToken.Kind));
                semicolon = SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken);
            }
            else
            {
                // In the case where we don't have an alias, only parse out a name for this using-directive.  This is
                // worse for error recovery, but it means all code that consumes a using-directive can keep on assuming
                // it has a name when there is no alias.  Only code that specifically has to process aliases then has to
                // deal with getting arbitrary types back.
                type = alias == null ? this.ParseQualifiedName() : this.ParseType();

                // If we can see a semicolon ahead, then the current token was probably supposed to be an identifier
                if (type.IsMissing && this.PeekToken(1).Kind == SyntaxKind.SemicolonToken)
                    type = AddTrailingSkippedSyntax(type, this.EatToken());

                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }

            return _syntaxFactory.UsingDirective(globalToken, usingToken, staticToken, unsafeToken, alias, type, semicolon);
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
            // Have to at least start with `[` to be an attribute
            if (this.CurrentToken.Kind != SyntaxKind.OpenBracketToken)
                return false;

            using (this.GetDisposableResetPoint(resetOnDispose: true))
            {
                // Eat the `[`
                EatToken();

                // `[ id` could definitely begin an attribute.
                if (this.IsTrueIdentifier())
                    return true;

                // `[ word: ...` could definitely start an attribute.
                if (IsAttributeTarget())
                    return true;

                // If we see `[lit` (like `[0`) then this is def not an attribute, and should be parsed as a collection
                // expr.  Note: this heuristic can be added to in the future.
                if (SyntaxFacts.IsLiteralExpression(this.CurrentToken.Kind))
                    return false;

                return true;
            }
        }

        private SyntaxList<AttributeListSyntax> ParseAttributeDeclarations(bool inExpressionContext)
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsAttributeDeclarationTerminator;

            // An attribute can never appear *inside* an attribute argument (since a lambda expression cannot be used as
            // a constant argument value).  However, during parsing we can end up in a state where we're trying to
            // exactly that, through a path of Attribute->Argument->Expression->Attribute (since attributes can not be
            // on lambda expressions).
            //
            // Worse, when we are in a deeply ambiguous (or gibberish) scenario, where we see lots of code with `... [
            // ... [ ... ] ... ] ...`, we can get into exponential speculative parsing where we try `[ ... ]` both as an
            // attribute *and* a collection expression.
            //
            // Since we cannot ever legally have an attribute within an attribute, we bail out here immediately
            // syntactically.  This does mean we won't parse something like: `[X([Y]() => {})]` without errors, but that
            // is not semantically legal anyway.
            if (saveTerm == _termState)
                return default;

            var attributes = _pool.Allocate<AttributeListSyntax>();
            while (this.IsPossibleAttributeDeclaration())
            {
                var attributeDeclaration = this.TryParseAttributeDeclaration(inExpressionContext);
                if (attributeDeclaration is null)
                    break;

                attributes.Add(attributeDeclaration);
            }

            _termState = saveTerm;

            return _pool.ToListAndFree(attributes);
        }

        private bool IsAttributeDeclarationTerminator()
        {
            return this.CurrentToken.Kind == SyntaxKind.CloseBracketToken
                || this.IsPossibleAttributeDeclaration(); // start of a new one...
        }

        private bool IsAttributeTarget()
            => IsSomeWord(this.CurrentToken.Kind) && this.PeekToken(1).Kind == SyntaxKind.ColonToken;

        private AttributeListSyntax? TryParseAttributeDeclaration(bool inExpressionContext)
        {
            if (this.IsIncrementalAndFactoryContextMatches &&
                this.CurrentNodeKind == SyntaxKind.AttributeList &&
                !inExpressionContext)
            {
                return (AttributeListSyntax)this.EatNode();
            }

            // May have to reset if we discover this is not an attribute but is instead a collection expression.
            using var resetPoint = GetDisposableResetPoint(resetOnDispose: false);

            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);

            // Check for optional location :
            var location = IsAttributeTarget()
                ? _syntaxFactory.AttributeTargetSpecifier(ConvertToKeyword(this.EatToken()), this.EatToken(SyntaxKind.ColonToken))
                : null;

            var attributes = this.ParseCommaSeparatedSyntaxList(
                ref openBracket,
                SyntaxKind.CloseBracketToken,
                static @this => @this.IsPossibleAttribute(),
                static @this => @this.ParseAttribute(),
                skipBadAttributeListTokens,
                allowTrailingSeparator: true,
                requireOneElement: true,
                allowSemicolonAsSeparator: false);

            var closeBracket = this.EatToken(SyntaxKind.CloseBracketToken);
            if (inExpressionContext && shouldParseAsCollectionExpression())
            {
                // we're in an expression and we've seen `[A, B].`  This is actually the start of a collection expression
                // that someone is explicitly accessing a member off of.
                resetPoint.Reset();
                return null;
            }

            return _syntaxFactory.AttributeList(openBracket, location, attributes, closeBracket);

            bool shouldParseAsCollectionExpression()
            {
                // `[A, B].` is a member access off of a collection expression. 
                if (this.CurrentToken.Kind == SyntaxKind.DotToken)
                    return true;

                // `[A, B]->` is a member access off of a collection expression. Note: this will always be illegal
                // semantically (as a collection expression has the natural type List<> which is not a pointer type).  But
                // we leave that check to binding.
                if (this.CurrentToken.Kind == SyntaxKind.MinusGreaterThanToken)
                    return true;

                // `[A, B]?.`  The `?` is unnecessary (as a collection expression is always non-null), but is still
                // syntactically legal.
                if (this.CurrentToken.Kind == SyntaxKind.QuestionToken &&
                    this.PeekToken(1).Kind == SyntaxKind.DotToken)
                {
                    return true;
                }

                return false;
            }

            static PostSkipAction skipBadAttributeListTokens(
                LanguageParser @this, ref SyntaxToken openBracket, SeparatedSyntaxListBuilder<AttributeSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref openBracket, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleAttribute(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
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

            return _syntaxFactory.Attribute(
                this.ParseQualifiedName(),
                this.ParseAttributeArgumentList());
        }

        internal AttributeArgumentListSyntax? ParseAttributeArgumentList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.AttributeArgumentList)
            {
                return (AttributeArgumentListSyntax)this.EatNode();
            }

            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
                return null;

            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var argNodes = this.ParseCommaSeparatedSyntaxList(
                ref openParen,
                SyntaxKind.CloseParenToken,
                static @this => @this.IsPossibleAttributeArgument(),
                static @this => @this.ParseAttributeArgument(),
                immediatelyAbort,
                skipBadAttributeArgumentTokens,
                allowTrailingSeparator: false,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.AttributeArgumentList(
                openParen,
                argNodes,
                this.EatToken(SyntaxKind.CloseParenToken));

            static PostSkipAction skipBadAttributeArgumentTokens(
                LanguageParser @this, ref SyntaxToken openParen, SeparatedSyntaxListBuilder<AttributeArgumentSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref openParen, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleAttributeArgument(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }

            static bool immediatelyAbort(AttributeArgumentSyntax argument)
            {
                // We can be very thrown off by incomplete strings in an attribute argument (especially as the lexer
                // will restart on the next line with the contents of the string then being interpreted as more
                // arguments).  Bail out in this case to prevent going off the rails.
                if (argument.expression is LiteralExpressionSyntax { Kind: SyntaxKind.StringLiteralExpression, Token: var literalToken } &&
                    literalToken.GetDiagnostics().Contains(d => d.Code == (int)ErrorCode.ERR_NewlineInConst))
                {
                    return true;
                }

                if (argument.expression is InterpolatedStringExpressionSyntax { StringStartToken.Kind: SyntaxKind.InterpolatedStringStartToken, StringEndToken.IsMissing: true })
                    return true;

                return false;
            }
        }

        private bool IsPossibleAttributeArgument()
        {
            return this.IsPossibleExpression();
        }

        private AttributeArgumentSyntax ParseAttributeArgument()
        {
            // Need to parse both "real" named arguments and attribute-style named arguments.
            // We track attribute-style named arguments only with fShouldHaveName.

            NameEqualsSyntax? nameEquals = null;
            NameColonSyntax? nameColon = null;
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                switch (this.PeekToken(1).Kind)
                {
                    case SyntaxKind.EqualsToken:
                        nameEquals = _syntaxFactory.NameEquals(
                            _syntaxFactory.IdentifierName(this.ParseIdentifierToken()),
                            this.EatToken(SyntaxKind.EqualsToken));

                        break;
                    case SyntaxKind.ColonToken:
                        nameColon = _syntaxFactory.NameColon(
                            this.ParseIdentifierName(),
                            this.EatToken(SyntaxKind.ColonToken));

                        break;
                }
            }

            return _syntaxFactory.AttributeArgument(
                nameEquals, nameColon, this.ParseExpressionCore());
        }

        private static DeclarationModifiers GetModifierExcludingScoped(SyntaxToken token)
            => GetModifierExcludingScoped(token.Kind, token.ContextualKind);

        internal static DeclarationModifiers GetModifierExcludingScoped(SyntaxKind kind, SyntaxKind contextualKind)
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
                        case SyntaxKind.RequiredKeyword:
                            return DeclarationModifiers.Required;
                        case SyntaxKind.FileKeyword:
                            return DeclarationModifiers.File;
                    }

                    goto default;
                default:
                    return DeclarationModifiers.None;
            }
        }

        private void ParseModifiers(SyntaxListBuilder tokens, bool forAccessors, bool forTopLevelStatements, out bool isPossibleTypeDeclaration)
        {
            Debug.Assert(!(forAccessors && forTopLevelStatements));

            isPossibleTypeDeclaration = true;

            while (true)
            {
                var newMod = GetModifierExcludingScoped(this.CurrentToken);

                Debug.Assert(newMod != DeclarationModifiers.Scoped);
                if (newMod == DeclarationModifiers.None)
                {
                    if (!forAccessors)
                    {
                        SyntaxToken scopedKeyword = ParsePossibleScopedKeyword(isFunctionPointerParameter: false, isLambdaParameter: false);

                        if (scopedKeyword != null)
                        {
                            isPossibleTypeDeclaration = false;
                            tokens.Add(scopedKeyword);
                        }
                    }

                    break;
                }

                SyntaxToken modTok;
                switch (newMod)
                {
                    case DeclarationModifiers.Partial:
                        var nextToken = PeekToken(1);
                        if (this.IsPartialType() || this.IsPartialMember())
                        {
                            // Standard legal cases.
                            modTok = ConvertToKeyword(this.EatToken());
                        }
                        else if (nextToken.Kind == SyntaxKind.NamespaceKeyword)
                        {
                            // Error reported in binding
                            modTok = ConvertToKeyword(this.EatToken());
                        }
                        else if (
                            nextToken.Kind is SyntaxKind.EnumKeyword or SyntaxKind.DelegateKeyword ||
                            (IsPossibleStartOfTypeDeclaration(nextToken.Kind) && GetModifierExcludingScoped(nextToken) != DeclarationModifiers.None))
                        {
                            // Error reported in ModifierUtils.
                            modTok = ConvertToKeyword(this.EatToken());
                        }
                        else
                        {
                            return;
                        }

                        break;

                    case DeclarationModifiers.Ref:
                        // 'ref' is only a modifier if used on a ref struct
                        // it must be either immediately before the 'struct'
                        // keyword, or immediately before 'partial struct' if
                        // this is a partial ref struct declaration
                        {
                            var next = PeekToken(1);
                            if (isStructOrRecordKeyword(next) ||
                                (next.ContextualKind == SyntaxKind.PartialKeyword &&
                                 isStructOrRecordKeyword(PeekToken(2))))
                            {
                                modTok = this.EatToken();
                            }
                            else if (forAccessors && this.IsPossibleAccessorModifier())
                            {
                                // Accept ref as a modifier for properties and event accessors, to produce an error later during binding.
                                modTok = this.EatToken();
                            }
                            else
                            {
                                return;
                            }
                            break;
                        }

                    case DeclarationModifiers.File:
                        if ((!IsFeatureEnabled(MessageID.IDS_FeatureFileTypes) || forTopLevelStatements) && !ShouldContextualKeywordBeTreatedAsModifier(parsingStatementNotDeclaration: false))
                        {
                            return;
                        }

                        // LangVersion errors for 'file' modifier are given during binding.
                        modTok = ConvertToKeyword(EatToken());
                        break;

                    case DeclarationModifiers.Async:
                        if (!ShouldContextualKeywordBeTreatedAsModifier(parsingStatementNotDeclaration: false))
                        {
                            return;
                        }

                        modTok = ConvertToKeyword(this.EatToken());
                        break;

                    case DeclarationModifiers.Required:
                        // In C# 11, required in a modifier position is always a keyword if not escaped. Otherwise, we reuse the async detection
                        // machinery to make a conservative guess as to whether the user meant required to be a keyword, so that they get a good langver
                        // diagnostic and all the machinery to upgrade their project kicks in. The only exception to this rule is top level statements,
                        // where the user could conceivably have a local named required. For these locations, we need to disambiguate as well.
                        if ((!IsFeatureEnabled(MessageID.IDS_FeatureRequiredMembers) || forTopLevelStatements) && !ShouldContextualKeywordBeTreatedAsModifier(parsingStatementNotDeclaration: false))
                        {
                            return;
                        }

                        modTok = ConvertToKeyword(this.EatToken());

                        break;

                    default:
                        modTok = this.EatToken();
                        break;
                }

                Debug.Assert(modTok.Kind is not (SyntaxKind.OutKeyword or SyntaxKind.InKeyword));
                tokens.Add(modTok);
            }

            bool isStructOrRecordKeyword(SyntaxToken token)
            {
                if (token.Kind == SyntaxKind.StructKeyword)
                {
                    return true;
                }

                if (token.ContextualKind == SyntaxKind.RecordKeyword)
                {
                    // This is an unusual use of LangVersion. Normally we only produce errors when the langversion
                    // does not support a feature, but in this case we are effectively making a language breaking
                    // change to consider "record" a type declaration in all ambiguous cases. To avoid breaking
                    // older code that is not using C# 9 we conditionally parse based on langversion
                    return IsFeatureEnabled(MessageID.IDS_FeatureRecords);
                }

                return false;
            }
        }

        private bool ShouldContextualKeywordBeTreatedAsModifier(bool parsingStatementNotDeclaration)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IdentifierToken && GetModifierExcludingScoped(this.CurrentToken) != DeclarationModifiers.None);

            // Adapted from CParser::IsAsyncMethod.

            if (IsNonContextualModifier(PeekToken(1)))
            {
                // If the next token is a (non-contextual) modifier keyword, then this token is
                // definitely a modifier
                return true;
            }

            // Some of our helpers start at the current token, so we'll have to advance for their
            // sake and then backtrack when we're done.  Don't leave this block without releasing
            // the reset point.
            using var _ = GetDisposableResetPoint(resetOnDispose: true);

            this.EatToken(); //move past contextual token

            if (!parsingStatementNotDeclaration &&
                (this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword))
            {
                this.EatToken(); // "partial" doesn't affect our decision, so look past it.
            }

            // ... 'TOKEN' [partial] <typedecl> ...
            // ... 'TOKEN' [partial] <event> ...
            // ... 'TOKEN' [partial] <implicit> <operator> ...
            // ... 'TOKEN' [partial] <explicit> <operator> ...
            // ... 'TOKEN' [partial] <typename> <operator> ...
            // ... 'TOKEN' [partial] <typename> <membername> ...
            // DEVNOTE: Although we parse async user defined conversions, operators, etc. here,
            // anything other than async methods are detected as erroneous later, during the define phase
            // Generally wherever we refer to 'async' here, it can also be 'required' or 'file'.

            if (!parsingStatementNotDeclaration)
            {
                var currentTokenKind = this.CurrentToken.Kind;
                if (IsTypeModifierOrTypeKeyword(currentTokenKind) ||
                    currentTokenKind == SyntaxKind.EventKeyword ||
                    (currentTokenKind is SyntaxKind.ExplicitKeyword or SyntaxKind.ImplicitKeyword && PeekToken(1).Kind == SyntaxKind.OperatorKeyword))
                {
                    return true;
                }
            }

            if (ScanType() != ScanTypeFlags.NotType)
            {
                // We've seen "TOKEN TypeName".  Now we have to determine if we should we treat 
                // 'TOKEN' as a modifier.  Or is the user actually writing something like 
                // "public TOKEN Goo" where 'TOKEN' is actually the return type.

                if (IsPossibleMemberName())
                {
                    // we have: "TOKEN Type X" or "TOKEN Type this", 'TOKEN' is definitely a 
                    // modifier here.
                    return true;
                }

                var currentTokenKind = this.CurrentToken.Kind;

                // The file ends with "TOKEN TypeName", it's not legal code, and it's much 
                // more likely that this is meant to be a modifier.
                if (currentTokenKind == SyntaxKind.EndOfFileToken)
                {
                    return true;
                }

                // "TOKEN TypeName }".  In this case, we just have an incomplete member, and 
                // we should definitely default to 'TOKEN' being considered a return type here.
                if (currentTokenKind == SyntaxKind.CloseBraceToken)
                {
                    return true;
                }

                // "TOKEN TypeName void". In this case, we just have an incomplete member before
                // an existing member.  Treat this 'TOKEN' as a keyword.
                if (SyntaxFacts.IsPredefinedType(this.CurrentToken.Kind))
                {
                    return true;
                }

                // "TOKEN TypeName public".  In this case, we just have an incomplete member before
                // an existing member.  Treat this 'TOKEN' as a keyword.
                if (IsNonContextualModifier(this.CurrentToken))
                {
                    return true;
                }

                // "TOKEN TypeName class". In this case, we just have an incomplete member before
                // an existing type declaration.  Treat this 'TOKEN' as a keyword.
                if (IsTypeDeclarationStart())
                {
                    return true;
                }

                // "TOKEN TypeName namespace". In this case, we just have an incomplete member before
                // an existing namespace declaration.  Treat this 'TOKEN' as a keyword.
                if (currentTokenKind == SyntaxKind.NamespaceKeyword)
                {
                    return true;
                }

                if (!parsingStatementNotDeclaration && currentTokenKind == SyntaxKind.OperatorKeyword)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNonContextualModifier(SyntaxToken nextToken)
        {
            return !SyntaxFacts.IsContextualKeyword(nextToken.ContextualKind) && GetModifierExcludingScoped(nextToken) != DeclarationModifiers.None;
        }

        private bool IsPartialType()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword);
            var nextToken = this.PeekToken(1);
            switch (nextToken.Kind)
            {
                case SyntaxKind.StructKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.InterfaceKeyword:
                    return true;
            }

            if (nextToken.ContextualKind == SyntaxKind.RecordKeyword)
            {
                // This is an unusual use of LangVersion. Normally we only produce errors when the langversion
                // does not support a feature, but in this case we are effectively making a language breaking
                // change to consider "record" a type declaration in all ambiguous cases. To avoid breaking
                // older code that is not using C# 9 we conditionally parse based on langversion
                return IsFeatureEnabled(MessageID.IDS_FeatureRecords);
            }

            return false;
        }

        private bool IsPartialMember()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.PartialKeyword);

            // Check for:
            //   partial event
            if (this.PeekToken(1).Kind == SyntaxKind.EventKeyword)
            {
                return true;
            }

            // Check for constructor:
            //   partial Identifier(
            if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken &&
                this.PeekToken(2).Kind == SyntaxKind.OpenParenToken)
            {
                return IsFeatureEnabled(MessageID.IDS_FeaturePartialEventsAndConstructors);
            }

            // Check for method/property:
            //   partial ReturnType MemberName
            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            this.EatToken(); // partial

            if (this.ScanType() == ScanTypeFlags.NotType)
            {
                return false;
            }

            return IsPossibleMemberName();
        }

        private bool IsPossibleMemberName()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.IdentifierToken:
                    if (this.CurrentToken.ContextualKind == SyntaxKind.GlobalKeyword && this.PeekToken(1).Kind == SyntaxKind.UsingKeyword)
                    {
                        return false;
                    }

                    return true;
                case SyntaxKind.ThisKeyword:
                    return true;
                default:
                    return false;
            }
        }

        private MemberDeclarationSyntax ParseTypeDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            cancellationToken.ThrowIfCancellationRequested();

            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ClassKeyword:
                    return this.ParseMainTypeDeclaration(attributes, modifiers);

                case SyntaxKind.StructKeyword:
                    return this.ParseMainTypeDeclaration(attributes, modifiers);

                case SyntaxKind.InterfaceKeyword:
                    return this.ParseMainTypeDeclaration(attributes, modifiers);

                case SyntaxKind.DelegateKeyword:
                    return this.ParseDelegateDeclaration(attributes, modifiers);

                case SyntaxKind.EnumKeyword:
                    return this.ParseEnumDeclaration(attributes, modifiers);

                case SyntaxKind.IdentifierToken:
                    Debug.Assert(CurrentToken.ContextualKind is SyntaxKind.RecordKeyword or SyntaxKind.ExtensionKeyword);
                    return ParseMainTypeDeclaration(attributes, modifiers);

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.CurrentToken.Kind);
            }
        }

        private TypeDeclarationSyntax ParseMainTypeDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.ClassKeyword or SyntaxKind.StructKeyword or SyntaxKind.InterfaceKeyword ||
                this.CurrentToken.ContextualKind is SyntaxKind.RecordKeyword or SyntaxKind.ExtensionKeyword);

            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);

            if (!tryScanRecordStart(out var keyword, out var recordModifier))
            {
                keyword = ConvertToKeyword(this.EatToken());
            }

            bool isExtension = keyword.Kind == SyntaxKind.ExtensionKeyword;
            var outerSaveTerm = _termState;
            _termState |= TerminatorState.IsEndOfTypeSignature;

            var saveTerm = _termState;
            _termState |= TerminatorState.IsPossibleAggregateClauseStartOrStop;

            SyntaxToken? name;
            if (isExtension)
            {
                name = null;
                if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    keyword = AddTrailingSkippedSyntax(keyword, this.AddError(this.EatToken(), ErrorCode.ERR_ExtensionDisallowsName));
                }
            }
            else
            {
                name = this.ParseIdentifierToken();
            }

            var typeParameters = this.ParseTypeParameterList();

            // For extension declarations, there must be a parameter list
            var paramList = CurrentToken.Kind == SyntaxKind.OpenParenToken || isExtension
                ? ParseParenthesizedParameterList(forExtension: isExtension) : null;

            var baseList = isExtension ? null : this.ParseBaseList();
            _termState = saveTerm;

            // Parse class body
            bool parseMembers = true;
            SyntaxListBuilder<MemberDeclarationSyntax> members = default;
            SyntaxListBuilder<TypeParameterConstraintClauseSyntax> constraints = default;
            try
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                {
                    constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                    this.ParseTypeParameterConstraintClauses(constraints);
                }

                _termState = outerSaveTerm;

                SyntaxToken semicolon;
                SyntaxToken? openBrace;
                SyntaxToken? closeBrace;
                if (CurrentToken.Kind == SyntaxKind.SemicolonToken)
                {
                    semicolon = EatToken(SyntaxKind.SemicolonToken);
                    openBrace = null;
                    closeBrace = null;
                }
                else
                {
                    openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

                    // ignore members if missing open curly
                    if (openBrace.IsMissing)
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

                                var member = this.ParseMemberDeclaration(keyword.Kind);
                                if (member != null)
                                {
                                    // statements are accepted here, a semantic error will be reported later
                                    members.Add(member);
                                }
                                else
                                {
                                    // we get here if we couldn't parse the lookahead as a statement or a declaration (we haven't consumed any tokens):
                                    this.SkipBadMemberListTokens(ref openBrace, members);
                                }

                                _termState = saveTerm2;
                            }
                            else if (kind is SyntaxKind.CloseBraceToken or SyntaxKind.EndOfFileToken || this.IsTerminator())
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

                    closeBrace = openBrace.IsMissing
                        ? this.CreateMissingToken(SyntaxKind.CloseBraceToken, this.CurrentToken.Kind)
                        : this.EatToken(SyntaxKind.CloseBraceToken);

                    semicolon = TryEatToken(SyntaxKind.SemicolonToken);
                }

                return constructTypeDeclaration(_syntaxFactory, attributes, modifiers, keyword, recordModifier, name, typeParameters, paramList, baseList, constraints, openBrace, members, closeBrace, semicolon);
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

            bool tryScanRecordStart([NotNullWhen(true)] out SyntaxToken? keyword, out SyntaxToken? recordModifier)
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.RecordKeyword)
                {
                    keyword = ConvertToKeyword(this.EatToken());
                    recordModifier = this.CurrentToken.Kind is SyntaxKind.ClassKeyword or SyntaxKind.StructKeyword
                        ? EatToken()
                        : null;

                    return true;
                }

                if (this.CurrentToken.Kind is SyntaxKind.StructKeyword or SyntaxKind.ClassKeyword &&
                    this.PeekToken(1).ContextualKind == SyntaxKind.RecordKeyword &&
                    this.PeekToken(2).Kind is SyntaxKind.IdentifierToken)
                {
                    // Provide a specific diagnostic on `struct record S` or `class record C`
                    var misplacedToken = this.EatToken();

                    // Parse out 'record' but place 'struct/class' as leading skipped trivia on it.
                    keyword = AddLeadingSkippedSyntax(
                        this.AddError(ConvertToKeyword(this.EatToken()), ErrorCode.ERR_MisplacedRecord),
                        misplacedToken);

                    // Treat `struct record` as a RecordStructDeclaration, and `class record` as a RecordDeclaration.
                    recordModifier = SyntaxFactory.MissingToken(misplacedToken.Kind);
                    return true;
                }

                keyword = null;
                recordModifier = null;
                return false;
            }

            static TypeDeclarationSyntax constructTypeDeclaration(ContextAwareSyntax syntaxFactory, SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers, SyntaxToken keyword, SyntaxToken? recordModifier,
                SyntaxToken? name, TypeParameterListSyntax typeParameters, ParameterListSyntax? paramList, BaseListSyntax? baseList, SyntaxListBuilder<TypeParameterConstraintClauseSyntax> constraints,
                SyntaxToken? openBrace, SyntaxListBuilder<MemberDeclarationSyntax> members, SyntaxToken? closeBrace, SyntaxToken semicolon)
            {
                var modifiersList = (SyntaxList<SyntaxToken>)modifiers.ToList();
                var membersList = (SyntaxList<MemberDeclarationSyntax>)members;
                var constraintsList = (SyntaxList<TypeParameterConstraintClauseSyntax>)constraints;
                switch (keyword.Kind)
                {
                    case SyntaxKind.ClassKeyword:
                        Debug.Assert(name is not null);
                        return syntaxFactory.ClassDeclaration(
                            attributes,
                            modifiersList,
                            keyword,
                            name,
                            typeParameters,
                            paramList,
                            baseList,
                            constraintsList,
                            openBrace,
                            membersList,
                            closeBrace,
                            semicolon);

                    case SyntaxKind.StructKeyword:
                        Debug.Assert(name is not null);
                        return syntaxFactory.StructDeclaration(
                            attributes,
                            modifiersList,
                            keyword,
                            name,
                            typeParameters,
                            paramList,
                            baseList,
                            constraintsList,
                            openBrace,
                            membersList,
                            closeBrace,
                            semicolon);

                    case SyntaxKind.InterfaceKeyword:
                        Debug.Assert(name is not null);
                        return syntaxFactory.InterfaceDeclaration(
                            attributes,
                            modifiersList,
                            keyword,
                            name,
                            typeParameters,
                            paramList,
                            baseList,
                            constraintsList,
                            openBrace,
                            membersList,
                            closeBrace,
                            semicolon);

                    case SyntaxKind.RecordKeyword:
                        // record struct ...
                        // record ...
                        // record class ...
                        Debug.Assert(name is not null);
                        SyntaxKind declarationKind = recordModifier?.Kind == SyntaxKind.StructKeyword ? SyntaxKind.RecordStructDeclaration : SyntaxKind.RecordDeclaration;
                        return syntaxFactory.RecordDeclaration(
                            declarationKind,
                            attributes,
                            modifiers.ToList(),
                            keyword,
                            classOrStructKeyword: recordModifier,
                            name,
                            typeParameters,
                            paramList,
                            baseList,
                            constraints,
                            openBrace,
                            members,
                            closeBrace,
                            semicolon);

                    case SyntaxKind.ExtensionKeyword:
                        Debug.Assert(name is null);
                        Debug.Assert(baseList is null);
                        return syntaxFactory.ExtensionBlockDeclaration(
                            attributes,
                            modifiers.ToList(),
                            keyword,
                            typeParameters,
                            paramList,
                            constraints,
                            openBrace,
                            members,
                            closeBrace,
                            semicolon);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(keyword.Kind);
                }
            }
        }

#nullable disable

        private void SkipBadMemberListTokens(ref SyntaxToken openBrace, SyntaxListBuilder members)
        {
            if (members.Count > 0)
            {
                var tmp = members[^1];
                this.SkipBadMemberListTokens(ref tmp);
                members[^1] = tmp;
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
                    !(kind == SyntaxKind.DelegateKeyword && this.PeekToken(1).Kind is SyntaxKind.OpenBraceToken or SyntaxKind.OpenParenToken))
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

            previousNode = AddTrailingSkippedSyntax(
                (CSharpSyntaxNode)previousNode,
                _pool.ToTokenListAndFree(tokens).Node);
        }

        private bool IsPossibleMemberStartOrStop()
        {
            return this.IsPossibleMemberStart() || this.CurrentToken.Kind == SyntaxKind.CloseBraceToken;
        }

        private bool IsPossibleAggregateClauseStartOrStop()
        {
            return this.CurrentToken.Kind is SyntaxKind.ColonToken or SyntaxKind.OpenBraceToken
                || this.IsCurrentTokenWhereOfConstraintClause();
        }

        private BaseListSyntax ParseBaseList()
        {
            // We are only called from ParseMainTypeDeclaration which unilaterally sets this.
            Debug.Assert((_termState & TerminatorState.IsEndOfTypeSignature) != 0);

            var colon = this.TryEatToken(SyntaxKind.ColonToken);
            if (colon == null)
                return null;

            var list = _pool.AllocateSeparated<BaseTypeSyntax>();

            // Grammar requires at least one base type follow the colon.
            var firstType = this.ParseType();
            list.Add(this.CurrentToken.Kind == SyntaxKind.OpenParenToken
                ? _syntaxFactory.PrimaryConstructorBaseType(firstType, this.ParseParenthesizedArgumentList())
                : _syntaxFactory.SimpleBaseType(firstType));

            // Parse any optional base types that follow.
            while (true)
            {
                if (this.CurrentToken.Kind is SyntaxKind.OpenBraceToken or SyntaxKind.SemicolonToken ||
                    this.IsCurrentTokenWhereOfConstraintClause())
                {
                    break;
                }

                if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    list.Add(_syntaxFactory.SimpleBaseType(this.ParseType()));
                    continue;
                }

                // Error recovery.  Code had an element in the base list, but wasn't followed by a comma or the start of
                // any production that normally follows in a type declaration.  See if this is just a case of a missing
                // comma between types in the base list.
                //
                // Note: if we see something that looks more like a modifier than a type (like 'file') do not try to
                // consume it as a type here, as we want to use that to better determine what member is actually following
                // this incomplete type declaration.
                if (GetModifierExcludingScoped(this.CurrentToken) != DeclarationModifiers.None)
                {
                    break;
                }

                if (this.IsPossibleType())
                {
                    list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    list.Add(_syntaxFactory.SimpleBaseType(this.ParseType()));
                    continue;
                }

                if (skipBadBaseListTokens(ref colon, list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                {
                    break;
                }
            }

            return _syntaxFactory.BaseList(colon, _pool.ToListAndFree(list));

            PostSkipAction skipBadBaseListTokens(ref SyntaxToken colon, SeparatedSyntaxListBuilder<BaseTypeSyntax> list, SyntaxKind expected)
            {
                return this.SkipBadSeparatedListTokensWithExpectedKind(ref colon, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleAttribute(),
                    static (p, _) => p.CurrentToken.Kind == SyntaxKind.OpenBraceToken || p.IsCurrentTokenWhereOfConstraintClause(),
                    expected);
            }
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

            // first bound
            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsCurrentTokenWhereOfConstraintClause())
            {
                bounds.Add(_syntaxFactory.TypeConstraint(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
            }
            else
            {
                TypeParameterConstraintSyntax constraint = this.ParseTypeParameterConstraint();
                bounds.Add(constraint);

                // remaining bounds
                while (true)
                {
                    bool haveComma;

                    if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                        || ((_termState & TerminatorState.IsEndOfTypeSignature) != 0 && this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
                        || this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken
                        || this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
                    {
                        break;
                    }
                    else if (haveComma = (this.CurrentToken.Kind == SyntaxKind.CommaToken) || this.IsPossibleTypeParameterConstraint())
                    {
                        SyntaxToken separatorToken = this.EatToken(SyntaxKind.CommaToken);

                        if (constraint.Kind == SyntaxKind.AllowsConstraintClause && haveComma && !this.IsPossibleTypeParameterConstraint())
                        {
                            AddTrailingSkippedSyntax(bounds, this.AddError(separatorToken, ErrorCode.ERR_UnexpectedToken, SyntaxFacts.GetText(SyntaxKind.CommaToken)));
                            break;
                        }

                        bounds.AddSeparator(separatorToken);
                        if (this.IsCurrentTokenWhereOfConstraintClause())
                        {
                            bounds.Add(_syntaxFactory.TypeConstraint(this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TypeExpected)));
                            break;
                        }
                        else
                        {
                            constraint = this.ParseTypeParameterConstraint();
                            bounds.Add(constraint);
                        }
                    }
                    else if (skipBadTypeParameterConstraintTokens(bounds, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                    {
                        break;
                    }
                }
            }

            return _syntaxFactory.TypeParameterConstraintClause(
                where,
                name,
                colon,
                _pool.ToListAndFree(bounds));

            PostSkipAction skipBadTypeParameterConstraintTokens(SeparatedSyntaxListBuilder<TypeParameterConstraintSyntax> list, SyntaxKind expected)
            {
                CSharpSyntaxNode tmp = null;
                Debug.Assert(list.Count > 0);
                return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleTypeParameterConstraint(),
                    static (p, _) => p.CurrentToken.Kind == SyntaxKind.OpenBraceToken || p.IsCurrentTokenWhereOfConstraintClause(),
                    expected);
            }
        }

        private bool IsPossibleTypeParameterConstraint()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.NewKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.StructKeyword:
                case SyntaxKind.DefaultKeyword:
                    return true;
                case SyntaxKind.IdentifierToken:

                    return (this.CurrentToken.ContextualKind == SyntaxKind.AllowsKeyword && PeekToken(1).Kind == SyntaxKind.RefKeyword) || this.IsTrueIdentifier();
                default:
                    return IsPredefinedType(this.CurrentToken.Kind);
            }
        }

        private TypeParameterConstraintSyntax ParseTypeParameterConstraint()
        {
            return this.CurrentToken.Kind switch
            {
                SyntaxKind.NewKeyword =>
                    _syntaxFactory.ConstructorConstraint(
                        newKeyword: this.EatToken(),
                        this.EatToken(SyntaxKind.OpenParenToken),
                        this.EatToken(SyntaxKind.CloseParenToken)),

                SyntaxKind.StructKeyword =>
                    _syntaxFactory.ClassOrStructConstraint(
                        SyntaxKind.StructConstraint,
                        classOrStructKeyword: this.EatToken(),
                        this.CurrentToken.Kind == SyntaxKind.QuestionToken
                            ? this.AddError(this.EatToken(), ErrorCode.ERR_UnexpectedToken, SyntaxFacts.GetText(SyntaxKind.QuestionToken))
                            : null),

                SyntaxKind.ClassKeyword =>
                    _syntaxFactory.ClassOrStructConstraint(
                        SyntaxKind.ClassConstraint,
                        classOrStructKeyword: this.EatToken(),
                        this.TryEatToken(SyntaxKind.QuestionToken)),

                SyntaxKind.DefaultKeyword =>
                    _syntaxFactory.DefaultConstraint(defaultKeyword: this.EatToken()),

                SyntaxKind.EnumKeyword =>
                    _syntaxFactory.TypeConstraint(AddTrailingSkippedSyntax(
                        this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_NoEnumConstraint),
                        this.EatToken())),

                // Produce a specific diagnostic for `where T : delegate`
                // but not `where T : delegate*<...>
                SyntaxKind.DelegateKeyword =>
                    PeekToken(1).Kind == SyntaxKind.AsteriskToken
                        ? _syntaxFactory.TypeConstraint(this.ParseType())
                        : _syntaxFactory.TypeConstraint(AddTrailingSkippedSyntax(
                            this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_NoDelegateConstraint),
                            this.EatToken())),

                _ => parseTypeOrAllowsConstraint(),
            };

            TypeParameterConstraintSyntax parseTypeOrAllowsConstraint()
            {
                if (this.CurrentToken.ContextualKind == SyntaxKind.AllowsKeyword &&
                    PeekToken(1).Kind == SyntaxKind.RefKeyword)
                {
                    var allows = this.EatContextualToken(SyntaxKind.AllowsKeyword);

                    var bounds = _pool.AllocateSeparated<AllowsConstraintSyntax>();

                    while (true)
                    {
                        bounds.Add(
                            _syntaxFactory.RefStructConstraint(
                                this.EatToken(SyntaxKind.RefKeyword),
                                this.EatToken(SyntaxKind.StructKeyword)));

                        if (this.CurrentToken.Kind == SyntaxKind.CommaToken && PeekToken(1).Kind == SyntaxKind.RefKeyword)
                        {
                            bounds.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                            continue;
                        }

                        break;
                    }

                    return _syntaxFactory.AllowsConstraintClause(allows, _pool.ToListAndFree(bounds));
                }

                return _syntaxFactory.TypeConstraint(this.ParseType());
            }
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

        private bool IsTypeDeclarationStart()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.DelegateKeyword when !IsFunctionPointerStart():
                case SyntaxKind.EnumKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.StructKeyword:
                    return true;

                case SyntaxKind.IdentifierToken:
                    if (CurrentToken.ContextualKind == SyntaxKind.RecordKeyword)
                    {
                        // This is an unusual use of LangVersion. Normally we only produce errors when the langversion
                        // does not support a feature, but in this case we are effectively making a language breaking
                        // change to consider "record" a type declaration in all ambiguous cases. To avoid breaking
                        // older code that is not using C# 9 we conditionally parse based on langversion
                        return IsFeatureEnabled(MessageID.IDS_FeatureRecords);
                    }

                    if (IsExtensionContainerStart())
                    {
                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }

        private bool CanReuseMemberDeclaration(SyntaxKind kind, bool isGlobal)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.FileScopedNamespaceDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    return true;
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                    if (!isGlobal || IsScript)
                    {
                        return true;
                    }

                    // We can reuse original nodes if they came from the global context as well.
                    return (this.CurrentNode.Parent is Syntax.CompilationUnitSyntax);

                case SyntaxKind.GlobalStatement:
                    return isGlobal;

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
                static @this => @this.ParseMemberDeclaration(parentKind),
                createEmptyNodeFunc);

            // Creates a dummy declaration node to which we can attach a stack overflow message
            static MemberDeclarationSyntax createEmptyNodeFunc(LanguageParser @this)
            {
                return @this._syntaxFactory.IncompleteMember(
                    new SyntaxList<AttributeListSyntax>(),
                    new SyntaxList<SyntaxToken>(),
                    @this.CreateMissingIdentifierName());
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

        /// <summary>
        /// Changes in this function around member parsing should be mirrored in <see cref="ParseMemberDeclarationCore"/>.
        /// Try keeping structure of both functions similar to simplify this task. The split was made to 
        /// reduce the stack usage during recursive parsing.
        /// </summary>
        /// <returns>Returns null if we can't parse anything (even partially).</returns>
        private MemberDeclarationSyntax ParseMemberDeclarationOrStatementCore(SyntaxKind parentKind)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);
            Debug.Assert(parentKind == SyntaxKind.CompilationUnit);

            cancellationToken.ThrowIfCancellationRequested();

            // don't reuse members if they were previously declared under a different type keyword kind
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseMemberDeclaration(CurrentNodeKind, isGlobal: true))
                return (MemberDeclarationSyntax)this.EatNode();

            var saveTermState = _termState;

            var attributes = this.ParseStatementAttributeDeclarations();
            bool haveAttributes = attributes.Count > 0;

            var afterAttributesPoint = this.GetResetPoint();

            var modifiers = _pool.Allocate();

            try
            {
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
                if (!haveAttributes || !IsScript)
                {
                    bool wasInAsync = IsInAsync;
                    if (!IsScript)
                    {
                        IsInAsync = true; // We are implicitly in an async context
                    }

                    try
                    {
                        switch (this.CurrentToken.Kind)
                        {
                            case SyntaxKind.UnsafeKeyword:
                                if (this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken)
                                {
                                    return _syntaxFactory.GlobalStatement(ParseUnsafeStatement(attributes));
                                }
                                break;

                            case SyntaxKind.FixedKeyword:
                                if (this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
                                {
                                    return _syntaxFactory.GlobalStatement(ParseFixedStatement(attributes));
                                }
                                break;

                            case SyntaxKind.DelegateKeyword:
                                // Check if this is an anonymous delegate expression or a delegate type declaration.
                                // Anonymous delegate: delegate { } or delegate (params) { }
                                // Delegate declaration: delegate Type Name(params);
                                if (IsAnonymousDelegateExpression())
                                {
                                    return _syntaxFactory.GlobalStatement(ParseExpressionStatement(attributes));
                                }
                                break;

                            case SyntaxKind.NewKeyword:
                                if (IsPossibleNewExpression())
                                {
                                    return _syntaxFactory.GlobalStatement(ParseExpressionStatement(attributes));
                                }
                                break;
                        }
                    }
                    finally
                    {
                        IsInAsync = wasInAsync;
                    }
                }

                // All modifiers that might start an expression are processed above.
                bool isPossibleTypeDeclaration;
                this.ParseModifiers(modifiers, forAccessors: false, forTopLevelStatements: true, out isPossibleTypeDeclaration);
                bool haveModifiers = (modifiers.Count > 0);
                MemberDeclarationSyntax result;

                // Check for constructor form
                if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
                {
                    // Script: 
                    // Constructor definitions are not allowed. We parse them as method calls with semicolon missing error:
                    //
                    // Script(...) { ... } 
                    //            ^
                    //            missing ';'
                    //
                    // Unless modifiers or attributes are present this is more likely to be a method call than a method definition.
                    if (haveAttributes || haveModifiers)
                    {
                        var voidType = _syntaxFactory.PredefinedType(
                            this.AddError(SyntaxFactory.MissingToken(SyntaxKind.VoidKeyword), ErrorCode.ERR_MemberNeedsType));

                        if (!IsScript)
                        {
                            if (tryParseLocalDeclarationStatementFromStartPoint<LocalFunctionStatementSyntax>(attributes, ref afterAttributesPoint, out result))
                            {
                                return result;
                            }
                        }
                        else
                        {
                            var identifier = this.EatToken();
                            return this.ParseMethodDeclaration(attributes, modifiers, voidType, explicitInterfaceOpt: null, identifier: identifier, typeParameterList: null);
                        }
                    }
                }

                // Destructors are disallowed in global code, skipping check for them.
                // TODO: better error messages for script

                // Check for constant
                if (this.CurrentToken.Kind == SyntaxKind.ConstKeyword)
                {
                    if (!IsScript &&
                        tryParseLocalDeclarationStatementFromStartPoint<LocalDeclarationStatementSyntax>(attributes, ref afterAttributesPoint, out result))
                    {
                        return result;
                    }

                    // Prefers const field over const local variable decl
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
                result = this.TryParseConversionOperatorDeclaration(attributes, modifiers);
                if (result is not null)
                {
                    return result;
                }

                if (this.CurrentToken.Kind == SyntaxKind.NamespaceKeyword)
                {
                    return ParseNamespaceDeclaration(attributes, modifiers);
                }

                // It's valid to have a type declaration here -- check for those
                if (isPossibleTypeDeclaration && IsTypeDeclarationStart())
                {
                    return this.ParseTypeDeclaration(attributes, modifiers);
                }

                TypeSyntax type = ParseReturnType();

                var afterTypeResetPoint = this.GetResetPoint();

                try
                {
                    // Try as a regular statement rather than a member declaration, if appropriate.
                    if ((!haveAttributes || !IsScript) && !haveModifiers && (type.Kind == SyntaxKind.RefType || !IsOperatorStart(out _, advanceParser: false)))
                    {
                        this.Reset(ref afterAttributesPoint);

                        if (this.CurrentToken.Kind is not SyntaxKind.CloseBraceToken and not SyntaxKind.EndOfFileToken &&
                            this.IsPossibleStatement())
                        {
                            var saveTerm = _termState;
                            _termState |= TerminatorState.IsPossibleStatementStartOrStop; // partial statements can abort if a new statement starts
                            bool wasInAsync = IsInAsync;
                            if (!IsScript)
                            {
                                IsInAsync = true; // We are implicitly in an async context
                            }
                            // In Script we don't allow local declaration statements at the top level.  We want
                            // to fall out below and parse them instead as fields. For top-level statements, we allow
                            // them, but want to try properties , etc. first.
                            var statement = this.ParseStatementCore(attributes, isGlobal: true);

                            IsInAsync = wasInAsync;
                            _termState = saveTerm;

                            if (isAcceptableNonDeclarationStatement(statement, IsScript))
                            {
                                return _syntaxFactory.GlobalStatement(statement);
                            }
                        }

                        this.Reset(ref afterTypeResetPoint);
                    }

                    // Everything that's left -- methods, fields, properties, locals,
                    // indexers, and non-conversion operators -- starts with a type 
                    // (possibly void).

                    // Check for misplaced modifiers.  if we see any, then consider this member
                    // terminated and restart parsing.
                    if (IsMisplacedModifier(modifiers, attributes, type, out result))
                    {
                        return result;
                    }

parse_member_name:;
                    // If we've seen the ref keyword, we know we must have an indexer, method, property, or local.
                    bool typeIsRef = type.IsRef;
                    ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt;

                    // Check here for operators
                    // Allow old-style implicit/explicit casting operator syntax, just so we can give a better error
                    if (!typeIsRef && IsOperatorStart(out explicitInterfaceOpt))
                    {
                        return this.ParseOperatorDeclaration(attributes, modifiers, type, explicitInterfaceOpt);
                    }

                    if ((!typeIsRef || !IsScript) && IsFieldDeclaration(isEvent: false, isGlobalScriptLevel: true))
                    {
                        var saveTerm = _termState;

                        if ((!haveAttributes && !haveModifiers) || !IsScript)
                        {
                            // if we are at top-level then statements can occur
                            _termState |= TerminatorState.IsPossibleStatementStartOrStop;

                            if (!IsScript)
                            {
                                this.Reset(ref afterAttributesPoint);
                                if (tryParseLocalDeclarationStatement<LocalDeclarationStatementSyntax>(attributes, out result))
                                {
                                    return result;
                                }

                                this.Reset(ref afterTypeResetPoint);
                            }
                        }

                        if (!typeIsRef)
                        {
                            return this.ParseNormalFieldDeclaration(attributes, modifiers, type, parentKind);
                        }
                        else
                        {
                            _termState = saveTerm;
                        }
                    }

                    // At this point we can either have indexers, methods, or 
                    // properties (or something unknown).  Try to break apart
                    // the following name and determine what to do from there.
                    SyntaxToken identifierOrThisOpt;
                    TypeParameterListSyntax typeParameterListOpt;
                    this.ParseMemberName(out explicitInterfaceOpt, out identifierOrThisOpt, out typeParameterListOpt, isEvent: false);

                    if (!haveModifiers && !haveAttributes && !IsScript &&
                        explicitInterfaceOpt == null && identifierOrThisOpt == null && typeParameterListOpt == null &&
                        !type.IsMissing && type.Kind != SyntaxKind.RefType &&
                        !isFollowedByPossibleUsingDirective() &&
                        tryParseLocalDeclarationStatementFromStartPoint<LocalDeclarationStatementSyntax>(attributes, ref afterAttributesPoint, out result))
                    {
                        return result;
                    }

                    // First, check if we got absolutely nothing.  If so, then 
                    // We need to consume a bad member and try again.
                    if (IsNoneOrIncompleteMember(parentKind, attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt, out result))
                    {
                        return result;
                    }

                    // If the modifiers did not include "async", and the type we got was "async", and there was an
                    // error in the identifier or its type parameters, then the user is probably in the midst of typing
                    // an async method.  In that case we reconsider "async" to be a modifier, and treat the identifier
                    // (with the type parameters) as the type (with type arguments).  Then we go back to looking for
                    // the member name again.
                    // For example, if we get
                    //     async Task<
                    // then we want async to be a modifier and Task<MISSING> to be a type.
                    if (ReconsideredTypeAsAsyncModifier(ref modifiers, ref type, ref afterTypeResetPoint, ref explicitInterfaceOpt, ref identifierOrThisOpt, ref typeParameterListOpt))
                    {
                        goto parse_member_name;
                    }

                    Debug.Assert(identifierOrThisOpt != null);

                    if (TryParseIndexerOrPropertyDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt, out result))
                    {
                        return result;
                    }

                    if (!IsScript)
                    {
                        if (explicitInterfaceOpt is null &&
                            tryParseLocalDeclarationStatementFromStartPoint<LocalFunctionStatementSyntax>(attributes, ref afterAttributesPoint, out result))
                        {
                            return result;
                        }

                        if (!haveModifiers &&
                            tryParseStatement(attributes, ref afterAttributesPoint, out result))
                        {
                            return result;
                        }
                    }

                    // treat anything else as a method.

                    return this.ParseMethodDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);
                }
                finally
                {
                    this.Release(ref afterTypeResetPoint);
                }
            }
            finally
            {
                _pool.Free(modifiers);
                _termState = saveTermState;
                this.Release(ref afterAttributesPoint);
            }

            bool tryParseLocalDeclarationStatement<DeclarationSyntax>(SyntaxList<AttributeListSyntax> attributes, out MemberDeclarationSyntax result) where DeclarationSyntax : StatementSyntax
            {
                bool wasInAsync = IsInAsync;
                IsInAsync = true; // We are implicitly in an async context
                int lastTokenPosition = -1;
                IsMakingProgress(ref lastTokenPosition);

                var topLevelStatement = ParseLocalDeclarationStatement(attributes);
                IsInAsync = wasInAsync;

                if (topLevelStatement is DeclarationSyntax declaration && IsMakingProgress(ref lastTokenPosition, assertIfFalse: false))
                {
                    result = _syntaxFactory.GlobalStatement(declaration);
                    return true;
                }

                result = null;
                return false;
            }

            bool tryParseStatement(SyntaxList<AttributeListSyntax> attributes, ref ResetPoint afterAttributesPoint, out MemberDeclarationSyntax result)
            {
                using var resetOnFailurePoint = this.GetDisposableResetPoint(resetOnDispose: false);

                this.Reset(ref afterAttributesPoint);

                if (this.IsPossibleStatement())
                {
                    var saveTerm = _termState;
                    _termState |= TerminatorState.IsPossibleStatementStartOrStop; // partial statements can abort if a new statement starts
                    bool wasInAsync = IsInAsync;
                    IsInAsync = true; // We are implicitly in an async context

                    var statement = this.ParseStatementCore(attributes, isGlobal: true);

                    IsInAsync = wasInAsync;
                    _termState = saveTerm;

                    if (statement is not null)
                    {
                        result = _syntaxFactory.GlobalStatement(statement);
                        return true;
                    }
                }

                resetOnFailurePoint.Reset();

                result = null;
                return false;
            }

            bool tryParseLocalDeclarationStatementFromStartPoint<DeclarationSyntax>(SyntaxList<AttributeListSyntax> attributes, ref ResetPoint startPoint, out MemberDeclarationSyntax result) where DeclarationSyntax : StatementSyntax
            {
                using var resetOnFailurePoint = this.GetDisposableResetPoint(resetOnDispose: false);

                this.Reset(ref startPoint);

                if (tryParseLocalDeclarationStatement<DeclarationSyntax>(attributes, out result))
                {
                    return true;
                }

                resetOnFailurePoint.Reset();
                return false;
            }

            static bool isAcceptableNonDeclarationStatement(StatementSyntax statement, bool isScript)
            {
                switch (statement?.Kind)
                {
                    case null:
                    case SyntaxKind.LocalFunctionStatement:
                    case SyntaxKind.ExpressionStatement when
                            !isScript &&
                            // Do not parse a single identifier as an expression statement in a Simple Program, this could be a beginning of a keyword and
                            // we want completion to offer it.
                            statement is ExpressionStatementSyntax { Expression.Kind: SyntaxKind.IdentifierName, SemicolonToken.IsMissing: true }:

                        return false;

                    case SyntaxKind.LocalDeclarationStatement:
                        return !isScript && statement is LocalDeclarationStatementSyntax { UsingKeyword: not null };

                    default:
                        return true;
                }
            }

            bool isFollowedByPossibleUsingDirective()
            {
                if (CurrentToken.Kind == SyntaxKind.UsingKeyword)
                {
                    return !IsPossibleTopLevelUsingLocalDeclarationStatement();
                }

                if (CurrentToken.ContextualKind == SyntaxKind.GlobalKeyword && this.PeekToken(1).Kind == SyntaxKind.UsingKeyword)
                {
                    using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

                    // Skip 'global' keyword
                    EatToken();
                    return !IsPossibleTopLevelUsingLocalDeclarationStatement();
                }

                return false;
            }
        }

        private bool IsMisplacedModifier(SyntaxListBuilder modifiers, SyntaxList<AttributeListSyntax> attributes, TypeSyntax type, out MemberDeclarationSyntax result)
        {
            if (GetModifierExcludingScoped(this.CurrentToken) != DeclarationModifiers.None &&
                this.CurrentToken.ContextualKind is not (SyntaxKind.PartialKeyword or SyntaxKind.AsyncKeyword or SyntaxKind.RequiredKeyword or SyntaxKind.FileKeyword) &&
                IsComplete(type))
            {
                var misplacedModifier = this.CurrentToken;
                type = this.AddError(
                    type,
                    // We're attaching a diagnostic for the misplaced modifier on the 'type' node.  So the offset will
                    // be *relative* to the *start* (not *full start*) of 'type'. That offset will then be the width of
                    // type itself, plus any trailing trivia it has, plus the leading trivia of the modifier itself.
                    offset: type.Width + type.GetTrailingTriviaWidth() + misplacedModifier.GetLeadingTriviaWidth(),
                    misplacedModifier.Width,
                    ErrorCode.ERR_BadModifierLocation,
                    misplacedModifier.Text);

                result = _syntaxFactory.IncompleteMember(attributes, modifiers.ToList(), type);
                return true;
            }

            result = null;
            return false;
        }

        private bool IsNoneOrIncompleteMember(SyntaxKind parentKind, SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers, TypeSyntax type,
                                              ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt, SyntaxToken identifierOrThisOpt, TypeParameterListSyntax typeParameterListOpt,
                                              out MemberDeclarationSyntax result)
        {
            if (explicitInterfaceOpt == null && identifierOrThisOpt == null && typeParameterListOpt == null)
            {
                if (attributes.Count == 0 && modifiers.Count == 0 && type.IsMissing && type.Kind != SyntaxKind.RefType)
                {
                    // we haven't advanced, the caller needs to consume the tokens ahead
                    result = null;
                    return true;
                }

                var incompleteMember = _syntaxFactory.IncompleteMember(attributes, modifiers.ToList(), type.IsMissing ? null : type);
                if (ContainsErrorDiagnostic(incompleteMember))
                {
                    result = incompleteMember;
                }
                else if (parentKind is SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration ||
                         parentKind == SyntaxKind.CompilationUnit && !IsScript)
                {
                    result = this.AddErrorToLastToken(incompleteMember, ErrorCode.ERR_NamespaceUnexpected);
                }
                else
                {
                    //the error position should indicate CurrentToken
                    result = this.AddError(
                        incompleteMember,
                        // We're attaching a diagnostic for the current token on the 'incompleteMember' node.  So the
                        // offset will be *relative* to the *start* (not *full start*) of 'incompleteMember'. That
                        // offset will then be the width of incompleteMember itself, plus any trailing trivia it has,
                        // plus the leading trivia of the modifier itself.
                        offset: incompleteMember.Width + incompleteMember.GetTrailingTriviaWidth() + this.CurrentToken.GetLeadingTriviaWidth(),
                        this.CurrentToken.Width,
                        ErrorCode.ERR_InvalidMemberDecl,
                        this.CurrentToken.Text);
                }

                return true;
            }

            result = null;
            return false;
        }

        private bool ReconsideredTypeAsAsyncModifier(ref SyntaxListBuilder modifiers, ref TypeSyntax type, ref ResetPoint afterTypeResetPoint,
                                                     ref ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt, ref SyntaxToken identifierOrThisOpt,
                                                     ref TypeParameterListSyntax typeParameterListOpt)
        {
            if (type.Kind != SyntaxKind.RefType &&
                identifierOrThisOpt != null &&
                (typeParameterListOpt != null && typeParameterListOpt.ContainsDiagnostics
                  || this.CurrentToken.Kind is not SyntaxKind.OpenParenToken and not SyntaxKind.OpenBraceToken and not SyntaxKind.EqualsGreaterThanToken) &&
                ReconsiderTypeAsAsyncModifier(ref modifiers, type, identifierOrThisOpt))
            {
                this.Reset(ref afterTypeResetPoint);
                explicitInterfaceOpt = null;
                identifierOrThisOpt = null;
                typeParameterListOpt = null;
                this.Release(ref afterTypeResetPoint);
                type = ParseReturnType();
                afterTypeResetPoint = this.GetResetPoint();
                return true;
            }

            return false;
        }

        private bool TryParseIndexerOrPropertyDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers, TypeSyntax type,
                                                          ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt, SyntaxToken identifierOrThisOpt,
                                                          TypeParameterListSyntax typeParameterListOpt, out MemberDeclarationSyntax result)
        {
            if (identifierOrThisOpt.Kind == SyntaxKind.ThisKeyword)
            {
                result = this.ParseIndexerDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);
                return true;
            }

            // `{` or `=>` definitely start a property.  Also allow
            // `; {` and `; =>` as error recovery for a misplaced semicolon.
            if (IsStartOfPropertyBody(this.CurrentToken.Kind) ||
                (this.CurrentToken.Kind is SyntaxKind.SemicolonToken && IsStartOfPropertyBody(this.PeekToken(1).Kind)))
            {
                result = this.ParsePropertyDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);
                return true;
            }

            result = null;
            return false;
        }

        private static bool IsStartOfPropertyBody(SyntaxKind kind)
            => kind is SyntaxKind.OpenBraceToken or SyntaxKind.EqualsGreaterThanToken;

        // Returns null if we can't parse anything (even partially).
        internal MemberDeclarationSyntax ParseMemberDeclaration(SyntaxKind parentKind)
        {
            _recursionDepth++;
            StackGuard.EnsureSufficientExecutionStack(_recursionDepth);
            var result = ParseMemberDeclarationCore(parentKind);
            _recursionDepth--;
            return result;
        }

        /// <summary>
        /// Changes in this function should be mirrored in <see cref="ParseMemberDeclarationOrStatementCore"/>.
        /// Try keeping structure of both functions similar to simplify this task. The split was made to 
        /// reduce the stack usage during recursive parsing.
        /// </summary>
        /// <returns>Returns null if we can't parse anything (even partially).</returns>
        private MemberDeclarationSyntax ParseMemberDeclarationCore(SyntaxKind parentKind)
        {
            // "top-level" expressions and statements should never occur inside an asynchronous context
            Debug.Assert(!IsInAsync);
            Debug.Assert(parentKind != SyntaxKind.CompilationUnit);

            cancellationToken.ThrowIfCancellationRequested();

            // don't reuse members if they were previously declared under a different type keyword kind
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseMemberDeclaration(CurrentNodeKind, isGlobal: false))
                return (MemberDeclarationSyntax)this.EatNode();

            var modifiers = _pool.Allocate();

            var saveTermState = _termState;

            try
            {
                var attributes = this.ParseAttributeDeclarations(inExpressionContext: false);

                bool isPossibleTypeDeclaration;
                this.ParseModifiers(modifiers, forAccessors: false, forTopLevelStatements: false, out isPossibleTypeDeclaration);

                if (IsExtensionContainerStart())
                {
                    return this.ParseMainTypeDeclaration(attributes, modifiers);
                }

                // Check for constructor form
                if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
                {
                    return this.ParseConstructorDeclaration(attributes, modifiers);
                }

                // Check for destructor form
                if (this.CurrentToken.Kind == SyntaxKind.TildeToken)
                {
                    return this.ParseDestructorDeclaration(attributes, modifiers);
                }

                // Check for constant
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
                MemberDeclarationSyntax result = this.TryParseConversionOperatorDeclaration(attributes, modifiers);
                if (result is not null)
                {
                    return result;
                }

                // Namespaces should be handled by the caller, not checking for them

                // It's valid to have a type declaration here -- check for those
                if (isPossibleTypeDeclaration && IsTypeDeclarationStart())
                {
                    return this.ParseTypeDeclaration(attributes, modifiers);
                }

                // Everything that's left -- methods, fields, properties, 
                // indexers, and non-conversion operators -- starts with a type 
                // (possibly void).
                TypeSyntax type = ParseReturnType();

                var afterTypeResetPoint = this.GetResetPoint();

                try
                {
                    // Check for misplaced modifiers.  if we see any, then consider this member
                    // terminated and restart parsing.
                    if (IsMisplacedModifier(modifiers, attributes, type, out result))
                    {
                        return result;
                    }

parse_member_name:;
                    ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt;

                    // If we've seen the ref keyword, we know we must have an indexer, method, field, or property.
                    if (type.Kind != SyntaxKind.RefType)
                    {
                        // Check here for operators
                        // Allow old-style implicit/explicit casting operator syntax, just so we can give a better error
                        if (IsOperatorStart(out explicitInterfaceOpt))
                        {
                            return this.ParseOperatorDeclaration(attributes, modifiers, type, explicitInterfaceOpt);
                        }
                    }

                    if (IsFieldDeclaration(isEvent: false, isGlobalScriptLevel: false))
                    {
                        return this.ParseNormalFieldDeclaration(attributes, modifiers, type, parentKind);
                    }

                    // At this point we can either have indexers, methods, or 
                    // properties (or something unknown).  Try to break apart
                    // the following name and determine what to do from there.
                    SyntaxToken identifierOrThisOpt;
                    TypeParameterListSyntax typeParameterListOpt;
                    this.ParseMemberName(out explicitInterfaceOpt, out identifierOrThisOpt, out typeParameterListOpt, isEvent: false);

                    // First, check if we got absolutely nothing.  If so, then 
                    // We need to consume a bad member and try again.
                    if (IsNoneOrIncompleteMember(parentKind, attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt, out result))
                    {
                        return result;
                    }

                    // If the modifiers did not include "async", and the type we got was "async", and there was an
                    // error in the identifier or its type parameters, then the user is probably in the midst of typing
                    // an async method.  In that case we reconsider "async" to be a modifier, and treat the identifier
                    // (with the type parameters) as the type (with type arguments).  Then we go back to looking for
                    // the member name again.
                    // For example, if we get
                    //     async Task<
                    // then we want async to be a modifier and Task<MISSING> to be a type.
                    if (ReconsideredTypeAsAsyncModifier(ref modifiers, ref type, ref afterTypeResetPoint, ref explicitInterfaceOpt, ref identifierOrThisOpt, ref typeParameterListOpt))
                    {
                        goto parse_member_name;
                    }

                    Debug.Assert(identifierOrThisOpt != null);

                    if (TryParseIndexerOrPropertyDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt, out result))
                    {
                        return result;
                    }

                    // treat anything else as a method.
                    return this.ParseMethodDeclaration(attributes, modifiers, type, explicitInterfaceOpt, identifierOrThisOpt, typeParameterListOpt);
                }
                finally
                {
                    this.Release(ref afterTypeResetPoint);
                }
            }
            finally
            {
                _pool.Free(modifiers);
                _termState = saveTermState;
            }
        }

        private bool IsExtensionContainerStart()
        {
            // For error recovery, we recognize `extension` followed by `<` even in older language versions
            return this.CurrentToken.ContextualKind == SyntaxKind.ExtensionKeyword &&
                (IsFeatureEnabled(MessageID.IDS_FeatureExtensions) || this.PeekToken(1).Kind == SyntaxKind.LessThanToken);
        }

        // if the modifiers do not contain async or replace and the type is the identifier "async" or "replace", then
        // add that identifier to the modifiers and assign a new type from the identifierOrThisOpt and the
        // type parameter list
        private static bool ReconsiderTypeAsAsyncModifier(
            ref SyntaxListBuilder modifiers,
            TypeSyntax type,
            SyntaxToken identifierOrThisOpt)
        {
            if (type.Kind != SyntaxKind.IdentifierName)
                return false;

            if (identifierOrThisOpt.Kind != SyntaxKind.IdentifierToken)
                return false;

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

        private bool IsFieldDeclaration(bool isEvent, bool isGlobalScriptLevel)
        {
            if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
            {
                return false;
            }

            if (this.CurrentToken.ContextualKind == SyntaxKind.GlobalKeyword && this.PeekToken(1).Kind == SyntaxKind.UsingKeyword)
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

            // Error recovery, don't allow a misplaced semicolon after the name in a property to throw off the entire parse.
            //
            // e.g. `public int MyProperty; { get; set; }` should still be parsed as a property with a skipped token.
            if (!isGlobalScriptLevel &&
                kind == SyntaxKind.SemicolonToken &&
                IsStartOfPropertyBody(this.PeekToken(2).Kind))
            {
                return false;
            }

            switch (kind)
            {
                case SyntaxKind.DotToken:                   // Goo.     explicit
                case SyntaxKind.ColonColonToken:            // Goo::    explicit
                case SyntaxKind.LessThanToken:              // Goo<     explicit or generic method
                case SyntaxKind.OpenBraceToken:             // Goo {    property
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
            return this.CurrentToken.Kind is SyntaxKind.ImplicitKeyword or SyntaxKind.ExplicitKeyword or SyntaxKind.OperatorKeyword;
        }

        public static bool IsComplete(CSharpSyntaxNode node)
        {
            if (node == null)
            {
                return false;
            }

            foreach (var child in node.ChildNodesAndTokens().Reverse())
            {
                if (child is not SyntaxToken token)
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

#nullable enable

        private ConstructorDeclarationSyntax ParseConstructorDeclaration(
            SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            var name = this.ParseIdentifierToken();
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;
            try
            {
                var paramList = this.ParseParenthesizedParameterList(forExtension: false);
                var initializer = this.TryParseConstructorInitializer();

                this.ParseBlockAndExpressionBodiesWithSemicolon(out var body, out var expressionBody, out var semicolon);

                return _syntaxFactory.ConstructorDeclaration(attributes, modifiers.ToList(), name, paramList, initializer, body, expressionBody, semicolon);
            }
            finally
            {
                _termState = saveTerm;
            }
        }

        private ConstructorInitializerSyntax? TryParseConstructorInitializer()
        {
            var currentTokenKind = this.CurrentToken.Kind;
            var shouldParse = currentTokenKind is SyntaxKind.ColonToken ||
                (currentTokenKind is SyntaxKind.EqualsGreaterThanToken &&
                 this.PeekToken(1).Kind is SyntaxKind.ThisKeyword or SyntaxKind.BaseKeyword &&
                 this.PeekToken(2).Kind is SyntaxKind.OpenParenToken);

            if (!shouldParse)
                return null;

            return ParseConstructorInitializer();
        }

        private ConstructorInitializerSyntax ParseConstructorInitializer()
        {
            // Normally called for `:` but also in some error recovery circumstances for `=>`. EatTokenAsKind handles
            // both cases properly, producing the right errors we need in the latter case, and always consuming
            // whichever token we're coming into this method on.
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.ColonToken or SyntaxKind.EqualsGreaterThanToken);
            var colon = this.EatTokenAsKind(SyntaxKind.ColonToken);

            var token = this.CurrentToken.Kind is SyntaxKind.BaseKeyword or SyntaxKind.ThisKeyword
                ? this.EatToken()
                : this.EatToken(SyntaxKind.ThisKeyword, ErrorCode.ERR_ThisOrBaseExpected);

            var argumentList = this.CurrentToken.Kind == SyntaxKind.OpenParenToken
                ? this.ParseParenthesizedArgumentList()
                : _syntaxFactory.ArgumentList(
                    this.EatToken(SyntaxKind.OpenParenToken, reportError: !token.ContainsDiagnostics),
                    arguments: default,
                    this.EatToken(SyntaxKind.CloseParenToken, reportError: !token.ContainsDiagnostics));

            return _syntaxFactory.ConstructorInitializer(
                token.Kind == SyntaxKind.BaseKeyword
                    ? SyntaxKind.BaseConstructorInitializer
                    : SyntaxKind.ThisConstructorInitializer,
                colon, token, argumentList);
        }

#nullable disable

        private DestructorDeclarationSyntax ParseDestructorDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.TildeToken);
            var tilde = this.EatToken(SyntaxKind.TildeToken);

            var name = this.ParseIdentifierToken();
            var parameterList = _syntaxFactory.ParameterList(
                this.EatToken(SyntaxKind.OpenParenToken),
                default(SeparatedSyntaxList<ParameterSyntax>),
                this.EatToken(SyntaxKind.CloseParenToken));

            this.ParseBlockAndExpressionBodiesWithSemicolon(
                out BlockSyntax body, out ArrowExpressionClauseSyntax expressionBody, out SyntaxToken semicolon);

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
            bool parseSemicolonAfterBlock = true)
        {
            // Check for 'forward' declarations with no block of any kind
            if (this.CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                blockBody = null;
                expressionBody = null;
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
                return;
            }

            blockBody = this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                ? this.ParseMethodOrAccessorBodyBlock(attributes: default, isAccessorBody: false)
                : null;

            expressionBody = this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken
                ? this.ParseArrowExpressionClause()
                : null;

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
            else
            {
                semicolon = null;
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
            => this.CurrentToken.Kind is SyntaxKind.SemicolonToken or SyntaxKind.OpenBraceToken;

        private bool IsEndOfTypeSignature()
        {
            return this.CurrentToken.Kind is SyntaxKind.SemicolonToken or SyntaxKind.OpenBraceToken;
        }

        private bool IsEndOfNameInExplicitInterface()
            => this.CurrentToken.Kind is SyntaxKind.DotToken or SyntaxKind.ColonColonToken;

        private bool IsEndOfFunctionPointerParameterList(bool errored)
            => this.CurrentToken.Kind == (errored ? SyntaxKind.CloseParenToken : SyntaxKind.GreaterThanToken);

        private bool IsEndOfFunctionPointerCallingConvention()
            => this.CurrentToken.Kind == SyntaxKind.CloseBracketToken;

        private MethodDeclarationSyntax ParseMethodDeclaration(
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList)
        {
            // Parse the name (it could be qualified)
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;

            var paramList = this.ParseParenthesizedParameterList(forExtension: false);

            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);
            if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
            {
                constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                this.ParseTypeParameterConstraintClauses(constraints);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                // Use else if, rather than if, because if we see both a constructor initializer and a constraint clause, we're too lost to recover.
                var colonToken = this.CurrentToken;

                var initializer = this.ParseConstructorInitializer();
                initializer = this.AddErrorToFirstToken(initializer, ErrorCode.ERR_UnexpectedToken, colonToken.Text);
                paramList = AddTrailingSkippedSyntax(paramList, initializer);

                // CONSIDER: Parsing an invalid constructor initializer could, conceivably, get us way
                // off track.  If this becomes a problem, an alternative approach would be to generalize
                // EatTokenWithPrejudice in such a way that we can just skip everything until we recognize
                // our context again (perhaps an open brace).
            }

            _termState = saveTerm;

            // Method declarations cannot be nested or placed inside async lambdas, and so cannot occur in an
            // asynchronous context. Therefore the IsInAsync state of the parent scope is not saved and
            // restored, just assumed to be false and reset accordingly after parsing the method body.
            Debug.Assert(!IsInAsync);

            IsInAsync = modifiers.Any((int)SyntaxKind.AsyncKeyword);

            this.ParseBlockAndExpressionBodiesWithSemicolon(out var blockBody, out var expressionBody, out var semicolon);

            IsInAsync = false;

            return _syntaxFactory.MethodDeclaration(
                attributes,
                modifiers.ToList(),
                type,
                explicitInterfaceOpt,
                identifier,
                typeParameterList,
                paramList,
                _pool.ToListAndFree(constraints),
                blockBody,
                expressionBody,
                semicolon);
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

        private ConversionOperatorDeclarationSyntax TryParseConversionOperatorDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            var point = GetResetPoint();

            try
            {
                bool haveExplicitInterfaceName = false;

                if (this.CurrentToken.Kind is not (SyntaxKind.ImplicitKeyword or SyntaxKind.ExplicitKeyword))
                {
                    SyntaxKind separatorKind = SyntaxKind.None;

                    if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                    {
                        // Scan possible ExplicitInterfaceSpecifier

                        while (true)
                        {
                            // now, scan past the next name.  if it's followed by a dot then
                            // it's part of the explicit name we're building up.  Otherwise,
                            // it should be an operator token

                            if (this.CurrentToken.Kind == SyntaxKind.OperatorKeyword)
                            {
                                // We're past any explicit interface portion
                                break;
                            }
                            else
                            {
                                using var scanNamePartPoint = GetDisposableResetPoint(resetOnDispose: false);

                                int lastTokenPosition = -1;
                                IsMakingProgress(ref lastTokenPosition, assertIfFalse: true);
                                ScanNamedTypePart();

                                if (IsDotOrColonColon() ||
                                    (IsMakingProgress(ref lastTokenPosition, assertIfFalse: false) && this.CurrentToken.Kind != SyntaxKind.OpenParenToken))
                                {
                                    haveExplicitInterfaceName = true;

                                    if (IsDotOrColonColon())
                                    {
                                        separatorKind = this.CurrentToken.Kind;
                                        EatToken();
                                    }
                                    else
                                    {
                                        separatorKind = SyntaxKind.None;
                                    }

                                }
                                else
                                {
                                    scanNamePartPoint.Reset();

                                    // We're past any explicit interface portion
                                    break;
                                }
                            }
                        }
                    }

                    bool possibleConversion;

                    if (this.CurrentToken.Kind != SyntaxKind.OperatorKeyword ||
                        (haveExplicitInterfaceName && separatorKind is not SyntaxKind.DotToken))
                    {
                        possibleConversion = false;
                    }
                    else if (this.PeekToken(1).Kind is SyntaxKind.CheckedKeyword or SyntaxKind.UncheckedKeyword)
                    {
                        possibleConversion = !SyntaxFacts.IsAnyOverloadableOperator(this.PeekToken(2).Kind);
                    }
                    else
                    {
                        possibleConversion = !SyntaxFacts.IsAnyOverloadableOperator(this.PeekToken(1).Kind);
                    }

                    this.Reset(ref point);

                    if (!possibleConversion)
                    {
                        return null;
                    }
                }

                var style = this.CurrentToken.Kind is SyntaxKind.ImplicitKeyword or SyntaxKind.ExplicitKeyword
                    ? this.EatToken()
                    : this.EatToken(SyntaxKind.ExplicitKeyword);

                ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt = tryParseExplicitInterfaceSpecifier();
                Debug.Assert(!style.IsMissing || haveExplicitInterfaceName == explicitInterfaceOpt is not null);

                SyntaxToken opKeyword;
                TypeSyntax type;

                if (!style.IsMissing && explicitInterfaceOpt is not null && this.CurrentToken.Kind != SyntaxKind.OperatorKeyword && style.TrailingTrivia.Any((int)SyntaxKind.EndOfLineTrivia))
                {
                    // Not likely an explicit interface implementation. Likely a beginning of the next member on the next line.
                    this.Reset(ref point);
                    style = this.EatToken();
                    explicitInterfaceOpt = null;
                    opKeyword = this.EatToken(SyntaxKind.OperatorKeyword);
                    type = this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_IdentifierExpected);

                    return _syntaxFactory.ConversionOperatorDeclaration(
                        attributes,
                        modifiers.ToList(),
                        style,
                        explicitInterfaceOpt,
                        opKeyword,
                        checkedKeyword: null,
                        type,
                        _syntaxFactory.ParameterList(
                            SyntaxFactory.MissingToken(SyntaxKind.OpenParenToken),
                            parameters: default,
                            SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken)),
                        body: null,
                        expressionBody: null,
                        semicolonToken: SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken));
                }

                opKeyword = this.EatToken(SyntaxKind.OperatorKeyword);
                var checkedKeyword = TryEatCheckedOrHandleUnchecked(ref opKeyword);

                this.Release(ref point);
                point = GetResetPoint();

                bool couldBeParameterList = this.CurrentToken.Kind == SyntaxKind.OpenParenToken;
                type = this.ParseType();

                if (couldBeParameterList && type is TupleTypeSyntax { Elements: { Count: 2, SeparatorCount: 1 } } tupleType &&
                    tupleType.Elements.GetSeparator(0).IsMissing && tupleType.Elements[1].IsMissing &&
                    this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
                {
                    // It looks like the type is missing and we parsed parameter list as the type. Recover.
                    this.Reset(ref point);
                    type = ParseIdentifierName();
                }

                var paramList = this.ParseParenthesizedParameterList(forExtension: false);

                this.ParseBlockAndExpressionBodiesWithSemicolon(out var blockBody, out var expressionBody, out var semicolon);

                return _syntaxFactory.ConversionOperatorDeclaration(
                    attributes,
                    modifiers.ToList(),
                    style,
                    explicitInterfaceOpt,
                    opKeyword,
                    checkedKeyword,
                    type,
                    paramList,
                    blockBody,
                    expressionBody,
                    semicolon);
            }
            finally
            {
                this.Release(ref point);
            }

            ExplicitInterfaceSpecifierSyntax tryParseExplicitInterfaceSpecifier()
            {
                if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
                {
                    return null;
                }

                NameSyntax explicitInterfaceName = null;
                SyntaxToken separator = null;

                while (true)
                {
                    // now, scan past the next name.  if it's followed by a dot then
                    // it's part of the explicit name we're building up.  Otherwise,
                    // it should be an operator token

                    bool isPartOfInterfaceName;
                    using (GetDisposableResetPoint(resetOnDispose: true))
                    {
                        if (this.CurrentToken.Kind == SyntaxKind.OperatorKeyword)
                        {
                            isPartOfInterfaceName = false;
                        }
                        else
                        {
                            int lastTokenPosition = -1;
                            IsMakingProgress(ref lastTokenPosition, assertIfFalse: true);
                            ScanNamedTypePart();
                            isPartOfInterfaceName = IsDotOrColonColon() ||
                                (IsMakingProgress(ref lastTokenPosition, assertIfFalse: false) && this.CurrentToken.Kind != SyntaxKind.OpenParenToken);
                        }
                    }

                    if (!isPartOfInterfaceName)
                    {
                        // We're past any explicit interface portion
                        if (separator?.Kind == SyntaxKind.ColonColonToken)
                        {
                            separator = this.AddError(separator, ErrorCode.ERR_AliasQualAsExpression);
                            separator = this.ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                        }

                        break;
                    }
                    else
                    {
                        // If we saw a . or :: then we must have something explicit.
                        AccumulateExplicitInterfaceName(ref explicitInterfaceName, ref separator);
                    }
                }

                if (explicitInterfaceName is null)
                {
                    return null;
                }

                if (separator.Kind != SyntaxKind.DotToken)
                {
                    separator = WithAdditionalDiagnostics(separator, GetExpectedTokenError(SyntaxKind.DotToken, separator.Kind, separator.GetLeadingTriviaWidth(), separator.Width));
                    separator = ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                }

                return _syntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceName, separator);
            }
        }

        private SyntaxToken TryEatCheckedOrHandleUnchecked(ref SyntaxToken operatorKeyword)
        {
            if (CurrentToken.Kind == SyntaxKind.UncheckedKeyword)
            {
                // if we encounter `operator unchecked`, we place the `unchecked` as skipped trivia on `operator`
                var misplacedToken = this.AddError(this.EatToken(), ErrorCode.ERR_MisplacedUnchecked);
                operatorKeyword = AddTrailingSkippedSyntax(operatorKeyword, misplacedToken);
                return null;
            }

            return TryEatToken(SyntaxKind.CheckedKeyword);
        }

        private MemberDeclarationSyntax ParseOperatorDeclaration(
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt)
        {
            // We can get here after seeing `explicit` or `implicit` or `operator`.  `ret-type explicit op ...` is not
            // legal though.
            var firstToken = this.CurrentToken;
            if (firstToken.Kind is SyntaxKind.ExplicitKeyword or SyntaxKind.ImplicitKeyword &&
                this.PeekToken(1).Kind is SyntaxKind.OperatorKeyword)
            {
                var conversionOperator = TryParseConversionOperatorDeclaration(attributes, modifiers);
                if (conversionOperator is not null)
                {
                    // We need to ensure the type syntax the user provided gets an appropriate error and is placed as
                    // leading skipped trivia for the explicit/implicit keyword.
                    var newImplicitOrExplicitKeyword = AddLeadingSkippedSyntax(
                        conversionOperator.ImplicitOrExplicitKeyword,
                        AddError(type, ErrorCode.ERR_BadOperatorSyntax, firstToken.Text));
                    return conversionOperator.Update(
                        conversionOperator.AttributeLists,
                        conversionOperator.Modifiers,
                        newImplicitOrExplicitKeyword,
                        conversionOperator.ExplicitInterfaceSpecifier,
                        conversionOperator.OperatorKeyword,
                        conversionOperator.CheckedKeyword,
                        conversionOperator.Type,
                        conversionOperator.ParameterList,
                        conversionOperator.Body,
                        conversionOperator.ExpressionBody,
                        conversionOperator.SemicolonToken);
                }
            }

            var opKeyword = this.EatToken(SyntaxKind.OperatorKeyword);
            var checkedKeyword = TryEatCheckedOrHandleUnchecked(ref opKeyword);
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
                if (this.CurrentToken.Kind is SyntaxKind.ImplicitKeyword or SyntaxKind.ExplicitKeyword)
                {
                    // Grab the offset and width before we consume the invalid keyword and change our position.
                    (opTokenErrorOffset, opTokenErrorWidth) = (0, this.CurrentToken.Width);
                    opToken = this.ConvertToMissingWithTrailingTrivia(this.EatToken(), SyntaxKind.PlusToken);
                    Debug.Assert(opToken.IsMissing); // ConvertToMissingWithTrailingTrivia should have converted to a missing token.

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
                    // Consume whatever follows the operator keyword as the operator token.  If it is not we'll add an
                    // error below (when we can guess the arity). Handle .. as well so we can give the user a good
                    // message if they do `operator ..`
                    opToken = IsAtDotDotToken() ? EatDotDotToken() : EatToken();
                    Debug.Assert(!opToken.IsMissing);
                    opTokenErrorOffset = opToken.GetLeadingTriviaWidth();
                    opTokenErrorWidth = opToken.Width;
                }
            }

            // check for >> and >>>
            if (opToken.Kind == SyntaxKind.GreaterThanToken)
            {
                var tk = this.CurrentToken;

                if (tk.Kind == SyntaxKind.GreaterThanToken)
                {
                    if (NoTriviaBetween(opToken, tk)) // no trailing trivia and no leading trivia
                    {
                        var opToken2 = this.EatToken();
                        tk = this.CurrentToken;

                        if (tk.Kind == SyntaxKind.GreaterThanToken &&
                            NoTriviaBetween(opToken2, tk)) // no trailing trivia and no leading trivia
                        {
                            opToken2 = this.EatToken();
                            opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), SyntaxKind.GreaterThanGreaterThanGreaterThanToken, opToken2.GetTrailingTrivia());
                            opTokenErrorWidth = opToken.Width;
                        }
                        else if (tk.Kind == SyntaxKind.GreaterThanEqualsToken &&
                                 NoTriviaBetween(opToken2, tk)) // no trailing trivia and no leading trivia
                        {
                            opToken2 = this.EatToken();
                            opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken, opToken2.GetTrailingTrivia());
                            opTokenErrorWidth = opToken.Width;
                        }
                        else
                        {
                            opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), SyntaxKind.GreaterThanGreaterThanToken, opToken2.GetTrailingTrivia());
                            opTokenErrorWidth = opToken.Width;
                        }
                    }
                }
                else if (tk.Kind == SyntaxKind.GreaterThanEqualsToken &&
                         NoTriviaBetween(opToken, tk)) // no trailing trivia and no leading trivia
                {
                    var opToken2 = this.EatToken();
                    opToken = SyntaxFactory.Token(opToken.GetLeadingTrivia(), SyntaxKind.GreaterThanGreaterThanEqualsToken, opToken2.GetTrailingTrivia());
                    opTokenErrorWidth = opToken.Width;
                }
            }

            var opKind = opToken.Kind;
            var paramList = this.ParseParenthesizedParameterList(forExtension: false);

            switch (paramList.Parameters.Count)
            {
                case 1:
                    if (opToken.IsMissing || !(SyntaxFacts.IsOverloadableUnaryOperator(opKind) || SyntaxFacts.IsOverloadableCompoundAssignmentOperator(opKind)))
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
                        if (opKind is not (SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken) || paramList.Parameters.Count != 0)
                        {
                            opToken = this.AddError(opToken, ErrorCode.ERR_BadUnOpArgs, SyntaxFacts.GetText(opKind));
                        }
                    }
                    else if (SyntaxFacts.IsOverloadableCompoundAssignmentOperator(opKind))
                    {
                        opToken = this.AddError(opToken, ErrorCode.ERR_BadCompoundAssignmentOpArgs, SyntaxFacts.GetText(opKind));
                    }
                    else
                    {
                        opToken = this.AddError(opToken, ErrorCode.ERR_OvlOperatorExpected);
                    }

                    break;
            }

            this.ParseBlockAndExpressionBodiesWithSemicolon(out var blockBody, out var expressionBody, out var semicolon);

            // if the operator is invalid, then switch it to plus (which will work either way) so that
            // we can finish building the tree
            if (!(opKind == SyntaxKind.IsKeyword ||
                  SyntaxFacts.IsOverloadableUnaryOperator(opKind) ||
                  SyntaxFacts.IsOverloadableBinaryOperator(opKind) ||
                  SyntaxFacts.IsOverloadableCompoundAssignmentOperator(opKind)))
            {
                opToken = ConvertToMissingWithTrailingTrivia(opToken, SyntaxKind.PlusToken);
            }

            return _syntaxFactory.OperatorDeclaration(
                attributes,
                modifiers.ToList(),
                type,
                explicitInterfaceOpt,
                opKeyword,
                checkedKeyword,
                opToken,
                paramList,
                blockBody,
                expressionBody,
                semicolon);
        }

        private IndexerDeclarationSyntax ParseIndexerDeclaration(
            SyntaxList<AttributeListSyntax> attributes,
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
                semicolon = this.EatToken(SyntaxKind.SemicolonToken);
            }
            else
            {
                accessorList = this.ParseAccessorList(AccessorDeclaringKind.Indexer);
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
            SyntaxList<AttributeListSyntax> attributes,
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

            // Error recovery: add an errant semicolon to the identifier token and keep going.
            if (this.CurrentToken.Kind is SyntaxKind.SemicolonToken)
            {
                identifier = AddTrailingSkippedSyntax(identifier, this.EatTokenEvenWithIncorrectKind(SyntaxKind.OpenBraceToken));
            }

            // We know we are parsing a property because we have seen either an open brace or an arrow token
            Debug.Assert(IsStartOfPropertyBody(this.CurrentToken.Kind));

            var accessorList = this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                ? this.ParseAccessorList(AccessorDeclaringKind.Property)
                : null;

            ArrowExpressionClauseSyntax expressionBody = null;
            EqualsValueClauseSyntax initializer = null;

            // Check for expression body
            if (this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                using (new FieldKeywordContext(this, isInFieldKeywordContext: true))
                {
                    expressionBody = this.ParseArrowExpressionClause();
                }
            }
            // Check if we have an initializer
            else if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                var equals = this.EatToken(SyntaxKind.EqualsToken);
                var value = this.ParseVariableInitializer();
                initializer = _syntaxFactory.EqualsValueClause(equals, value: value);
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

        private readonly ref struct FieldKeywordContext : IDisposable
        {
            private readonly LanguageParser _parser;
            private readonly bool _previousInFieldKeywordContext;

            public FieldKeywordContext(LanguageParser parser, bool isInFieldKeywordContext)
            {
                _parser = parser;
                _previousInFieldKeywordContext = parser.IsInFieldKeywordContext;
                _parser.IsInFieldKeywordContext = isInFieldKeywordContext;
            }

            public void Dispose()
            {
                _parser.IsInFieldKeywordContext = _previousInFieldKeywordContext;
            }
        }

        private readonly ref struct AsyncContext : IDisposable
        {
            private readonly LanguageParser _parser;
            private readonly bool _previousInAsync;

            public AsyncContext(LanguageParser parser, bool isInAsync)
            {
                _parser = parser;
                _previousInAsync = parser.IsInAsync;
                _parser.IsInAsync = isInAsync;
            }

            public void Dispose()
            {
                _parser.IsInAsync = _previousInAsync;
            }
        }

        private enum AccessorDeclaringKind
        {
            Property,
            Indexer,
            Event,
        }

        private AccessorListSyntax ParseAccessorList(AccessorDeclaringKind declaringKind)
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var accessors = default(SyntaxList<AccessorDeclarationSyntax>);

            if (!openBrace.IsMissing || !this.IsTerminator())
            {
                // parse property accessors
                var builder = _pool.Allocate<AccessorDeclarationSyntax>();

                while (true)
                {
                    if (this.CurrentToken.Kind == SyntaxKind.CloseBraceToken)
                    {
                        break;
                    }
                    else if (this.IsPossibleAccessor())
                    {
                        var acc = this.ParseAccessorDeclaration(declaringKind);
                        builder.Add(acc);
                    }
                    else if (this.SkipBadAccessorListTokens(ref openBrace, builder,
                        declaringKind == AccessorDeclaringKind.Event ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected) == PostSkipAction.Abort)
                    {
                        break;
                    }
                }

                accessors = _pool.ToListAndFree(builder);
            }

            return _syntaxFactory.AccessorList(
                openBrace,
                accessors,
                this.EatToken(SyntaxKind.CloseBraceToken));
        }

        private ArrowExpressionClauseSyntax ParseArrowExpressionClause()
        {
            return _syntaxFactory.ArrowExpressionClause(
                this.EatToken(SyntaxKind.EqualsGreaterThanToken),
                ParsePossibleRefExpression());
        }

        private ExpressionSyntax ParsePossibleRefExpression()
        {
            // check for lambda expression with explicit ref return type: `ref int () => { ... }`
            var refKeyword = this.CurrentToken.Kind == SyntaxKind.RefKeyword && !this.IsPossibleLambdaExpression(Precedence.Expression)
                ? this.EatToken()
                : null;

            var expression = this.ParseExpressionCore();
            return refKeyword == null ? expression : _syntaxFactory.RefExpression(refKeyword, expression);
        }

        private PostSkipAction SkipBadAccessorListTokens(ref SyntaxToken openBrace, SyntaxListBuilder<AccessorDeclarationSyntax> list, ErrorCode error)
        {
            return this.SkipBadListTokensWithErrorCode(ref openBrace, list,
                static p => p.CurrentToken.Kind != SyntaxKind.CloseBraceToken && !p.IsPossibleAccessor(),
                static p => p.IsTerminator(),
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

            if (GetModifierExcludingScoped(this.CurrentToken) == DeclarationModifiers.None)
            {
                return false;
            }

            var peekIndex = 1;
            while (GetModifierExcludingScoped(this.PeekToken(peekIndex)) != DeclarationModifiers.None)
            {
                peekIndex++;
            }

            var token = this.PeekToken(peekIndex);
            if (token.Kind is SyntaxKind.CloseBraceToken or SyntaxKind.EndOfFileToken)
            {
                // If we see "{ get { } public }
                // then we will think that "public" likely starts an accessor.
                return true;
            }

            switch (token.ContextualKind)
            {
                case SyntaxKind.GetKeyword:
                case SyntaxKind.SetKeyword:
                case SyntaxKind.InitKeyword:
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
            Func<LanguageParser, SyntaxKind, bool> abortFunction,
            SyntaxKind expected,
            SyntaxKind closeKind = SyntaxKind.None)
            where T : CSharpSyntaxNode
            where TNode : CSharpSyntaxNode
        {
            // We're going to cheat here and pass the underlying SyntaxListBuilder of "list" to the helper method so that
            // it can append skipped trivia to the last element, regardless of whether that element is a node or a token.
            GreenNode trailingTrivia;
            var action = this.SkipBadListTokensWithExpectedKindHelper(list.UnderlyingBuilder, isNotExpectedFunction, abortFunction, expected, closeKind, out trailingTrivia);
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
            Func<LanguageParser, SyntaxKind, bool> abortFunction,
            SyntaxKind expected,
            SyntaxKind closeKind,
            out GreenNode trailingTrivia)
        {
            if (list.Count == 0)
            {
                return SkipBadTokensWithExpectedKind(isNotExpectedFunction, abortFunction, expected, closeKind, out trailingTrivia);
            }
            else
            {
                GreenNode lastItemTrailingTrivia;
                var action = SkipBadTokensWithExpectedKind(isNotExpectedFunction, abortFunction, expected, closeKind, out lastItemTrailingTrivia);
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
            Func<LanguageParser, SyntaxKind, bool> abortFunction,
            SyntaxKind expected,
            SyntaxKind closeKind,
            out GreenNode trailingTrivia)
        {
            var nodes = _pool.Allocate();
            bool first = true;
            var action = PostSkipAction.Continue;
            while (isNotExpectedFunction(this))
            {
                if (abortFunction(this, closeKind) || this.IsTerminator())
                {
                    action = PostSkipAction.Abort;
                    break;
                }

                var token = (first && !this.CurrentToken.ContainsDiagnostics) ? this.EatTokenEvenWithIncorrectKind(expected) : this.EatToken();
                first = false;
                nodes.Add(token);
            }

            trailingTrivia = _pool.ToTokenListAndFree(nodes).Node;
            return action;
        }

        private PostSkipAction SkipBadTokensWithErrorCode(
            Func<LanguageParser, bool> isNotExpectedFunction,
            Func<LanguageParser, bool> abortFunction,
            ErrorCode errorCode,
            out GreenNode trailingTrivia)
        {
            var nodes = _pool.Allocate();
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

            trailingTrivia = _pool.ToTokenListAndFree(nodes).Node;
            return action;
        }

        private AccessorDeclarationSyntax ParseAccessorDeclaration(AccessorDeclaringKind declaringKind)
        {
            if (this.IsIncrementalAndFactoryContextMatches && SyntaxFacts.IsAccessorDeclaration(this.CurrentNodeKind))
            {
                return (AccessorDeclarationSyntax)this.EatNode();
            }

            using var __ = new FieldKeywordContext(this, isInFieldKeywordContext: declaringKind is AccessorDeclaringKind.Property);

            var accMods = _pool.Allocate();

            var accAttrs = this.ParseAttributeDeclarations(inExpressionContext: false);
            this.ParseModifiers(accMods, forAccessors: true, forTopLevelStatements: false, isPossibleTypeDeclaration: out _);

            var accessorName = this.EatToken(SyntaxKind.IdentifierToken,
                declaringKind == AccessorDeclaringKind.Event ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected);
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
                        declaringKind == AccessorDeclaringKind.Event ? ErrorCode.ERR_AddOrRemoveExpected : ErrorCode.ERR_GetOrSetExpected);
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
                    out blockBody, out expressionBody, out semicolon);
            }
            else if (currentTokenIsSemicolon)
            {
                semicolon = EatAccessorSemicolon();
            }
            else
            {
                // We didn't get something we recognized.  If we got an accessor type we 
                // recognized (i.e. get/set/init/add/remove) then try to parse out a block.
                // Only do this if it doesn't seem like we're at the end of the accessor/property.
                // for example, if we have "get set", don't actually try to parse out the 
                // block.  Otherwise we'll consume the 'set'.  In that case, just end the
                // current accessor with a semicolon so we can properly consume the next
                // in the calling method's loop.
                if (accessorKind != SyntaxKind.UnknownAccessorDeclaration)
                {
                    if (!IsTerminator())
                    {
                        blockBody = this.ParseMethodOrAccessorBodyBlock(attributes: default, isAccessorBody: true);
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
                accessorKind,
                accAttrs,
                _pool.ToTokenListAndFree(accMods),
                accessorName,
                blockBody,
                expressionBody,
                semicolon);
        }

        private SyntaxToken EatAccessorSemicolon()
            => this.EatToken(SyntaxKind.SemicolonToken,
                IsFeatureEnabled(MessageID.IDS_FeatureExpressionBodiedAccessor)
                    ? ErrorCode.ERR_SemiOrLBraceOrArrowExpected
                    : ErrorCode.ERR_SemiOrLBraceExpected);

        private static SyntaxKind GetAccessorKind(SyntaxToken accessorName)
        {
            return accessorName.ContextualKind switch
            {
                SyntaxKind.GetKeyword => SyntaxKind.GetAccessorDeclaration,
                SyntaxKind.SetKeyword => SyntaxKind.SetAccessorDeclaration,
                SyntaxKind.InitKeyword => SyntaxKind.InitAccessorDeclaration,
                SyntaxKind.AddKeyword => SyntaxKind.AddAccessorDeclaration,
                SyntaxKind.RemoveKeyword => SyntaxKind.RemoveAccessorDeclaration,
                _ => SyntaxKind.UnknownAccessorDeclaration,
            };
        }

        internal ParameterListSyntax ParseParenthesizedParameterList(bool forExtension)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseParameterList(this.CurrentNode as CSharp.Syntax.ParameterListSyntax, allowOptionalIdentifier: forExtension))
            {
                return (ParameterListSyntax)this.EatNode();
            }

            var parameters = this.ParseParameterList(out var open, out var close, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken, forExtension);
            return _syntaxFactory.ParameterList(open, parameters, close);
        }

        internal BracketedParameterListSyntax ParseBracketedParameterList()
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseBracketedParameterList(this.CurrentNode as CSharp.Syntax.BracketedParameterListSyntax))
            {
                return (BracketedParameterListSyntax)this.EatNode();
            }

            var parameters = this.ParseParameterList(out var open, out var close, SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken, forExtension: false);
            return _syntaxFactory.BracketedParameterList(open, parameters, close);
        }

        private static bool CanReuseParameterList(Syntax.ParameterListSyntax list, bool allowOptionalIdentifier)
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
                if (!CanReuseParameter(parameter, allowOptionalIdentifier))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CanReuseBracketedParameterList(Syntax.BracketedParameterListSyntax list)
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
                if (!CanReuseParameter(parameter, allowOptionalIdentifier: false))
                {
                    return false;
                }
            }

            return true;
        }

        private SeparatedSyntaxList<ParameterSyntax> ParseParameterList(
            out SyntaxToken open,
            out SyntaxToken close,
            SyntaxKind openKind,
            SyntaxKind closeKind,
            bool forExtension)
        {
            open = this.EatToken(openKind);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfParameterList;

            Func<LanguageParser, ParameterSyntax> parseElement = forExtension
                    ? static @this => @this.ParseParameter(allowOptionalIdentifier: true)
                    : static @this => @this.ParseParameter(allowOptionalIdentifier: false);

            var parameters = ParseCommaSeparatedSyntaxList(
                ref open,
                closeKind,
                static @this => @this.IsPossibleParameter(),
                parseElement,
                skipBadParameterListTokens,
                allowTrailingSeparator: false,
                requireOneElement: forExtension, // For extension declarations, we require at least one receiver parameter
                allowSemicolonAsSeparator: false);

            _termState = saveTerm;
            close = this.EatToken(closeKind);

            return parameters;

            static PostSkipAction skipBadParameterListTokens(
                LanguageParser @this, ref SyntaxToken open, SeparatedSyntaxListBuilder<ParameterSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleParameter(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
        }

        private bool IsEndOfParameterList()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken or SyntaxKind.SemicolonToken;
        }

        private bool IsPossibleParameter()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.OpenBracketToken: // attribute
                case SyntaxKind.ArgListKeyword:
                case SyntaxKind.OpenParenToken:   // tuple
                case SyntaxKind.DelegateKeyword when IsFunctionPointerStart(): // Function pointer type
                    return true;

                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();

                default:
                    return IsParameterModifierExcludingScoped(this.CurrentToken) || IsPossibleScopedKeyword(isFunctionPointerParameter: false) || IsPredefinedType(this.CurrentToken.Kind);
            }
        }

        private static bool CanReuseParameter(CSharp.Syntax.ParameterSyntax parameter, bool allowOptionalIdentifier)
        {
            if (parameter == null)
            {
                return false;
            }

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

            // We can only reuse parameters without identifiers (found in extension declarations) in context that allow optional identifiers.
            // The reverse is fine though.  Normal parameters (from non extensions) can be re-used into an extension declaration
            // as all normal parameters are legal extension parameters.
            if (!allowOptionalIdentifier && parameter.Identifier.Kind() == SyntaxKind.None)
            {
                return false;
            }

            return true;
        }

#nullable enable

        private ParameterSyntax ParseParameter(bool allowOptionalIdentifier)
        {
            if (this.IsIncrementalAndFactoryContextMatches && CanReuseParameter(this.CurrentNode as Syntax.ParameterSyntax, allowOptionalIdentifier))
            {
                return (ParameterSyntax)this.EatNode();
            }

            var attributes = this.ParseAttributeDeclarations(inExpressionContext: false);

            var modifiers = _pool.Allocate();
            this.ParseParameterModifiers(modifiers, isFunctionPointerParameter: false, isLambdaParameter: false);

            if (this.CurrentToken.Kind == SyntaxKind.ArgListKeyword)
            {
                // We store an __arglist parameter as a parameter with null type and whose 
                // .Identifier has the kind ArgListKeyword.
                return _syntaxFactory.Parameter(
                    attributes, modifiers.ToList(), type: null, this.EatToken(SyntaxKind.ArgListKeyword), @default: null);
            }

            var type = this.ParseType(mode: ParseTypeMode.Parameter);

            SyntaxToken? identifier;
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken && IsCurrentTokenWhereOfConstraintClause())
            {
                identifier = allowOptionalIdentifier ? null : this.AddError(CreateMissingIdentifierToken(), ErrorCode.ERR_IdentifierExpected);
            }
            else
            {
                // The receiver parameter on an extension declaration may have a name or not
                identifier = allowOptionalIdentifier && this.CurrentToken.Kind != SyntaxKind.IdentifierToken
                    ? null
                    : this.ParseIdentifierToken();
            }

            // When the user type "int goo[]", give them a useful error
            if (identifier is not null && this.CurrentToken.Kind is SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind is SyntaxKind.CloseBracketToken)
            {
                identifier = AddTrailingSkippedSyntax(identifier, SyntaxList.List(
                    this.AddError(this.EatToken(), ErrorCode.ERR_BadArraySyntax),
                    this.EatToken()));
            }

            var equalsToken = TryEatToken(SyntaxKind.EqualsToken);

            return _syntaxFactory.Parameter(
                attributes,
                _pool.ToTokenListAndFree(modifiers),
                type,
                identifier,
                equalsToken == null ? null : _syntaxFactory.EqualsValueClause(equalsToken, this.ParseExpressionCore()));
        }

        internal static bool NoTriviaBetween(SyntaxToken token1, SyntaxToken token2)
            => token1.GetTrailingTriviaWidth() == 0 && token2.GetLeadingTriviaWidth() == 0;

#nullable disable

        private static bool IsParameterModifierIncludingScoped(SyntaxToken token)
            => IsParameterModifierExcludingScoped(token) || token.ContextualKind == SyntaxKind.ScopedKeyword;

        private static bool IsParameterModifierExcludingScoped(SyntaxToken token)
        {
            switch (token.Kind)
            {
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.ParamsKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                    return true;
            }

            return false;
        }

        private void ParseParameterModifiers(SyntaxListBuilder modifiers, bool isFunctionPointerParameter, bool isLambdaParameter)
        {
            bool tryScoped = true;

            while (IsParameterModifierExcludingScoped(this.CurrentToken))
            {
                if (this.CurrentToken.Kind is SyntaxKind.RefKeyword or SyntaxKind.OutKeyword or SyntaxKind.InKeyword or SyntaxKind.ReadOnlyKeyword)
                {
                    tryScoped = false;
                }

                modifiers.Add(this.EatToken());
            }

            if (tryScoped)
            {
                SyntaxToken scopedKeyword = ParsePossibleScopedKeyword(isFunctionPointerParameter, isLambdaParameter);

                if (scopedKeyword != null)
                {
                    modifiers.Add(scopedKeyword);

                    // Look if ref/out/in/readonly are next
                    while (this.CurrentToken.Kind is SyntaxKind.RefKeyword or SyntaxKind.OutKeyword or SyntaxKind.InKeyword or SyntaxKind.ReadOnlyKeyword)
                    {
                        modifiers.Add(this.EatToken());
                    }
                }
            }
        }

        private FieldDeclarationSyntax ParseFixedSizeBufferDeclaration(
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxKind parentKind)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.FixedKeyword);

            modifiers.Add(this.EatToken());

            var type = this.ParseType();

            return _syntaxFactory.FieldDeclaration(
                attributes, modifiers.ToList(),
                _syntaxFactory.VariableDeclaration(
                    type, this.ParseFieldDeclarationVariableDeclarators(type, VariableFlags.Fixed, parentKind)),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private MemberDeclarationSyntax ParseEventDeclaration(
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            SyntaxKind parentKind)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.EventKeyword);

            var eventToken = this.EatToken();
            var type = this.ParseType();

            return IsFieldDeclaration(isEvent: true, isGlobalScriptLevel: parentKind == SyntaxKind.CompilationUnit)
                ? this.ParseEventFieldDeclaration(attributes, modifiers, eventToken, type, parentKind)
                : this.ParseEventDeclarationWithAccessors(attributes, modifiers, eventToken, type);
        }

        private EventDeclarationSyntax ParseEventDeclarationWithAccessors(
            SyntaxList<AttributeListSyntax> attributes,
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
            if (explicitInterfaceOpt != null && this.CurrentToken.Kind is not SyntaxKind.OpenBraceToken and not SyntaxKind.SemicolonToken)
            {
                Debug.Assert(typeParameterList == null, "Exit condition of ParseMemberName in this scenario");
                return _syntaxFactory.EventDeclaration(
                    attributes,
                    modifiers.ToList(),
                    eventToken,
                    type,
                    //already has an appropriate error attached
                    explicitInterfaceOpt,
                    // No need for a diagnostic, ParseMemberName has already added one.
                    identifierOrThisOpt == null ? CreateMissingIdentifierToken() : identifierOrThisOpt,
                    _syntaxFactory.AccessorList(
                        SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                        default(SyntaxList<AccessorDeclarationSyntax>),
                        SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken)),
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
                accessorList = this.ParseAccessorList(AccessorDeclaringKind.Event);
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
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxListBuilder modifiers,
            TypeSyntax type,
            SyntaxKind parentKind)
        {
            var variables = this.ParseFieldDeclarationVariableDeclarators(type, flags: VariableFlags.LocalOrField, parentKind);

            // Make 'scoped' part of the type when it is the last token in the modifiers list
            if (modifiers is [.., SyntaxToken { Kind: SyntaxKind.ScopedKeyword } scopedKeyword])
            {
                type = _syntaxFactory.ScopedType(scopedKeyword, type);
                modifiers.RemoveLast();
            }

            return _syntaxFactory.FieldDeclaration(
                attributes,
                modifiers.ToList(),
                _syntaxFactory.VariableDeclaration(type, variables),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private EventFieldDeclarationSyntax ParseEventFieldDeclaration(
            SyntaxList<AttributeListSyntax> attributes,
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

            var variables = this.ParseFieldDeclarationVariableDeclarators(type, flags: 0, parentKind);
            if (this.CurrentToken.Kind == SyntaxKind.DotToken)
            {
                // Better error message for confusing event situation.
                eventToken = this.AddError(eventToken, ErrorCode.ERR_ExplicitEventFieldImpl);
            }

            return _syntaxFactory.EventFieldDeclaration(
                attributes,
                modifiers.ToList(),
                eventToken,
                _syntaxFactory.VariableDeclaration(type, variables),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private bool IsEndOfFieldDeclaration()
        {
            return this.CurrentToken.Kind == SyntaxKind.SemicolonToken;
        }

        private SeparatedSyntaxList<VariableDeclaratorSyntax> ParseFieldDeclarationVariableDeclarators(
            TypeSyntax type, VariableFlags flags, SyntaxKind parentKind)
        {
            // Although we try parse variable declarations in contexts where they are not allowed (non-interactive top-level or a namespace) 
            // the reported errors should take into consideration whether or not one expects them in the current context.
            bool variableDeclarationsExpected =
                parentKind is not SyntaxKind.NamespaceDeclaration and not SyntaxKind.FileScopedNamespaceDeclaration &&
                (parentKind != SyntaxKind.CompilationUnit || IsScript);

            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFieldDeclaration;

            ParseVariableDeclarators(
                type,
                flags,
                variables,
                variableDeclarationsExpected,
                allowLocalFunctions: false,
                // A field declaration doesn't have a `(...)` construct.  So no need to stop if we hit a close paren
                // after a declarator.  Let normal error recovery kick in.
                stopOnCloseParen: false,
                attributes: default,
                mods: default,
                out var localFunction);
            Debug.Assert(localFunction == null);

            _termState = saveTerm;
            return _pool.ToListAndFree(variables);
        }

        private void ParseVariableDeclarators(
            TypeSyntax type,
            VariableFlags flags,
            SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables,
            bool variableDeclarationsExpected,
            bool allowLocalFunctions,
            bool stopOnCloseParen,
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxList<SyntaxToken> mods,
            out LocalFunctionStatementSyntax localFunction)
        {
            variables.Add(
                this.ParseVariableDeclarator(
                    type,
                    flags,
                    isFirst: true,
                    allowLocalFunctions: allowLocalFunctions,
                    attributes: attributes,
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
                else if (stopOnCloseParen && this.CurrentToken.Kind == SyntaxKind.CloseParenToken)
                {
                    break;
                }
                else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    // If we see `for (int i = 0, i < ...` then we do not want to consume the second 'i' as the next declarator as it
                    // is more likely that the user meant to write `for (int i = 0; i < ...` instead and accidentally
                    // used a comma instead of a semicolon.
                    //
                    // Note: the legal forms that we must keep parsing as a variable declarator are:
                    //
                    //      for (int i = 0, j, k; ...       // identifier comma
                    //      for (int i = 0, j = ...         // identifier equals
                    //      for (int i = 0, j; ...          // identifier semicolon
                    //
                    // We also accept: `for (int i = 0, ;` as that's likely an intermediary state prior to writing the
                    // next variable. Anything else we'll treat as as more likely to be the following conditional.

                    if (flags.HasFlag(VariableFlags.ForStatement) && this.PeekToken(1).Kind != SyntaxKind.SemicolonToken)
                    {
                        var isLegalVariableDeclaratorStart =
                            IsTrueIdentifier(this.PeekToken(1)) &&
                            this.PeekToken(2).Kind is SyntaxKind.CommaToken or SyntaxKind.EqualsToken or SyntaxKind.SemicolonToken;

                        if (!isLegalVariableDeclaratorStart)
                            break;
                    }

                    variables.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    variables.Add(
                        this.ParseVariableDeclarator(
                            type,
                            flags,
                            isFirst: false,
                            allowLocalFunctions: false,
                            attributes: attributes,
                            mods: mods,
                            localFunction: out localFunction));

                    Debug.Assert(localFunction is null);
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
                static p => p.CurrentToken.Kind != SyntaxKind.CommaToken,
                static (p, _) => p.CurrentToken.Kind == SyntaxKind.SemicolonToken,
                expected);
        }

        [Flags]
        private enum VariableFlags
        {
            None = 0,
            Fixed = 0x01,
            Const = 0x02,
            LocalOrField = 0x04,
            ForStatement = 0x08,
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
                    case SyntaxKind.InitAccessorDeclaration:
                        return ((CSharp.Syntax.AccessorDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                        return ((CSharp.Syntax.TypeDeclarationSyntax)decl).Modifiers;
                    case SyntaxKind.DelegateDeclaration:
                        return ((CSharp.Syntax.DelegateDeclarationSyntax)decl).Modifiers;
                }
            }

            return default(SyntaxTokenList);
        }

        private static bool WasFirstVariable(CSharp.Syntax.VariableDeclaratorSyntax variable)
        {
            if (GetOldParent(variable) is CSharp.Syntax.VariableDeclarationSyntax parent)
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
                flags |= VariableFlags.LocalOrField;
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
            SyntaxList<AttributeListSyntax> attributes,
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
                using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

                var currentTokenKind = this.CurrentToken.Kind;
                if (currentTokenKind == SyntaxKind.IdentifierToken && !parentType.IsMissing)
                {
                    var isAfterNewLine = parentType.GetLastToken().TrailingTrivia.Any((int)SyntaxKind.EndOfLineTrivia);
                    if (isAfterNewLine)
                    {
                        // Note: this token is free to get.  There is a cached singleton in SyntaxFactory for it.
                        var missingIdentifier = CreateMissingIdentifierToken();
                        var (offset, width) = this.GetDiagnosticSpanForMissingNodeOrToken(missingIdentifier);

                        this.EatToken();
                        currentTokenKind = this.CurrentToken.Kind;

                        var isNonEqualsBinaryToken =
                            currentTokenKind != SyntaxKind.EqualsToken &&
                            SyntaxFacts.IsBinaryExpressionOperatorToken(currentTokenKind);

                        if (currentTokenKind is SyntaxKind.DotToken or SyntaxKind.OpenParenToken or SyntaxKind.MinusGreaterThanToken ||
                            isNonEqualsBinaryToken)
                        {
                            var isPossibleLocalFunctionToken = currentTokenKind is SyntaxKind.OpenParenToken or SyntaxKind.LessThanToken;

                            // Make sure this isn't a local function
                            if (!isPossibleLocalFunctionToken || !IsLocalFunctionAfterIdentifier())
                            {
                                missingIdentifier = this.AddError(missingIdentifier, offset, width, ErrorCode.ERR_IdentifierExpected);

                                localFunction = null;
                                return _syntaxFactory.VariableDeclarator(missingIdentifier, null, null);
                            }
                        }
                    }
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
            bool isLocalOrField = (flags & VariableFlags.LocalOrField) != 0;

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

                    // check for lambda expression with explicit ref return type: `ref int () => { ... }`
                    var refKeyword = isLocalOrField && !isConst && this.CurrentToken.Kind == SyntaxKind.RefKeyword && !this.IsPossibleLambdaExpression(Precedence.Expression)
                        ? this.EatToken()
                        : null;

                    var init = this.ParseVariableInitializer();
                    initializer = _syntaxFactory.EqualsValueClause(
                        equals,
                        refKeyword == null ? init : _syntaxFactory.RefExpression(refKeyword, init));
                    break;

                case SyntaxKind.LessThanToken:
                    if (allowLocalFunctions && isFirst)
                    {
                        localFunction = TryParseLocalFunctionStatementBody(attributes, mods, parentType, name);
                        if (localFunction != null)
                        {
                            return null;
                        }
                    }
                    goto default;

                case SyntaxKind.OpenParenToken:
                    if (allowLocalFunctions && isFirst)
                    {
                        localFunction = TryParseLocalFunctionStatementBody(attributes, mods, parentType, name);
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
                    var withSeps = sizes.GetWithSeparators();
                    foreach (var item in withSeps)
                    {
                        if (item is ExpressionSyntax expression)
                        {
                            bool isOmitted = expression.Kind == SyntaxKind.OmittedArraySizeExpression;
                            if (!isFixed && !isOmitted)
                            {
                                expression = this.AddError(expression, ErrorCode.ERR_ArraySizeInDeclaration);
                            }

                            args.Add(_syntaxFactory.Argument(null, refKindKeyword: null, expression));
                        }
                        else
                        {
                            args.AddSeparator((SyntaxToken)item);
                        }
                    }

                    argumentList = _syntaxFactory.BracketedArgumentList(open, _pool.ToListAndFree(args), close);
                    if (!isFixed)
                    {
                        argumentList = this.AddError(argumentList, ErrorCode.ERR_CStyleArray);
                        // If we have "int x[] = new int[10];" then parse the initializer.
                        if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
                        {
                            goto case SyntaxKind.EqualsToken;
                        }
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
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.OpenParenToken or SyntaxKind.LessThanToken);

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            var typeParameterListOpt = this.ParseTypeParameterList();
            var paramList = ParseParenthesizedParameterList(forExtension: false);

            if (!paramList.IsMissing &&
                 (this.CurrentToken.Kind is SyntaxKind.OpenBraceToken or SyntaxKind.EqualsGreaterThanToken ||
                  this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword))
            {
                return true;
            }

            return false;
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
            return this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                ? this.ParseArrayInitializer()
                : this.ParseExpressionCore();
        }

        private bool IsPossibleVariableInitializer()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBraceToken || this.IsPossibleExpression();
        }

        private FieldDeclarationSyntax ParseConstantFieldDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers, SyntaxKind parentKind)
        {
            modifiers.Add(this.EatToken(SyntaxKind.ConstKeyword));

            var type = this.ParseType();
            return _syntaxFactory.FieldDeclaration(
                attributes,
                modifiers.ToList(),
                _syntaxFactory.VariableDeclaration(
                    type,
                    this.ParseFieldDeclarationVariableDeclarators(type, VariableFlags.Const, parentKind)),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private DelegateDeclarationSyntax ParseDelegateDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.DelegateKeyword);

            var delegateToken = this.EatToken(SyntaxKind.DelegateKeyword);
            var type = this.ParseReturnType();
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;
            var name = this.ParseIdentifierToken();
            var typeParameters = this.ParseTypeParameterList();
            var parameterList = this.ParseParenthesizedParameterList(forExtension: false);
            var constraints = default(SyntaxListBuilder<TypeParameterConstraintClauseSyntax>);

            if (this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword)
            {
                constraints = _pool.Allocate<TypeParameterConstraintClauseSyntax>();
                this.ParseTypeParameterConstraintClauses(constraints);
            }

            _termState = saveTerm;

            return _syntaxFactory.DelegateDeclaration(
                attributes,
                modifiers.ToList(),
                delegateToken,
                type,
                name,
                typeParameters,
                parameterList,
                _pool.ToListAndFree(constraints),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private EnumDeclarationSyntax ParseEnumDeclaration(SyntaxList<AttributeListSyntax> attributes, SyntaxListBuilder modifiers)
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
                baseList = _syntaxFactory.BaseList(
                    colon,
                    _pool.ToListAndFree(tmpList));
            }

            var members = default(SeparatedSyntaxList<EnumMemberDeclarationSyntax>);
            SyntaxToken semicolon;
            SyntaxToken openBrace;
            SyntaxToken closeBrace;

            if (CurrentToken.Kind == SyntaxKind.SemicolonToken)
            {
                semicolon = EatToken(SyntaxKind.SemicolonToken);
                openBrace = null;
                closeBrace = null;
            }
            else
            {
                openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

                if (!openBrace.IsMissing)
                {
                    // It's not uncommon for people to use semicolons to separate out enum members.  So be resilient to
                    // that, successfully consuming them as separators, while telling the user it needs to be a comma
                    // instead.
                    members = this.ParseCommaSeparatedSyntaxList(
                        ref openBrace,
                        SyntaxKind.CloseBraceToken,
                        static @this => @this.IsPossibleEnumMemberDeclaration(),
                        static @this => @this.ParseEnumMemberDeclaration(),
                        skipBadEnumMemberListTokens,
                        allowTrailingSeparator: true,
                        requireOneElement: false,
                        allowSemicolonAsSeparator: true);
                }

                closeBrace = this.EatToken(SyntaxKind.CloseBraceToken);
                semicolon = TryEatToken(SyntaxKind.SemicolonToken);
            }

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

            static PostSkipAction skipBadEnumMemberListTokens(
                LanguageParser @this, ref SyntaxToken openBrace, SeparatedSyntaxListBuilder<EnumMemberDeclarationSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref openBrace, list,
                    static p => p.CurrentToken.Kind is not SyntaxKind.CommaToken and not SyntaxKind.SemicolonToken && !p.IsPossibleEnumMemberDeclaration(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
        }

        private EnumMemberDeclarationSyntax ParseEnumMemberDeclaration()
        {
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.EnumMemberDeclaration)
            {
                return (EnumMemberDeclarationSyntax)this.EatNode();
            }

            var memberAttrs = this.ParseAttributeDeclarations(inExpressionContext: false);
            var memberName = this.ParseIdentifierToken();
            EqualsValueClauseSyntax equalsValue = null;
            if (this.CurrentToken.Kind == SyntaxKind.EqualsToken)
            {
                //an identifier is a valid expression
                equalsValue = _syntaxFactory.EqualsValueClause(
                    this.EatToken(SyntaxKind.EqualsToken),
                    this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.CloseBraceToken
                        ? this.ParseIdentifierName(ErrorCode.ERR_ConstantExpected)
                        : this.ParseExpressionCore());
            }

            return _syntaxFactory.EnumMemberDeclaration(memberAttrs, modifiers: default, memberName, equalsValue);
        }

        private bool IsPossibleEnumMemberDeclaration()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken || this.IsTrueIdentifier();
        }

        private bool IsDotOrColonColon()
        {
            return this.CurrentToken.Kind is SyntaxKind.DotToken or SyntaxKind.ColonColonToken;
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
                if (!IsCurrentTokenPartialKeywordOfPartialMemberOrType() &&
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

            return SyntaxFactory.IdentifierName(
                ParseIdentifierToken(code));
        }

        private SyntaxToken ParseIdentifierToken(ErrorCode code = ErrorCode.ERR_IdentifierExpected)
        {
            var ctk = this.CurrentToken.Kind;
            if (ctk == SyntaxKind.IdentifierToken)
            {
                // Error tolerance for IntelliSense. Consider the following case: [EditorBrowsable( partial class Goo {
                // } Because we're parsing an attribute argument we'll end up consuming the "partial" identifier and
                // we'll eventually end up in a pretty confused state.  Because of that it becomes very difficult to
                // show the correct parameter help in this case.  So, when we see "partial" we check if it's being used
                // as an identifier or as a contextual keyword.  If it's the latter then we bail out.  See
                // Bug: vswhidbey/542125
                if (IsCurrentTokenPartialKeywordOfPartialMemberOrType() || IsCurrentTokenQueryKeywordInQuery())
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
                return this.AddError(CreateMissingIdentifierToken(), code);
            }
        }

        private bool IsCurrentTokenQueryKeywordInQuery()
        {
            return this.IsInQuery && this.IsCurrentTokenQueryContextualKeyword;
        }

        private bool IsCurrentTokenPartialKeywordOfPartialMemberOrType()
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

        private bool IsCurrentTokenFieldInKeywordContext()
        {
            return CurrentToken.ContextualKind == SyntaxKind.FieldKeyword &&
                IsInFieldKeywordContext &&
                IsFeatureEnabled(MessageID.IDS_FeatureFieldKeyword);
        }

        private TypeParameterListSyntax ParseTypeParameterList()
        {
            if (this.CurrentToken.Kind != SyntaxKind.LessThanToken)
            {
                return null;
            }

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfTypeParameterList;

            var open = this.EatToken(SyntaxKind.LessThanToken);
            var parameters = this.ParseCommaSeparatedSyntaxList(
                ref open,
                SyntaxKind.GreaterThanToken,
                static @this => @this.IsStartOfTypeParameter(),
                static @this => @this.ParseTypeParameter(),
                skipBadTypeParameterListTokens,
                allowTrailingSeparator: false,
                requireOneElement: true,
                allowSemicolonAsSeparator: false);

            _termState = saveTerm;

            return _syntaxFactory.TypeParameterList(
                open,
                parameters,
                this.EatToken(SyntaxKind.GreaterThanToken));

            static PostSkipAction skipBadTypeParameterListTokens(
                LanguageParser @this, ref SyntaxToken open, SeparatedSyntaxListBuilder<TypeParameterSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken,
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
        }

        private bool IsStartOfTypeParameter()
        {
            if (this.IsCurrentTokenWhereOfConstraintClause())
                return false;

            // possible attributes
            if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind != SyntaxKind.CloseBracketToken)
                return true;

            // Variance.
            if (this.CurrentToken.Kind is SyntaxKind.InKeyword or SyntaxKind.OutKeyword)
                return true;

            return IsTrueIdentifier();
        }

        private TypeParameterSyntax ParseTypeParameter()
        {
            if (this.IsCurrentTokenWhereOfConstraintClause())
            {
                return _syntaxFactory.TypeParameter(
                    default(SyntaxList<AttributeListSyntax>),
                    varianceKeyword: null,
                    this.AddError(CreateMissingIdentifierToken(), ErrorCode.ERR_IdentifierExpected));
            }

            var attrs = default(SyntaxList<AttributeListSyntax>);
            if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken && this.PeekToken(1).Kind != SyntaxKind.CloseBracketToken)
            {
                var saveTerm = _termState;
                _termState = TerminatorState.IsEndOfTypeArgumentList;
                attrs = this.ParseAttributeDeclarations(inExpressionContext: false);
                _termState = saveTerm;
            }

            return _syntaxFactory.TypeParameter(
                attrs,
                this.CurrentToken.Kind is SyntaxKind.InKeyword or SyntaxKind.OutKeyword ? EatToken() : null,
                this.ParseIdentifierToken());
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
                ScanTypeArgumentListKind kind;
                using (this.GetDisposableResetPoint(resetOnDispose: true))
                {
                    kind = this.ScanTypeArgumentList(options);
                }

                if (kind == ScanTypeArgumentListKind.DefiniteTypeArgumentList || (kind == ScanTypeArgumentListKind.PossibleTypeArgumentList && (options & NameOptions.InTypeList) != 0))
                {
                    Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);

                    var types = _pool.AllocateSeparated<TypeSyntax>();
                    this.ParseTypeArgumentList(out var open, types, out var close);
                    name = _syntaxFactory.GenericName(
                        id.Identifier,
                        _syntaxFactory.TypeArgumentList(
                            open,
                            _pool.ToListAndFree(types),
                            close));
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
            ScanTypeFlags possibleTypeArgumentFlags = ScanPossibleTypeArgumentList(
                out var greaterThanToken, out bool isDefinitelyTypeArgumentList);

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
            Debug.Assert(greaterThanToken.Kind == SyntaxKind.GreaterThanToken);

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
                        (options & NameOptions.AfterTupleComma) != 0 && this.PeekToken(1).Kind is SyntaxKind.CommaToken or SyntaxKind.CloseParenToken ||
                        (options & NameOptions.FirstElementOfPossibleTupleLiteral) != 0 && this.PeekToken(1).Kind == SyntaxKind.CommaToken)
                    {
                        // we allow 'G<T,U> x' as a pattern-matching operation and a declaration expression in a tuple.
                        return ScanTypeArgumentListKind.DefiniteTypeArgumentList;
                    }

                    return ScanTypeArgumentListKind.PossibleTypeArgumentList;

                case SyntaxKind.EndOfFileToken:          // e.g. `e is A<B>`
                    // This is useful for parsing expressions in isolation
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                case SyntaxKind.EqualsGreaterThanToken:  // e.g. `e switch { A<B> => 1 }`
                    // This token was added to 7.5.4.2 Grammar Ambiguities in C# 9.0
                    return ScanTypeArgumentListKind.DefiniteTypeArgumentList;

                default:
                    return ScanTypeArgumentListKind.PossibleTypeArgumentList;
            }
        }

        private ScanTypeFlags ScanPossibleTypeArgumentList(
            out SyntaxToken greaterThanToken,
            out bool isDefinitelyTypeArgumentList)
        {
            isDefinitelyTypeArgumentList = false;
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);

            // If we have `X<>`, or `X<,>` or `X<,,,,,,>` then none of these are legal expression or types. However,
            // it seems likelier that they are invalid open-types in an expression context, versus expressions
            // missing values (note that we only support this when the open name does have the final `>` token).
            if (IsOpenName())
            {
                isDefinitelyTypeArgumentList = true;

                var start = this.EatToken();
                while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                    this.EatToken();
                greaterThanToken = this.EatToken();
                Debug.Assert(start.Kind == SyntaxKind.LessThanToken);
                Debug.Assert(greaterThanToken.Kind == SyntaxKind.GreaterThanToken);

                return ScanTypeFlags.GenericTypeOrMethod;
            }

            ScanTypeFlags result = ScanTypeFlags.GenericTypeOrExpression;
            ScanTypeFlags lastScannedType;

            do
            {
                this.EatToken();

                // Type arguments cannot contain attributes, so if this is an open square, we early out and assume it is not a type argument
                if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                {
                    greaterThanToken = null;
                    return ScanTypeFlags.NotType;
                }

                if (this.CurrentToken.Kind == SyntaxKind.GreaterThanToken)
                {
                    greaterThanToken = EatToken();
                    return result;
                }

                // Allow for any chain of errant commas in the generic name.  like `Dictionary<,int>` or
                // `Dictionary<int,,>` We still want to think of these as generics, just with missing type-arguments, vs
                // some invalid tree-expression that we would otherwise form.
                if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    lastScannedType = default;
                    continue;
                }

                lastScannedType = this.ScanType(out _);
                switch (lastScannedType)
                {
                    case ScanTypeFlags.NotType:
                        greaterThanToken = null;
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
                        isDefinitelyTypeArgumentList = isDefinitelyTypeArgumentList || this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.GreaterThanToken;
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
                        // See above.  If we have `X<Y?,` or `X<Y?>` then this is definitely a type argument list.
                        isDefinitelyTypeArgumentList = isDefinitelyTypeArgumentList || this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.GreaterThanToken;
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
                        // We'd see a nullable type here, but this is definitely not a type arg list.

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

                    case ScanTypeFlags.NonGenericTypeOrExpression:
                        // Explicitly keeping this case in the switch for clarity.  We parsed out another portion of the
                        // type argument list that looks like it's a non-generic-type-or-expr (the simplest case just
                        // being "X").  That changes nothing here wrt determining what type of entity we have here, so
                        // just fall through and see if we're followed by a "," (in which case keep going), or a ">", in
                        // which case we're done.
                        break;
                }
            }
            while (this.CurrentToken.Kind == SyntaxKind.CommaToken);

            if (this.CurrentToken.Kind != SyntaxKind.GreaterThanToken)
            {
                // Error recovery after missing > token:

                // In the case of an identifier, we assume that there could be a missing > token
                // For example, we have reached C in X<A, B C
                if (this.CurrentToken.Kind is SyntaxKind.IdentifierToken)
                {
                    greaterThanToken = this.EatToken(SyntaxKind.GreaterThanToken);
                    return result;
                }

                // As for tuples, we do not expect direct invocation right after the parenthesis
                // EXAMPLE: X<(string, string)(), where we imply a missing > token between )(
                // as the user probably wants to invoke X by X<(string, string)>()
                if (lastScannedType is ScanTypeFlags.TupleType && this.CurrentToken.Kind is SyntaxKind.OpenParenToken)
                {
                    greaterThanToken = this.EatToken(SyntaxKind.GreaterThanToken);
                    return result;
                }

                greaterThanToken = null;
                return ScanTypeFlags.NotType;
            }

            greaterThanToken = this.EatToken();

            // If we have `X<Y>)` then this would definitely be a type argument list.
            isDefinitelyTypeArgumentList = isDefinitelyTypeArgumentList || this.CurrentToken.Kind is SyntaxKind.CloseParenToken;
            if (isDefinitelyTypeArgumentList)
            {
                result = ScanTypeFlags.GenericTypeOrMethod;
            }

            return result;
        }

        // ParseInstantiation: Parses the generic argument/parameter parts of the name.
        private void ParseTypeArgumentList(out SyntaxToken open, SeparatedSyntaxListBuilder<TypeSyntax> types, out SyntaxToken close)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);
            var isOpenName = this.IsOpenName();
            open = this.EatToken(SyntaxKind.LessThanToken);
            open = CheckFeatureAvailability(open, MessageID.IDS_FeatureGenerics);

            if (isOpenName)
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

                // We prefer early terminating the argument list over parsing until exhaustion
                // for better error recovery
                if (tokenBreaksTypeArgumentList(this.CurrentToken))
                {
                    break;
                }

                // We are currently past parsing a type and we encounter an unexpected identifier token
                // followed by tokens that are not part of a type argument list
                // Example: List<(string a, string b) Method() { }
                //                 current token:     ^^^^^^
                if (this.CurrentToken.Kind is SyntaxKind.IdentifierToken && tokenBreaksTypeArgumentList(this.PeekToken(1)))
                {
                    break;
                }

                // This is for the case where we are in a this[] accessor, and the last one of the parameters in the parameter list
                // is missing a > on its type
                // Example: X this[IEnumerable<string parameter] => 
                //                 current token:     ^^^^^^^^^
                if (this.CurrentToken.Kind is SyntaxKind.IdentifierToken
                    && this.PeekToken(1).Kind is SyntaxKind.CloseBracketToken)
                {
                    break;
                }

                if (this.CurrentToken.Kind == SyntaxKind.CommaToken || this.IsPossibleType())
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

            static bool tokenBreaksTypeArgumentList(SyntaxToken token)
            {
                var contextualKind = SyntaxFacts.GetContextualKeywordKind(token.ValueText);
                switch (contextualKind)
                {
                    // Example: x is IEnumerable<string or IList<int>
                    case SyntaxKind.OrKeyword:
                    // Example: x is IEnumerable<string and IDisposable
                    case SyntaxKind.AndKeyword:
                        return true;
                }

                switch (token.Kind)
                {
                    // Example: Method<string(argument)
                    // Note: We would do a bad job handling a tuple argument with a missing comma,
                    //       like: Method<string (int x, int y)>
                    //       but since we do not look as far as possible to determine whether it is
                    //       a tuple type or an argument list, we resort to considering it as an
                    //       argument list
                    case SyntaxKind.OpenParenToken:

                    // Example: IEnumerable<string Method<T>() --- (< in <T>)
                    case SyntaxKind.LessThanToken:
                    // Example: Method(IEnumerable<string parameter)
                    case SyntaxKind.CloseParenToken:
                    // Example: IEnumerable<string field;
                    case SyntaxKind.SemicolonToken:
                    // Example: IEnumerable<string Property { get; set; }
                    case SyntaxKind.OpenBraceToken:
                    // Example:
                    // {
                    //     IEnumerable<string field
                    // }
                    case SyntaxKind.CloseBraceToken:
                    // Examples:
                    // - IEnumerable<string field = null;
                    // - Method(IEnumerable<string parameter = null)
                    case SyntaxKind.EqualsToken:
                    // Example: IEnumerable<string Property => null;
                    case SyntaxKind.EqualsGreaterThanToken:
                    // Example: IEnumerable<string this[string key] { get; set; }
                    case SyntaxKind.ThisKeyword:
                    // Example: static IEnumerable<string operator +(A left, A right);
                    case SyntaxKind.OperatorKeyword:
                        return true;
                }

                return false;
            }
        }

        private PostSkipAction SkipBadTypeArgumentListTokens(SeparatedSyntaxListBuilder<TypeSyntax> list, SyntaxKind expected)
        {
            CSharpSyntaxNode tmp = null;
            Debug.Assert(list.Count > 0);
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleType(),
                static (p, _) => p.CurrentToken.Kind == SyntaxKind.GreaterThanToken,
                expected);
        }

        // Parses the individual generic parameter/arguments in a name.
        private TypeSyntax ParseTypeArgument()
        {
            var attrs = default(SyntaxList<AttributeListSyntax>);
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
                attrs = this.ParseAttributeDeclarations(inExpressionContext: false);
                _termState = saveTerm;
            }

            // Recognize the variance syntax, but give an error as it's only appropriate in a type parameter list.
            var varianceToken = this.CurrentToken.Kind is SyntaxKind.InKeyword or SyntaxKind.OutKeyword
                ? this.AddError(this.EatToken(), ErrorCode.ERR_IllegalVarianceSyntax)
                : null;

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

            if (result.IsMissing &&
                this.CurrentToken.Kind is not SyntaxKind.CommaToken and not SyntaxKind.GreaterThanToken &&
                this.PeekToken(1).Kind is SyntaxKind.CommaToken or SyntaxKind.GreaterThanToken)
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
                result = AddLeadingSkippedSyntax(result, attrs.Node);
                result = this.AddError(result, ErrorCode.ERR_TypeExpected);
            }

            return result;
        }

        private bool IsEndOfTypeArgumentList()
            => this.CurrentToken.Kind == SyntaxKind.GreaterThanToken;

        private bool IsOpenName()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LessThanToken);
            var n = 1;
            while (this.PeekToken(n).Kind == SyntaxKind.CommaToken)
                n++;

            return this.PeekToken(n).Kind == SyntaxKind.GreaterThanToken;
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
                    bool isMemberName;
                    using (GetDisposableResetPoint(resetOnDispose: true))
                    {
                        ScanNamedTypePart();
                        isMemberName = !IsDotOrColonColon();
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
                        AccumulateExplicitInterfaceName(ref explicitInterfaceName, ref separator);
                    }
                }

                if (explicitInterfaceName != null)
                {
                    if (separator.Kind != SyntaxKind.DotToken)
                    {
                        separator = WithAdditionalDiagnostics(separator, GetExpectedTokenError(SyntaxKind.DotToken, separator.Kind, separator.GetLeadingTriviaWidth(), separator.Width));
                        separator = ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                    }

                    if (isEvent && this.CurrentToken.Kind is not SyntaxKind.OpenBraceToken and not SyntaxKind.SemicolonToken)
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

        private void AccumulateExplicitInterfaceName(ref NameSyntax explicitInterfaceName, ref SyntaxToken separator)
        {
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
                if (this.CurrentToken.Kind == SyntaxKind.ColonColonToken)
                {
                    separator = this.EatToken();
                    separator = this.AddError(separator, ErrorCode.ERR_UnexpectedAliasedName);
                    separator = this.ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                }
                else
                {
                    separator = this.EatToken(SyntaxKind.DotToken);
                }
            }

            _termState = saveTerm;
        }

        /// <summary>
        /// This is an adjusted version of <see cref="ParseMemberName"/>.
        /// When it returns true, it stops at operator keyword (<see cref="IsOperatorKeyword"/>).
        /// When it returns false, it does not advance in the token stream.
        /// </summary>
        private bool IsOperatorStart(out ExplicitInterfaceSpecifierSyntax explicitInterfaceOpt, bool advanceParser = true)
        {
            explicitInterfaceOpt = null;

            if (IsOperatorKeyword())
            {
                return true;
            }

            if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
            {
                return false;
            }

            NameSyntax explicitInterfaceName = null;
            SyntaxToken separator = null;

            using var beforeIdentifierPoint = GetDisposableResetPoint(resetOnDispose: false);

            while (true)
            {
                // now, scan past the next name.  if it's followed by a dot then
                // it's part of the explicit name we're building up.  Otherwise,
                // it should be an operator token
                bool isPartOfInterfaceName;
                using (GetDisposableResetPoint(resetOnDispose: true))
                {
                    if (IsOperatorKeyword())
                    {
                        isPartOfInterfaceName = false;
                    }
                    else
                    {
                        ScanNamedTypePart();

                        // If we have part of the interface name, but no dot before the operator token, then
                        // for the purpose of error recovery, treat this as an operator start with a
                        // missing dot token.
                        isPartOfInterfaceName = IsDotOrColonColon() || IsOperatorKeyword();
                    }
                }

                if (!isPartOfInterfaceName)
                {
                    // We're past any explicit interface portion
                    if (separator != null && separator.Kind == SyntaxKind.ColonColonToken)
                    {
                        separator = this.AddError(separator, ErrorCode.ERR_AliasQualAsExpression);
                        separator = this.ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
                    }

                    break;
                }
                else
                {
                    // If we saw a . or :: then we must have something explicit.
                    AccumulateExplicitInterfaceName(ref explicitInterfaceName, ref separator);
                }
            }

            if (!IsOperatorKeyword() || explicitInterfaceName is null)
            {
                beforeIdentifierPoint.Reset();
                return false;
            }

            if (!advanceParser)
            {
                beforeIdentifierPoint.Reset();
                return true;
            }

            if (separator.Kind != SyntaxKind.DotToken)
            {
                separator = WithAdditionalDiagnostics(separator, GetExpectedTokenError(SyntaxKind.DotToken, separator.Kind, separator.GetLeadingTriviaWidth(), separator.Width));
                separator = ConvertToMissingWithTrailingTrivia(separator, SyntaxKind.DotToken);
            }

            explicitInterfaceOpt = _syntaxFactory.ExplicitInterfaceSpecifier(explicitInterfaceName, separator);
            return true;
        }

        private NameSyntax ParseAliasQualifiedName(NameOptions allowedParts = NameOptions.None)
        {
            var name = this.ParseSimpleName(allowedParts);
            return this.CurrentToken.Kind == SyntaxKind.ColonColonToken
                ? ParseQualifiedNameRight(allowedParts, name, this.EatToken())
                : name;
        }

        private NameSyntax ParseQualifiedName(NameOptions options = NameOptions.None)
        {
            NameSyntax name = this.ParseAliasQualifiedName(options);

            // Handle .. tokens for error recovery purposes.
            while (IsDotOrColonColon())
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
            Debug.Assert(separator.Kind is SyntaxKind.DotToken or SyntaxKind.ColonColonToken);
            var right = this.ParseSimpleName(options);

            switch (separator.Kind)
            {
                case SyntaxKind.DotToken:
                    return _syntaxFactory.QualifiedName(left, separator, right);

                case SyntaxKind.ColonColonToken:
                    if (left.Kind != SyntaxKind.IdentifierName)
                    {
                        separator = this.AddError(separator, ErrorCode.ERR_UnexpectedAliasedName);
                    }

                    // If the left hand side is not an identifier name then the user has done
                    // something like Goo.Bar::Blah. We've already made an error node for the
                    // ::, so just pretend that they typed Goo.Bar.Blah and continue on.

                    if (left is not IdentifierNameSyntax identifierLeft)
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

                        // If the name on the right had errors or warnings then we need to preserve
                        // them in the tree.
                        return WithAdditionalDiagnostics(_syntaxFactory.AliasQualifiedName(identifierLeft, separator, right), left.GetDiagnostics());
                    }

                default:
                    throw ExceptionUtilities.Unreachable();
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
            /// type (ending with a [] brackets), or a pointer type (ending with *s), or a function
            /// pointer type (ending with > in valid cases, or a *, ), or calling convention
            /// identifier, in invalid cases).
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
            /// A type name with alias prefix (Alias::Name).  Note that Alias::Name.X would not fall under this.  This
            /// only is returned for exactly Alias::Name.
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
            ScanNamedTypePart(out _);
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
                return this.ScanPossibleTypeArgumentList(out lastTokenOfType, out _);
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

            // Handle :: as well for error case of an alias used without a preceding identifier.
            if (this.CurrentToken.Kind is SyntaxKind.IdentifierToken or SyntaxKind.ColonColonToken)
            {
                bool isAlias;
                if (this.CurrentToken.Kind is SyntaxKind.ColonColonToken)
                {
                    result = ScanTypeFlags.NonGenericTypeOrExpression;

                    // Definitely seems like an alias if we're starting with a ::
                    isAlias = true;

                    // We set this to null to appease the flow checker.  It will always be the case that this will be
                    // set to an appropriate value inside the `for` loop below.  We'll consume the :: there and then
                    // call ScanNamedTypePart which will always set this to a valid value.
                    lastTokenOfType = null;
                }
                else
                {
                    Debug.Assert(this.CurrentToken.Kind is SyntaxKind.IdentifierToken);

                    // We're an alias if we start with an: id::
                    isAlias = this.PeekToken(1).Kind == SyntaxKind.ColonColonToken;

                    result = this.ScanNamedTypePart(out lastTokenOfType);
                    if (result == ScanTypeFlags.NotType)
                    {
                        return ScanTypeFlags.NotType;
                    }

                    Debug.Assert(result is ScanTypeFlags.GenericTypeOrExpression or ScanTypeFlags.GenericTypeOrMethod or ScanTypeFlags.NonGenericTypeOrExpression);
                }

                // Scan a name
                for (bool firstLoop = true; IsDotOrColonColon(); firstLoop = false)
                {
                    // If we consume any more dots or colons, don't consider us an alias anymore.  For dots, we now have
                    // x::y.z (which is now back to a normal expr/type, not an alias), and for colons that means we have
                    // x::y::z or x.y::z both of which are effectively gibberish.
                    if (!firstLoop)
                    {
                        isAlias = false;
                    }

                    this.EatToken();
                    result = this.ScanNamedTypePart(out lastTokenOfType);
                    if (result == ScanTypeFlags.NotType)
                    {
                        return ScanTypeFlags.NotType;
                    }

                    Debug.Assert(result is ScanTypeFlags.GenericTypeOrExpression or ScanTypeFlags.GenericTypeOrMethod or ScanTypeFlags.NonGenericTypeOrExpression);
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
            else if (IsFunctionPointerStart())
            {
                result = ScanFunctionPointerType(out lastTokenOfType);
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
                            when lastTokenOfType.Kind is not SyntaxKind.QuestionToken // don't allow `Type??`
                                                      and not SyntaxKind.AsteriskToken: // don't allow `Type*?`
                        lastTokenOfType = this.EatToken();
                        result = ScanTypeFlags.NullableType;
                        break;
                    case SyntaxKind.AsteriskToken:
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
                                if (result is ScanTypeFlags.GenericTypeOrExpression or ScanTypeFlags.NonGenericTypeOrExpression)
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

#nullable enable
        private ScanTypeFlags ScanFunctionPointerType(out SyntaxToken lastTokenOfType)
        {
            Debug.Assert(IsFunctionPointerStart());
            _ = EatToken(SyntaxKind.DelegateKeyword);
            lastTokenOfType = EatToken(SyntaxKind.AsteriskToken);

            TerminatorState saveTerm;

            if (CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                var peek1 = PeekToken(1);
                switch (CurrentToken)
                {
                    case { ContextualKind: SyntaxKind.ManagedKeyword }:
                    case { ContextualKind: SyntaxKind.UnmanagedKeyword }:
                    case var _ when IsPossibleFunctionPointerParameterListStart(peek1):
                    case var _ when peek1.Kind == SyntaxKind.OpenBracketToken:
                        lastTokenOfType = EatToken();
                        break;

                    default:
                        // Whatever is next, it's probably not part of the type. We know that delegate* must be
                        // a function pointer start, however, so say the asterisk is the last element and bail
                        return ScanTypeFlags.MustBeType;
                }

                if (CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                {
                    lastTokenOfType = EatToken(SyntaxKind.OpenBracketToken);
                    saveTerm = _termState;
                    _termState |= TerminatorState.IsEndOfFunctionPointerCallingConvention;

                    try
                    {
                        while (true)
                        {
                            lastTokenOfType = TryEatToken(SyntaxKind.IdentifierToken) ?? lastTokenOfType;

                            if (skipBadFunctionPointerTokens() == PostSkipAction.Abort)
                            {
                                break;
                            }

                            Debug.Assert(CurrentToken.Kind == SyntaxKind.CommaToken);
                            lastTokenOfType = EatToken();
                        }

                        lastTokenOfType = TryEatToken(SyntaxKind.CloseBracketToken) ?? lastTokenOfType;
                    }
                    finally
                    {
                        _termState = saveTerm;
                    }
                }
            }

            if (!IsPossibleFunctionPointerParameterListStart(CurrentToken))
            {
                // Even though this function pointer type is incomplete, we know that it
                // must be the start of a type, as there is no other possible interpretation
                // of delegate*. By always treating it as a type, we ensure that any disambiguation
                // done in later parsing treats this as a type, which will produce better
                // errors at later stages.
                return ScanTypeFlags.MustBeType;
            }

            var validStartingToken = EatToken().Kind == SyntaxKind.LessThanToken;

            saveTerm = _termState;
            _termState |= validStartingToken ? TerminatorState.IsEndOfFunctionPointerParameterList : TerminatorState.IsEndOfFunctionPointerParameterListErrored;
            var ignoredModifiers = _pool.Allocate<SyntaxToken>();

            try
            {
                do
                {
                    ParseParameterModifiers(ignoredModifiers, isFunctionPointerParameter: true, isLambdaParameter: false);
                    ignoredModifiers.Clear();

                    _ = ScanType(out _);

                    if (skipBadFunctionPointerTokens() == PostSkipAction.Abort)
                    {
                        break;
                    }

                    _ = EatToken(SyntaxKind.CommaToken);
                }
                while (true);
            }
            finally
            {
                _termState = saveTerm;
                _pool.Free(ignoredModifiers);
            }

            if (!validStartingToken && CurrentToken.Kind == SyntaxKind.CloseParenToken)
            {
                lastTokenOfType = EatTokenAsKind(SyntaxKind.GreaterThanToken);
            }
            else
            {
                lastTokenOfType = EatToken(SyntaxKind.GreaterThanToken);
            }

            return ScanTypeFlags.MustBeType;

            PostSkipAction skipBadFunctionPointerTokens()
            {
                return SkipBadTokensWithExpectedKind(
                    isNotExpectedFunction: static p => p.CurrentToken.Kind != SyntaxKind.CommaToken,
                    abortFunction: static (p, _) => p.IsTerminator(),
                    expected: SyntaxKind.CommaToken,
                    closeKind: SyntaxKind.None,
                    trailingTrivia: out _);
            }
        }
#nullable disable

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
            AfterRef,
            AfterTupleComma,
            AsExpression,
            NewExpression,
            FirstElementOfPossibleTupleLiteral,
        }

        private TypeSyntax ParseType(ParseTypeMode mode = ParseTypeMode.Normal)
        {
            if (this.CurrentToken.Kind == SyntaxKind.RefKeyword)
            {
                return _syntaxFactory.RefType(
                    this.EatToken(),
                    this.CurrentToken.Kind == SyntaxKind.ReadOnlyKeyword ? this.EatToken() : null,
                    ParseTypeCore(ParseTypeMode.AfterRef));
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
                case ParseTypeMode.AfterRef:
                    nameOptions = NameOptions.None;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(mode);
            }

            var type = this.ParseUnderlyingType(mode, options: nameOptions);
            Debug.Assert(type != null);

            int lastTokenPosition = -1;
            while (IsMakingProgress(ref lastTokenPosition))
            {
                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.QuestionToken:
                        {
                            var question = TryEatNullableQualifierIfApplicable(type, mode);
                            if (question != null)
                            {
                                type = _syntaxFactory.NullableType(type, question);
                                continue;
                            }

                            // token not consumed
                            break;
                        }
                    case SyntaxKind.AsteriskToken:
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
                            case ParseTypeMode.AfterRef:
                            case ParseTypeMode.AsExpression:
                            case ParseTypeMode.NewExpression:
                                type = this.ParsePointerTypeMods(type);
                                continue;
                        }

                        // token not consumed
                        break;
                    case SyntaxKind.OpenBracketToken:
                        // Now check for arrays.
                        {
                            var ranks = _pool.Allocate<ArrayRankSpecifierSyntax>();
                            do
                            {
                                ranks.Add(this.ParseArrayRankSpecifier(out _));
                            }
                            while (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken);

                            type = _syntaxFactory.ArrayType(type, _pool.ToListAndFree(ranks));
                            continue;
                        }
                    default:
                        // token not consumed
                        break;
                }

                // token not consumed
                break;
            }

            Debug.Assert(type != null);
            return type;
        }

        private SyntaxToken TryEatNullableQualifierIfApplicable(
            TypeSyntax typeParsedSoFar, ParseTypeMode mode)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.QuestionToken);

            // These are the fast tests for (in)applicability. More expensive tests are follow.
            //
            // If we already have `x?` or `x*` then do not parse out a nullable type if we see `x??` or `x*?`.  These
            // are never legal as types in the language, so we can fast bail out.
            if (typeParsedSoFar.Kind is SyntaxKind.NullableType or SyntaxKind.PointerType)
                return null;

            using var outerResetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

            var questionToken = this.EatToken();
            if (!canFollowNullableType())
            {
                // Restore current token index
                outerResetPoint.Reset();
                return null;
            }

            return questionToken;

            bool canFollowNullableType()
            {
                if (mode == ParseTypeMode.AfterIs && this.CurrentToken.Kind is SyntaxKind.OpenBracketToken)
                {
                    // T?[
                    //
                    // This could be a array of nullable types (e.g. `is T?[]` or `is T?[,]`) or it's a
                    // conditional with a collection expression or lambda (e.g. `is T ? [...] :` or `is T ? [Attr]() => ...`)
                    //
                    // Note: `is T?[]` could be the start of either.  So we have to look to see if we have a
                    // `:` to know which case we're in.

                    switch (this.PeekToken(1).Kind)
                    {
                        // `is T?[,]`.  Definitely an array of nullable type.
                        case SyntaxKind.CommaToken:
                            return true;

                        // `is T?[]`.  Could be an array of a nullable type, or a conditional.  Have to
                        // see if it is followed by `:` to find out.  If there is a colon, it's a
                        // conditional.
                        case SyntaxKind.CloseBracketToken:
                            {
                                using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

                                // Consume the expression after the `?`.
                                var whenTrue = this.ParsePossibleRefExpression();

                                // Now see if we have a ':' following.  If so, this is a conditional.  If not, it's a nullable type.
                                return this.CurrentToken.Kind != SyntaxKind.ColonToken;
                            }

                        // `is T ? [...`.  Not an array.  This is a conditional with a collection expr
                        // or attributed lambda.
                        default:
                            return false;
                    }
                }

                switch (mode)
                {
                    case ParseTypeMode.AfterIs:
                    case ParseTypeMode.DefinitePattern:
                    case ParseTypeMode.AsExpression:
                        // We are currently after `?` token after a nullable type pattern and need to decide how to
                        // parse what we see next.  In the case of an identifier (e.g. `x ? a` there are two ways we can
                        // see things
                        //
                        // 1. As a start of conditional expression, e.g. `var a = obj is string ? a : b`
                        // 2. As a designation of a nullable-typed pattern, e.g. `if (obj is string? str)`
                        //
                        // Since nullable types (no matter reference or value types) are not valid in patterns by
                        // default we are biased towards the first option and consider case 2 only for error recovery
                        // purposes (if we parse here as nullable type pattern an error will be reported during
                        // binding). This condition checks for simple cases, where we better use option 2 and parse a
                        // nullable-typed pattern
                        if (IsTrueIdentifier(this.CurrentToken))
                        {
                            // 1. `async` can start a simple lambda in a conditional expression
                            // (e.g. `x is Y ? async a => ...`). The correct behavior is to treat `async` as a keyword
                            // 2. In a non-async method, `await` is a simple identifier.  However, if we see `x ? await`
                            // it's almost certainly the start of an `await expression` in a conditional expression
                            // (e.g. `x is Y ? await ...`), not a nullable type pattern (since users would not use
                            // 'await' as the name of a variable).  So just treat this as a conditional expression.
                            // 3. `from` most likely starts a linq query: `x is Y ? from item in collection select item : ...`
                            if (this.CurrentToken.ContextualKind is SyntaxKind.AsyncKeyword or SyntaxKind.AwaitKeyword or SyntaxKind.FromKeyword)
                                return false;

                            var nextToken = PeekToken(1);

                            // Cases like `x is Y ? someRecord with { } : ...`
                            if (nextToken.ContextualKind == SyntaxKind.WithKeyword)
                                return false;

                            var nextTokenKind = nextToken.Kind;

                            // These token either 100% end a pattern or start a new one:

                            // A literal token starts a new pattern. Can occur in list pattern with missing separation
                            // `,`.  For example, in `x is [int[]? arr 5]` we'd prefer to parse this as a missing `,`
                            // after `arr`
                            if (SyntaxFacts.IsLiteral(nextTokenKind))
                                return true;

                            // A predefined type is basically the same case: `x is [string[]? slice char ch]`. We'd
                            // prefer to parse this as a missing `,` after `slice`.
                            if (SyntaxFacts.IsPredefinedType(nextTokenKind))
                                return true;

                            // `)`, `]` and `}` obviously end a pattern.  For example:
                            // `if (x is int? i)`, `indexable[x is string? s]`, `x is { Prop: Type? typeVar }`
                            if (nextTokenKind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken or SyntaxKind.CloseBraceToken)
                                return true;

                            // `{` starts a new pattern.  For example: `x is A? { ...`. Note, that `[` and `(` are not
                            // in the list because they can start an invocation/indexer
                            if (nextTokenKind == SyntaxKind.OpenBraceToken)
                                return true;

                            // `,` ends a pattern in list/property pattern.  For example `x is { Prop1: Type1? type, Prop2: Type2 }` or
                            // `x is [Type1? type, ...]`
                            if (nextTokenKind == SyntaxKind.CommaToken)
                                return true;

                            // `;` ends a pattern if it finishes an expression statement: var y = x is bool? b;
                            if (nextTokenKind == SyntaxKind.SemicolonToken)
                                return true;

                            // EndOfFileToken is obviously the end of parsing. We are better parsing a pattern rather
                            // than an unfinished conditional expression
                            if (nextTokenKind == SyntaxKind.EndOfFileToken)
                                return true;

                            return false;
                        }

                        // If nothing from above worked permit the nullable qualifier if it is followed by a token that
                        // could not start an expression. If we have `T?[]` we do want to treat that as an array of
                        // nullables (following existing parsing), not a conditional that returns a list.
                        if (this.CurrentToken.Kind is SyntaxKind.OpenBracketToken)
                            return true;

                        return !CanStartExpression();
                    case ParseTypeMode.NewExpression:
                        // A nullable qualifier is permitted as part of the type in a `new` expression. e.g. `new
                        // int?()` is allowed.  It creates a null value of type `Nullable<int>`. Similarly `new int? {}`
                        // is allowed.
                        return
                            this.CurrentToken.Kind is SyntaxKind.OpenParenToken or   // ctor parameters
                                                      SyntaxKind.OpenBracketToken or   // array type
                                                      SyntaxKind.OpenBraceToken;   // object initializer
                    default:
                        return true;
                }
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

            var omittedArraySizeExpressionInstance = _syntaxFactory.OmittedArraySizeExpression(SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken));
            int lastTokenPosition = -1;
            while (IsMakingProgress(ref lastTokenPosition) && this.CurrentToken.Kind != SyntaxKind.CloseBracketToken)
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
                        list[i] = this.AddError(this.CreateMissingIdentifierName(), offset: 0, list[i].Width, ErrorCode.ERR_ValueExpected);
                }
            }

            return _syntaxFactory.ArrayRankSpecifier(
                open,
                _pool.ToListAndFree(list),
                this.EatToken(SyntaxKind.CloseBracketToken));
        }

        private TupleTypeSyntax ParseTupleType()
        {
            var open = this.EatToken(SyntaxKind.OpenParenToken);
            var list = _pool.AllocateSeparated<TupleElementSyntax>();

            if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
            {
                list.Add(ParseTupleElement());

                while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    list.Add(ParseTupleElement());
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

            return _syntaxFactory.TupleType(
                open,
                _pool.ToListAndFree(list),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private TupleElementSyntax ParseTupleElement()
        {
            return _syntaxFactory.TupleElement(
                ParseType(),
                IsTrueIdentifier() ? this.ParseIdentifierToken() : null);
        }

        private PostSkipAction SkipBadArrayRankSpecifierTokens(ref SyntaxToken openBracket, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expected)
        {
            return this.SkipBadSeparatedListTokensWithExpectedKind(ref openBracket, list,
                static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                static (p, _) => p.CurrentToken.Kind == SyntaxKind.CloseBracketToken,
                expected);
        }

        private TypeSyntax ParseUnderlyingType(ParseTypeMode mode, NameOptions options = NameOptions.None)
        {
            if (IsPredefinedType(this.CurrentToken.Kind))
            {
                // This is a predefined type
                var token = this.EatToken();
                if (token.Kind == SyntaxKind.VoidKeyword && this.CurrentToken.Kind != SyntaxKind.AsteriskToken)
                {
                    token = this.AddError(token, mode == ParseTypeMode.Parameter ? ErrorCode.ERR_NoVoidParameter : ErrorCode.ERR_NoVoidHere);
                }

                return _syntaxFactory.PredefinedType(token);
            }

            // The :: case is for error recovery.
            if (IsTrueIdentifier() || this.CurrentToken.Kind == SyntaxKind.ColonColonToken)
            {
                return this.ParseQualifiedName(options);
            }

            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                return this.ParseTupleType();
            }
            else if (IsFunctionPointerStart())
            {
                return ParseFunctionPointerTypeSyntax();
            }

            return this.AddError(
                this.CreateMissingIdentifierName(),
                mode == ParseTypeMode.NewExpression ? ErrorCode.ERR_BadNewExpr : ErrorCode.ERR_TypeExpected);
        }

#nullable enable
        private FunctionPointerTypeSyntax ParseFunctionPointerTypeSyntax()
        {
            Debug.Assert(IsFunctionPointerStart());
            var @delegate = EatToken(SyntaxKind.DelegateKeyword);
            var asterisk = EatToken(SyntaxKind.AsteriskToken);

            FunctionPointerCallingConventionSyntax? callingConvention = parseCallingConvention();

            if (!IsPossibleFunctionPointerParameterListStart(CurrentToken))
            {
                var lessThanTokenError = CreateMissingToken(SyntaxKind.LessThanToken, SyntaxKind.None);

                var missingTypes = _pool.AllocateSeparated<FunctionPointerParameterSyntax>();
                var missingType = SyntaxFactory.FunctionPointerParameter(attributeLists: default, modifiers: default, CreateMissingIdentifierName());
                missingTypes.Add(missingType);

                // Handle the simple case of delegate*>. We don't try to deal with any variation of delegate*invalid>, as
                // we don't know for sure that the expression isn't a relational with something else.
                return SyntaxFactory.FunctionPointerType(
                    @delegate,
                    asterisk,
                    callingConvention,
                    SyntaxFactory.FunctionPointerParameterList(
                        lessThanTokenError,
                        _pool.ToListAndFree(missingTypes),
                        TryEatToken(SyntaxKind.GreaterThanToken) ?? SyntaxFactory.MissingToken(SyntaxKind.GreaterThanToken)));
            }

            var lessThanToken = EatTokenAsKind(SyntaxKind.LessThanToken);
            var saveTerm = _termState;
            _termState |= (lessThanToken.IsMissing ? TerminatorState.IsEndOfFunctionPointerParameterListErrored : TerminatorState.IsEndOfFunctionPointerParameterList);
            var types = _pool.AllocateSeparated<FunctionPointerParameterSyntax>();

            try
            {
                while (true)
                {
                    var modifiers = _pool.Allocate<SyntaxToken>();

                    ParseParameterModifiers(modifiers, isFunctionPointerParameter: true, isLambdaParameter: false);

                    types.Add(SyntaxFactory.FunctionPointerParameter(
                        attributeLists: default,
                        _pool.ToTokenListAndFree(modifiers),
                        ParseTypeOrVoid()));

                    if (skipBadFunctionPointerTokens(types) == PostSkipAction.Abort)
                    {
                        break;
                    }

                    Debug.Assert(CurrentToken.Kind == SyntaxKind.CommaToken);
                    types.AddSeparator(EatToken(SyntaxKind.CommaToken));
                }

                return SyntaxFactory.FunctionPointerType(
                    @delegate,
                    asterisk,
                    callingConvention,
                    SyntaxFactory.FunctionPointerParameterList(
                        lessThanToken,
                        _pool.ToListAndFree(types),
                        lessThanToken.IsMissing && CurrentToken.Kind == SyntaxKind.CloseParenToken
                            ? EatTokenAsKind(SyntaxKind.GreaterThanToken)
                            : EatToken(SyntaxKind.GreaterThanToken)));
            }
            finally
            {
                _termState = saveTerm;
            }

            PostSkipAction skipBadFunctionPointerTokens<T>(SeparatedSyntaxListBuilder<T> list) where T : CSharpSyntaxNode
            {
                CSharpSyntaxNode? tmp = null;
                Debug.Assert(list.Count > 0);
                return SkipBadSeparatedListTokensWithExpectedKind(ref tmp,
                    list,
                    isNotExpectedFunction: static p => p.CurrentToken.Kind != SyntaxKind.CommaToken,
                    // this.IsTerminator() (called by our caller) is the only thing that aborts parsing.
                    abortFunction: static (p, _) => false,
                    expected: SyntaxKind.CommaToken);
            }

            FunctionPointerCallingConventionSyntax? parseCallingConvention()
            {
                if (CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    SyntaxToken managedSpecifier;
                    SyntaxToken peek1 = PeekToken(1);
                    switch (CurrentToken)
                    {
                        case { ContextualKind: SyntaxKind.ManagedKeyword }:
                        case { ContextualKind: SyntaxKind.UnmanagedKeyword }:
                            managedSpecifier = EatContextualToken(CurrentToken.ContextualKind);
                            break;

                        case var _ when IsPossibleFunctionPointerParameterListStart(peek1):
                            // If there's a possible parameter list next, treat this as a bad identifier that should have been managed or unmanaged
                            managedSpecifier = EatTokenAsKind(SyntaxKind.ManagedKeyword);
                            break;

                        case var _ when peek1.Kind == SyntaxKind.OpenBracketToken:
                            // If there's an open brace next, treat this as a bad identifier that should have been unmanaged
                            managedSpecifier = EatTokenAsKind(SyntaxKind.UnmanagedKeyword);
                            break;

                        default:
                            // Whatever is next, it's probably not a calling convention or a function pointer type.
                            // Bail out
                            return null;
                    }

                    FunctionPointerUnmanagedCallingConventionListSyntax? unmanagedCallingConventions = null;
                    if (CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                    {
                        var openBracket = EatToken(SyntaxKind.OpenBracketToken);
                        var callingConventionModifiers = _pool.AllocateSeparated<FunctionPointerUnmanagedCallingConventionSyntax>();
                        var saveTerm = _termState;
                        _termState |= TerminatorState.IsEndOfFunctionPointerCallingConvention;

                        try
                        {
                            while (true)
                            {
                                callingConventionModifiers.Add(SyntaxFactory.FunctionPointerUnmanagedCallingConvention(EatToken(SyntaxKind.IdentifierToken)));

                                if (skipBadFunctionPointerTokens(callingConventionModifiers) == PostSkipAction.Abort)
                                {
                                    break;
                                }

                                Debug.Assert(CurrentToken.Kind == SyntaxKind.CommaToken);
                                callingConventionModifiers.AddSeparator(EatToken(SyntaxKind.CommaToken));
                            }

                            var closeBracket = EatToken(SyntaxKind.CloseBracketToken);

                            unmanagedCallingConventions = SyntaxFactory.FunctionPointerUnmanagedCallingConventionList(
                                openBracket,
                                _pool.ToListAndFree(callingConventionModifiers), closeBracket);
                        }
                        finally
                        {
                            _termState = saveTerm;
                        }
                    }

                    if (managedSpecifier.Kind == SyntaxKind.ManagedKeyword && unmanagedCallingConventions != null)
                    {
                        // 'managed' calling convention cannot be combined with unmanaged calling convention specifiers.
                        unmanagedCallingConventions = AddError(unmanagedCallingConventions, ErrorCode.ERR_CannotSpecifyManagedWithUnmanagedSpecifiers);
                    }

                    return SyntaxFactory.FunctionPointerCallingConvention(managedSpecifier, unmanagedCallingConventions);
                }

                return null;
            }
        }

        private bool IsFunctionPointerStart()
            => CurrentToken.Kind == SyntaxKind.DelegateKeyword && PeekToken(1).Kind == SyntaxKind.AsteriskToken;

        private static bool IsPossibleFunctionPointerParameterListStart(SyntaxToken token)
            // We consider both ( and < to be possible starts, in order to make error recovery more graceful
            // in the scenario where a user accidentally surrounds their function pointer type list with parens.
            => token.Kind == SyntaxKind.LessThanToken || token.Kind == SyntaxKind.OpenParenToken;
#nullable disable

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
                static @this => @this.ParsePossiblyAttributedStatement() ?? @this.ParseExpressionStatement(attributes: default),
                static @this => SyntaxFactory.EmptyStatement(attributeLists: default, SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken)));
        }

        private StatementSyntax ParsePossiblyAttributedStatement()
            => ParseStatementCore(ParseStatementAttributeDeclarations(), isGlobal: false);

        private SyntaxList<AttributeListSyntax> ParseStatementAttributeDeclarations()
        {
            if (this.CurrentToken.Kind != SyntaxKind.OpenBracketToken)
                return default;

            // See if we should treat this as a collection expression.  At the top-level or statement-level, this should
            // only be considered a collection if followed by a `.`, `?` or `!` (indicating it's a value, not an
            // attribute).
            var resetPoint = GetResetPoint();

            // Grab the first part as a collection expression.
            ParseCollectionExpression();

            // Continue consuming element access expressions for `[x][y]...`.  We have to determine if this is a
            // collection expression being indexed into, or if it's a sequence of attributes.
            var hadBracketArgumentList = false;
            while (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
            {
                ParseBracketedArgumentList();
                hadBracketArgumentList = true;
            }

            // Check the next token to see if it indicates the `[...]` sequence we have is a term or not. This is the
            // same set of tokens that ParsePostFixExpression looks for.
            //
            // Note `SyntaxKind.DotToken` handles both the `[...].Name` case as well as the `[...]..Range` case.
            var isCollectionExpression = this.CurrentToken.Kind
                is SyntaxKind.DotToken
                or SyntaxKind.QuestionToken
                or SyntaxKind.ExclamationToken
                or SyntaxKind.PlusPlusToken
                or SyntaxKind.MinusMinusToken
                or SyntaxKind.MinusGreaterThanToken;

            // Now look for another set of items that indicate that we're not an attribute, but instead are a collection
            // expression misplaced in an invalid top level expression-statement. (like `[] + b`).  These are
            // technically invalid. But checking for this allows us to parse effectively to then give a good semantic
            // error later on. These cases came from: ParseExpressionContinued
            isCollectionExpression = isCollectionExpression
                || IsExpectedBinaryOperator(this.CurrentToken.Kind)
                || IsExpectedAssignmentOperator(this.CurrentToken.Kind)
                || (this.CurrentToken.ContextualKind is SyntaxKind.SwitchKeyword or SyntaxKind.WithKeyword && this.PeekToken(1).Kind is SyntaxKind.OpenBraceToken);

            if (!isCollectionExpression &&
                hadBracketArgumentList &&
                this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                // There are a few things that could be happening here:
                //
                // First is that we have an actual collection expression that we're invoking.  For example:
                //
                //      `[() => {}][rand.NextInt() % x]();`
                //
                // Second would be the start of a local function that returns a tuple.  For example:
                //
                //      `[Attr] (A, B) LocalFunc() { }
                //
                // Have to figure out what the parenthesized thing is in order to parse this.  By parsing out a type
                // and looking for an identifier next, we handle the cases of:
                //
                //      `[Attr] (A, B) LocalFunc() { }
                //      `[Attr] (A, B)[] LocalFunc() { }
                //      `[Attr] (A, B)[,] LocalFunc() { }
                //      `[Attr] (A, B)? LocalFunc() { }
                //      `[Attr] (A, B)* LocalFunc() { }
                //
                // etc.
                //
                // Note: we do not accept the naked `[...](...)` as an invocation of a collection expression.  Collection
                // literals never have a type that itself could possibly be invoked, so this ensures a more natural parse
                // with what users may be expecting here.
                var returnType = this.ParseReturnType();
                isCollectionExpression = ContainsErrorDiagnostic(returnType) || !IsTrueIdentifier();
            }

            // If this was a collection expression, not an attribute declaration, return no attributes so that the
            // caller will parse this out as a collection expression. Otherwise re-parse the code as the actual
            // attribute declarations.
            this.Reset(ref resetPoint);
            var attributes = isCollectionExpression ? default : ParseAttributeDeclarations(inExpressionContext: true);
            this.Release(ref resetPoint);

            return attributes;
        }

        /// <param name="isGlobal">If we're being called while parsing a C# top-level statements (Script or Simple Program).
        /// At the top level in Script, we allow most statements *except* for local-decls/local-funcs.
        /// Those will instead be parsed out as script-fields/methods.</param>
        private StatementSyntax ParseStatementCore(SyntaxList<AttributeListSyntax> attributes, bool isGlobal)
        {
            if (TryReuseStatement(attributes, isGlobal) is { } reused)
            {
                return reused;
            }

            ResetPoint resetPointBeforeStatement = this.GetResetPoint();
            try
            {
                _recursionDepth++;
                StackGuard.EnsureSufficientExecutionStack(_recursionDepth);

                StatementSyntax result;

                // Main switch to handle processing almost any statement.
                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.FixedKeyword:
                        return this.ParseFixedStatement(attributes);
                    case SyntaxKind.BreakKeyword:
                        return this.ParseBreakStatement(attributes);
                    case SyntaxKind.ContinueKeyword:
                        return this.ParseContinueStatement(attributes);
                    case SyntaxKind.TryKeyword:
                    case SyntaxKind.CatchKeyword:
                    case SyntaxKind.FinallyKeyword:
                        return this.ParseTryStatement(attributes);
                    case SyntaxKind.CheckedKeyword:
                    case SyntaxKind.UncheckedKeyword:
                        return this.ParseCheckedStatement(attributes);
                    case SyntaxKind.DoKeyword:
                        return this.ParseDoStatement(attributes);
                    case SyntaxKind.ForKeyword:
                        return this.ParseForOrForEachStatement(attributes);
                    case SyntaxKind.ForEachKeyword:
                        return this.ParseForEachStatement(attributes, awaitTokenOpt: null);
                    case SyntaxKind.GotoKeyword:
                        return this.ParseGotoStatement(attributes);
                    case SyntaxKind.IfKeyword:
                        return this.ParseIfStatement(attributes);
                    case SyntaxKind.ElseKeyword:
                        // Including 'else' keyword to handle 'else without if' error cases 
                        return this.ParseMisplacedElse(attributes);
                    case SyntaxKind.LockKeyword:
                        return this.ParseLockStatement(attributes);
                    case SyntaxKind.ReturnKeyword:
                        return this.ParseReturnStatement(attributes);
                    case SyntaxKind.SwitchKeyword:
                    case SyntaxKind.CaseKeyword: // error recovery case.
                        return this.ParseSwitchStatement(attributes);
                    case SyntaxKind.ThrowKeyword:
                        return this.ParseThrowStatement(attributes);
                    case SyntaxKind.UnsafeKeyword:
                        result = TryParseStatementStartingWithUnsafe(attributes);
                        if (result != null)
                            return result;
                        break;
                    case SyntaxKind.UsingKeyword:
                        return ParseStatementStartingWithUsing(attributes);
                    case SyntaxKind.WhileKeyword:
                        return this.ParseWhileStatement(attributes);
                    case SyntaxKind.OpenBraceToken:
                        return this.ParseBlock(attributes);
                    case SyntaxKind.SemicolonToken:
                        return _syntaxFactory.EmptyStatement(attributes, this.EatToken());
                    case SyntaxKind.IdentifierToken:
                        result = TryParseStatementStartingWithIdentifier(attributes, isGlobal);
                        if (result != null)
                            return result;
                        break;
                }

                return ParseStatementCoreRest(attributes, isGlobal, ref resetPointBeforeStatement);
            }
            finally
            {
                _recursionDepth--;
                this.Release(ref resetPointBeforeStatement);
            }
        }

        private StatementSyntax TryReuseStatement(SyntaxList<AttributeListSyntax> attributes, bool isGlobal)
        {
            if (this.IsIncrementalAndFactoryContextMatches &&
                this.CurrentNode is Syntax.StatementSyntax &&
                !isGlobal && // Top-level statements are reused by ParseMemberDeclarationOrStatementCore when possible.
                attributes.Count == 0)
            {
                return (StatementSyntax)this.EatNode();
            }

            return null;
        }

        private StatementSyntax ParseStatementCoreRest(SyntaxList<AttributeListSyntax> attributes, bool isGlobal, ref ResetPoint resetPointBeforeStatement)
        {
            isGlobal = isGlobal && IsScript;

            if (!this.IsPossibleLocalDeclarationStatement(isGlobal))
            {
                return this.ParseExpressionStatement(attributes);
            }

            if (isGlobal)
            {
                // if we're at the global script level, then we don't support local-decls or
                // local-funcs. The caller instead will look for those and parse them as
                // fields/methods in the global script scope.
                return null;
            }

            bool beginsWithAwait = this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword;
            var result = ParseLocalDeclarationStatement(attributes);

            // didn't get any sort of statement.  This was something else entirely
            // (like just a `}`).  No need to retry anything here.  Just reset back
            // to where we started from and bail entirely from parsing a statement.
            if (result == null)
            {
                this.Reset(ref resetPointBeforeStatement);
                return null;
            }

            if (result.ContainsDiagnostics &&
                beginsWithAwait &&
                !IsInAsync)
            {
                // Local decl had issues.  We were also starting with 'await' in a non-async
                // context. Retry parsing this as if we were in an 'async' context as it's much
                // more likely that this was a misplace await-expr' than a local decl.
                //
                // The user will still get a later binding error about an await-expr in a non-async
                // context.
                this.Reset(ref resetPointBeforeStatement);

                IsInAsync = true;
                result = ParseExpressionStatement(attributes);
                IsInAsync = false;
            }

            // Didn't want to retry as an `await expr`.  Just return what we actually
            // produced.
            return result;
        }

        private StatementSyntax TryParseStatementStartingWithIdentifier(SyntaxList<AttributeListSyntax> attributes, bool isGlobal)
        {
            if (this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword &&
                this.PeekToken(1).Kind == SyntaxKind.ForEachKeyword)
            {
                return this.ParseForEachStatement(attributes, this.EatContextualToken(SyntaxKind.AwaitKeyword));
            }
            else if (IsPossibleAwaitUsing())
            {
                if (PeekToken(2).Kind == SyntaxKind.OpenParenToken)
                {
                    // `await using Type ...` is handled below in ParseLocalDeclarationStatement
                    return this.ParseUsingStatement(attributes, this.EatContextualToken(SyntaxKind.AwaitKeyword));
                }
            }
            else if (this.IsPossibleLabeledStatement())
            {
                return this.ParseLabeledStatement(attributes);
            }
            else if (this.IsPossibleYieldStatement())
            {
                return this.ParseYieldStatement(attributes);
            }
            else if (this.IsPossibleAwaitExpressionStatement())
            {
                return this.ParseExpressionStatement(attributes);
            }
            else if (this.IsQueryExpression(mayBeVariableDeclaration: true, mayBeMemberDeclaration: isGlobal && IsScript))
            {
                return this.ParseExpressionStatement(attributes, this.ParseQueryExpression(0));
            }

            return null;
        }

        private StatementSyntax ParseStatementStartingWithUsing(SyntaxList<AttributeListSyntax> attributes)
            => PeekToken(1).Kind == SyntaxKind.OpenParenToken ? ParseUsingStatement(attributes) : ParseLocalDeclarationStatement(attributes);

        // Checking for brace to disambiguate between unsafe statement and unsafe local function
        private StatementSyntax TryParseStatementStartingWithUnsafe(SyntaxList<AttributeListSyntax> attributes)
            => IsPossibleUnsafeStatement() ? ParseUnsafeStatement(attributes) : null;

        private bool IsPossibleAwaitUsing()
            => CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword && PeekToken(1).Kind == SyntaxKind.UsingKeyword;

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
            return this.CurrentToken.ContextualKind == SyntaxKind.YieldKeyword &&
                   this.PeekToken(1).Kind is SyntaxKind.ReturnKeyword or SyntaxKind.BreakKeyword;
        }

        private bool IsPossibleLocalDeclarationStatement(bool isGlobalScriptLevel)
        {
            // This method decides whether to parse a statement as a
            // declaration or as an expression statement. In the old
            // compiler it would simply call IsLocalDeclaration.

            var tk = this.CurrentToken.Kind;
            if (tk == SyntaxKind.RefKeyword ||
                IsDeclarationModifier(tk) || // treat `static int x = 2;` as a local variable declaration
                (SyntaxFacts.IsPredefinedType(tk) &&
                    this.PeekToken(1).Kind is not SyntaxKind.DotToken // e.g. `int.Parse()` is an expression
                                           and not SyntaxKind.OpenParenToken)) // e.g. `int (x, y)` is an error decl expression
            {
                return true;
            }

            // note: `using (` and `await using (` are already handled in ParseStatementCore.
            if (tk == SyntaxKind.UsingKeyword)
            {
                Debug.Assert(PeekToken(1).Kind != SyntaxKind.OpenParenToken);
                return true;
            }

            if (IsPossibleAwaitUsing())
            {
                Debug.Assert(PeekToken(2).Kind != SyntaxKind.OpenParenToken);
                return true;
            }

            if (IsPossibleScopedKeyword(isFunctionPointerParameter: false))
            {
                return true;
            }

            tk = this.CurrentToken.ContextualKind;

            var isPossibleModifier =
                IsAdditionalLocalFunctionModifier(tk)
                && (tk is not (SyntaxKind.AsyncKeyword or SyntaxKind.ScopedKeyword) || ShouldContextualKeywordBeTreatedAsModifier(parsingStatementNotDeclaration: true));
            if (isPossibleModifier)
            {
                return true;
            }

            return IsPossibleFirstTypedIdentifierInLocalDeclarationStatement(isGlobalScriptLevel);
        }

        private bool IsPossibleScopedKeyword(bool isFunctionPointerParameter)
        {
            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);
            return ParsePossibleScopedKeyword(isFunctionPointerParameter, isLambdaParameter: false) != null;
        }

        private bool IsPossibleFirstTypedIdentifierInLocalDeclarationStatement(bool isGlobalScriptLevel)
        {
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
            // In this case we don't want to parse this as a local declaration like:
            //
            //      Task.await Task
            //
            // This does not represent user intent, and it causes all sorts of problems to higher 
            // layers.  This is because both the parse tree is strange, and the symbol tables have
            // entries that throw things off (like a bogus 'Task' local).
            //
            // Note that we explicitly do this check when we see that the code spreads over multiple 
            // lines.  We don't want this if the user has actually written "X.Y z"
            var tk = this.CurrentToken.ContextualKind;

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

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            ScanTypeFlags st = this.ScanType();

            // We could always return true for st == AliasQualName in addition to MustBeType on the first line, however, we want it to return false in the case where
            // CurrentToken.Kind != SyntaxKind.Identifier so that error cases, like: A::N(), are not parsed as variable declarations and instead are parsed as A.N() where we can give
            // a better error message saying "did you meant to use a '.'?"
            if (st == ScanTypeFlags.MustBeType && this.CurrentToken.Kind is not SyntaxKind.DotToken and not SyntaxKind.OpenParenToken)
            {
                return true;
            }

            if (st == ScanTypeFlags.NotType)
            {
                return false;
            }

            if (this.CurrentToken.Kind != SyntaxKind.IdentifierToken)
            {
                // In the case of something like:
                //     List<SomeType>
                //     if
                // we know that we're in an error case, as the following keyword must be the start of a new statement.
                // We'd prefer to assume that this is an incomplete local declaration over an expression, as it's more likely
                // the user is just in the middle of writing a local declaration, and not an expression.
                return st == ScanTypeFlags.GenericTypeOrExpression && (IsDefiniteStatement() || IsTypeDeclarationStart() || IsAccessibilityModifier(CurrentToken.Kind));
            }

            // T? and T* might start an expression, we need to parse further to disambiguate:
            if (isGlobalScriptLevel)
            {
                if (st == ScanTypeFlags.PointerOrMultiplication)
                {
                    return false;
                }
                else if (st == ScanTypeFlags.NullableType)
                {
                    return IsPossibleDeclarationStatementFollowingNullableType(isGlobalScriptLevel);
                }
            }

            return true;
        }

        private bool IsPossibleTopLevelUsingLocalDeclarationStatement()
        {
            if (this.CurrentToken.Kind != SyntaxKind.UsingKeyword)
            {
                return false;
            }

            var tk = PeekToken(1).Kind;

            if (tk == SyntaxKind.RefKeyword)
            {
                return true;
            }

            if (IsDeclarationModifier(tk)) // treat `const int x = 2;` as a local variable declaration
            {
                if (tk != SyntaxKind.StaticKeyword) // For `static` we still need to make sure we have a typed identifier after it, because `using static type;` is a valid using directive.
                {
                    return true;
                }
            }
            else if (SyntaxFacts.IsPredefinedType(tk))
            {
                return true;
            }

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            // Skip 'using' keyword
            EatToken();

            if (IsPossibleScopedKeyword(isFunctionPointerParameter: false))
            {
                return true;
            }

            if (tk == SyntaxKind.StaticKeyword)
            {
                // Skip 'static' keyword
                EatToken();
            }

            return IsPossibleFirstTypedIdentifierInLocalDeclarationStatement(isGlobalScriptLevel: false);
        }

        // Looks ahead for a declaration of a field, property or method declaration following a nullable type T?.
        private bool IsPossibleDeclarationStatementFollowingNullableType(bool isGlobalScriptLevel)
        {
            if (IsFieldDeclaration(isEvent: false, isGlobalScriptLevel))
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
            //      T? Goo {
            //
            // Importantly, we don't consider `T? Goo =>` to be the start of a property.  This is because it's legal to write:
            //      T ? Goo => Goo : Bar => Bar
            if (this.CurrentToken.Kind is SyntaxKind.OpenBraceToken)
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

            return this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.SemicolonToken;
        }

        private bool IsPossibleMethodDeclarationFollowingNullableType()
        {
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfMethodSignature;

            var paramList = this.ParseParenthesizedParameterList(forExtension: false);

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

        /// <summary>
        /// Determines if the current 'delegate' keyword starts an anonymous delegate expression
        /// rather than a delegate type declaration.
        /// </summary>
        /// <returns>
        /// true if this is an anonymous delegate expression (e.g., delegate { } or delegate (params) { }),
        /// false if this is likely a delegate type declaration (e.g., delegate Type Name(params);)
        /// </returns>
        private bool IsAnonymousDelegateExpression()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.DelegateKeyword);

            var nextToken = this.PeekToken(1);

            // delegate { } is definitely an anonymous delegate
            if (nextToken.Kind == SyntaxKind.OpenBraceToken)
            {
                return true;
            }

            // If not followed by '(', it's a delegate type declaration
            if (nextToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            // Now we have 'delegate (' - need to distinguish:
            // - Anonymous delegate: delegate (params) { }
            // - Delegate declaration: delegate (TupleType) Name(params);
            //
            // Try to parse what's in the parentheses as a tuple type, and check if
            // it's followed by an identifier (which would indicate a delegate declaration).

            using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: true);

            // Skip 'delegate'
            this.EatToken();

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenParenToken);
            // Skip '('
            this.EatToken();

            // Try to scan as a tuple type
            var scanResult = this.ScanTupleType(out _);

            // If it successfully scanned as a tuple type and is followed by an identifier,
            // it's a delegate type declaration with a tuple return type.
            if (scanResult == ScanTypeFlags.TupleType && this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                return false;
            }

            // Otherwise, assume it's an anonymous delegate expression.
            return true;
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
            //   new partial []
            //
            if (SyntaxFacts.GetBaseTypeDeclarationKind(nextToken.Kind) != SyntaxKind.None)
            {
                return false;
            }

            DeclarationModifiers modifier = GetModifierExcludingScoped(nextToken);
            if (modifier == DeclarationModifiers.Partial)
            {
                if (SyntaxFacts.IsPredefinedType(PeekToken(2).Kind))
                {
                    return false;
                }

                // class, struct, enum, interface keywords, but also other modifiers that are not allowed after 
                // partial keyword but start class declaration, so we can assume the user just swapped them.
                if (IsTypeModifierOrTypeKeyword(PeekToken(2).Kind))
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

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            // skips new keyword
            EatToken();
            ScanTypeFlags st = this.ScanType();

            return !IsPossibleMemberName() || st == ScanTypeFlags.NotType;
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

        private BlockSyntax ParsePossiblyAttributedBlock() => ParseBlock(this.ParseAttributeDeclarations(inExpressionContext: false));

        /// <summary>
        /// Used to parse the block-body for a method or accessor.  For blocks that appear *inside*
        /// method bodies, call <see cref="ParseBlock"/>.
        /// </summary>
        /// <param name="isAccessorBody">If is true, then we produce a special diagnostic if the
        /// open brace is missing.</param>
        private BlockSyntax ParseMethodOrAccessorBodyBlock(SyntaxList<AttributeListSyntax> attributes, bool isAccessorBody)
        {
            // Check again for incremental re-use.  This way if a method signature is edited we can
            // still quickly re-sync on the body.
            if (this.IsIncrementalAndFactoryContextMatches &&
                this.CurrentNodeKind == SyntaxKind.Block &&
                attributes.Count == 0)
            {
                return (BlockSyntax)this.EatNode();
            }

            // There's a special error code for a missing token after an accessor keyword
            CSharpSyntaxNode openBrace = isAccessorBody && this.CurrentToken.Kind != SyntaxKind.OpenBraceToken
                ? this.AddError(
                    SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                    IsFeatureEnabled(MessageID.IDS_FeatureExpressionBodiedAccessor)
                        ? ErrorCode.ERR_SemiOrLBraceOrArrowExpected
                        : ErrorCode.ERR_SemiOrLBraceExpected)
                : this.EatToken(SyntaxKind.OpenBraceToken);

            var statements = _pool.Allocate<StatementSyntax>();
            this.ParseStatements(ref openBrace, statements, stopOnSwitchSections: false);

            var block = _syntaxFactory.Block(
                attributes,
                (SyntaxToken)openBrace,
                // Force creation a many-children list, even if only 1, 2, or 3 elements in the statement list.
                IsLargeEnoughNonEmptyStatementList(statements)
                    ? new SyntaxList<StatementSyntax>(SyntaxList.List(((SyntaxListBuilder)statements).ToArray()))
                    : statements,
                this.EatToken(SyntaxKind.CloseBraceToken));

            _pool.Free(statements);
            return block;
        }

        /// <summary>
        /// Used to parse normal blocks that appear inside method bodies.  For the top level block
        /// of a method/accessor use <see cref="ParseMethodOrAccessorBodyBlock"/>.
        /// </summary>
        private BlockSyntax ParseBlock(SyntaxList<AttributeListSyntax> attributes)
        {
            // Check again for incremental re-use, since ParseBlock is called from a bunch of places
            // other than ParseStatementCore()
            // Also, if our caller produced any attributes, we don't want to reuse an existing block syntax
            // directly as we don't want to lose those attributes
            if (this.IsIncrementalAndFactoryContextMatches && this.CurrentNodeKind == SyntaxKind.Block && attributes.Count == 0)
                return (BlockSyntax)this.EatNode();

            CSharpSyntaxNode openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            var statements = _pool.Allocate<StatementSyntax>();
            this.ParseStatements(ref openBrace, statements, stopOnSwitchSections: false);

            return _syntaxFactory.Block(
                attributes,
                (SyntaxToken)openBrace,
                _pool.ToListAndFree(statements),
                this.EatToken(SyntaxKind.CloseBraceToken));
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

            int lastTokenPosition = -1;
            while (this.CurrentToken.Kind is not SyntaxKind.CloseBraceToken and not SyntaxKind.EndOfFileToken
                && !(stopOnSwitchSections && this.IsPossibleSwitchSection())
                && IsMakingProgress(ref lastTokenPosition))
            {
                if (this.IsPossibleStatement())
                {
                    var statement = this.ParsePossiblyAttributedStatement();
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
                || this.IsPossibleStatement();
        }

        private PostSkipAction SkipBadStatementListTokens(SyntaxListBuilder<StatementSyntax> statements, SyntaxKind expected, out GreenNode trailingTrivia)
        {
            return this.SkipBadListTokensWithExpectedKindHelper(
                statements,
                // We know we have a bad statement, so it can't be a local
                // function, meaning we shouldn't consider accessibility
                // modifiers to be the start of a statement
                static p => !p.IsPossibleStatement(),
                static (p, _) => p.CurrentToken.Kind == SyntaxKind.CloseBraceToken,
                expected,
                closeKind: SyntaxKind.None,
                out trailingTrivia);
        }

        private bool IsDefiniteStatement()
        {
            var tk = this.CurrentToken.Kind;
            // Only those cases that can be certain start a new statement, regardless of context
            switch (tk)
            {
                case SyntaxKind.FixedKeyword:
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.TryKeyword:
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.DoKeyword:
                case SyntaxKind.ForKeyword:
                case SyntaxKind.ForEachKeyword:
                case SyntaxKind.GotoKeyword:
                case SyntaxKind.IfKeyword:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.LockKeyword:
                case SyntaxKind.ReturnKeyword:
                case SyntaxKind.UnsafeKeyword:
                case SyntaxKind.UsingKeyword:
                case SyntaxKind.WhileKeyword:
                case SyntaxKind.VolatileKeyword:
                case SyntaxKind.ExternKeyword:
                case SyntaxKind.CaseKeyword: // for parsing an errant case without a switch.
                    return true;

                default:
                    return false;
            }
        }

        private bool IsPossibleStatement()
        {
            if (IsDefiniteStatement())
            {
                return true;
            }

            var tk = this.CurrentToken.Kind;
            switch (tk)
            {
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                case SyntaxKind.ThrowKeyword:
                case SyntaxKind.SwitchKeyword:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OpenBracketToken:
                    return true;

                case SyntaxKind.IdentifierToken:
                    return IsTrueIdentifier();

                default:
                    return IsPredefinedType(tk)
                        || IsPossibleExpression();
            }
        }

        private FixedStatementSyntax ParseFixedStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            var @fixed = this.EatToken(SyntaxKind.FixedKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfFixedStatement;
            var decl = ParseParenthesizedVariableDeclaration(VariableFlags.None, scopedKeyword: null);
            _termState = saveTerm;

            return _syntaxFactory.FixedStatement(
                attributes,
                @fixed,
                openParen,
                decl,
                this.EatToken(SyntaxKind.CloseParenToken),
                this.ParseEmbeddedStatement());
        }

        private bool IsEndOfFixedStatement()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.OpenBraceToken or SyntaxKind.SemicolonToken;
        }

        private StatementSyntax ParseEmbeddedStatement()
        {
            // ParseEmbeddedStatement is called through many recursive statement parsing cases. We
            // keep the body exceptionally simple, and we optimize for the common case, to ensure it
            // is inlined into the callers.  Otherwise the overhead of this single method can have a
            // deep impact on the number of recursive calls we can make (more than a hundred during
            // empirical testing).

            return parseEmbeddedStatementRest(this.ParsePossiblyAttributedStatement());

            StatementSyntax parseEmbeddedStatementRest(StatementSyntax statement)
            {
                if (statement == null)
                {
                    // The consumers of embedded statements are expecting to receive a non-null statement 
                    // yet there are several error conditions that can lead ParseStatementCore to return 
                    // null.  When that occurs create an error empty Statement and return it to the caller.
                    return SyntaxFactory.EmptyStatement(attributeLists: default, EatToken(SyntaxKind.SemicolonToken));
                }

                // In scripts, stand-alone expression statements may not be followed by semicolons.
                // ParseExpressionStatement hides the error.
                // However, embedded expression statements are required to be followed by semicolon. 
                if (statement.Kind == SyntaxKind.ExpressionStatement &&
                    IsScript)
                {
                    var expressionStatementSyntax = (ExpressionStatementSyntax)statement;
                    var semicolonToken = expressionStatementSyntax.SemicolonToken;

                    // Do not add a new error if the same error was already added.
                    if (semicolonToken.IsMissing &&
                        !semicolonToken.GetDiagnostics().Contains(diagnosticInfo => (ErrorCode)diagnosticInfo.Code == ErrorCode.ERR_SemicolonExpected))
                    {
                        semicolonToken = this.AddError(semicolonToken, ErrorCode.ERR_SemicolonExpected);
                        return expressionStatementSyntax.Update(expressionStatementSyntax.AttributeLists, expressionStatementSyntax.Expression, semicolonToken);
                    }
                }

                return statement;
            }
        }

        private BreakStatementSyntax ParseBreakStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            return _syntaxFactory.BreakStatement(
                attributes,
                this.EatToken(SyntaxKind.BreakKeyword),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private ContinueStatementSyntax ParseContinueStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            return _syntaxFactory.ContinueStatement(
                attributes,
                this.EatToken(SyntaxKind.ContinueKeyword),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private TryStatementSyntax ParseTryStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.TryKeyword or SyntaxKind.CatchKeyword or SyntaxKind.FinallyKeyword);

            // We are called into on try/catch/finally, so eating the try may actually fail.
            var @try = this.EatToken(SyntaxKind.TryKeyword);

            BlockSyntax tryBlock;
            if (@try.IsMissing)
            {
                // If there was no actual `try`, then we got here because of a misplaced `catch`/`finally`.  In that
                // case just synthesize a fully missing try-block.  We will already have issued a diagnostic on the
                // `try` keyword, so we don't need to issue any more.

                Debug.Assert(@try.ContainsDiagnostics);
                Debug.Assert(this.CurrentToken.Kind is SyntaxKind.CatchKeyword or SyntaxKind.FinallyKeyword);

                tryBlock = missingBlock();
            }
            else
            {
                var saveTerm = _termState;
                _termState |= TerminatorState.IsEndOfTryBlock;
                tryBlock = this.ParsePossiblyAttributedBlock();
                _termState = saveTerm;
            }

            SyntaxListBuilder<CatchClauseSyntax> catchClauses = default;
            FinallyClauseSyntax finallyClause = null;
            if (this.CurrentToken.Kind == SyntaxKind.CatchKeyword)
            {
                catchClauses = _pool.Allocate<CatchClauseSyntax>();
                while (this.CurrentToken.Kind == SyntaxKind.CatchKeyword)
                {
                    catchClauses.Add(this.ParseCatchClause());
                }
            }

            if (this.CurrentToken.Kind == SyntaxKind.FinallyKeyword)
            {
                finallyClause = _syntaxFactory.FinallyClause(
                    this.EatToken(),
                    this.ParsePossiblyAttributedBlock());
            }

            if (catchClauses.IsNull && finallyClause == null)
            {
                if (!ContainsErrorDiagnostic(tryBlock))
                    tryBlock = this.AddErrorToLastToken(tryBlock, ErrorCode.ERR_ExpectedEndTry);

                // synthesize missing tokens for "finally { }":
                finallyClause = _syntaxFactory.FinallyClause(
                    SyntaxFactory.MissingToken(SyntaxKind.FinallyKeyword),
                    missingBlock());
            }

            return _syntaxFactory.TryStatement(
                attributes,
                @try,
                tryBlock,
                _pool.ToListAndFree(catchClauses),
                finallyClause);

            BlockSyntax missingBlock()
                => _syntaxFactory.Block(
                    attributeLists: default,
                    SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken),
                    statements: default,
                    SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken));
        }

        private bool IsEndOfTryBlock()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseBraceToken or SyntaxKind.CatchKeyword or SyntaxKind.FinallyKeyword;
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

                _termState |= TerminatorState.IsEndOfFilterClause;
                var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                var filterExpression = this.ParseExpressionCore();

                _termState = saveTerm;
                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                filter = _syntaxFactory.CatchFilterClause(whenKeyword, openParen, filterExpression, closeParen);
            }

            _termState |= TerminatorState.IsEndOfCatchBlock;
            var block = this.ParsePossiblyAttributedBlock();
            _termState = saveTerm;

            return _syntaxFactory.CatchClause(@catch, decl, filter, block);
        }

        private bool IsEndOfCatchClause()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseParenToken
                or SyntaxKind.OpenBraceToken
                or SyntaxKind.CloseBraceToken
                or SyntaxKind.CatchKeyword
                or SyntaxKind.FinallyKeyword;
        }

        private bool IsEndOfFilterClause()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseParenToken
                or SyntaxKind.OpenBraceToken
                or SyntaxKind.CloseBraceToken
                or SyntaxKind.CatchKeyword
                or SyntaxKind.FinallyKeyword;
        }
        private bool IsEndOfCatchBlock()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseBraceToken
                or SyntaxKind.CatchKeyword
                or SyntaxKind.FinallyKeyword;
        }

        private StatementSyntax ParseCheckedStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.CheckedKeyword or SyntaxKind.UncheckedKeyword);

            if (this.PeekToken(1).Kind == SyntaxKind.OpenParenToken)
            {
                return this.ParseExpressionStatement(attributes);
            }

            var keyword = this.EatToken();
            return _syntaxFactory.CheckedStatement(
                SyntaxFacts.GetCheckStatement(keyword.Kind),
                attributes,
                keyword,
                this.ParsePossiblyAttributedBlock());
        }

        private DoStatementSyntax ParseDoStatement(SyntaxList<AttributeListSyntax> attributes)
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

            return _syntaxFactory.DoStatement(
                attributes,
                @do,
                statement,
                @while,
                openParen,
                expression,
                this.EatToken(SyntaxKind.CloseParenToken),
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private bool IsEndOfDoWhileExpression()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.SemicolonToken;
        }

        private StatementSyntax ParseForOrForEachStatement(SyntaxList<AttributeListSyntax> attributes)
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
            using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForKeyword);
            this.EatToken();
            if (this.EatToken().Kind == SyntaxKind.OpenParenToken &&
                this.ScanType() != ScanTypeFlags.NotType &&
                this.EatToken().Kind == SyntaxKind.IdentifierToken &&
                this.EatToken().Kind == SyntaxKind.InKeyword)
            {
                // Looks like a foreach statement.  Parse it that way instead
                resetPoint.Reset();
                return this.ParseForEachStatement(attributes, awaitTokenOpt: null);
            }
            else
            {
                // Normal for statement.
                resetPoint.Reset();
                return this.ParseForStatement(attributes);
            }
        }

        private ForStatementSyntax ParseForStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ForKeyword);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfForStatementArgument;

            var forToken = this.EatToken(SyntaxKind.ForKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var (variableDeclaration, initializers) = eatVariableDeclarationOrInitializers();

            var firstSemicolonToken = eatCommaOrSemicolon();
            var condition = this.CurrentToken.Kind is not SyntaxKind.SemicolonToken and not SyntaxKind.CommaToken
                ? this.ParseExpressionCore()
                : null;

            // Used to place skipped tokens we run into when parsing the incrementors list.
            var secondSemicolonToken = eatCommaOrSemicolon();

            // Do allow semicolons (with diagnostics) in the incrementors list.  This allows us to consume
            // accidental extra incrementors that should have been separated by commas.
            var incrementors = this.CurrentToken.Kind != SyntaxKind.CloseParenToken
                ? parseForStatementExpressionList(ref secondSemicolonToken, allowSemicolonAsSeparator: true)
                : default;

            var forStatement = _syntaxFactory.ForStatement(
                attributes,
                forToken,
                openParen,
                variableDeclaration,
                initializers,
                firstSemicolonToken,
                condition,
                secondSemicolonToken,
                incrementors,
                eatUnexpectedTokensAndCloseParenToken(),
                ParseEmbeddedStatement());

            _termState = saveTerm;

            return forStatement;

            (VariableDeclarationSyntax variableDeclaration, SeparatedSyntaxList<ExpressionSyntax> initializers) eatVariableDeclarationOrInitializers()
            {
                using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

                // Here can be either a declaration or an expression statement list.  Scan
                // for a declaration first.
                bool isDeclaration = false;

                if (this.CurrentToken.ContextualKind == SyntaxKind.ScopedKeyword)
                {
                    if (this.PeekToken(1).Kind == SyntaxKind.RefKeyword)
                    {
                        isDeclaration = true;
                    }
                    else
                    {
                        this.EatToken();
                        isDeclaration = ScanType() != ScanTypeFlags.NotType && this.CurrentToken.Kind == SyntaxKind.IdentifierToken;
                        resetPoint.Reset();
                    }
                }
                else if (this.CurrentToken.Kind == SyntaxKind.RefKeyword)
                {
                    isDeclaration = true;
                }

                if (!isDeclaration)
                {
                    isDeclaration = !this.IsQueryExpression(mayBeVariableDeclaration: true, mayBeMemberDeclaration: false) &&
                                    this.ScanType() != ScanTypeFlags.NotType &&
                                    this.IsTrueIdentifier();
                    resetPoint.Reset();
                }

                if (isDeclaration)
                {
                    return (ParseParenthesizedVariableDeclaration(VariableFlags.ForStatement, ParsePossibleScopedKeyword(isFunctionPointerParameter: false, isLambdaParameter: false)), initializers: default);
                }
                else if (this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
                {
                    // Not a type followed by an identifier, so it must be the initializer expression list.
                    //
                    // Do not consume semicolons here as they are used to separate the initializers out from the
                    // condition of the for loop.
                    return (variableDeclaration: null, parseForStatementExpressionList(ref openParen, allowSemicolonAsSeparator: false));
                }
                else
                {
                    return default;
                }
            }

            SyntaxToken eatCommaOrSemicolon()
                => this.CurrentToken.Kind is SyntaxKind.CommaToken
                    ? this.EatTokenAsKind(SyntaxKind.SemicolonToken)
                    : this.EatToken(SyntaxKind.SemicolonToken);

            SyntaxToken eatUnexpectedTokensAndCloseParenToken()
            {
                var skippedTokens = _pool.Allocate();

                while (this.CurrentToken.Kind is SyntaxKind.SemicolonToken or SyntaxKind.CommaToken)
                    skippedTokens.Add(this.EatTokenEvenWithIncorrectKind(SyntaxKind.CloseParenToken));

                var result = this.EatToken(SyntaxKind.CloseParenToken);
                return AddLeadingSkippedSyntax(result, _pool.ToTokenListAndFree(skippedTokens).Node);
            }

            // Parses out a sequence of expressions.  Both for the initializer section (the `for (initializer1,
            // initializer2, ...` section), as well as the incrementor section (the `for (;; incrementor1, incrementor2,
            // ...` section).
            SeparatedSyntaxList<ExpressionSyntax> parseForStatementExpressionList(ref SyntaxToken startToken, bool allowSemicolonAsSeparator)
                => ParseCommaSeparatedSyntaxList(
                    ref startToken,
                    SyntaxKind.CloseParenToken,
                    static @this => @this.IsPossibleExpression(),
                    static @this => @this.ParseExpressionCore(),
                    skipBadForStatementExpressionListTokens,
                    allowTrailingSeparator: false,
                    requireOneElement: false,
                    allowSemicolonAsSeparator);

            static PostSkipAction skipBadForStatementExpressionListTokens(
                LanguageParser @this, ref SyntaxToken startToken, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                if (@this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.SemicolonToken)
                    return PostSkipAction.Abort;

                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref startToken, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind || p.CurrentToken.Kind == SyntaxKind.SemicolonToken,
                    expectedKind, closeKind);
            }
        }

        private bool IsEndOfForStatementArgument()
        {
            return this.CurrentToken.Kind is SyntaxKind.SemicolonToken or SyntaxKind.CloseParenToken or SyntaxKind.OpenBraceToken;
        }

        private CommonForEachStatementSyntax ParseForEachStatement(
            SyntaxList<AttributeListSyntax> attributes, SyntaxToken awaitTokenOpt)
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
                skippedForToken = this.AddError(skippedForToken, ErrorCode.ERR_SyntaxError, SyntaxFacts.GetText(SyntaxKind.ForEachKeyword));
                @foreach = ConvertToMissingWithTrailingTrivia(skippedForToken, SyntaxKind.ForEachKeyword);
            }
            else
            {
                @foreach = this.EatToken(SyntaxKind.ForEachKeyword);
            }

            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            var variable = ParseExpressionOrDeclaration(ParseTypeMode.Normal, permitTupleDesignation: true);
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

                    return _syntaxFactory.ForEachStatement(attributes, awaitTokenOpt, @foreach, openParen, decl.Type, identifier, @in, expression, closeParen, statement);
                }
            }

            return _syntaxFactory.ForEachVariableStatement(attributes, awaitTokenOpt, @foreach, openParen, variable, @in, expression, closeParen, statement);
        }

        //
        // Parse an expression where a declaration expression would be permitted. This is suitable for use after
        // the `out` keyword in an argument list, or in the elements of a tuple literal (because they may
        // be on the left-hand-side of a positional subpattern). The first element of a tuple is handled slightly
        // differently, as we check for the comma before concluding that the identifier should cause a
        // disambiguation. For example, for the input `(A < B , C > D)`, we treat this as a tuple with
        // two elements, because if we considered the `A<B,C>` to be a type, it wouldn't be a tuple at
        // all. Since we don't have such a thing as a one-element tuple (even for positional subpattern), the
        // absence of the comma after the `D` means we don't treat the `D` as contributing to the
        // disambiguation of the expression/type. More formally, ...
        //
        // If a sequence of tokens can be parsed(in context) as a* simple-name* (§7.6.3), *member-access* (§7.6.5),
        // or* pointer-member-access* (§18.5.2) ending with a* type-argument-list* (§4.4.1), the token immediately
        // following the closing `>` token is examined, to see if it is
        // - One of `(  )  ]  }  :  ;  ,  .  ?  ==  !=  |  ^  &&  ||  &  [`; or
        // - One of the relational operators `<  >  <=  >=  is as`; or
        // - A contextual query keyword appearing inside a query expression; or
        // - In certain contexts, we treat *identifier* as a disambiguating token.Those contexts are where the
        //   sequence of tokens being disambiguated is immediately preceded by one of the keywords `is`, `case`
        //   or `out`, or arises while parsing the first element of a tuple literal(in which case the tokens are
        //   preceded by `(` or `:` and the identifier is followed by a `,`) or a subsequent element of a tuple literal.
        //
        // If the following token is among this list, or an identifier in such a context, then the *type-argument-list* is
        // retained as part of the *simple-name*, *member-access* or  *pointer-member-access* and any other possible parse
        // of the sequence of tokens is discarded.Otherwise, the *type-argument-list* is not considered to be part of the
        // *simple-name*, *member-access* or *pointer-member-access*, even if there is no other possible parse of the
        // sequence of tokens.Note that these rules are not applied when parsing a *type-argument-list* in a *namespace-or-type-name* (§3.8).
        //
        // See also ScanTypeArgumentList where these disambiguation rules are encoded.
        //
        private ExpressionSyntax ParseExpressionOrDeclaration(ParseTypeMode mode, bool permitTupleDesignation)
        {
            return IsPossibleDeclarationExpression(mode, permitTupleDesignation, out var isScoped)
                ? this.ParseDeclarationExpression(mode, isScoped)
                : this.ParseSubExpression(Precedence.Expression);
        }

        private bool IsPossibleDeclarationExpression(ParseTypeMode mode, bool permitTupleDesignation, out bool isScoped)
        {
            Debug.Assert(mode is ParseTypeMode.Normal or ParseTypeMode.FirstElementOfPossibleTupleLiteral or ParseTypeMode.AfterTupleComma);
            isScoped = false;

            if (this.IsInAsync && this.CurrentToken.ContextualKind == SyntaxKind.AwaitKeyword)
            {
                // can't be a declaration expression.
                return false;
            }

            using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: true);

            if (this.CurrentToken.ContextualKind == SyntaxKind.ScopedKeyword)
            {
                this.EatToken();
                if (ScanType() != ScanTypeFlags.NotType && this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    switch (mode)
                    {
                        case ParseTypeMode.FirstElementOfPossibleTupleLiteral:
                            if (this.PeekToken(1).Kind == SyntaxKind.CommaToken)
                            {
                                isScoped = true;
                                return true;
                            }
                            break;

                        case ParseTypeMode.AfterTupleComma:
                            if (this.PeekToken(1).Kind is SyntaxKind.CommaToken or SyntaxKind.CloseParenToken)
                            {
                                isScoped = true;
                                return true;
                            }
                            break;

                        default:
                            // The other case where we disambiguate between a declaration and expression is before the `in` of a foreach loop.
                            // There we err on the side of accepting a declaration.
                            isScoped = true;
                            return true;
                    }
                }

                resetPoint.Reset();
            }

            bool typeIsVar = IsVarType();
            SyntaxToken lastTokenOfType;
            if (ScanType(mode, out lastTokenOfType) == ScanTypeFlags.NotType)
            {
                return false;
            }

            // check for a designation
            if (!ScanDesignation(permitTupleDesignation && (typeIsVar || IsPredefinedType(lastTokenOfType.Kind))))
            {
                return false;
            }

            switch (mode)
            {
                case ParseTypeMode.FirstElementOfPossibleTupleLiteral:
                    return this.CurrentToken.Kind == SyntaxKind.CommaToken;
                case ParseTypeMode.AfterTupleComma:
                    return this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.CloseParenToken;
                default:
                    // The other case where we disambiguate between a declaration and expression is before the `in` of a foreach loop.
                    // There we err on the side of accepting a declaration.
                    return true;
            }
        }

        /// <summary>
        /// Is the following set of tokens, interpreted as a type, the type <c>var</c>?
        /// </summary>
        private bool IsVarType()
        {
            if (!this.CurrentToken.IsIdentifierVar())
            {
                return false;
            }

            switch (this.PeekToken(1).Kind)
            {
                case SyntaxKind.DotToken:
                case SyntaxKind.ColonColonToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.AsteriskToken:
                case SyntaxKind.QuestionToken:
                case SyntaxKind.LessThanToken:
                    return false;
                default:
                    return true;
            }
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

        private GotoStatementSyntax ParseGotoStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.GotoKeyword);

            var @goto = this.EatToken(SyntaxKind.GotoKeyword);

            SyntaxToken caseOrDefault = null;
            ExpressionSyntax arg = null;
            SyntaxKind kind;

            if (this.CurrentToken.Kind is SyntaxKind.CaseKeyword or SyntaxKind.DefaultKeyword)
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

            return _syntaxFactory.GotoStatement(
                kind, attributes, @goto, caseOrDefault, arg, this.EatToken(SyntaxKind.SemicolonToken));
        }

        private IfStatementSyntax ParseIfStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IfKeyword);

            var stack = ArrayBuilder<(SyntaxToken, SyntaxToken, ExpressionSyntax, SyntaxToken, StatementSyntax, SyntaxToken)>.GetInstance();

            StatementSyntax alternative = null;
            while (true)
            {
                var ifKeyword = this.EatToken(SyntaxKind.IfKeyword);
                var openParen = this.EatToken(SyntaxKind.OpenParenToken);
                var condition = this.ParseExpressionCore();
                var closeParen = this.EatToken(SyntaxKind.CloseParenToken);
                var consequence = this.ParseEmbeddedStatement();

                var elseKeyword = this.CurrentToken.Kind != SyntaxKind.ElseKeyword ?
                    null :
                    this.EatToken(SyntaxKind.ElseKeyword);
                stack.Push((ifKeyword, openParen, condition, closeParen, consequence, elseKeyword));

                if (elseKeyword is null)
                {
                    alternative = null;
                    break;
                }

                if (this.CurrentToken.Kind != SyntaxKind.IfKeyword)
                {
                    alternative = this.ParseEmbeddedStatement();
                    break;
                }

                alternative = TryReuseStatement(attributes: default, isGlobal: false);
                if (alternative is not null)
                {
                    break;
                }
            }

            IfStatementSyntax ifStatement;
            do
            {
                var (ifKeyword, openParen, condition, closeParen, consequence, elseKeyword) = stack.Pop();
                var elseClause = alternative is null ?
                    null :
                    _syntaxFactory.ElseClause(
                        elseKeyword,
                        alternative);
                ifStatement = _syntaxFactory.IfStatement(
                    attributeLists: stack.Any() ? default : attributes,
                    ifKeyword,
                    openParen,
                    condition,
                    closeParen,
                    consequence,
                    elseClause);
                alternative = ifStatement;
            }
            while (stack.Any());

            stack.Free();

            return ifStatement;
        }

        private IfStatementSyntax ParseMisplacedElse(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ElseKeyword);

            return _syntaxFactory.IfStatement(
                attributes,
                this.EatToken(SyntaxKind.IfKeyword, ErrorCode.ERR_ElseCannotStartStatement),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseExpressionCore(),
                this.EatToken(SyntaxKind.CloseParenToken),
                this.ParseExpressionStatement(attributes: default),
                this.ParseElseClauseOpt());
        }

        private ElseClauseSyntax ParseElseClauseOpt()
        {
            return this.CurrentToken.Kind != SyntaxKind.ElseKeyword
                ? null
                : _syntaxFactory.ElseClause(
                    this.EatToken(SyntaxKind.ElseKeyword),
                    this.ParseEmbeddedStatement());
        }

        private LockStatementSyntax ParseLockStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.LockKeyword);
            return _syntaxFactory.LockStatement(
                attributes,
                this.EatToken(SyntaxKind.LockKeyword),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseExpressionCore(),
                this.EatToken(SyntaxKind.CloseParenToken),
                this.ParseEmbeddedStatement());
        }

        private ReturnStatementSyntax ParseReturnStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ReturnKeyword);
            return _syntaxFactory.ReturnStatement(
                attributes,
                this.EatToken(SyntaxKind.ReturnKeyword),
                this.CurrentToken.Kind != SyntaxKind.SemicolonToken ? this.ParsePossibleRefExpression() : null,
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private YieldStatementSyntax ParseYieldStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.YieldKeyword);

            var yieldToken = ConvertToKeyword(this.EatToken());
            SyntaxToken returnOrBreak;
            ExpressionSyntax arg = null;
            SyntaxKind kind;

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

            return _syntaxFactory.YieldStatement(
                kind,
                attributes,
                yieldToken,
                returnOrBreak,
                arg,
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private SwitchStatementSyntax ParseSwitchStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.SwitchKeyword or SyntaxKind.CaseKeyword);

            parseSwitchHeader(out var switchKeyword, out var openParen, out var expression, out var closeParen, out var openBrace);
            var sections = _pool.Allocate<SwitchSectionSyntax>();

            while (this.IsPossibleSwitchSection())
                sections.Add(this.ParseSwitchSection());

            return _syntaxFactory.SwitchStatement(
                attributes,
                switchKeyword,
                openParen,
                expression,
                closeParen,
                openBrace,
                _pool.ToListAndFree(sections),
                this.EatToken(SyntaxKind.CloseBraceToken));

            void parseSwitchHeader(
                out SyntaxToken switchKeyword,
                out SyntaxToken openParen,
                out ExpressionSyntax expression,
                out SyntaxToken closeParen,
                out SyntaxToken openBrace)
            {
                if (this.CurrentToken.Kind is SyntaxKind.CaseKeyword)
                {
                    // try to eat a 'switch' so the user gets a good error message about what's wrong. then directly
                    // creating missing tokens for the rest so they don't get cascading errors.
                    switchKeyword = EatToken(SyntaxKind.SwitchKeyword);
                    openParen = SyntaxFactory.MissingToken(SyntaxKind.OpenParenToken);
                    expression = CreateMissingIdentifierName();
                    closeParen = SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken);
                    openBrace = SyntaxFactory.MissingToken(SyntaxKind.OpenBraceToken);
                }
                else
                {
                    switchKeyword = this.EatToken(SyntaxKind.SwitchKeyword);
                    expression = this.ParseExpressionCore();
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
                        openParen = closeParen = null;
                    }
                    else
                    {
                        // Some other expression has appeared without parens. Give a syntax error.
                        openParen = SyntaxFactory.MissingToken(SyntaxKind.OpenParenToken);
                        expression = this.AddError(expression, ErrorCode.ERR_SwitchGoverningExpressionRequiresParens);
                        closeParen = SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken);
                    }

                    openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
                }
            }
        }

        private bool IsPossibleSwitchSection()
        {
            return this.CurrentToken.Kind == SyntaxKind.CaseKeyword ||
                   (this.CurrentToken.Kind == SyntaxKind.DefaultKeyword && this.PeekToken(1).Kind != SyntaxKind.OpenParenToken);
        }

        private SwitchSectionSyntax ParseSwitchSection()
        {
            Debug.Assert(this.IsPossibleSwitchSection());

            // First, parse case label(s)
            var labels = _pool.Allocate<SwitchLabelSyntax>();
            var statements = _pool.Allocate<StatementSyntax>();

            do
            {
                SwitchLabelSyntax label;
                if (this.CurrentToken.Kind == SyntaxKind.CaseKeyword)
                {
                    var caseKeyword = this.EatToken();

                    if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                    {
                        label = _syntaxFactory.CaseSwitchLabel(
                            caseKeyword,
                            ParseIdentifierName(ErrorCode.ERR_ConstantExpected),
                            this.EatToken(SyntaxKind.ColonToken));
                    }
                    else
                    {
                        var node = ParseExpressionOrPatternForSwitchStatement();

                        // if there is a 'when' token, we treat a case expression as a constant pattern.
                        if (this.CurrentToken.ContextualKind == SyntaxKind.WhenKeyword && node is ExpressionSyntax ex)
                            node = _syntaxFactory.ConstantPattern(ex);

                        if (node.Kind == SyntaxKind.DiscardPattern)
                            node = this.AddError(node, ErrorCode.ERR_DiscardPatternInSwitchStatement);

                        if (node is PatternSyntax pat)
                        {
                            label = _syntaxFactory.CasePatternSwitchLabel(
                                caseKeyword,
                                pat,
                                ParseWhenClause(Precedence.Expression),
                                this.EatToken(SyntaxKind.ColonToken));
                        }
                        else
                        {
                            label = _syntaxFactory.CaseSwitchLabel(
                                caseKeyword,
                                (ExpressionSyntax)node,
                                this.EatToken(SyntaxKind.ColonToken));
                        }
                    }
                }
                else
                {
                    Debug.Assert(this.CurrentToken.Kind == SyntaxKind.DefaultKeyword);
                    label = _syntaxFactory.DefaultSwitchLabel(
                        this.EatToken(SyntaxKind.DefaultKeyword),
                        this.EatToken(SyntaxKind.ColonToken));
                }

                labels.Add(label);
            }
            while (IsPossibleSwitchSection());

            // Next, parse statement list stopping for new sections
            CSharpSyntaxNode tmp = labels[^1];
            this.ParseStatements(ref tmp, statements, stopOnSwitchSections: true);
            labels[^1] = (SwitchLabelSyntax)tmp;

            return _syntaxFactory.SwitchSection(
                _pool.ToListAndFree(labels),
                _pool.ToListAndFree(statements));
        }

        private ThrowStatementSyntax ParseThrowStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.ThrowKeyword);
            return _syntaxFactory.ThrowStatement(
                attributes,
                this.EatToken(SyntaxKind.ThrowKeyword),
                this.CurrentToken.Kind != SyntaxKind.SemicolonToken ? this.ParseExpressionCore() : null,
                this.EatToken(SyntaxKind.SemicolonToken));
        }

        private UnsafeStatementSyntax ParseUnsafeStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.UnsafeKeyword);
            return _syntaxFactory.UnsafeStatement(
                attributes,
                this.EatToken(SyntaxKind.UnsafeKeyword),
                this.ParsePossiblyAttributedBlock());
        }

        private UsingStatementSyntax ParseUsingStatement(SyntaxList<AttributeListSyntax> attributes, SyntaxToken awaitTokenOpt = null)
        {
            var @using = this.EatToken(SyntaxKind.UsingKeyword);
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);

            VariableDeclarationSyntax declaration = null;
            ExpressionSyntax expression = null;

            var resetPoint = this.GetResetPoint();
            ParseUsingExpression(ref declaration, ref expression, ref resetPoint);
            this.Release(ref resetPoint);

            return _syntaxFactory.UsingStatement(
                attributes,
                awaitTokenOpt,
                @using,
                openParen,
                declaration,
                expression,
                this.EatToken(SyntaxKind.CloseParenToken),
                this.ParseEmbeddedStatement());
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
                SyntaxToken scopedKeyword = ParsePossibleScopedKeyword(isFunctionPointerParameter: false, isLambdaParameter: false);

                if (scopedKeyword != null)
                {
                    declaration = ParseParenthesizedVariableDeclaration(VariableFlags.None, scopedKeyword);
                    return;
                }
                else
                {
                    st = this.ScanType();
                }
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
                            declaration = ParseParenthesizedVariableDeclaration(VariableFlags.None, scopedKeyword: null);
                            break;

                        case SyntaxKind.EqualsToken:
                            // Parse it as a decl. If the next token is a : and only one variable was parsed,
                            // convert the whole thing to ?: expression.
                            this.Reset(ref resetPoint);
                            declaration = ParseParenthesizedVariableDeclaration(VariableFlags.None, scopedKeyword: null);

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
                declaration = ParseParenthesizedVariableDeclaration(VariableFlags.None, scopedKeyword: null);
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

        private WhileStatementSyntax ParseWhileStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.WhileKeyword);
            return _syntaxFactory.WhileStatement(
                attributes,
                this.EatToken(SyntaxKind.WhileKeyword),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseExpressionCore(),
                this.EatToken(SyntaxKind.CloseParenToken),
                this.ParseEmbeddedStatement());
        }

        private LabeledStatementSyntax ParseLabeledStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            // We have an identifier followed by a colon. But if the identifier is a contextual keyword in a query context,
            // ParseIdentifier will result in a missing name and Eat(Colon) will fail. We won't make forward progress.
            Debug.Assert(this.IsTrueIdentifier() && this.PeekToken(1).Kind == SyntaxKind.ColonToken);

            return _syntaxFactory.LabeledStatement(
                attributes,
                this.ParseIdentifierToken(),
                this.EatToken(SyntaxKind.ColonToken),
                this.ParsePossiblyAttributedStatement() ?? SyntaxFactory.EmptyStatement(attributeLists: default, EatToken(SyntaxKind.SemicolonToken)));
        }

        /// <summary>
        /// Parses any kind of local declaration statement: local variable or local function.
        /// </summary>
        private StatementSyntax ParseLocalDeclarationStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            SyntaxToken awaitKeyword, usingKeyword;
            bool canParseAsLocalFunction = false;
            if (IsPossibleAwaitUsing())
            {
                awaitKeyword = this.EatContextualToken(SyntaxKind.AwaitKeyword);
                usingKeyword = EatToken();
            }
            else if (this.CurrentToken.Kind == SyntaxKind.UsingKeyword)
            {
                awaitKeyword = null;
                usingKeyword = EatToken();
            }
            else
            {
                awaitKeyword = null;
                usingKeyword = null;
                canParseAsLocalFunction = true;
            }

            var mods = _pool.Allocate();
            this.ParseDeclarationModifiers(mods, isUsingDeclaration: usingKeyword is not null);

            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            try
            {
                SyntaxToken scopedKeyword = ParsePossibleScopedKeyword(isFunctionPointerParameter: false, isLambdaParameter: false);

                // For local functions, 'scoped' is a modifier in LocalFunctionStatementSyntax
                if (scopedKeyword != null)
                {
                    mods.Add(scopedKeyword);
                }

                this.ParseLocalDeclaration(variables,
                    allowLocalFunctions: canParseAsLocalFunction,
                    // A local declaration doesn't have a `(...)` construct.  So no need to stop if we hit a close paren
                    // after a declarator.  Let normal error recovery kick in.
                    stopOnCloseParen: false,
                    attributes,
                    mods.ToList(),
                    scopedKeyword: null,
                    initialFlags: VariableFlags.None,
                    out var type,
                    out var localFunction);

                if (localFunction != null)
                {
                    Debug.Assert(variables.Count == 0);
                    return localFunction;
                }

                if (canParseAsLocalFunction)
                {
                    // If we find an accessibility modifier but no local function it's likely
                    // the user forgot a closing brace. Let's back out of statement parsing.
                    // We check just for a leading accessibility modifier in the syntax because
                    // SkipBadStatementListTokens will not skip attribute lists.
                    if (attributes.Count == 0 && mods.Count > 0 && IsAccessibilityModifier(((SyntaxToken)mods[0]).ContextualKind))
                    {
                        return null;
                    }
                }

                // For locals, 'scoped' is part of ScopedTypeSyntax.
                if (scopedKeyword != null)
                {
                    mods.RemoveLast();
                    type = _syntaxFactory.ScopedType(scopedKeyword, type);
                }

                // We've already reported all modifiers for local_using_declaration as errors
                if (usingKeyword is null)
                {
                    for (int i = 0; i < mods.Count; i++)
                    {
                        var mod = (SyntaxToken)mods[i];

                        if (IsAdditionalLocalFunctionModifier(mod.ContextualKind))
                        {
                            mods[i] = this.AddError(mod, ErrorCode.ERR_BadMemberFlag, mod.Text);
                        }
                    }
                }

                return _syntaxFactory.LocalDeclarationStatement(
                    attributes,
                    awaitKeyword,
                    usingKeyword,
                    mods.ToList(),
                    _syntaxFactory.VariableDeclaration(type, variables.ToList()),
                    this.EatToken(SyntaxKind.SemicolonToken));
            }
            finally
            {
                _pool.Free(variables);
                _pool.Free(mods);
            }
        }

        private SyntaxToken ParsePossibleScopedKeyword(
            bool isFunctionPointerParameter,
            bool isLambdaParameter)
        {
            if (this.CurrentToken.ContextualKind != SyntaxKind.ScopedKeyword)
                return null;

            // In C# 14 we decided that within a lambda 'scoped' would *always* be a keyword.
            if (isLambdaParameter && IsFeatureEnabled(MessageID.IDS_FeatureSimpleLambdaParameterModifiers))
                return this.EatContextualToken(SyntaxKind.ScopedKeyword);

            using var beforeScopedResetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

            var scopedKeyword = this.EatContextualToken(SyntaxKind.ScopedKeyword);

            // trivial case.  scoped ref/out/in  is definitely the scoped keyword.
            if (this.CurrentToken.Kind is (SyntaxKind.RefKeyword or SyntaxKind.OutKeyword or SyntaxKind.InKeyword))
                return scopedKeyword;

            // More complex cases.  We have to check for `scoped Type ...` now.
            using var afterScopedResetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

            if (ScanType() is ScanTypeFlags.NotType ||
                !isValidScopedTypeCase())
            {
                // We didn't see a type, or it wasn't a legal usage of a type.  This is not a scoped-keyword.  Rollback to
                // before the keyword so the caller has to handle it.
                beforeScopedResetPoint.Reset();
                return null;
            }

            // We had a Type syntax in a supported production.  Roll back to just after the scoped-keyword and
            // return it successfully.
            afterScopedResetPoint.Reset();
            return scopedKeyword;

            bool isValidScopedTypeCase()
            {
                // Had `scoped Type ...`
                //
                // 1. This is a function pointer `delegate<T1, scoped T2>`
                // 2. this is a parameter `scoped T x`.

                if (isFunctionPointerParameter)
                {
                    return this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.GreaterThanToken;
                }
                else if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                {
                    return true;
                }

                return false;
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

                result = _syntaxFactory.ParenthesizedVariableDesignation(
                    openParen,
                    _pool.ToListAndFree(listOfDesignations),
                    this.EatToken(SyntaxKind.CloseParenToken));
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
            return CurrentToken.ContextualKind == SyntaxKind.UnderscoreToken
                ? _syntaxFactory.DiscardDesignation(this.EatContextualToken(SyntaxKind.UnderscoreToken))
                : _syntaxFactory.SingleVariableDesignation(this.EatToken(SyntaxKind.IdentifierToken));
        }

        private WhenClauseSyntax ParseWhenClause(Precedence precedence)
        {
            if (this.CurrentToken.ContextualKind != SyntaxKind.WhenKeyword)
            {
                return null;
            }

            return _syntaxFactory.WhenClause(
                this.EatContextualToken(SyntaxKind.WhenKeyword),
                ParseSubExpression(precedence));
        }

#nullable enable

        /// <summary>
        /// Parse a local variable declaration for constructs where the variable declaration is enclosed in parentheses.
        /// Specifically, only for the `fixed (...)` `for(...)` or `using (...)` statements.
        /// </summary>
        private VariableDeclarationSyntax ParseParenthesizedVariableDeclaration(
            VariableFlags initialFlags, SyntaxToken? scopedKeyword)
        {
            var variables = _pool.AllocateSeparated<VariableDeclaratorSyntax>();
            ParseLocalDeclaration(
                variables,
                allowLocalFunctions: false,
                // Always stop on a close paren as the parent `fixed(...)/for(...)/using(...)` statement wants to
                // consume it.
                stopOnCloseParen: true,
                attributes: default,
                mods: default,
                scopedKeyword,
                initialFlags,
                out var type,
                out var localFunction);
            Debug.Assert(localFunction == null);
            return _syntaxFactory.VariableDeclaration(
                type,
                _pool.ToListAndFree(variables));
        }

        private void ParseLocalDeclaration(
            SeparatedSyntaxListBuilder<VariableDeclaratorSyntax> variables,
            bool allowLocalFunctions,
            bool stopOnCloseParen,
            SyntaxList<AttributeListSyntax> attributes,
            SyntaxList<SyntaxToken> mods,
            SyntaxToken? scopedKeyword,
            VariableFlags initialFlags,
            out TypeSyntax type,
            out LocalFunctionStatementSyntax localFunction)
        {
            type = allowLocalFunctions ? ParseReturnType() : this.ParseType();

            if (scopedKeyword != null)
                type = _syntaxFactory.ScopedType(scopedKeyword, type);

            VariableFlags flags = initialFlags | VariableFlags.LocalOrField;
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
                allowLocalFunctions,
                stopOnCloseParen,
                attributes,
                mods,
                out localFunction);
            _termState = saveTerm;

            if (allowLocalFunctions && localFunction == null && type is PredefinedTypeSyntax { Keyword.Kind: SyntaxKind.VoidKeyword })
            {
                type = this.AddError(type, ErrorCode.ERR_NoVoidHere);
            }
        }

#nullable disable

        private bool IsEndOfDeclarationClause()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.ColonToken:
                    return true;
                default:
                    return false;
            }
        }

        private void ParseDeclarationModifiers(SyntaxListBuilder list, bool isUsingDeclaration)
        {
            SyntaxKind k;
            while (IsDeclarationModifier(k = this.CurrentToken.ContextualKind) || IsAdditionalLocalFunctionModifier(k))
            {
                SyntaxToken mod;
                if (k == SyntaxKind.AsyncKeyword)
                {
                    // check for things like "async async()" where async is the type and/or the function name
                    if (!shouldTreatAsModifier())
                    {
                        break;
                    }

                    mod = this.EatContextualToken(k);
                }
                else
                {
                    mod = this.EatToken();
                }

                if (isUsingDeclaration)
                {
                    mod = this.AddError(mod, ErrorCode.ERR_NoModifiersOnUsing);
                }
                else if (k is SyntaxKind.ReadOnlyKeyword or SyntaxKind.VolatileKeyword)
                {
                    mod = this.AddError(mod, ErrorCode.ERR_BadMemberFlag, mod.Text);
                }
                else if (list.Any(mod.RawKind))
                {
                    // check for duplicates, can only be const
                    mod = this.AddError(mod, ErrorCode.ERR_TypeExpected);
                }

                list.Add(mod);
            }

            bool shouldTreatAsModifier()
            {
                using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

                Debug.Assert(this.CurrentToken.Kind == SyntaxKind.IdentifierToken);

                do
                {
                    this.EatToken();

                    if (IsDeclarationModifier(this.CurrentToken.Kind) ||
                        IsAdditionalLocalFunctionModifier(this.CurrentToken.Kind))
                    {
                        return true;
                    }

                    using var _2 = this.GetDisposableResetPoint(resetOnDispose: true);

                    if (ScanType() != ScanTypeFlags.NotType && this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
                    {
                        return true;
                    }
                }
                // If current token might be a contextual modifier we need to check ahead the next token after it
                // If the next token appears to be a modifier, we treat current token as a modifier as well
                // This allows to correctly parse things like local functions with several `async` modifiers
                while (IsAdditionalLocalFunctionModifier(this.CurrentToken.ContextualKind));

                return false;
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
                case SyntaxKind.ExternKeyword:
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
            SyntaxList<AttributeListSyntax> attributes,
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
                        continue;
                    case SyntaxKind.ExternKeyword:
                        continue;
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
            ParameterListSyntax paramList = this.ParseParenthesizedParameterList(forExtension: false);
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

            return _syntaxFactory.LocalFunctionStatement(
                attributes,
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

        private ExpressionStatementSyntax ParseExpressionStatement(SyntaxList<AttributeListSyntax> attributes)
        {
            return ParseExpressionStatement(attributes, this.ParseExpressionCore());
        }

        private ExpressionStatementSyntax ParseExpressionStatement(SyntaxList<AttributeListSyntax> attributes, ExpressionSyntax expression)
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

            return _syntaxFactory.ExpressionStatement(attributes, expression, semicolon);
        }

        public ExpressionSyntax ParseExpression()
        {
            return ParseWithStackGuard(
                static @this => @this.ParseExpressionCore(),
                static @this => @this.CreateMissingIdentifierName());
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
                case SyntaxKind.Utf8StringLiteralToken:
                case SyntaxKind.SingleLineRawStringLiteralToken:
                case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                case SyntaxKind.MultiLineRawStringLiteralToken:
                case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                case SyntaxKind.InterpolatedStringToken:
                case SyntaxKind.InterpolatedStringStartToken:
                case SyntaxKind.InterpolatedVerbatimStringStartToken:
                case SyntaxKind.InterpolatedSingleLineRawStringStartToken:
                case SyntaxKind.InterpolatedMultiLineRawStringStartToken:
                case SyntaxKind.CharacterLiteralToken:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.ColonColonToken: // bad aliased name
                case SyntaxKind.ThrowKeyword:
                case SyntaxKind.StackAllocKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OpenBracketToken: // attributes on a lambda, or a collection expression.
                    return true;
                case SyntaxKind.DotToken when IsAtDotDotToken():
                    return true;
                case SyntaxKind.StaticKeyword:
                    return IsPossibleAnonymousMethodExpression() || IsPossibleLambdaExpression(Precedence.Expression);
                case SyntaxKind.IdentifierToken:
                    // Specifically allow the from contextual keyword, because it can always be the start of an
                    // expression (whether it is used as an identifier or a keyword).
                    return this.IsTrueIdentifier() || this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword;
                default:
                    return IsPredefinedType(tk)
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
                case SyntaxKind.UnsignedRightShiftAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                case SyntaxKind.CoalesceExpression:
                    return true;
                default:
                    return false;
            }
        }

        private enum Precedence : uint
        {
            Expression = 0, // Loosest possible precedence, used to accept all expressions
            Assignment = Expression,
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
            Multiplicative,
            Switch,
            Range,
            Unary,
            Cast,
            PointerIndirection,
            AddressOf,
            Primary,
        }

        private static Precedence GetPrecedence(SyntaxKind op)
        {
            switch (op)
            {
                case SyntaxKind.QueryExpression:
                    return Precedence.Expression;
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return Precedence.Lambda;
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
                case SyntaxKind.UnsignedRightShiftAssignmentExpression:
                case SyntaxKind.CoalesceAssignmentExpression:
                    return Precedence.Assignment;
                case SyntaxKind.CoalesceExpression:
                case SyntaxKind.ThrowExpression:
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
                case SyntaxKind.WithExpression:
                    return Precedence.Switch;
                case SyntaxKind.LeftShiftExpression:
                case SyntaxKind.RightShiftExpression:
                case SyntaxKind.UnsignedRightShiftExpression:
                    return Precedence.Shift;
                case SyntaxKind.AddExpression:
                case SyntaxKind.SubtractExpression:
                    return Precedence.Additive;
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.DivideExpression:
                case SyntaxKind.ModuloExpression:
                    return Precedence.Multiplicative;
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
                case SyntaxKind.AliasQualifiedName:
                case SyntaxKind.AnonymousObjectCreationExpression:
                case SyntaxKind.ArgListExpression:
                case SyntaxKind.ArrayCreationExpression:
                case SyntaxKind.BaseExpression:
                case SyntaxKind.CharacterLiteralExpression:
                case SyntaxKind.CollectionExpression:
                case SyntaxKind.ConditionalAccessExpression:
                case SyntaxKind.DeclarationExpression:
                case SyntaxKind.DefaultExpression:
                case SyntaxKind.DefaultLiteralExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.FalseLiteralExpression:
                case SyntaxKind.FieldExpression:
                case SyntaxKind.GenericName:
                case SyntaxKind.IdentifierName:
                case SyntaxKind.ImplicitArrayCreationExpression:
                case SyntaxKind.ImplicitStackAllocArrayCreationExpression:
                case SyntaxKind.ImplicitObjectCreationExpression:
                case SyntaxKind.InterpolatedStringExpression:
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.NullLiteralExpression:
                case SyntaxKind.NumericLiteralExpression:
                case SyntaxKind.ObjectCreationExpression:
                case SyntaxKind.ParenthesizedExpression:
                case SyntaxKind.PointerMemberAccessExpression:
                case SyntaxKind.PostDecrementExpression:
                case SyntaxKind.PostIncrementExpression:
                case SyntaxKind.PredefinedType:
                case SyntaxKind.RefExpression:
                case SyntaxKind.SimpleMemberAccessExpression:
                case SyntaxKind.StackAllocArrayCreationExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.Utf8StringLiteralExpression:
                case SyntaxKind.SuppressNullableWarningExpression:
                case SyntaxKind.ThisExpression:
                case SyntaxKind.TrueLiteralExpression:
                case SyntaxKind.TupleExpression:
                    return Precedence.Primary;
                default:
                    throw ExceptionUtilities.UnexpectedValue(op);
            }
        }

        private static bool IsExpectedPrefixUnaryOperator(SyntaxKind kind)
        {
            return SyntaxFacts.IsPrefixUnaryExpression(kind) && kind is not SyntaxKind.RefKeyword and not SyntaxKind.OutKeyword;
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
                var next = PeekToken(1);
                switch (next.Kind)
                {
                    case SyntaxKind.IdentifierToken:
                        return next.ContextualKind != SyntaxKind.WithKeyword;

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
                    case SyntaxKind.SingleLineRawStringLiteralToken:
                    case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                    case SyntaxKind.MultiLineRawStringLiteralToken:
                    case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                    case SyntaxKind.InterpolatedStringToken:
                    case SyntaxKind.Utf8StringLiteralToken:
                    case SyntaxKind.InterpolatedStringStartToken:
                    case SyntaxKind.InterpolatedVerbatimStringStartToken:
                    case SyntaxKind.InterpolatedSingleLineRawStringStartToken:
                    case SyntaxKind.InterpolatedMultiLineRawStringStartToken:
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
#if DEBUG
            // Ensure every expression kind is handled in GetPrecedence
            _ = GetPrecedence(result.Kind);
#endif
            _recursionDepth--;
            return result;
        }

        private ExpressionSyntax ParseSubExpressionCore(Precedence precedence)
        {
            // all of these are tokens that start statements and are invalid
            // to start a expression with. if we see one, then we must have
            // something like:
            //
            // return
            // if (...
            // parse out a missing name node for the expression, and keep on going
            var tk = this.CurrentToken.Kind;
            if (IsInvalidSubExpression(tk))
                return this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));

            return ParseExpressionContinued(parseUnaryOrPrimaryExpression(precedence), precedence);

            // Parses out a unary expression, or something with higher precedence (cast, addressof, primary).
            ExpressionSyntax parseUnaryOrPrimaryExpression(Precedence precedence)
            {
                // Parse a left operand -- possibly preceded by a unary operator.
                if (IsExpectedPrefixUnaryOperator(tk))
                {
                    var opKind = SyntaxFacts.GetPrefixUnaryExpression(tk);
                    return _syntaxFactory.PrefixUnaryExpression(
                        opKind,
                        this.EatToken(),
                        this.ParseSubExpression(GetPrecedence(opKind)));
                }

                // Check *explicitly* for `..` starting an expression.  This *is* the initial term we want to parse out.
                // If we have `expr..` though we don't do that here.  Instead, we'll parse out 'expr', and the `..`
                // portion will be handled in ParseExpressionContinued.
                if (IsAtDotDotToken())
                {
                    return _syntaxFactory.RangeExpression(
                        leftOperand: null,
                        this.EatDotDotToken(),
                        CanStartExpression()
                            ? this.ParseSubExpression(Precedence.Range)
                            : null);
                }

                if (IsAwaitExpression())
                {
                    return _syntaxFactory.AwaitExpression(
                        this.EatContextualToken(SyntaxKind.AwaitKeyword),
                        this.ParseSubExpression(GetPrecedence(SyntaxKind.AwaitExpression)));
                }

                if (this.IsQueryExpression(mayBeVariableDeclaration: false, mayBeMemberDeclaration: false))
                    return this.ParseQueryExpression(precedence);

                if (this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword && IsInQuery)
                {
                    // If this "from" token wasn't the start of a query then it's not really an expression.
                    // Consume it so that we don't try to parse it again as the next argument in an
                    // argument list.
                    return AddTrailingSkippedSyntax(
                        this.CreateMissingIdentifierName(),
                        this.AddError(this.EatToken(), ErrorCode.ERR_InvalidExprTerm, this.CurrentToken.Text));
                }

                if (tk == SyntaxKind.ThrowKeyword)
                {
                    var result = ParseThrowExpression();
                    // we parse a throw expression even at the wrong precedence for better recovery
                    return precedence <= Precedence.Coalescing
                        ? result
                        : this.AddError(result, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
                }

                if (this.IsPossibleDeconstructionLeft(precedence))
                    return ParseDeclarationExpression(ParseTypeMode.Normal, isScoped: false);

                // Not a unary operator - get a primary expression.
                return this.ParsePrimaryExpression(precedence);
            }
        }

#nullable enable

        /// <summary>
        /// Takes in an initial unary expression or primary expression, and then consumes what follows as long as its
        /// precedence is either lower than the <paramref name="precedence"/> we're parsing currently, or equal to that
        /// precedence if we have something right-associative <see cref="IsRightAssociative"/>.
        /// </summary>
        private ExpressionSyntax ParseExpressionContinued(ExpressionSyntax unaryOrPrimaryExpression, Precedence precedence)
        {
            var currentExpression = unaryOrPrimaryExpression;

            // Keep on expanding the left operand as long as what we see fits the precedence we're under.
            while (tryExpandExpression(currentExpression, precedence) is ExpressionSyntax expandedExpression)
                currentExpression = expandedExpression;

            // Finally, consume a conditional expression if precedence allows it.

            // https://github.com/dotnet/csharpstandard/blob/standard-v6/standard/expressions.md#1115-conditional-operator:
            //
            // conditional_expression
            //     : null_coalescing_expression
            //     | null_coalescing_expression '?' expression ':' expression
            //     ;
            //
            // 1. Only take the conditional part of the expression if we're at or below its precedence.
            // 2. When parsing the branches of the expression, parse at the highest precedence again ('expression').
            //    This allows for things like assignments/lambdas in the branches of the conditional.
            if (this.CurrentToken.Kind == SyntaxKind.QuestionToken && precedence <= Precedence.Conditional)
                return consumeConditionalExpression(currentExpression);

            return currentExpression;

            ExpressionSyntax? tryExpandExpression(ExpressionSyntax leftOperand, Precedence precedence)
            {
                // Look for operators that can follow what we've seen so far, and which are acceptable at this
                // precedence level.  Examples include binary operator, assignment operators, range operators `..`, as
                // well as `switch` and `with` clauses.

                var (operatorTokenKind, operatorExpressionKind) = GetExpressionOperatorTokenKindAndExpressionKind();

                if (operatorTokenKind == SyntaxKind.None)
                    return null;

                var newPrecedence = GetPrecedence(operatorExpressionKind);

                // Check the precedence to see if we should "take" this operator.  A lower precedence means what's
                // coming isn't a child of us, but rather we will be a child of it.  So we bail out and let any higher
                // up expression parsing consume it with us as the left side.
                if (newPrecedence < precedence)
                    return null;

                // Same precedence, but not right-associative -- deal with this "later"
                if ((newPrecedence == precedence) && !IsRightAssociative(operatorExpressionKind))
                    return null;

                // Now consume the operator (including consuming multiple tokens in the case of merged operator tokens)
                var operatorToken = EatExpressionOperatorToken(operatorTokenKind);

                if (newPrecedence > GetPrecedence(leftOperand.Kind))
                {
                    // Normally, a left operand with a looser precedence will consume all right operands that
                    // have a tighter precedence.  For example, in the expression `a + b * c`, the `* c` part
                    // will be consumed as part of the right operand of the addition.  However, there are a
                    // few circumstances in which a tighter precedence is not consumed: that occurs when the
                    // left hand operator does not have an expression as its right operand.  This occurs for
                    // the is-type operator and the is-pattern operator.  Source text such as
                    // `a is {} + b` should produce a syntax error, as parsing the `+` with an `is`
                    // expression as its left operand would be a precedence inversion.  Similarly, it occurs
                    // with an anonymous method expression or a lambda expression with a block body.  No
                    // further parsing will find a way to fix things up, so we accept the operator but issue
                    // a diagnostic.
                    operatorToken = this.AddError(
                        operatorToken,
                        leftOperand.Kind == SyntaxKind.IsPatternExpression ? ErrorCode.ERR_UnexpectedToken : ErrorCode.WRN_PrecedenceInversion,
                        operatorToken.Text);
                }

                if (operatorExpressionKind == SyntaxKind.AsExpression)
                {
                    return _syntaxFactory.BinaryExpression(
                        operatorExpressionKind, leftOperand, operatorToken, this.ParseType(ParseTypeMode.AsExpression));
                }

                if (operatorExpressionKind == SyntaxKind.IsExpression)
                    return ParseIsExpression(leftOperand, operatorToken);

                if (operatorExpressionKind == SyntaxKind.SwitchExpression)
                    return ParseSwitchExpression(leftOperand, operatorToken);

                if (operatorExpressionKind == SyntaxKind.WithExpression)
                    return ParseWithExpression(leftOperand, operatorToken);

                if (operatorExpressionKind == SyntaxKind.RangeExpression)
                {
                    return _syntaxFactory.RangeExpression(
                        leftOperand,
                        operatorToken,
                        CanStartExpression()
                            ? this.ParseSubExpression(Precedence.Range)
                            : null);
                }

                if (IsExpectedAssignmentOperator(operatorToken.Kind))
                    return ParseAssignmentExpression(operatorExpressionKind, leftOperand, operatorToken);

                if (IsExpectedBinaryOperator(operatorToken.Kind))
                    return _syntaxFactory.BinaryExpression(operatorExpressionKind, leftOperand, operatorToken, this.ParseSubExpression(newPrecedence));

                throw ExceptionUtilities.Unreachable();
            }

            ConditionalExpressionSyntax consumeConditionalExpression(ExpressionSyntax leftOperand)
            {
                // Complex ambiguity with `?` and collection-expressions.  Specifically: b?[c]:d
                //
                // On its own, we want that to be a conditional expression with a collection expression in it.  However, for
                // back compat, we need to make sure that `a ? b?[c] : d` sees the inner `b?[c]` as a
                // conditional-access-expression.  So, if after consuming the portion after the initial `?` if we do not
                // have the `:` we need, and we can see a `?[` in that portion of the parse, then we retry consuming the
                // when-true portion, but this time forcing the prior way of handling `?[`.
                var questionToken = this.EatToken();

                using var afterQuestionToken = this.GetDisposableResetPoint(resetOnDispose: false);
                var whenTrue = this.ParsePossibleRefExpression();

                if (this.CurrentToken.Kind != SyntaxKind.ColonToken &&
                    !this.ForceConditionalAccessExpression &&
                    containsTernaryCollectionToReinterpret(whenTrue))
                {
                    // Keep track of where we are right now in case the new parse doesn't make things better.
                    using var originalAfterWhenTrue = this.GetDisposableResetPoint(resetOnDispose: false);

                    // Go back to right after the `?`
                    afterQuestionToken.Reset();

                    // try reparsing with `?[` as a conditional access, not a ternary+collection
                    this.ForceConditionalAccessExpression = true;
                    var newWhenTrue = this.ParsePossibleRefExpression();
                    this.ForceConditionalAccessExpression = false;

                    if (this.CurrentToken.Kind == SyntaxKind.ColonToken)
                    {
                        // if we now are at a colon, this was preferred parse.  
                        whenTrue = newWhenTrue;
                    }
                    else
                    {
                        // retrying the parse didn't help.  Use the original interpretation.
                        originalAfterWhenTrue.Reset();
                    }
                }

                if (this.CurrentToken.Kind == SyntaxKind.EndOfFileToken && this.lexer.InterpolationFollowedByColon)
                {
                    // We have an interpolated string with an interpolation that contains a conditional expression.
                    // Unfortunately, the precedence demands that the colon is considered to signal the start of the
                    // format string. Without this code, the compiler would complain about a missing colon, and point
                    // to the colon that is present, which would be confusing. We aim to give a better error message.
                    var conditionalExpression = _syntaxFactory.ConditionalExpression(
                        leftOperand,
                        questionToken,
                        whenTrue,
                        SyntaxFactory.MissingToken(SyntaxKind.ColonToken),
                        _syntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken)));
                    return this.AddError(conditionalExpression, ErrorCode.ERR_ConditionalInInterpolation);
                }
                else
                {
                    return _syntaxFactory.ConditionalExpression(
                        leftOperand,
                        questionToken,
                        whenTrue,
                        this.EatToken(SyntaxKind.ColonToken),
                        this.ParsePossibleRefExpression());
                }
            }

            static bool containsTernaryCollectionToReinterpret(ExpressionSyntax expression)
            {
                var stack = ArrayBuilder<GreenNode>.GetInstance();
                stack.Push(expression);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    if (current is ConditionalExpressionSyntax conditionalExpression &&
                        conditionalExpression.WhenTrue.GetFirstToken().Kind == SyntaxKind.OpenBracketToken)
                    {
                        stack.Free();
                        return true;
                    }

                    // Note: we could consider not recursing into anonymous-methods/lambdas (since we reset the 
                    // ForceConditionalAccessExpression flag when we go into that).  However, that adds a bit of
                    // fragile coupling between these different code blocks that i'd prefer to avoid.  In practice
                    // the extra cost here will almost never occur, so the simplicity is worth it.
                    foreach (var child in current.ChildNodesAndTokens())
                        stack.Push(child);
                }

                stack.Free();
                return false;
            }
        }

        private (SyntaxKind operatorTokenKind, SyntaxKind operatorExpressionKind) GetExpressionOperatorTokenKindAndExpressionKind()
        {
            // If the set of expression continuations is updated here, please review ParseStatementAttributeDeclarations
            // to see if it may need a similar look-ahead check to determine if something is a collection expression versus
            // an attribute.

            var token1 = this.CurrentToken;
            var token1Kind = token1.ContextualKind;

            // Merge two consecutive dots into a DotDotToken
            if (IsAtDotDotToken())
                return (SyntaxKind.DotDotToken, SyntaxKind.RangeExpression);

            // check for >>, >>=, >>> or >>>=
            //
            // In all those cases, update token1Kind to be the merged token kind.  It will then be handled by the code below.
            if (token1Kind == SyntaxKind.GreaterThanToken
                && this.PeekToken(1) is { Kind: SyntaxKind.GreaterThanToken or SyntaxKind.GreaterThanEqualsToken } token2
                && NoTriviaBetween(token1, token2)) // check to see if they really are adjacent
            {
                if (token2.Kind == SyntaxKind.GreaterThanToken)
                {
                    if (this.PeekToken(2) is { Kind: SyntaxKind.GreaterThanToken or SyntaxKind.GreaterThanEqualsToken } token3
                        && NoTriviaBetween(token2, token3)) // check to see if they really are adjacent
                    {
                        // >>>  or  >>>=
                        token1Kind = token3.Kind == SyntaxKind.GreaterThanToken
                            ? SyntaxKind.GreaterThanGreaterThanGreaterThanToken
                            : SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken;
                    }
                    else
                    {
                        // >>
                        token1Kind = SyntaxKind.GreaterThanGreaterThanToken;
                    }
                }
                else
                {
                    // >>=
                    token1Kind = SyntaxKind.GreaterThanGreaterThanEqualsToken;
                }
            }

            if (IsExpectedBinaryOperator(token1Kind))
                return (token1Kind, SyntaxFacts.GetBinaryExpression(token1Kind));

            if (IsExpectedAssignmentOperator(token1Kind))
                return (token1Kind, SyntaxFacts.GetAssignmentExpression(token1Kind));

            if (token1Kind == SyntaxKind.SwitchKeyword && this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken)
                return (token1Kind, SyntaxKind.SwitchExpression);

            if (token1Kind == SyntaxKind.WithKeyword && this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken)
                return (token1Kind, SyntaxKind.WithExpression);

            // Something that doesn't expand the current expression we're looking at.  Bail out and see if we
            // can end with a conditional expression.
            return (SyntaxKind.None, SyntaxKind.None);
        }

        private SyntaxToken EatExpressionOperatorToken(SyntaxKind operatorTokenKind)
        {
            // Combine tokens into a single token if needed

            if (operatorTokenKind is SyntaxKind.DotDotToken)
                return EatDotDotToken();

            if (operatorTokenKind is SyntaxKind.GreaterThanGreaterThanToken or SyntaxKind.GreaterThanGreaterThanEqualsToken)
            {
                // >> and >>=
                // Two tokens need to be consumed here.

                var token1 = EatToken();
                var token2 = EatToken();

                return SyntaxFactory.Token(
                    token1.GetLeadingTrivia(),
                    operatorTokenKind,
                    token2.GetTrailingTrivia());
            }
            else if (operatorTokenKind is SyntaxKind.GreaterThanGreaterThanGreaterThanToken or SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken)
            {
                // >>> and >>>=
                // Three tokens need to be consumed here.

                var token1 = EatToken();
                _ = EatToken();
                var token3 = EatToken();

                return SyntaxFactory.Token(
                    token1.GetLeadingTrivia(),
                    operatorTokenKind,
                    token3.GetTrailingTrivia());
            }
            else
            {
                // Normal operator.  Eat as a single token, converting contextual words cases (like 'with') to a keyword.
                return this.EatContextualToken(operatorTokenKind);
            }
        }

        private AssignmentExpressionSyntax ParseAssignmentExpression(SyntaxKind operatorExpressionKind, ExpressionSyntax leftOperand, SyntaxToken operatorToken)
        {
            Debug.Assert(IsExpectedAssignmentOperator(operatorToken.Kind));
            Debug.Assert(GetPrecedence(operatorExpressionKind) == Precedence.Assignment);

            ExpressionSyntax rhs;

            if (operatorExpressionKind == SyntaxKind.SimpleAssignmentExpression && CurrentToken.Kind == SyntaxKind.RefKeyword &&
                // check for lambda expression with explicit ref return type: `ref int () => { ... }`
                !this.IsPossibleLambdaExpression(Precedence.Assignment))
            {
                rhs = _syntaxFactory.RefExpression(
                    this.EatToken(),
                    this.ParseExpressionCore());
            }
            else
            {
                rhs = this.ParseSubExpression(Precedence.Assignment);
            }

            return _syntaxFactory.AssignmentExpression(
                operatorExpressionKind, leftOperand, operatorToken, rhs);
        }

        /// <summary>Check if we're currently at a .. sequence that can then be parsed out as a <see cref="SyntaxKind.DotDotToken"/>.</summary>
        public bool IsAtDotDotToken()
        {
            if (this.CurrentToken.Kind != SyntaxKind.DotToken)
                return false;

            var nextToken = this.PeekToken(1);
            return nextToken.Kind == SyntaxKind.DotToken && NoTriviaBetween(this.CurrentToken, nextToken);
        }

        public static bool IsAtDotDotToken(SyntaxToken token1, SyntaxToken token2)
            => token1.Kind == SyntaxKind.DotToken &&
               token2.Kind == SyntaxKind.DotToken &&
               NoTriviaBetween(token1, token2);

        /// <summary>Consume the next two tokens as a <see cref="SyntaxKind.DotDotToken"/>.  Note: if three dot tokens
        /// are in a row, an error will be placed on the <c>..</c> token to say that is illegal, and single DotDot token
        /// will be returned.</summary>
        public SyntaxToken EatDotDotToken()
        {
            Debug.Assert(IsAtDotDotToken());
            var token1 = this.EatToken();
            var token2 = this.EatToken();

            var dotDotToken = SyntaxFactory.Token(token1.GetLeadingTrivia(), SyntaxKind.DotDotToken, token2.GetTrailingTrivia());
            if (this.CurrentToken is { Kind: SyntaxKind.DotToken } token3 &&
                NoTriviaBetween(token2, token3))
            {
                // At least three dots directly in a row.  Definitely mark that this is always illegal.  We do not allow
                // `...` at all in case we want to use that syntax in the future.
                dotDotToken = AddError(
                    dotDotToken,
                    offset: dotDotToken.GetLeadingTriviaWidth(),
                    length: 0,
                    ErrorCode.ERR_TripleDotNotAllowed);

                // If we have exactly 3 dots in a row, then make the third dot into skipped trivia on the `..` as this
                // is likely just a mistyped range/slice and we'll recover better if we don't try to process the 3rd dot
                // as a member access or anything like that.
                //
                // If we have 4 dots in a row (`....`), then don't skip any of them.  We'll let the caller handle the
                // next two dots as a range/slice/whatever.
                if (this.PeekToken(1) is not { Kind: SyntaxKind.DotToken } token4 ||
                    !NoTriviaBetween(token3, token4))
                {
                    dotDotToken = AddSkippedSyntax(dotDotToken, this.EatToken(), trailing: true);
                }
            }

            return dotDotToken;
        }

#nullable disable

        private DeclarationExpressionSyntax ParseDeclarationExpression(ParseTypeMode mode, bool isScoped)
        {
            var scopedKeyword = isScoped
                ? EatContextualToken(SyntaxKind.ScopedKeyword)
                : null;

            var type = this.ParseType(mode);
            return _syntaxFactory.DeclarationExpression(
                scopedKeyword == null ? type : _syntaxFactory.ScopedType(scopedKeyword, type),
                ParseDesignation(forPattern: false));
        }

        private ExpressionSyntax ParseThrowExpression()
        {
            return _syntaxFactory.ThrowExpression(
                this.EatToken(SyntaxKind.ThrowKeyword),
                this.ParseSubExpression(Precedence.Coalescing));
        }

        private ExpressionSyntax ParseIsExpression(ExpressionSyntax leftOperand, SyntaxToken opToken)
        {
            var node = this.ParseTypeOrPatternForIsOperator();
            return node switch
            {
                PatternSyntax pattern => _syntaxFactory.IsPatternExpression(leftOperand, opToken, pattern),
                TypeSyntax type => _syntaxFactory.BinaryExpression(SyntaxKind.IsExpression, leftOperand, opToken, type),
                _ => throw ExceptionUtilities.UnexpectedValue(node),
            };
        }

        private ExpressionSyntax ParsePrimaryExpression(Precedence precedence)
        {
            // Primary expressions:
            // x.y, f(x), a[i], x?.y, x?[y], x++, x--, x!, new, typeof, checked, unchecked, default, nameof, delegate, sizeof, stackalloc, x->y
            //
            // Note that postfix operators (like ++) are still primary expressions, even though their prefix equivalents (`++x`) are unary.

            return parsePostFixExpression(parsePrimaryExpressionWithoutPostfix(precedence));

            ExpressionSyntax parsePrimaryExpressionWithoutPostfix(Precedence precedence)
            {
                var tk = this.CurrentToken.Kind;
                switch (tk)
                {
                    case SyntaxKind.TypeOfKeyword:
                        return this.ParseTypeOfExpression();
                    case SyntaxKind.DefaultKeyword:
                        return this.ParseDefaultExpression();
                    case SyntaxKind.SizeOfKeyword:
                        return this.ParseSizeOfExpression();
                    case SyntaxKind.MakeRefKeyword:
                        return this.ParseMakeRefExpression();
                    case SyntaxKind.RefTypeKeyword:
                        return this.ParseRefTypeExpression();
                    case SyntaxKind.CheckedKeyword:
                    case SyntaxKind.UncheckedKeyword:
                        return this.ParseCheckedOrUncheckedExpression();
                    case SyntaxKind.RefValueKeyword:
                        return this.ParseRefValueExpression();
                    case SyntaxKind.ColonColonToken:
                        // misplaced ::
                        // Calling ParseAliasQualifiedName will cause us to create a missing identifier node that then
                        // properly consumes the :: and the reset of the alias name afterwards.
                        return this.ParseAliasQualifiedName(NameOptions.InExpression);
                    case SyntaxKind.EqualsGreaterThanToken:
                        return this.ParseLambdaExpression();
                    case SyntaxKind.StaticKeyword:
                        if (this.IsPossibleAnonymousMethodExpression())
                        {
                            return this.ParseAnonymousMethodExpression();
                        }
                        else if (this.IsPossibleLambdaExpression(precedence))
                        {
                            return this.ParseLambdaExpression();
                        }
                        else
                        {
                            return this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_InvalidExprTerm, this.CurrentToken.Text);
                        }
                    case SyntaxKind.IdentifierToken:
                        {
                            if (this.IsTrueIdentifier())
                            {
                                if (this.IsPossibleAnonymousMethodExpression())
                                {
                                    return this.ParseAnonymousMethodExpression();
                                }
                                else if (this.IsPossibleLambdaExpression(precedence) && this.TryParseLambdaExpression() is { } lambda)
                                {
                                    return lambda;
                                }
                                else if (this.IsPossibleDeconstructionLeft(precedence))
                                {
                                    return ParseDeclarationExpression(ParseTypeMode.Normal, isScoped: false);
                                }
                                else if (IsCurrentTokenFieldInKeywordContext() && PeekToken(1).Kind != SyntaxKind.ColonColonToken)
                                {
                                    return _syntaxFactory.FieldExpression(this.EatContextualToken(SyntaxKind.FieldKeyword));
                                }
                                else
                                {
                                    return this.ParseAliasQualifiedName(NameOptions.InExpression);
                                }
                            }
                            else
                            {
                                return this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_InvalidExprTerm, this.CurrentToken.Text);
                            }
                        }
                    case SyntaxKind.OpenBracketToken:
                        return this.IsPossibleLambdaExpression(precedence)
                            ? this.ParseLambdaExpression()
                            : this.ParseCollectionExpression();
                    case SyntaxKind.ThisKeyword:
                        return _syntaxFactory.ThisExpression(this.EatToken());
                    case SyntaxKind.BaseKeyword:
                        return ParseBaseExpression();

                    case SyntaxKind.ArgListKeyword:
                    case SyntaxKind.FalseKeyword:
                    case SyntaxKind.TrueKeyword:
                    case SyntaxKind.NullKeyword:
                    case SyntaxKind.NumericLiteralToken:
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.Utf8StringLiteralToken:
                    case SyntaxKind.SingleLineRawStringLiteralToken:
                    case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                    case SyntaxKind.MultiLineRawStringLiteralToken:
                    case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                    case SyntaxKind.CharacterLiteralToken:
                        return _syntaxFactory.LiteralExpression(SyntaxFacts.GetLiteralExpression(tk), this.EatToken());
                    case SyntaxKind.InterpolatedStringStartToken:
                    case SyntaxKind.InterpolatedVerbatimStringStartToken:
                    case SyntaxKind.InterpolatedSingleLineRawStringStartToken:
                    case SyntaxKind.InterpolatedMultiLineRawStringStartToken:
                        throw new NotImplementedException(); // this should not occur because these tokens are produced and parsed immediately
                    case SyntaxKind.InterpolatedStringToken:
                        return this.ParseInterpolatedStringToken();
                    case SyntaxKind.OpenParenToken:
                        {
                            return IsPossibleLambdaExpression(precedence) && this.TryParseLambdaExpression() is { } lambda
                                ? lambda
                                : this.ParseCastOrParenExpressionOrTuple();
                        }
                    case SyntaxKind.NewKeyword:
                        return this.ParseNewExpression();
                    case SyntaxKind.StackAllocKeyword:
                        return this.ParseStackAllocExpression();
                    case SyntaxKind.DelegateKeyword:
                        // check for lambda expression with explicit function pointer return type
                        return this.IsPossibleLambdaExpression(precedence)
                            ? this.ParseLambdaExpression()
                            : this.ParseAnonymousMethodExpression();
                    case SyntaxKind.RefKeyword:
                        // check for lambda expression with explicit ref return type: `ref int () => { ... }`
                        if (this.IsPossibleLambdaExpression(precedence))
                        {
                            return this.ParseLambdaExpression();
                        }
                        // ref is not expected to appear in this position.
                        var refKeyword = this.EatToken();
                        return this.AddError(_syntaxFactory.RefExpression(refKeyword, this.ParseExpressionCore()), ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
                    default:
                        if (IsPredefinedType(tk))
                        {
                            if (this.IsPossibleLambdaExpression(precedence))
                            {
                                return this.ParseLambdaExpression();
                            }

                            // check for intrinsic type followed by '.'
                            var expr = _syntaxFactory.PredefinedType(this.EatToken());

                            if (this.CurrentToken.Kind != SyntaxKind.DotToken || tk == SyntaxKind.VoidKeyword)
                            {
                                expr = this.AddError(expr, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
                            }

                            return expr;
                        }
                        else
                        {
                            var expr = this.CreateMissingIdentifierName();

                            if (tk == SyntaxKind.EndOfFileToken)
                            {
                                expr = this.AddError(expr, ErrorCode.ERR_ExpressionExpected);
                            }
                            else
                            {
                                expr = this.AddError(expr, ErrorCode.ERR_InvalidExprTerm, SyntaxFacts.GetText(tk));
                            }

                            return expr;
                        }
                }
            }

            ExpressionSyntax parsePostFixExpression(ExpressionSyntax expr)
            {
                Debug.Assert(expr != null);

                while (true)
                {
                    // If the set of postfix expressions is updated here, please review ParseStatementAttributeDeclarations
                    // to see if it may need a similar look-ahead check to determine if something is a collection expression
                    // versus an attribute.

                    switch (this.CurrentToken.Kind)
                    {
                        case SyntaxKind.OpenParenToken:
                            expr = _syntaxFactory.InvocationExpression(expr, this.ParseParenthesizedArgumentList());
                            continue;

                        case SyntaxKind.OpenBracketToken:
                            expr = _syntaxFactory.ElementAccessExpression(expr, this.ParseBracketedArgumentList());
                            continue;

                        case SyntaxKind.PlusPlusToken:
                        case SyntaxKind.MinusMinusToken:
                            expr = _syntaxFactory.PostfixUnaryExpression(SyntaxFacts.GetPostfixUnaryExpression(this.CurrentToken.Kind), expr, this.EatToken());
                            continue;

                        case SyntaxKind.ColonColonToken:
                            if (this.PeekToken(1).Kind == SyntaxKind.IdentifierToken)
                            {
                                expr = _syntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    expr,
                                    // replace :: with missing dot and annotate with skipped text "::" and error
                                    this.ConvertToMissingWithTrailingTrivia(this.AddError(this.EatToken(), ErrorCode.ERR_UnexpectedAliasedName), SyntaxKind.DotToken),
                                    this.ParseSimpleName(NameOptions.InExpression));
                            }
                            else
                            {
                                // just some random trailing :: ?
                                expr = AddTrailingSkippedSyntax(expr, this.EatTokenEvenWithIncorrectKind(SyntaxKind.DotToken));
                            }

                            continue;

                        case SyntaxKind.MinusGreaterThanToken:
                            expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.PointerMemberAccessExpression, expr, this.EatToken(), this.ParseSimpleName(NameOptions.InExpression));
                            continue;

                        case SyntaxKind.DotToken when !IsAtDotDotToken():
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
                                return _syntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression, expr, this.EatToken(),
                                    this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_IdentifierExpected));
                            }

                            expr = _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, this.EatToken(), this.ParseSimpleName(NameOptions.InExpression));
                            continue;

                        case SyntaxKind.QuestionToken:
                            if (TryParseConditionalAccessExpression(expr, out var conditionalAccess))
                            {
                                expr = conditionalAccess;
                                continue;
                            }

                            return expr;

                        case SyntaxKind.ExclamationToken:
                            expr = _syntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, expr, this.EatToken());
                            continue;

                        default:
                            return expr;
                    }
                }
            }
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

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            this.EatToken(); // `var`
            return
                this.CurrentToken.Kind == SyntaxKind.OpenParenToken && ScanDesignator() &&
                this.CurrentToken.Kind == SyntaxKind.EqualsToken;
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

        private bool IsPossibleAnonymousMethodExpression()
        {
            // Skip past any static/async keywords.
            var tokenIndex = 0;
            while (this.PeekToken(tokenIndex).Kind == SyntaxKind.StaticKeyword ||
                   this.PeekToken(tokenIndex).ContextualKind == SyntaxKind.AsyncKeyword)
            {
                tokenIndex++;
            }

            return this.PeekToken(tokenIndex).Kind == SyntaxKind.DelegateKeyword &&
                this.PeekToken(tokenIndex + 1).Kind != SyntaxKind.AsteriskToken;
        }

#nullable enable

        /// <summary>
        /// Called when we could be at a <c>?</c> that could start a <see cref="ConditionalAccessExpressionSyntax"/> or
        /// a <see cref="ConditionalExpressionSyntax"/>.  Returns <see langword="true"/> if this succeeds at parsing the
        /// former, and <see langword="null"/> if we're not at the start of a conditional access expression.
        /// </summary>
        private bool TryParseConditionalAccessExpression(
            ExpressionSyntax primaryExpression,
            [NotNullWhen(true)] out ConditionalAccessExpressionSyntax? conditionalAccessExpression)
        {
            // From 
            // https://github.com/dotnet/csharpstandard/blob/standard-v7/standard/expressions.md#1288-null-conditional-member-access
            // https://github.com/dotnet/csharpstandard/blob/standard-v7/standard/expressions.md#12812-null-conditional-element-access

            // null_conditional_member_access
            //      : primary_expression '?' '.' identifier type_argument_list? dependent_access*
            //      ;
            //
            // null_conditional_element_access
            //      : primary_no_array_creation_expression '?' '[' argument_list ']' dependent_access*
            //      ;
            //
            // dependent_access
            //      : '.' identifier type_argument_list?    // member access
            //      | '[' argument_list ']'                 // element access
            //      | '(' argument_list ? ')'               // invocation
            //      ;

            // We get in here after parsing out the initial primary expression and seeing a `?` follow.

            var (questionToken, bindingExpression) = tryEatQuestionAndBindingExpression();
            if (questionToken is null || bindingExpression is null)
            {
                conditionalAccessExpression = null;
                return false;
            }

            conditionalAccessExpression = _syntaxFactory.ConditionalAccessExpression(
                primaryExpression, questionToken, parseWhenNotNull(bindingExpression));
            return true;

            (SyntaxToken? questionToken, ExpressionSyntax? bindingExpression) tryEatQuestionAndBindingExpression()
            {
                if (this.CurrentToken.Kind == SyntaxKind.QuestionToken)
                {
                    var nextToken = this.PeekToken(1);
                    var nextTokenKind = nextToken.Kind;

                    // ?.   is always the start of of a consequence expression.
                    //
                    // ?..  is a ternary with a range expression as it's 'whenTrue' clause.
                    if (nextTokenKind == SyntaxKind.DotToken && !IsAtDotDotToken(nextToken, this.PeekToken(2)))
                        return (questionToken: EatToken(), _syntaxFactory.MemberBindingExpression(this.EatToken(), this.ParseSimpleName(NameOptions.InExpression)));

                    if (isStartOfElementBindingExpression(nextTokenKind))
                        return (questionToken: EatToken(), _syntaxFactory.ElementBindingExpression(this.ParseBracketedArgumentList()));
                }

                // Anything else is either not a `?` at all, or is just a `?` that starts a conditional expression (not
                // a conditional access expression).
                return default;
            }

            bool isStartOfElementBindingExpression(SyntaxKind nextTokenKind)
            {
                if (nextTokenKind != SyntaxKind.OpenBracketToken)
                    return false;

                // could simply be `x?[0]`, or could be `x ? [0] : [1]`.

                // Caller only wants us to parse ?[ how it was originally parsed before collection expressions.
                if (this.ForceConditionalAccessExpression)
                    return true;

                using var _ = GetDisposableResetPoint(resetOnDispose: true);

                // Move past the '?'. Parse what comes next the same way that conditional expressions are parsed.
                this.EatToken();
                this.ParsePossibleRefExpression();

                // If we see a colon, then do not parse this as a conditional-access-expression, pop up to the caller
                // and have it reparse this as a conditional-expression instead.
                return this.CurrentToken.Kind != SyntaxKind.ColonToken;
            }

            ExpressionSyntax parseWhenNotNull(ExpressionSyntax expr)
            {
                while (true)
                {
                    // We should consume suppression '!'s which are in the middle of the 'whenNotNull', but not at the end.
                    // For example, 'a?.b!.c' should be a cond-access whose RHS is '.b!.c',
                    // while 'a?.b!' should be a suppression-expr containing a cond-access 'a?.b'.
                    using var beforeSuppressionsResetPoint = GetDisposableResetPoint(resetOnDispose: false);
                    var expressionBeforeSuppressions = expr;

                    while (this.CurrentToken.Kind == SyntaxKind.ExclamationToken)
                        expr = _syntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, expr, EatToken());

                    // Expand to consume the `dependent_access*` continuations.
                    if (tryParseDependentAccess(expr) is ExpressionSyntax expandedExpression)
                    {
                        expr = expandedExpression;
                        continue;
                    }

                    // A trailing cond-access or assignment is effectively the "end" of the current cond-access node.
                    // Due to right-associativity, everything that follows will be included in the child node.
                    // e.g. 'a?.b?.c' parses as '(a) ? (.b?.c)'
                    // e.g. 'a?.b = c?.d = e?.f' parses as 'a?.b = (c?.d = e?.f)'

                    // a?.b?.c
                    // a?.b!?.c
                    if (TryParseConditionalAccessExpression(expr, out var conditionalAccess))
                        return conditionalAccess;

                    // a?.b = c
                    // a?.b! = c
                    var (operatorTokenKind, operatorExpressionKind) = GetExpressionOperatorTokenKindAndExpressionKind();
                    if (IsExpectedAssignmentOperator(operatorTokenKind))
                    {
                        return ParseAssignmentExpression(operatorExpressionKind, expr, EatExpressionOperatorToken(operatorTokenKind));
                    }

                    // End of the cond-access.
                    // Any '!' suppressions which followed this are a parent of the cond-access, not a child of it.
                    beforeSuppressionsResetPoint.Reset();
                    return expressionBeforeSuppressions;
                }
            }

            ExpressionSyntax? tryParseDependentAccess(ExpressionSyntax expr)
                => this.CurrentToken.Kind switch
                {
                    SyntaxKind.OpenParenToken
                        => _syntaxFactory.InvocationExpression(expr, this.ParseParenthesizedArgumentList()),
                    SyntaxKind.OpenBracketToken
                        => _syntaxFactory.ElementAccessExpression(expr, this.ParseBracketedArgumentList()),
                    SyntaxKind.DotToken
                        => _syntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expr, this.EatToken(), this.ParseSimpleName(NameOptions.InExpression)),
                    _ => null,
                };
        }

#nullable disable

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
            Debug.Assert(openKind is SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken);
            Debug.Assert(closeKind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken);
            Debug.Assert((openKind == SyntaxKind.OpenParenToken) == (closeKind == SyntaxKind.CloseParenToken));
            bool isIndexer = openKind == SyntaxKind.OpenBracketToken;

            // convert `[` into `(` or vice versa for error recovery
            openToken = this.CurrentToken.Kind is SyntaxKind.OpenParenToken or SyntaxKind.OpenBracketToken
                ? this.EatTokenAsKind(openKind)
                : this.EatToken(openKind);

            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfArgumentList;

            if (this.CurrentToken.Kind != closeKind && this.CurrentToken.Kind != SyntaxKind.SemicolonToken)
            {
                if (isIndexer)
                {
                    // An indexer always expects at least one value.
                    arguments = ParseCommaSeparatedSyntaxList(
                        ref openToken,
                        SyntaxKind.CloseBracketToken,
                        static @this => @this.IsPossibleArgumentExpression(),
                        static @this => @this.ParseArgumentExpression(isIndexer: true),
                        skipBadArgumentListTokens,
                        allowTrailingSeparator: false,
                        requireOneElement: false,
                        allowSemicolonAsSeparator: false);
                }
                else
                {
                    arguments = ParseCommaSeparatedSyntaxList(
                        ref openToken,
                        SyntaxKind.CloseParenToken,
                        static @this => @this.IsPossibleArgumentExpression(),
                        static @this => @this.ParseArgumentExpression(isIndexer: false),
                        skipBadArgumentListTokens,
                        allowTrailingSeparator: false,
                        requireOneElement: false,
                        allowSemicolonAsSeparator: false);
                }
            }
            else if (isIndexer && this.CurrentToken.Kind == closeKind)
            {
                // An indexer always expects at least one value. And so we need to give an error
                // for the case where we see only "[]". ParseArgumentExpression gives it.
                var list = _pool.AllocateSeparated<ArgumentSyntax>();
                list.Add(this.ParseArgumentExpression(isIndexer));
                arguments = _pool.ToListAndFree(list);
            }
            else
            {
                arguments = default;
            }

            _termState = saveTerm;

            // convert `]` into `)` or vice versa for error recovery
            closeToken = this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken
                ? this.EatTokenAsKind(closeKind)
                : this.EatToken(closeKind);

            return;

            static PostSkipAction skipBadArgumentListTokens(
                LanguageParser @this, ref SyntaxToken open, SeparatedSyntaxListBuilder<ArgumentSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                if (@this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken or SyntaxKind.SemicolonToken)
                    return PostSkipAction.Abort;

                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref open, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleArgumentExpression(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind || p.CurrentToken.Kind == SyntaxKind.SemicolonToken,
                    expectedKind, closeKind);
            }
        }

        private bool IsEndOfArgumentList()
        {
            return this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.CloseBracketToken;
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
            var nameColon = this.CurrentToken.Kind == SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.ColonToken
                ? _syntaxFactory.NameColon(
                    this.ParseIdentifierName(),
                    this.EatToken(SyntaxKind.ColonToken))
                : null;

            SyntaxToken refKindKeyword = null;
            if (IsValidArgumentRefKindKeyword(this.CurrentToken.Kind) &&
                // check for lambda expression with explicit ref return type: `ref int () => { ... }`
                !(this.CurrentToken.Kind == SyntaxKind.RefKeyword &&
                 this.IsPossibleLambdaExpression(Precedence.Expression)))
            {
                refKindKeyword = this.EatToken();
            }

            ExpressionSyntax expression;

            if (isIndexer && this.CurrentToken.Kind is SyntaxKind.CommaToken or SyntaxKind.CloseBracketToken)
            {
                expression = this.ParseIdentifierName(ErrorCode.ERR_ValueExpected);
            }
            else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                expression = this.ParseIdentifierName(ErrorCode.ERR_MissingArgument);
            }
            else
            {
                // According to Language Specification, section 7.6.7 Element access
                //      The argument-list of an element-access is not allowed to contain ref or out arguments.
                // However, we actually do support ref indexing of indexed properties in COM interop
                // scenarios, and when indexing an object of static type "dynamic". So we enforce
                // that the ref/out of the argument must match the parameter when binding the argument list.

                expression = refKindKeyword?.Kind == SyntaxKind.OutKeyword
                    ? ParseExpressionOrDeclaration(ParseTypeMode.Normal, permitTupleDesignation: false)
                    : ParseSubExpression(Precedence.Expression);
            }

            return _syntaxFactory.Argument(nameColon, refKindKeyword, expression);
        }

        private TypeOfExpressionSyntax ParseTypeOfExpression()
        {
            return _syntaxFactory.TypeOfExpression(
                this.EatToken(),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseTypeOrVoid(),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private ExpressionSyntax ParseDefaultExpression()
        {
            var keyword = this.EatToken();
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                return _syntaxFactory.DefaultExpression(
                    keyword,
                    this.EatToken(SyntaxKind.OpenParenToken),
                    this.ParseType(),
                    this.EatToken(SyntaxKind.CloseParenToken));
            }
            else
            {
                return _syntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression, keyword);
            }
        }

        private SizeOfExpressionSyntax ParseSizeOfExpression()
        {
            return _syntaxFactory.SizeOfExpression(
                this.EatToken(),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseType(),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private MakeRefExpressionSyntax ParseMakeRefExpression()
        {
            return _syntaxFactory.MakeRefExpression(
                this.EatToken(),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseSubExpression(Precedence.Expression),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private RefTypeExpressionSyntax ParseRefTypeExpression()
        {
            return _syntaxFactory.RefTypeExpression(
                this.EatToken(),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseSubExpression(Precedence.Expression),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private CheckedExpressionSyntax ParseCheckedOrUncheckedExpression()
        {
            var checkedOrUnchecked = this.EatToken();
            Debug.Assert(checkedOrUnchecked.Kind is SyntaxKind.CheckedKeyword or SyntaxKind.UncheckedKeyword);
            var kind = checkedOrUnchecked.Kind == SyntaxKind.CheckedKeyword ? SyntaxKind.CheckedExpression : SyntaxKind.UncheckedExpression;

            return _syntaxFactory.CheckedExpression(
                kind,
                checkedOrUnchecked,
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseSubExpression(Precedence.Expression),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private RefValueExpressionSyntax ParseRefValueExpression()
        {
            return _syntaxFactory.RefValueExpression(
                this.EatToken(SyntaxKind.RefValueKeyword),
                this.EatToken(SyntaxKind.OpenParenToken),
                this.ParseSubExpression(Precedence.Expression),
                this.EatToken(SyntaxKind.CommaToken),
                this.ParseType(),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private bool ScanParenthesizedLambda(Precedence precedence)
        {
            return ScanImplicitlyTypedLambdaOrSimpleExplicitlyTypedParenthesizedLambda(precedence) || ScanExplicitlyTypedLambda(precedence);
        }

        /// <summary>
        /// Scans implicitly typed  lambdas (like <c>(a, b) =></c>) as well as basic explicitly typed lambdas (like
        /// <c>(A a, B b) =></c>).  More complex scanning of parenthesized lambdas happens in <see
        /// cref="ScanExplicitlyTypedLambda"/>.
        /// </summary>
        private bool ScanImplicitlyTypedLambdaOrSimpleExplicitlyTypedParenthesizedLambda(Precedence precedence)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.OpenParenToken);

            if (precedence > Precedence.Lambda)
                return false;

            //  (   ) =>
            //  ( x ) =>           or       ( ref x ) =>
            //  ( x , ... ) =>     or       ( ref x , ...) =>
            var index = 1;

            while (true)
            {
                var token = this.PeekToken(index++);

                // Keep skipping modifiers, commas, and identifiers to consume the rest of the lambda arguments. Note:
                // this *will* grab explicitly typed lambdas like `(A b) =>`.  However, that's ok.  The only caller of
                // this is ScanParenthesizedLambda, which just wants to know if it's on some form of lambda.
                if (this.IsTrueIdentifier(token) ||
                    token.Kind is SyntaxKind.CommaToken ||
                    IsParameterModifierIncludingScoped(token))
                {
                    continue;
                }

                return token.Kind == SyntaxKind.CloseParenToken &&
                       this.PeekToken(index).Kind == SyntaxKind.EqualsGreaterThanToken;
            }
        }

        private bool ScanExplicitlyTypedLambda(Precedence precedence)
        {
            Debug.Assert(CurrentToken.Kind == SyntaxKind.OpenParenToken);

            if (precedence > Precedence.Lambda)
            {
                return false;
            }

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            // Do we have the following, where the attributes, modifier, and type are
            // optional? If so then parse it as a lambda.
            //   (attributes modifier T x [, ...]) =>
            //
            // It's not sufficient to assume this is a lambda expression if we see a
            // modifier such as `(ref x,` because the caller of this method may have
            // scanned past a preceding identifier, and `F (ref x,` might be a call to
            // method F rather than a lambda expression with return type F.
            // Instead, we need to scan to `=>`.

            while (true)
            {
                // Advance past the open paren or comma.
                this.EatToken();

                ParseAttributeDeclarations(inExpressionContext: true);

                if (IsParameterModifierIncludingScoped(this.CurrentToken))
                {
                    SyntaxListBuilder modifiers = _pool.Allocate();
                    ParseParameterModifiers(modifiers, isFunctionPointerParameter: false, isLambdaParameter: true);
                    _pool.Free(modifiers);
                }

                if (ShouldParseLambdaParameterType() &&
                    this.ScanType() == ScanTypeFlags.NotType)
                {
                    return false;
                }

                // eat the parameter name.
                var identifier = this.IsTrueIdentifier() ? this.EatToken() : CreateMissingIdentifierToken();

                var equalsToken = TryEatToken(SyntaxKind.EqualsToken);

                // If we have an `=` then parse out a default value.  Note: this is not legal, but this allows us to
                // to be resilient to the user writing this so we don't go completely off the rails.
                if (equalsToken != null)
                {
                    // Note: we don't do this if we have `=[`.  Realistically, this is never going to be a lambda
                    // expression as a `[` can only start an attribute declaration or collection expression, neither of
                    // which can be a default arg.  Checking for this helps us from going off the rails in pathological
                    // cases with lots of nested tokens that look like the could be anything.
                    if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken)
                    {
                        return false;
                    }

                    this.ParseExpressionCore();
                }

                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.CommaToken:
                        continue;

                    case SyntaxKind.CloseParenToken:
                        return this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken;

                    default:
                        return false;
                }
            }
        }

        private ExpressionSyntax ParseCastOrParenExpressionOrTuple()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenParenToken);

            using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: false);

            // We have a decision to make -- is this a cast, or is it a parenthesized
            // expression?  Because look-ahead is cheap with our token stream, we check
            // to see if this "looks like" a cast (without constructing any parse trees)
            // to help us make the decision.
            if (this.ScanCast())
            {
                if (!IsCurrentTokenQueryKeywordInQuery())
                {
                    // Looks like a cast, so parse it as one.
                    resetPoint.Reset();
                    return _syntaxFactory.CastExpression(
                        this.EatToken(SyntaxKind.OpenParenToken),
                        this.ParseType(),
                        this.EatToken(SyntaxKind.CloseParenToken),
                        this.ParseSubExpression(Precedence.Cast));
                }
            }

            // Doesn't look like a cast, so parse this as a parenthesized expression or tuple.
            resetPoint.Reset();
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var expression = this.ParseExpressionOrDeclaration(ParseTypeMode.FirstElementOfPossibleTupleLiteral, permitTupleDesignation: true);

            //  ( <expr>,    must be a tuple
            if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                return ParseTupleExpressionTail(
                    openParen,
                    _syntaxFactory.Argument(nameColon: null, refKindKeyword: null, expression));
            }

            // ( name:
            if (expression.Kind == SyntaxKind.IdentifierName && this.CurrentToken.Kind == SyntaxKind.ColonToken)
            {
                return ParseTupleExpressionTail(
                    openParen,
                    _syntaxFactory.Argument(
                        _syntaxFactory.NameColon((IdentifierNameSyntax)expression, EatToken()),
                        refKindKeyword: null,
                        this.ParseExpressionOrDeclaration(ParseTypeMode.FirstElementOfPossibleTupleLiteral, permitTupleDesignation: true)));
            }

            return _syntaxFactory.ParenthesizedExpression(
                openParen,
                expression,
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private TupleExpressionSyntax ParseTupleExpressionTail(SyntaxToken openParen, ArgumentSyntax firstArg)
        {
            var list = _pool.AllocateSeparated<ArgumentSyntax>();
            list.Add(firstArg);

            while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));

                var expression = ParseExpressionOrDeclaration(ParseTypeMode.AfterTupleComma, permitTupleDesignation: true);
                var argument = expression.Kind != SyntaxKind.IdentifierName || this.CurrentToken.Kind != SyntaxKind.ColonToken
                    ? _syntaxFactory.Argument(nameColon: null, refKindKeyword: null, expression: expression)
                    : _syntaxFactory.Argument(
                        _syntaxFactory.NameColon((IdentifierNameSyntax)expression, EatToken()),
                        refKindKeyword: null,
                        ParseExpressionOrDeclaration(ParseTypeMode.AfterTupleComma, permitTupleDesignation: true));

                list.Add(argument);
            }

            if (list.Count < 2)
            {
                list.AddSeparator(SyntaxFactory.MissingToken(SyntaxKind.CommaToken));
                list.Add(_syntaxFactory.Argument(
                    nameColon: null, refKindKeyword: null,
                    this.AddError(this.CreateMissingIdentifierName(), ErrorCode.ERR_TupleTooFewElements)));
            }

            return _syntaxFactory.TupleExpression(
                openParen,
                _pool.ToListAndFree(list),
                this.EatToken(SyntaxKind.CloseParenToken));
        }

        private bool ScanCast(bool forPattern = false)
        {
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            this.EatToken();

            var type = this.ScanType(forPattern);
            if (type == ScanTypeFlags.NotType)
            {
                return false;
            }

            if (this.CurrentToken.Kind != SyntaxKind.CloseParenToken)
            {
                return false;
            }

            this.EatToken();

            if (forPattern && this.CurrentToken.Kind == SyntaxKind.IdentifierToken)
            {
                // In a pattern, an identifier can follow a cast unless it's a binary pattern token.
                return !isBinaryPattern();
            }

            switch (type)
            {
                // If we have any of the following, we know it must be a cast:
                // 1) (Goo*)bar;
                // 2) (Goo?)bar;
                // 3) "(int)bar" or "(int[])bar"
                // 4) (G::Goo)bar
                case ScanTypeFlags.PointerOrMultiplication:
                case ScanTypeFlags.NullableType:
                case ScanTypeFlags.MustBeType:
                case ScanTypeFlags.AliasQualifiedName:
                    // The thing between parens is unambiguously a type.
                    // In a pattern, we need more lookahead to confirm it is a cast and not
                    // a parenthesized type pattern.  In this case the tokens that
                    // have both unary and binary operator forms may appear in their unary form
                    // following a cast.
                    return !forPattern || this.CurrentToken.Kind switch
                    {
                        SyntaxKind.PlusToken or
                        SyntaxKind.MinusToken or
                        SyntaxKind.AmpersandToken or
                        SyntaxKind.AsteriskToken
                            => true,

                        // `(X)..` must be a cast of a range expression, not a member access of some arbitrary expression.
                        SyntaxKind.DotToken when IsAtDotDotToken()
                            => true,

                        var tk
                            => CanFollowCast(tk)
                    };

                case ScanTypeFlags.GenericTypeOrMethod:
                case ScanTypeFlags.TupleType:
                    // If we have `(X<Y>)[...` then we know this must be a cast of a collection expression, not an index
                    // into some expr. As most collections are generic, the common case is not ambiguous.
                    //
                    // Things are still ambiguous if you have `(X)[...` and for back compat we still parse that as
                    // indexing into an expression.  The user can still write `(X)([...` in this case though to get cast
                    // parsing. As non-generic casts are the rare case for collection expressions, this gives a good
                    // balance of back compat and user ease for the normal case.
                    return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken || CanFollowCast(this.CurrentToken.Kind);

                case ScanTypeFlags.GenericTypeOrExpression:
                case ScanTypeFlags.NonGenericTypeOrExpression:
                    // if we have `(A)[]` then treat that always as a cast of an empty collection expression.  `[]` is not
                    // legal on the RHS in any other circumstances for a parenthesized expr.
                    if (this.CurrentToken.Kind == SyntaxKind.OpenBracketToken &&
                        this.PeekToken(1).Kind == SyntaxKind.CloseBracketToken)
                    {
                        return true;
                    }

                    // check for ambiguous type or expression followed by disambiguating token.  i.e.
                    //
                    // "(A)b" is a cast.  But "(A)+b" is not a cast.  
                    return CanFollowCast(this.CurrentToken.Kind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(type);
            }

            bool isBinaryPattern()
            {
                if (!isBinaryPatternKeyword())
                {
                    return false;
                }

                bool lastTokenIsBinaryOperator = true;

                EatToken();
                while (isBinaryPatternKeyword())
                {
                    // If we see a subsequent binary pattern token, it can't be an operator.
                    // Later, it will be parsed as an identifier.
                    lastTokenIsBinaryOperator = !lastTokenIsBinaryOperator;
                    EatToken();
                }

                // In case a combinator token is used as a constant, we explicitly check that a pattern is NOT followed.
                // Such as `(e is (int)or or >= 0)` versus `(e is (int) or or)`
                return lastTokenIsBinaryOperator == IsPossibleSubpatternElement();
            }

            bool isBinaryPatternKeyword()
            {
                return this.CurrentToken.ContextualKind is SyntaxKind.OrKeyword or SyntaxKind.AndKeyword;
            }
        }

        /// <summary>
        /// Tokens that match the following are considered a possible lambda expression:
        /// <code>attribute-list* ('async' | 'static')* type? ('(' | identifier) ...</code>
        /// For better error recovery 'static =>' is also considered a possible lambda expression.
        /// </summary>
        private bool IsPossibleLambdaExpression(Precedence precedence)
        {
            if (precedence > Precedence.Lambda)
            {
                return false;
            }

            var token1 = this.PeekToken(1);

            // x => 
            //
            // Def a lambda.
            if (token1.Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }

            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            // A lambda could be starting with attributes, attempt to skip past them and check after that point.
            if (CurrentToken.Kind == SyntaxKind.OpenBracketToken)
            {
                // Subtle case to deal with.  Consider:
                //
                // [X, () => {}       vs:
                // [X] () => {}
                //
                // The former is a collection expression, the latter an attributed-lambda.  However, we will likely
                // successfully parse out `[X,` as an incomplete attribute, and thus think the former is the latter. So,
                // to ensure proper parsing of the collection expressions, bail out if the attribute is not complete.
                var attributeDeclarations = ParseAttributeDeclarations(inExpressionContext: true);
                if (attributeDeclarations is [.., { CloseBracketToken.IsMissing: true }])
                    return false;
            }

            bool seenStatic;
            if (this.CurrentToken.Kind == SyntaxKind.StaticKeyword)
            {
                EatToken();
                seenStatic = true;
            }
            else if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword &&
                     this.PeekToken(1).Kind == SyntaxKind.StaticKeyword)
            {
                EatToken();
                EatToken();
                seenStatic = true;
            }
            else
            {
                seenStatic = false;
            }

            if (seenStatic)
            {
                if (this.CurrentToken.Kind == SyntaxKind.EqualsGreaterThanToken)
                {
                    // 1. `static =>`
                    // 2. `async static =>`

                    // This is an error case, but we have enough code in front of us to be certain
                    // the user was trying to write a static lambda.
                    return true;
                }

                if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
                {
                    // 1. `static (...
                    // 2. `async static (...
                    return true;
                }
            }

            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken &&
                this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                // 1. `a => ...`
                // 1. `static a => ...`
                // 2. `async static a => ...`
                return true;
            }

            // Have checked all the static forms.  And have checked for the basic `a => a` form.  
            // At this point we have must be on 'async' or an explicit return type for this to still be a lambda.
            if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword &&
                IsAnonymousFunctionAsyncModifier())
            {
                EatToken();
            }

            using (var nestedResetPoint = this.GetDisposableResetPoint(resetOnDispose: false))
            {
                var st = ScanType();
                if (st == ScanTypeFlags.NotType || this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
                {
                    nestedResetPoint.Reset();
                }
            }

            // However, just because we're on `async` doesn't mean we're a lambda.  We might have
            // something lambda-like like:
            //
            //      async a => ...  // or
            //      async (a) => ...
            //
            // Or we could have something that isn't a lambda like:
            //
            //      async ();

            // 'async <identifier> => ...' looks like an async simple lambda
            if (this.CurrentToken.Kind == SyntaxKind.IdentifierToken &&
                this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                // async a => ...
                return true;
            }

            // Non-simple async lambda must be of the form 'async (...'
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            // Check whether looks like implicitly or explicitly typed lambda
            return ScanParenthesizedLambda(precedence);
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
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
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
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
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

        private CollectionExpressionSyntax ParseCollectionExpression()
        {
            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenBracketToken);
            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);
            var list = this.ParseCommaSeparatedSyntaxList(
                ref openBracket,
                SyntaxKind.CloseBracketToken,
                static @this => @this.IsPossibleCollectionElement(),
                static @this => @this.ParseCollectionElement(),
                skipBadCollectionElementTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.CollectionExpression(
                openBracket,
                list,
                this.EatToken(SyntaxKind.CloseBracketToken));

            static PostSkipAction skipBadCollectionElementTokens(
                LanguageParser @this, ref SyntaxToken openBracket, SeparatedSyntaxListBuilder<CollectionElementSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref openBracket, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleCollectionElement(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
        }

        private bool IsPossibleCollectionElement()
        {
            return this.IsPossibleExpression();
        }

        private CollectionElementSyntax ParseCollectionElement()
        {
            return IsAtDotDotToken()
                ? _syntaxFactory.SpreadElement(this.EatDotDotToken(), this.ParseExpressionCore())
                : _syntaxFactory.ExpressionElement(this.ParseExpressionCore());
        }

        private bool IsAnonymousType()
        {
            return this.CurrentToken.Kind == SyntaxKind.NewKeyword && this.PeekToken(1).Kind == SyntaxKind.OpenBraceToken;
        }

        private AnonymousObjectCreationExpressionSyntax ParseAnonymousTypeExpression()
        {
            Debug.Assert(IsAnonymousType());
            var @new = this.EatToken(SyntaxKind.NewKeyword);

            Debug.Assert(this.CurrentToken.Kind == SyntaxKind.OpenBraceToken);

            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var expressions = ParseCommaSeparatedSyntaxList(
                ref openBrace,
                SyntaxKind.CloseBraceToken,
                static @this => @this.IsPossibleExpression(),
                static @this => @this.ParseAnonymousTypeMemberInitializer(),
                SkipBadInitializerListTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.AnonymousObjectCreationExpression(
                @new,
                openBrace,
                expressions,
                this.EatToken(SyntaxKind.CloseBraceToken));
        }

        private AnonymousObjectMemberDeclaratorSyntax ParseAnonymousTypeMemberInitializer()
        {
            return _syntaxFactory.AnonymousObjectMemberDeclarator(
                this.IsNamedAssignment() ? ParseNameEquals() : null,
                this.ParseExpressionCore());
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

        private bool IsNamedMemberInitializer()
        {
            return IsTrueIdentifier() && this.PeekToken(1).Kind is SyntaxKind.EqualsToken or SyntaxKind.ColonToken;
        }

        private bool IsDictionaryInitializer()
        {
            return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken;
        }

        private ExpressionSyntax ParseArrayOrObjectCreationExpression()
        {
            SyntaxToken @new = this.EatToken(SyntaxKind.NewKeyword);

            TypeSyntax type = null;
            InitializerExpressionSyntax initializer = null;

            if (!IsImplicitObjectCreation())
            {
                type = this.ParseType(ParseTypeMode.NewExpression);
                if (type.Kind == SyntaxKind.ArrayType)
                {
                    // Check for an initializer.
                    if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
                    {
                        initializer = this.ParseArrayInitializer();
                    }

                    return _syntaxFactory.ArrayCreationExpression(@new, (ArrayTypeSyntax)type, initializer);
                }
            }

            ArgumentListSyntax argumentList = null;
            if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
            {
                argumentList = this.ParseParenthesizedArgumentList();
            }

            if (this.CurrentToken.Kind == SyntaxKind.OpenBraceToken)
            {
                initializer = this.ParseObjectOrCollectionInitializer();
            }

            // we need one or the other.  also, don't bother reporting this if we already complained about the new type.
            if (argumentList == null && initializer == null)
            {
                argumentList = _syntaxFactory.ArgumentList(
                    this.EatToken(SyntaxKind.OpenParenToken, ErrorCode.ERR_BadNewExpr, reportError: type?.ContainsDiagnostics == false),
                    default(SeparatedSyntaxList<ArgumentSyntax>),
                    SyntaxFactory.MissingToken(SyntaxKind.CloseParenToken));
            }

            return type is null
                ? _syntaxFactory.ImplicitObjectCreationExpression(@new, argumentList, initializer)
                : _syntaxFactory.ObjectCreationExpression(@new, type, argumentList, initializer);
        }

        private bool IsImplicitObjectCreation()
        {
            // The caller is expected to have consumed the new keyword.
            if (this.CurrentToken.Kind != SyntaxKind.OpenParenToken)
            {
                return false;
            }

            using var _1 = this.GetDisposableResetPoint(resetOnDispose: true);

            this.EatToken(); // open paren
            ScanTypeFlags scanTypeFlags = ScanTupleType(out _);
            if (scanTypeFlags != ScanTypeFlags.NotType)
            {
                switch (this.CurrentToken.Kind)
                {
                    case SyntaxKind.QuestionToken:    // e.g. `new(a, b)?()`
                    case SyntaxKind.OpenBracketToken: // e.g. `new(a, b)[]`
                    case SyntaxKind.OpenParenToken:   // e.g. `new(a, b)()` for better error recovery
                        return false;
                }
            }

            return true;
        }

#nullable enable

        private WithExpressionSyntax ParseWithExpression(ExpressionSyntax receiverExpression, SyntaxToken withKeyword)
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var list = this.ParseCommaSeparatedSyntaxList(
                ref openBrace,
                SyntaxKind.CloseBraceToken,
                static @this => @this.IsPossibleExpression(),
                static @this => @this.ParseExpressionCore(),
                SkipBadInitializerListTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.WithExpression(
                receiverExpression,
                withKeyword,
                _syntaxFactory.InitializerExpression(
                    SyntaxKind.WithInitializerExpression,
                    openBrace,
                    list,
                    this.EatToken(SyntaxKind.CloseBraceToken)));
        }

#nullable disable

        private InitializerExpressionSyntax ParseObjectOrCollectionInitializer()
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            var initializers = this.ParseCommaSeparatedSyntaxList(
                ref openBrace,
                SyntaxKind.CloseBraceToken,
                static @this => @this.IsInitializerMember(),
                static @this => @this.ParseObjectOrCollectionInitializerMember(),
                SkipBadInitializerListTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: true);

            var kind = isObjectInitializer(initializers) ? SyntaxKind.ObjectInitializerExpression : SyntaxKind.CollectionInitializerExpression;

            return _syntaxFactory.InitializerExpression(
                kind,
                openBrace,
                initializers,
                this.EatToken(SyntaxKind.CloseBraceToken));

            static bool isObjectInitializer(SeparatedSyntaxList<ExpressionSyntax> initializers)
            {
                // Empty initializer list must be parsed as an object initializer.
                if (initializers.Count == 0)
                    return true;

                // We have at least one initializer expression. If at least one initializer expression is a named
                // assignment, this is an object initializer. Otherwise, this is a collection initializer.
                for (int i = 0, n = initializers.Count; i < n; i++)
                {
                    if (initializers[i] is AssignmentExpressionSyntax
                        {
                            Kind: SyntaxKind.SimpleAssignmentExpression,
                            Left.Kind: SyntaxKind.IdentifierName or SyntaxKind.ImplicitElementAccess,
                        })
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private ExpressionSyntax ParseObjectOrCollectionInitializerMember()
        {
            if (this.IsComplexElementInitializer())
            {
                // { ... }
                return this.ParseComplexElementInitializer();
            }
            else if (IsDictionaryInitializer())
            {
                // [...] = { ... }
                // [...] = ref <expr>
                // [...] = <expr>
                return this.ParseDictionaryInitializer();
            }
            else if (this.IsNamedMemberInitializer())
            {
                // Name = { ... }
                // Name = ref <expr>
                // Name =  <expr>
                return this.ParseObjectInitializerNamedAssignment();
            }
            else
            {
                // <expr>
                // ref <expr>
                return this.ParsePossibleRefExpression();
            }
        }

        private static PostSkipAction SkipBadInitializerListTokens<T>(
            LanguageParser @this, ref SyntaxToken startToken, SeparatedSyntaxListBuilder<T> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            where T : CSharpSyntaxNode
        {
            return @this.SkipBadSeparatedListTokensWithExpectedKind(ref startToken, list,
                static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleExpression(),
                static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                expectedKind, closeKind);
        }

        private AssignmentExpressionSyntax ParseObjectInitializerNamedAssignment()
        {
            return _syntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                this.ParseIdentifierName(),
                this.CurrentToken.Kind == SyntaxKind.ColonToken
                    ? this.EatTokenAsKind(SyntaxKind.EqualsToken)
                    : this.EatToken(SyntaxKind.EqualsToken),
                this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                    ? this.ParseObjectOrCollectionInitializer()
                    : this.ParsePossibleRefExpression());
        }

        private AssignmentExpressionSyntax ParseDictionaryInitializer()
        {
            return _syntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                _syntaxFactory.ImplicitElementAccess(this.ParseBracketedArgumentList()),
                this.EatToken(SyntaxKind.EqualsToken),
                this.CurrentToken.Kind == SyntaxKind.OpenBraceToken
                    ? this.ParseObjectOrCollectionInitializer()
                    : this.ParsePossibleRefExpression());
        }

        private InitializerExpressionSyntax ParseComplexElementInitializer()
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);

            var initializers = this.ParseCommaSeparatedSyntaxList(
                ref openBrace,
                SyntaxKind.CloseBraceToken,
                static @this => @this.IsPossibleExpression(),
                static @this => @this.ParseExpressionCore(),
                SkipBadInitializerListTokens,
                allowTrailingSeparator: false,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.InitializerExpression(
                SyntaxKind.ComplexElementInitializerExpression,
                openBrace,
                initializers,
                this.EatToken(SyntaxKind.CloseBraceToken));
        }

        private bool IsImplicitlyTypedArray()
        {
            Debug.Assert(this.CurrentToken.Kind is SyntaxKind.NewKeyword or SyntaxKind.StackAllocKeyword);
            return this.PeekToken(1).Kind == SyntaxKind.OpenBracketToken;
        }

        private ImplicitArrayCreationExpressionSyntax ParseImplicitlyTypedArrayCreation()
        {
            var @new = this.EatToken(SyntaxKind.NewKeyword);
            var openBracket = this.EatToken(SyntaxKind.OpenBracketToken);

            var commas = _pool.Allocate();

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

            return _syntaxFactory.ImplicitArrayCreationExpression(
                @new,
                openBracket,
                _pool.ToTokenListAndFree(commas),
                this.EatToken(SyntaxKind.CloseBracketToken),
                this.ParseArrayInitializer());
        }

        private InitializerExpressionSyntax ParseArrayInitializer()
        {
            var openBrace = this.EatToken(SyntaxKind.OpenBraceToken);
            var list = this.ParseCommaSeparatedSyntaxList(
                ref openBrace,
                SyntaxKind.CloseBraceToken,
                static @this => @this.IsPossibleVariableInitializer(),
                static @this => @this.ParseVariableInitializer(),
                skipBadArrayInitializerTokens,
                allowTrailingSeparator: true,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            return _syntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                openBrace,
                list,
                this.EatToken(SyntaxKind.CloseBraceToken));

            static PostSkipAction skipBadArrayInitializerTokens(
                LanguageParser @this, ref SyntaxToken openBrace, SeparatedSyntaxListBuilder<ExpressionSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref openBrace, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleVariableInitializer(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
        }

        private ExpressionSyntax ParseStackAllocExpression()
        {
            return this.IsImplicitlyTypedArray()
                ? ParseImplicitlyTypedStackAllocExpression()
                : ParseRegularStackAllocExpression();
        }

        private ExpressionSyntax ParseImplicitlyTypedStackAllocExpression()
        {
            var @stackalloc = this.EatToken(SyntaxKind.StackAllocKeyword);
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

            return _syntaxFactory.ImplicitStackAllocArrayCreationExpression(
                @stackalloc,
                openBracket,
                this.EatToken(SyntaxKind.CloseBracketToken),
                this.ParseArrayInitializer());
        }

        private ExpressionSyntax ParseRegularStackAllocExpression()
        {
            return _syntaxFactory.StackAllocArrayCreationExpression(
                this.EatToken(SyntaxKind.StackAllocKeyword),
                this.ParseType(),
                this.CurrentToken.Kind == SyntaxKind.OpenBraceToken ? this.ParseArrayInitializer() : null);
        }

        private AnonymousMethodExpressionSyntax ParseAnonymousMethodExpression()
        {
            var parentScopeIsInAsync = this.IsInAsync;

            var parentScopeForceConditionalAccess = this.ForceConditionalAccessExpression;
            this.ForceConditionalAccessExpression = false;

            var result = parseAnonymousMethodExpressionWorker();

            this.ForceConditionalAccessExpression = parentScopeForceConditionalAccess;
            this.IsInAsync = parentScopeIsInAsync;

            return result;

            AnonymousMethodExpressionSyntax parseAnonymousMethodExpressionWorker()
            {
                var modifiers = ParseAnonymousFunctionModifiers();
                if (modifiers.Any((int)SyntaxKind.AsyncKeyword))
                {
                    this.IsInAsync = true;
                }

                var @delegate = this.EatToken(SyntaxKind.DelegateKeyword);

                ParameterListSyntax parameterList = null;
                if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
                {
                    parameterList = this.ParseParenthesizedParameterList(forExtension: false);
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
                        modifiers,
                        @delegate,
                        parameterList,
                        _syntaxFactory.Block(
                            attributeLists: default,
                            openBrace,
                            statements: default,
                            SyntaxFactory.MissingToken(SyntaxKind.CloseBraceToken)),
                        expressionBody: null);
                }

                return _syntaxFactory.AnonymousMethodExpression(
                    modifiers,
                    @delegate,
                    parameterList,
                    this.ParseBlock(attributes: default),
                    expressionBody: null);
            }
        }

        private SyntaxList<SyntaxToken> ParseAnonymousFunctionModifiers()
        {
            var modifiers = _pool.Allocate();

            while (true)
            {
                if (this.CurrentToken.Kind == SyntaxKind.StaticKeyword)
                {
                    modifiers.Add(this.EatToken(SyntaxKind.StaticKeyword));
                    continue;
                }

                if (this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword &&
                    IsAnonymousFunctionAsyncModifier())
                {
                    modifiers.Add(this.EatContextualToken(SyntaxKind.AsyncKeyword));
                    continue;
                }

                break;
            }

            return _pool.ToTokenListAndFree(modifiers);
        }

        private bool IsAnonymousFunctionAsyncModifier()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.AsyncKeyword);

            switch (this.PeekToken(1).Kind)
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.IdentifierToken:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.DelegateKeyword:
                    return true;
                case var kind:
                    return IsPredefinedType(kind);
            }
        }

        /// <summary>
        /// Parse expected lambda expression but assume `x ? () => y :` is a conditional
        /// expression rather than a lambda expression with an explicit return type and
        /// return null in that case only.
        /// </summary>
        private LambdaExpressionSyntax TryParseLambdaExpression()
        {
            using var resetPoint = this.GetDisposableResetPoint(resetOnDispose: false);
            var result = ParseLambdaExpression();

            if (this.CurrentToken.Kind == SyntaxKind.ColonToken &&
                result is ParenthesizedLambdaExpressionSyntax { ReturnType: NullableTypeSyntax })
            {
                resetPoint.Reset();
                return null;
            }

            return result;
        }

        private LambdaExpressionSyntax ParseLambdaExpression()
        {
            var attributes = ParseAttributeDeclarations(inExpressionContext: true);
            var parentScopeIsInAsync = this.IsInAsync;

            var parentScopeForceConditionalAccess = this.ForceConditionalAccessExpression;
            this.ForceConditionalAccessExpression = false;

            var result = parseLambdaExpressionWorker();

            this.ForceConditionalAccessExpression = parentScopeForceConditionalAccess;
            this.IsInAsync = parentScopeIsInAsync;

            return result;

            LambdaExpressionSyntax parseLambdaExpressionWorker()
            {
                var modifiers = ParseAnonymousFunctionModifiers();
                if (modifiers.Any((int)SyntaxKind.AsyncKeyword))
                {
                    this.IsInAsync = true;
                }

                TypeSyntax returnType;
                using (var resetPoint = this.GetDisposableResetPoint(resetOnDispose: false))
                {
                    returnType = ParseReturnType();
                    if (CurrentToken.Kind != SyntaxKind.OpenParenToken)
                    {
                        resetPoint.Reset();
                        returnType = null;
                    }
                }

                if (this.CurrentToken.Kind == SyntaxKind.OpenParenToken)
                {
                    var paramList = this.ParseLambdaParameterList();
                    var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);
                    var (block, expression) = ParseLambdaBody();

                    return _syntaxFactory.ParenthesizedLambdaExpression(
                        attributes, modifiers, returnType, paramList, arrow, block, expression);
                }
                else
                {
                    // Unparenthesized lambda case
                    // x => ...
                    var identifier = (this.CurrentToken.Kind != SyntaxKind.IdentifierToken && this.PeekToken(1).Kind == SyntaxKind.EqualsGreaterThanToken)
                        ? this.EatTokenAsKind(SyntaxKind.IdentifierToken)
                        : this.ParseIdentifierToken();

                    // Case x=>, x =>
                    var arrow = this.EatToken(SyntaxKind.EqualsGreaterThanToken);

                    var parameter = _syntaxFactory.Parameter(
                        attributeLists: default, modifiers: default, type: null, identifier, @default: null);
                    var (block, expression) = ParseLambdaBody();
                    return _syntaxFactory.SimpleLambdaExpression(
                        attributes, modifiers, parameter, arrow, block, expression);
                }
            }
        }

        private (BlockSyntax, ExpressionSyntax) ParseLambdaBody()
            => CurrentToken.Kind == SyntaxKind.OpenBraceToken
                ? (ParseBlock(attributes: default), null)
                : (null, ParsePossibleRefExpression());

        private ParameterListSyntax ParseLambdaParameterList()
        {
            var openParen = this.EatToken(SyntaxKind.OpenParenToken);
            var saveTerm = _termState;
            _termState |= TerminatorState.IsEndOfParameterList;

            var nodes = ParseCommaSeparatedSyntaxList(
                ref openParen,
                SyntaxKind.CloseParenToken,
                static @this => @this.IsPossibleLambdaParameter(),
                static @this => @this.ParseLambdaParameter(),
                skipBadLambdaParameterListTokens,
                allowTrailingSeparator: false,
                requireOneElement: false,
                allowSemicolonAsSeparator: false);

            _termState = saveTerm;

            return _syntaxFactory.ParameterList(
                openParen,
                nodes,
                this.EatToken(SyntaxKind.CloseParenToken));

            static PostSkipAction skipBadLambdaParameterListTokens(
                LanguageParser @this, ref SyntaxToken openParen, SeparatedSyntaxListBuilder<ParameterSyntax> list, SyntaxKind expectedKind, SyntaxKind closeKind)
            {
                return @this.SkipBadSeparatedListTokensWithExpectedKind(ref openParen, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken && !p.IsPossibleLambdaParameter(),
                    static (p, closeKind) => p.CurrentToken.Kind == closeKind,
                    expectedKind, closeKind);
            }
        }

        private bool IsPossibleLambdaParameter()
        {
            switch (this.CurrentToken.Kind)
            {
                case SyntaxKind.ParamsKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.OpenParenToken:   // tuple
                case SyntaxKind.OpenBracketToken: // attribute
                    return true;

                case SyntaxKind.IdentifierToken:
                    return this.IsTrueIdentifier();

                case SyntaxKind.DelegateKeyword:
                    return this.IsFunctionPointerStart();

                default:
                    return IsPredefinedType(this.CurrentToken.Kind);
            }
        }

        private ParameterSyntax ParseLambdaParameter()
        {
            var attributes = ParseAttributeDeclarations(inExpressionContext: false);

            // Params are actually illegal in a lambda, but we'll allow it for error recovery purposes and
            // give the "params unexpected" error at semantic analysis time.
            SyntaxListBuilder modifiers = _pool.Allocate();
            if (IsParameterModifierIncludingScoped(this.CurrentToken))
            {
                ParseParameterModifiers(modifiers, isFunctionPointerParameter: false, isLambdaParameter: true);
            }

            var paramType = ShouldParseLambdaParameterType()
                ? ParseType(ParseTypeMode.Parameter)
                : null;

            var identifier = this.ParseIdentifierToken();

            // Parse default value if any
            var equalsToken = TryEatToken(SyntaxKind.EqualsToken);

            return _syntaxFactory.Parameter(
                attributes,
                _pool.ToTokenListAndFree(modifiers),
                paramType,
                identifier,
                equalsToken != null
                    ? _syntaxFactory.EqualsValueClause(equalsToken, this.ParseExpressionCore())
                    : null);
        }

        private bool ShouldParseLambdaParameterType()
        {
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

            if (this.IsFunctionPointerStart())
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
                //      (a =
                //
                // In all other cases, parse out a type.
                var peek1 = this.PeekToken(1);
                if (peek1.Kind
                        is not SyntaxKind.CommaToken
                        and not SyntaxKind.CloseParenToken
                        and not SyntaxKind.EqualsGreaterThanToken
                        and not SyntaxKind.OpenBraceToken
                        and not SyntaxKind.EqualsToken)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCurrentTokenQueryContextualKeyword
            => IsTokenQueryContextualKeyword(this.CurrentToken);

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
            return this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword &&
                this.IsQueryExpressionAfterFrom(mayBeVariableDeclaration, mayBeMemberDeclaration);
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
                    if (pk2 is SyntaxKind.SemicolonToken or    // from x;
                               SyntaxKind.CommaToken or        // from x, y;
                               SyntaxKind.EqualsToken)         // from x = null;
                    {
                        return false;
                    }
                }

                if (mayBeMemberDeclaration)
                {
                    // from idf { ...   property decl
                    // from idf(...     method decl
                    if (pk2 is SyntaxKind.OpenParenToken or SyntaxKind.OpenBraceToken)
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
            using var _ = this.GetDisposableResetPoint(resetOnDispose: true);

            this.EatToken();
            return this.ScanType() != ScanTypeFlags.NotType && this.CurrentToken.Kind is SyntaxKind.IdentifierToken or SyntaxKind.InKeyword;
        }

        private QueryExpressionSyntax ParseQueryExpression(Precedence precedence)
        {
            var previousIsInQuery = this.IsInQuery;
            this.IsInQuery = true;
            var fc = this.ParseFromClause();
            if (precedence > Precedence.Assignment)
            {
                fc = this.AddError(fc, ErrorCode.WRN_PrecedenceInversion, SyntaxFacts.GetText(SyntaxKind.FromKeyword));
            }

            var body = this.ParseQueryBody();
            this.IsInQuery = previousIsInQuery;
            return _syntaxFactory.QueryExpression(fc, body);
        }

        private QueryBodySyntax ParseQueryBody()
        {
            var clauses = _pool.Allocate<QueryClauseSyntax>();

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
            SelectOrGroupClauseSyntax selectOrGroupBy = this.CurrentToken.ContextualKind switch
            {
                SyntaxKind.SelectKeyword => this.ParseSelectClause(),
                SyntaxKind.GroupKeyword => this.ParseGroupClause(),
                _ => _syntaxFactory.SelectClause(
                    this.EatToken(SyntaxKind.SelectKeyword, ErrorCode.ERR_ExpectedSelectOrGroup),
                    this.CreateMissingIdentifierName()),
            };

            return _syntaxFactory.QueryBody(
                _pool.ToListAndFree(clauses),
                selectOrGroupBy,
                this.CurrentToken.ContextualKind == SyntaxKind.IntoKeyword
                    ? this.ParseQueryContinuation()
                    : null);
        }

        private FromClauseSyntax ParseFromClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.FromKeyword);
            var @from = this.EatContextualToken(SyntaxKind.FromKeyword);

            var type = this.PeekToken(1).Kind != SyntaxKind.InKeyword
                ? this.ParseType()
                : null;

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

            return _syntaxFactory.FromClause(
                @from,
                type,
                name,
                this.EatToken(SyntaxKind.InKeyword),
                this.ParseExpressionCore());
        }

        private JoinClauseSyntax ParseJoinClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.JoinKeyword);
            return _syntaxFactory.JoinClause(
                joinKeyword: this.EatContextualToken(SyntaxKind.JoinKeyword),
                type: this.PeekToken(1).Kind != SyntaxKind.InKeyword
                    ? this.ParseType()
                    : null,
                identifier: this.ParseIdentifierToken(),
                inKeyword: this.EatToken(SyntaxKind.InKeyword),
                inExpression: this.ParseExpressionCore(),
                onKeyword: this.EatContextualToken(SyntaxKind.OnKeyword, ErrorCode.ERR_ExpectedContextualKeywordOn),
                leftExpression: this.ParseExpressionCore(),
                equalsKeyword: this.EatContextualToken(SyntaxKind.EqualsKeyword, ErrorCode.ERR_ExpectedContextualKeywordEquals),
                rightExpression: this.ParseExpressionCore(),
                into: this.CurrentToken.ContextualKind == SyntaxKind.IntoKeyword
                    ? _syntaxFactory.JoinIntoClause(ConvertToKeyword(this.EatToken()), this.ParseIdentifierToken())
                    : null);
        }

        private LetClauseSyntax ParseLetClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.LetKeyword);
            return _syntaxFactory.LetClause(
                this.EatContextualToken(SyntaxKind.LetKeyword),
                // If we see a keyword followed by '=', use EatTokenAsKind to produce a better error message and recover well.
                SyntaxFacts.IsReservedKeyword(this.CurrentToken.Kind) && this.PeekToken(1).Kind == SyntaxKind.EqualsToken
                    ? this.EatTokenAsKind(SyntaxKind.IdentifierToken)
                    : this.ParseIdentifierToken(),
                this.EatToken(SyntaxKind.EqualsToken),
                this.ParseExpressionCore());
        }

        private WhereClauseSyntax ParseWhereClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.WhereKeyword);
            return _syntaxFactory.WhereClause(
                this.EatContextualToken(SyntaxKind.WhereKeyword),
                this.ParseExpressionCore());
        }

        private OrderByClauseSyntax ParseOrderByClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.OrderByKeyword);
            var @orderby = this.EatContextualToken(SyntaxKind.OrderByKeyword);

            var list = _pool.AllocateSeparated<OrderingSyntax>();
            // first argument
            list.Add(this.ParseOrdering());

            // additional arguments
            while (this.CurrentToken.Kind == SyntaxKind.CommaToken)
            {
                if (this.CurrentToken.Kind is SyntaxKind.CloseParenToken or SyntaxKind.SemicolonToken)
                {
                    break;
                }
                else if (this.CurrentToken.Kind == SyntaxKind.CommaToken)
                {
                    list.AddSeparator(this.EatToken(SyntaxKind.CommaToken));
                    list.Add(this.ParseOrdering());
                    continue;
                }
                else if (skipBadOrderingListTokens(list, SyntaxKind.CommaToken) == PostSkipAction.Abort)
                {
                    break;
                }
            }

            return _syntaxFactory.OrderByClause(
                @orderby,
                _pool.ToListAndFree(list));

            PostSkipAction skipBadOrderingListTokens(SeparatedSyntaxListBuilder<OrderingSyntax> list, SyntaxKind expected)
            {
                CSharpSyntaxNode tmp = null;
                Debug.Assert(list.Count > 0);
                return this.SkipBadSeparatedListTokensWithExpectedKind(ref tmp, list,
                    static p => p.CurrentToken.Kind != SyntaxKind.CommaToken,
                    static (p, _) => p.CurrentToken.Kind == SyntaxKind.CloseParenToken
                        || p.CurrentToken.Kind == SyntaxKind.SemicolonToken
                        || p.IsCurrentTokenQueryContextualKeyword,
                    expected);
            }
        }

        private OrderingSyntax ParseOrdering()
        {
            var expression = this.ParseExpressionCore();
            SyntaxToken direction = null;
            SyntaxKind kind = SyntaxKind.AscendingOrdering;

            if (this.CurrentToken.ContextualKind is SyntaxKind.AscendingKeyword or SyntaxKind.DescendingKeyword)
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
            return _syntaxFactory.SelectClause(
                this.EatContextualToken(SyntaxKind.SelectKeyword),
                this.ParseExpressionCore());
        }

        private GroupClauseSyntax ParseGroupClause()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.GroupKeyword);
            return _syntaxFactory.GroupClause(
                this.EatContextualToken(SyntaxKind.GroupKeyword),
                this.ParseExpressionCore(),
                this.EatContextualToken(SyntaxKind.ByKeyword, ErrorCode.ERR_ExpectedContextualKeywordBy),
                this.ParseExpressionCore());
        }

        private QueryContinuationSyntax ParseQueryContinuation()
        {
            Debug.Assert(this.CurrentToken.ContextualKind == SyntaxKind.IntoKeyword);
            return _syntaxFactory.QueryContinuation(
                this.EatContextualToken(SyntaxKind.IntoKeyword),
                this.ParseIdentifierToken(),
                this.ParseQueryBody());
        }

        [Obsolete("Use IsIncrementalAndFactoryContextMatches")]
#pragma warning disable IDE0051 // Remove unused private members
        private new bool IsIncremental
#pragma warning restore IDE0051 // Remove unused private members
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
                context.IsInQuery == green.ParsedInQuery &&
                context.IsInFieldKeywordContext == green.ParsedInFieldKeywordContext;
        }

        private bool IsInAsync
        {
            get => _syntaxFactoryContext.IsInAsync;
            set => _syntaxFactoryContext.IsInAsync = value;
        }

        private bool ForceConditionalAccessExpression
        {
            get => _syntaxFactoryContext.ForceConditionalAccessExpression;
            set => _syntaxFactoryContext.ForceConditionalAccessExpression = value;
        }

        private bool IsInQuery
        {
            get => _syntaxFactoryContext.IsInQuery;
            set => _syntaxFactoryContext.IsInQuery = value;
        }

        internal bool IsInFieldKeywordContext
        {
            get => _syntaxFactoryContext.IsInFieldKeywordContext;
            set => _syntaxFactoryContext.IsInFieldKeywordContext = value;
        }

        private delegate PostSkipAction SkipBadTokens<TNode>(
            LanguageParser parser, ref SyntaxToken openToken, SeparatedSyntaxListBuilder<TNode> builder, SyntaxKind expectedKind, SyntaxKind closeTokenKind) where TNode : GreenNode;

#nullable enable

        /// <summary>
        /// Parses a comma separated list of nodes.
        /// </summary>
        /// <typeparam name="TNode">The type of node to return back in the <see cref="SeparatedSyntaxList{TNode}"/>.</typeparam>
        /// <param name="openToken">The token preceding the separated elements.  Used to attach skipped tokens to if no
        /// elements have been parsed out yet, and the error recovery algorithm chooses to continue parsing, versus
        /// aborting the list parsing.</param>
        /// <param name="closeTokenKind">The token kind to look for that indicates the list is complete</param>
        /// <param name="isPossibleElement">Callback to indicate if the parser is at a point in the source that could
        /// parse out a <typeparamref name="TNode"/>.</param>
        /// <param name="parseElement">Callback to actually parse out an element.  May be called even at a location
        /// where <paramref name="isPossibleElement"/> returned <see langword="false"/> for.</param>
        /// <param name="skipBadTokens">Error recovery callback.  Used to determine if the list parsing routine should
        /// skip tokens (attaching them to the last thing successfully parsed), and continue looking for more elements.
        /// Or if it should abort parsing the list entirely.</param>
        /// <param name="allowTrailingSeparator">Whether or not a trailing comma is allowed at the end of the list. For
        /// example, an array initializer allows for a trailing comma at the end of it, while a parameter list does
        /// not.</param>
        /// <param name="requireOneElement">Whether or not at least one element is required in the list.  For example, a
        /// parameter list does not require any elements, while an attribute list "<c>[...]</c>" does.</param>
        /// <param name="allowSemicolonAsSeparator">Whether or not an errant semicolon found in a location where a comma
        /// is expected should just be treated as a comma (still with an error reported).  Useful for constructs where users
        /// often forget which separator is needed and use the wrong one.</param>
        /// <remarks>
        /// All the callbacks should passed as static lambdas or static methods to prevent unnecessary delegate
        /// allocations.
        /// </remarks>
        private SeparatedSyntaxList<TNode> ParseCommaSeparatedSyntaxList<TNode>(
            ref SyntaxToken openToken,
            SyntaxKind closeTokenKind,
            Func<LanguageParser, bool> isPossibleElement,
            Func<LanguageParser, TNode> parseElement,
            SkipBadTokens<TNode> skipBadTokens,
            bool allowTrailingSeparator,
            bool requireOneElement,
            bool allowSemicolonAsSeparator) where TNode : GreenNode
        {
            return ParseCommaSeparatedSyntaxList(
                ref openToken,
                closeTokenKind,
                isPossibleElement,
                parseElement,
                immediatelyAbort: null,
                skipBadTokens,
                allowTrailingSeparator,
                requireOneElement,
                allowSemicolonAsSeparator);
        }

        private SeparatedSyntaxList<TNode> ParseCommaSeparatedSyntaxList<TNode>(
            ref SyntaxToken openToken,
            SyntaxKind closeTokenKind,
            Func<LanguageParser, bool> isPossibleElement,
            Func<LanguageParser, TNode> parseElement,
            Func<TNode, bool>? immediatelyAbort,
            SkipBadTokens<TNode> skipBadTokens,
            bool allowTrailingSeparator,
            bool requireOneElement,
            bool allowSemicolonAsSeparator) where TNode : GreenNode
        {
            // If we ever want this function to parse out separated lists with a different separator, we can
            // parameterize this method on this value.
            var separatorTokenKind = SyntaxKind.CommaToken;
            var nodes = _pool.AllocateSeparated<TNode>();

tryAgain:
            if (requireOneElement || this.CurrentToken.Kind != closeTokenKind)
            {
                if (requireOneElement || shouldParseSeparatorOrElement())
                {
                    // first argument
                    var node = parseElement(this);
                    nodes.Add(node);

                    // now that we've gotten one element, we don't require any more.
                    requireOneElement = false;

                    // Ensure that if parsing separators/elements doesn't move us forward, that we always bail out from
                    // parsing this list.
                    int lastTokenPosition = -1;

                    while (immediatelyAbort?.Invoke(node) != true && IsMakingProgress(ref lastTokenPosition))
                    {
                        if (this.CurrentToken.Kind == closeTokenKind)
                            break;

                        if (shouldParseSeparatorOrElement())
                        {
                            // If we got a semicolon instead of comma, consume it with error and act as if it were a
                            // comma. Note: we do not change the kind of the token, so we can end up with a separated
                            // syntax list whose separators are a mix of commas and semicolons.  That is ok and is part
                            // of the expected contract of separated lists.  There will still be a diagnostic on the 
                            // token letting the user know there is an error.  This allows us to recover gracefully,
                            // especially for higher levels like the IDE.
                            nodes.AddSeparator(this.CurrentToken.Kind == SyntaxKind.SemicolonToken
                                ? this.EatTokenEvenWithIncorrectKind(separatorTokenKind)
                                : this.EatToken(separatorTokenKind));

                            if (allowTrailingSeparator)
                            {
                                // check for exit case after legal trailing comma
                                if (this.CurrentToken.Kind == closeTokenKind)
                                {
                                    break;
                                }
                                else if (!isPossibleElement(this))
                                {
                                    goto tryAgain;
                                }
                            }

                            node = parseElement(this);
                            nodes.Add(node);
                            continue;
                        }

                        // Something we didn't recognize, try to skip tokens, reporting that we expected a separator here.
                        if (skipBadTokens(this, ref openToken, nodes, separatorTokenKind, closeTokenKind) == PostSkipAction.Abort)
                            break;
                    }
                }
                else if (skipBadTokens(this, ref openToken, nodes, SyntaxKind.IdentifierToken, closeTokenKind) == PostSkipAction.Continue)
                {
                    // Something we didn't recognize, try to skip tokens, reporting that we expected an identifier here.
                    // While 'identifier' may not be completely accurate in terms of what the list needs, it's a
                    // generally good 'catch all' indicating that some name/expr was needed, where something else
                    // invalid was found.
                    goto tryAgain;
                }
            }

            return _pool.ToListAndFree(nodes);

            bool shouldParseSeparatorOrElement()
            {
                // if we're on a separator, we def should parse it out as such.
                if (this.CurrentToken.Kind == separatorTokenKind)
                    return true;

                // We're not on a valid separator, but we want to be resilient for the user accidentally using the wrong
                // one in common cases.
                if (allowSemicolonAsSeparator && this.CurrentToken.Kind is SyntaxKind.SemicolonToken)
                    return true;

                if (isPossibleElement(this))
                    return true;

                return false;
            }
        }

#nullable disable

        private DisposableResetPoint GetDisposableResetPoint(bool resetOnDispose)
            => new DisposableResetPoint(this, resetOnDispose, GetResetPoint());

        private new ResetPoint GetResetPoint()
        {
            return new ResetPoint(
                base.GetResetPoint(),
                _termState,
                IsInAsync,
                IsInQuery,
                IsInFieldKeywordContext);
        }

        private void Reset(ref ResetPoint state)
        {
            _termState = state.TerminatorState;
            IsInAsync = state.IsInAsync;
            IsInQuery = state.IsInQuery;
            IsInFieldKeywordContext = state.IsInFieldKeywordContext;
            base.Reset(ref state.BaseResetPoint);
        }

        private void Release(ref ResetPoint state)
        {
            base.Release(ref state.BaseResetPoint);
        }

        private ref struct DisposableResetPoint
        {
            private readonly LanguageParser _languageParser;
            private readonly bool _resetOnDispose;
            private ResetPoint _resetPoint;

            public DisposableResetPoint(LanguageParser languageParser, bool resetOnDispose, ResetPoint resetPoint)
            {
                _languageParser = languageParser;
                _resetOnDispose = resetOnDispose;
                _resetPoint = resetPoint;
            }

            public void Reset()
                => _languageParser.Reset(ref _resetPoint);

            public void Dispose()
            {
                if (_resetOnDispose)
                    this.Reset();

                _languageParser.Release(ref _resetPoint);
            }
        }

        private new struct ResetPoint
        {
            internal SyntaxParser.ResetPoint BaseResetPoint;
            internal readonly TerminatorState TerminatorState;
            internal readonly bool IsInAsync;
            internal readonly bool IsInQuery;
            internal readonly bool IsInFieldKeywordContext;

            internal ResetPoint(
                SyntaxParser.ResetPoint resetPoint,
                TerminatorState terminatorState,
                bool isInAsync,
                bool isInQuery,
                bool isInFieldKeywordContext)
            {
                this.BaseResetPoint = resetPoint;
                this.TerminatorState = terminatorState;
                this.IsInAsync = isInAsync;
                this.IsInQuery = isInQuery;
                this.IsInFieldKeywordContext = isInFieldKeywordContext;
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

        private static bool ContainsErrorDiagnostic(GreenNode node)
        {
            // ContainsDiagnostics returns true if this node (or any descendants) contain any sort of error.  However,
            // GetDiagnostics() only returns diagnostics at that node itself.  So we have to explicitly walk down the
            // tree to find out if the diagnostics are error or not.

            // Quick check to avoid any unnecessary work.
            if (node.ContainsDiagnostics)
            {
                var stack = ArrayBuilder<GreenNode>.GetInstance();
                try
                {
                    stack.Push(node);

                    while (stack.Count > 0)
                    {
                        var current = stack.Pop();
                        if (!current.ContainsDiagnostics)
                            continue;

                        foreach (var diagnostic in current.GetDiagnostics())
                        {
                            if (diagnostic.Severity == DiagnosticSeverity.Error)
                                return true;
                        }

                        foreach (var child in current.ChildNodesAndTokens())
                            stack.Push(child);
                    }
                }
                finally
                {
                    stack.Free();
                }
            }

            return false;
        }
    }
}
