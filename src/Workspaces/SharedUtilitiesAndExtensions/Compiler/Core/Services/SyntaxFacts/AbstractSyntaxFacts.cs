// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFacts : ISyntaxFacts
    {
        private static readonly ObjectPool<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>> s_stackPool
            = SharedPools.Default<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>>();

        public abstract ISyntaxKinds SyntaxKinds { get; }

        protected AbstractSyntaxFacts()
        {
        }

        public abstract bool IsPreprocessorDirective(SyntaxTrivia trivia);

        public bool IsOnSingleLine(SyntaxNode node, bool fullSpan)
        {
            // The stack logic assumes the initial node is not null
            Contract.ThrowIfNull(node);

            // Use an actual Stack so we can write out deeply recursive structures without overflowing.
            // Note: algorithm is taken from GreenNode.WriteTo.
            //
            // General approach is that we recurse down the nodes, using a real stack object to
            // keep track of what node we're on.  If full-span is true we'll examine all tokens
            // and all the trivia on each token.  If full-span is false we'll examine all tokens
            // but we'll ignore the leading trivia on the very first trivia and the trailing trivia
            // on the very last token.
            var stack = s_stackPool.Allocate();
            stack.Push((node, leading: fullSpan, trailing: fullSpan));

            var result = IsOnSingleLine(stack);

            s_stackPool.ClearAndFree(stack);

            return result;
        }

        private bool IsOnSingleLine(
            Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)> stack)
        {
            while (stack.Count > 0)
            {
                var (currentNodeOrToken, currentLeading, currentTrailing) = stack.Pop();
                if (currentNodeOrToken.IsToken)
                {
                    // If this token isn't on a single line, then the original node definitely
                    // isn't on a single line.
                    if (!IsOnSingleLine(currentNodeOrToken.AsToken(), currentLeading, currentTrailing))
                    {
                        return false;
                    }
                }
                else
                {
                    var currentNode = currentNodeOrToken.AsNode()!;

                    var childNodesAndTokens = currentNode.ChildNodesAndTokens();
                    var childCount = childNodesAndTokens.Count;

                    // Walk the children of this node in reverse, putting on the stack to process.
                    // This way we process the children in the actual child-order they are in for
                    // this node.
                    var index = 0;
                    foreach (var child in childNodesAndTokens.Reverse())
                    {
                        // Since we're walking the children in reverse, if we're on hte 0th item,
                        // that's the last child.
                        var last = index == 0;

                        // Once we get all the way to the end of the reversed list, we're actually
                        // on the first.
                        var first = index == childCount - 1;

                        // We want the leading trivia if we've asked for it, or if we're not the first
                        // token being processed.  We want the trailing trivia if we've asked for it,
                        // or if we're not the last token being processed.
                        stack.Push((child, currentLeading | !first, currentTrailing | !last));
                        index++;
                    }
                }
            }

            // All tokens were on a single line.  This node is on a single line.
            return true;
        }

        private bool IsOnSingleLine(SyntaxToken token, bool leading, bool trailing)
        {
            // If any of our trivia is not on a single line, then we're not on a single line.
            if (!IsOnSingleLine(token.LeadingTrivia, leading) ||
                !IsOnSingleLine(token.TrailingTrivia, trailing))
            {
                return false;
            }

            // Only string literals can span multiple lines.  Only need to check those.
            if (this.SyntaxKinds.StringLiteralToken == token.RawKind ||
                this.SyntaxKinds.InterpolatedStringTextToken == token.RawKind)
            {
                // This allocated.  But we only do it in the string case. For all other tokens
                // we don't need any allocations.
                if (!IsOnSingleLine(token.ToString()))
                {
                    return false;
                }
            }

            // Any other type of token is on a single line.
            return true;
        }

        private bool IsOnSingleLine(SyntaxTriviaList triviaList, bool checkTrivia)
        {
            if (checkTrivia)
            {
                foreach (var trivia in triviaList)
                {
                    if (trivia.HasStructure)
                    {
                        // For structured trivia, we recurse into the trivia to see if it
                        // is on a single line or not.  If it isn't, then we're definitely
                        // not on a single line.
                        if (!IsOnSingleLine(trivia.GetStructure()!, fullSpan: true))
                        {
                            return false;
                        }
                    }
                    else if (this.IsEndOfLineTrivia(trivia))
                    {
                        // Contained an end-of-line trivia.  Definitely not on a single line.
                        return false;
                    }
                    else if (!this.IsWhitespaceTrivia(trivia))
                    {
                        // Was some other form of trivia (like a comment).  Easiest thing
                        // to do is just stringify this and count the number of newlines.
                        // these should be rare.  So the allocation here is ok.
                        if (!IsOnSingleLine(trivia.ToString()))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool IsOnSingleLine(string value)
            => value.GetNumberOfLineBreaks() == 0;

        public bool ContainsInterleavedDirective(
            ImmutableArray<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            if (nodes.Length > 0)
            {
                var span = TextSpan.FromBounds(nodes.First().Span.Start, nodes.Last().Span.End);

                foreach (var node in nodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ContainsInterleavedDirective(span, node, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsInterleavedDirective(SyntaxNode node, CancellationToken cancellationToken)
            => ContainsInterleavedDirective(node.Span, node, cancellationToken);

        public bool ContainsInterleavedDirective(
            TextSpan span, SyntaxNode node, CancellationToken cancellationToken)
        {
            foreach (var token in node.DescendantTokens())
            {
                if (ContainsInterleavedDirective(span, token, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        protected abstract bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken);

        public bool SpansPreprocessorDirective(IEnumerable<SyntaxNode> nodes)
        {
            if (nodes == null || nodes.IsEmpty())
            {
                return false;
            }

            return SpansPreprocessorDirective(nodes.SelectMany(n => n.DescendantTokens()));
        }

        /// <summary>
        /// Determines if there is preprocessor trivia *between* any of the <paramref name="tokens"/>
        /// provided.  The <paramref name="tokens"/> will be deduped and then ordered by position.
        /// Specifically, the first token will not have it's leading trivia checked, and the last
        /// token will not have it's trailing trivia checked.  All other trivia will be checked to
        /// see if it contains a preprocessor directive.
        /// </summary>
        public bool SpansPreprocessorDirective(IEnumerable<SyntaxToken> tokens)
        {
            // we want to check all leading trivia of all tokens (except the 
            // first one), and all trailing trivia of all tokens (except the
            // last one).

            var first = true;
            var previousToken = default(SyntaxToken);

            // Allow duplicate nodes/tokens to be passed in.  Also, allow the nodes/tokens
            // to not be in any particular order when passed in.
            var orderedTokens = tokens.Distinct().OrderBy(t => t.SpanStart);

            foreach (var token in orderedTokens)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    // check the leading trivia of this token, and the trailing trivia
                    // of the previous token.
                    if (SpansPreprocessorDirective(token.LeadingTrivia) ||
                        SpansPreprocessorDirective(previousToken.TrailingTrivia))
                    {
                        return true;
                    }
                }

                previousToken = token;
            }

            return false;
        }

        private bool SpansPreprocessorDirective(SyntaxTriviaList list)
            => list.Any(t => IsPreprocessorDirective(t));

        public abstract SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        public abstract bool IsParameterNameXmlElementSyntax(SyntaxNode node);

        public abstract SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        public bool HasIncompleteParentMember([NotNullWhen(true)] SyntaxNode? node)
            => node?.Parent?.RawKind == SyntaxKinds.IncompleteMember;

        public abstract bool IsCaseSensitive { get; }
        public abstract StringComparer StringComparer { get; }
        public abstract SyntaxTrivia ElasticMarker { get; }
        public abstract SyntaxTrivia ElasticCarriageReturnLineFeed { get; }

        public abstract bool SupportsIndexingInitializer(ParseOptions options);
        public abstract bool SupportsLocalFunctionDeclaration(ParseOptions options);
        public abstract bool SupportsNotPattern(ParseOptions options);
        public abstract bool SupportsRecord(ParseOptions options);
        public abstract bool SupportsRecordStruct(ParseOptions options);
        public abstract bool SupportsThrowExpression(ParseOptions options);
        public abstract SyntaxToken ParseToken(string text);
        public abstract SyntaxTriviaList ParseLeadingTrivia(string text);
        public abstract string EscapeIdentifier(string identifier);
        public abstract bool IsVerbatimIdentifier(SyntaxToken token);
        public abstract bool IsOperator(SyntaxToken token);
        public abstract bool IsPredefinedType(SyntaxToken token);
        public abstract bool IsPredefinedType(SyntaxToken token, PredefinedType type);
        public abstract bool IsPredefinedOperator(SyntaxToken token);
        public abstract bool IsPredefinedOperator(SyntaxToken token, PredefinedOperator op);
        public abstract bool IsReservedKeyword(SyntaxToken token);
        public abstract bool IsContextualKeyword(SyntaxToken token);
        public abstract bool IsPreprocessorKeyword(SyntaxToken token);
        public abstract bool IsPreProcessorDirectiveContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        public abstract bool IsLiteral(SyntaxToken token);
        public abstract bool IsStringLiteralOrInterpolatedStringLiteral(SyntaxToken token);
        public abstract bool IsNumericLiteral(SyntaxToken token);
        public abstract bool IsVerbatimStringLiteral(SyntaxToken token);
        public abstract bool IsUsingOrExternOrImport([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsGlobalAssemblyAttribute([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsGlobalModuleAttribute([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeclaration(SyntaxNode node);
        public abstract bool IsTypeDeclaration(SyntaxNode node);
        public abstract bool IsUsingAliasDirective([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsRegularComment(SyntaxTrivia trivia);
        public abstract bool IsDocumentationComment(SyntaxTrivia trivia);
        public abstract bool IsElastic(SyntaxTrivia trivia);
        public abstract bool IsPragmaDirective(SyntaxTrivia trivia, out bool isDisable, out bool isActive, out SeparatedSyntaxList<SyntaxNode> errorCodes);
        public abstract bool IsDocumentationComment(SyntaxNode node);
        public abstract string GetText(int kind);
        public abstract bool IsEntirelyWithinStringOrCharOrNumericLiteral([NotNullWhen(true)] SyntaxTree? syntaxTree, int position, CancellationToken cancellationToken);
        public abstract bool TryGetPredefinedType(SyntaxToken token, out PredefinedType type);
        public abstract bool TryGetPredefinedOperator(SyntaxToken token, out PredefinedOperator op);
        public abstract bool TryGetExternalSourceInfo([NotNullWhen(true)] SyntaxNode? directive, out ExternalSourceInfo info);
        public abstract bool IsDeclarationExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsIsExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsIsPatternExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsConversionExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsCastExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsExpressionOfForeach([NotNullWhen(true)] SyntaxNode? node);
        public abstract void GetPartsOfTupleExpression<TArgumentSyntax>(SyntaxNode node, out SyntaxToken openParen, out SeparatedSyntaxList<TArgumentSyntax> arguments, out SyntaxToken closeParen) where TArgumentSyntax : SyntaxNode;
        public abstract void GetPartsOfInterpolationExpression(SyntaxNode node, out SyntaxToken stringStartToken, out SyntaxList<SyntaxNode> contents, out SyntaxToken stringEndToken);
        public abstract bool IsVerbatimInterpolatedStringExpression(SyntaxNode node);
        public abstract bool IsLeftSideOfAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsSimpleAssignmentStatement([NotNullWhen(true)] SyntaxNode? statement);
        public abstract void GetPartsOfAssignmentStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfAssignmentExpressionOrStatement(SyntaxNode statement, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract bool IsLeftSideOfAnyAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfCompoundAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxNode GetRightHandSideOfAssignment(SyntaxNode node);
        public abstract bool IsInferredAnonymousObjectMemberDeclarator([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsOperandOfIncrementExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsOperandOfIncrementOrDecrementExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLeftSideOfDot([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxNode? GetRightSideOfDot(SyntaxNode? node);
        public abstract SyntaxNode? GetLeftSideOfDot(SyntaxNode? node, bool allowImplicitTarget = false);
        public abstract bool IsLeftSideOfExplicitInterfaceSpecifier([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfSimpleMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfAnyMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? GetStandaloneExpression(SyntaxNode? node);
        public abstract SyntaxNode? GetRootConditionalAccessExpression(SyntaxNode? node);
        public abstract SyntaxNode? GetExpressionOfMemberAccessExpression(SyntaxNode node, bool allowImplicitTarget = false);
        public abstract SyntaxNode? GetTargetOfMemberBinding(SyntaxNode? node);
        public abstract SyntaxNode GetNameOfMemberBindingExpression(SyntaxNode node);
        public abstract bool IsPointerMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNamedArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNameOfNamedArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxToken? GetNameOfParameter([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxNode? GetDefaultOfParameter(SyntaxNode node);
        public abstract SyntaxNode? GetParameterList(SyntaxNode node);
        public abstract bool IsParameterList([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDocumentationCommentExteriorTrivia(SyntaxTrivia trivia);
        public abstract void GetPartsOfElementAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode argumentList);
        public abstract SyntaxNode GetExpressionOfArgument(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfInterpolation(SyntaxNode node);
        public abstract SyntaxNode GetNameOfAttribute(SyntaxNode node);
        public abstract bool IsMemberBindingExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsPostfixUnaryExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxToken GetIdentifierOfGenericName(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfSimpleName(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfParameter(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfTypeDeclaration(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfVariableDeclarator(SyntaxNode node);
        public abstract SyntaxToken GetIdentifierOfIdentifierName(SyntaxNode node);
        public abstract SyntaxNode GetTypeOfVariableDeclarator(SyntaxNode node);
        public abstract bool IsSimpleArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsArgument([NotNullWhen(true)] SyntaxNode? node);
        public abstract RefKind GetRefKindOfArgument(SyntaxNode node);
        public abstract void GetNameAndArityOfSimpleName(SyntaxNode node, out string name, out int arity);
        public abstract bool LooksGeneric(SyntaxNode simpleName);
        public abstract SyntaxList<SyntaxNode> GetContentsOfInterpolatedString(SyntaxNode interpolatedString);
        public abstract SeparatedSyntaxList<SyntaxNode> GetArgumentsOfInvocationExpression(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetArgumentsOfObjectCreationExpression(SyntaxNode node);
        public abstract SeparatedSyntaxList<SyntaxNode> GetArgumentsOfArgumentList(SyntaxNode node);
        public abstract bool IsUsingDirectiveName([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsAttributeName(SyntaxNode node);
        public abstract bool IsAttributeNamedArgumentIdentifier([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberInitializerNamedAssignmentIdentifier([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberInitializerNamedAssignmentIdentifier([NotNullWhen(true)] SyntaxNode? node, [NotNullWhen(true)] out SyntaxNode? initializedInstance);
        public abstract bool IsDirective([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsStatement([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsExecutableStatement([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeconstructionAssignment([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeconstructionForEachStatement([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMethodBody([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeclaratorOfLocalDeclarationStatement(SyntaxNode declarator, SyntaxNode localDeclarationStatement);
        public abstract SeparatedSyntaxList<SyntaxNode> GetVariablesOfLocalDeclarationStatement(SyntaxNode node);
        public abstract SyntaxNode? GetInitializerOfVariableDeclarator(SyntaxNode node);
        public abstract bool IsThisConstructorInitializer(SyntaxToken token);
        public abstract bool IsBaseConstructorInitializer(SyntaxToken token);
        public abstract bool IsQueryKeyword(SyntaxToken token);
        public abstract bool IsElementAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsIndexerMemberCRef([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsIdentifierStartCharacter(char c);
        public abstract bool IsIdentifierPartCharacter(char c);
        public abstract bool IsIdentifierEscapeCharacter(char c);
        public abstract bool IsStartOfUnicodeEscapeSequence(char c);
        public abstract bool IsValidIdentifier(string identifier);
        public abstract bool IsVerbatimIdentifier(string identifier);
        public abstract bool IsTypeCharacter(char c);
        public abstract bool IsBindableToken(SyntaxToken token);
        public abstract bool IsInStaticContext(SyntaxNode node);
        public abstract bool IsUnsafeContext(SyntaxNode node);
        public abstract bool IsInNamespaceOrTypeContext([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBaseTypeList([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsInConstantContext([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsInConstructor(SyntaxNode node);
        public abstract bool IsMethodLevelMember([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsTopLevelNodeWithMembers([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsScopeBlock([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsExecutableBlock([NotNullWhen(true)] SyntaxNode? node);
        public abstract IReadOnlyList<SyntaxNode> GetExecutableBlockStatements(SyntaxNode? node);
        public abstract SyntaxNode? FindInnermostCommonExecutableBlock(IEnumerable<SyntaxNode> nodes);
        public abstract bool IsStatementContainer([NotNullWhen(true)] SyntaxNode? node);
        public abstract IReadOnlyList<SyntaxNode> GetStatementContainerStatements(SyntaxNode? node);
        public abstract bool AreEquivalent(SyntaxToken token1, SyntaxToken token2);
        public abstract bool AreEquivalent(SyntaxNode? node1, SyntaxNode? node2);
        public abstract string GetDisplayName(SyntaxNode? node, DisplayNameOptions options, string? rootNamespace = null);
        public abstract SyntaxNode? GetContainingTypeDeclaration(SyntaxNode? root, int position);
        public abstract SyntaxNode? GetContainingMemberDeclaration(SyntaxNode? root, int position, bool useFullSpan = true);
        public abstract SyntaxNode? GetContainingVariableDeclaratorOfFieldDeclaration(SyntaxNode? node);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? WalkDownParentheses(SyntaxNode? node);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? ConvertToSingleLine(SyntaxNode? node, bool useElasticTrivia = false);
        public abstract List<SyntaxNode> GetTopLevelAndMethodLevelMembers(SyntaxNode? root);
        public abstract List<SyntaxNode> GetMethodLevelMembers(SyntaxNode? root);
        public abstract SyntaxList<SyntaxNode> GetMembersOfTypeDeclaration(SyntaxNode typeDeclaration);
        public abstract bool ContainsInMemberBody([NotNullWhen(true)] SyntaxNode? node, TextSpan span);
        public abstract TextSpan GetInactiveRegionSpanAroundPosition(SyntaxTree tree, int position, CancellationToken cancellationToken);
        public abstract TextSpan GetMemberBodySpanForSpeculativeBinding(SyntaxNode node);
        public abstract SyntaxNode? TryGetBindableParent(SyntaxToken token);
        public abstract IEnumerable<SyntaxNode> GetConstructors(SyntaxNode? root, CancellationToken cancellationToken);
        public abstract bool TryGetCorrespondingOpenBrace(SyntaxToken token, out SyntaxToken openBrace);
        public abstract string GetNameForArgument(SyntaxNode? argument);
        public abstract string GetNameForAttributeArgument(SyntaxNode? argument);
        public abstract bool IsNameOfSubpattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsPropertyPatternClause(SyntaxNode node);
        public abstract bool IsAnyPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsAndPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBinaryPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsConstantPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsDeclarationPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsNotPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsOrPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsParenthesizedPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsRecursivePattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsTypePattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsUnaryPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsVarPattern([NotNullWhen(true)] SyntaxNode? node);
        public abstract SyntaxNode GetExpressionOfConstantPattern(SyntaxNode node);
        public abstract SyntaxNode GetTypeOfTypePattern(SyntaxNode node);
        public abstract void GetPartsOfParenthesizedPattern(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode pattern, out SyntaxToken closeParen);
        public abstract void GetPartsOfBinaryPattern(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfDeclarationPattern(SyntaxNode node, out SyntaxNode type, out SyntaxNode designation);
        public abstract void GetPartsOfRecursivePattern(SyntaxNode node, out SyntaxNode? type, out SyntaxNode? positionalPart, out SyntaxNode? propertyPart, out SyntaxNode? designation);
        public abstract void GetPartsOfUnaryPattern(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode pattern);
        public abstract SyntaxTokenList GetModifiers(SyntaxNode? node);
        [return: NotNullIfNotNull("node")]
        public abstract SyntaxNode? WithModifiers(SyntaxNode? node, SyntaxTokenList modifiers);
        public abstract Location GetDeconstructionReferenceLocation(SyntaxNode node);
        public abstract SyntaxToken? GetDeclarationIdentifierIfOverride(SyntaxToken token);
        public abstract bool IsAnonymousFunctionExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBaseNamespaceDeclaration([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsBinaryExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsLiteralExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsMemberAccessExpression([NotNullWhen(true)] SyntaxNode? node);
        public abstract bool IsSimpleName([NotNullWhen(true)] SyntaxNode? node);
        public abstract void GetPartsOfBaseNamespaceDeclaration(SyntaxNode node, out SyntaxNode name, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> members);
        public abstract void GetPartsOfBinaryExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken operatorToken, out SyntaxNode right);
        public abstract void GetPartsOfCastExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode expression);
        public abstract void GetPartsOfCompilationUnit(SyntaxNode node, out SyntaxList<SyntaxNode> imports, out SyntaxList<SyntaxNode> attributeLists, out SyntaxList<SyntaxNode> members);
        public abstract void GetPartsOfConditionalAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode whenNotNull);
        public abstract void GetPartsOfConditionalExpression(SyntaxNode node, out SyntaxNode condition, out SyntaxNode whenTrue, out SyntaxNode whenFalse);
        public abstract void GetPartsOfInvocationExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxNode? argumentList);
        public abstract void GetPartsOfIsPatternExpression(SyntaxNode node, out SyntaxNode left, out SyntaxToken isToken, out SyntaxNode right);
        public abstract void GetPartsOfMemberAccessExpression(SyntaxNode node, out SyntaxNode expression, out SyntaxToken operatorToken, out SyntaxNode name);
        public abstract void GetPartsOfObjectCreationExpression(SyntaxNode node, out SyntaxNode type, out SyntaxNode? argumentList, out SyntaxNode? initializer);
        public abstract void GetPartsOfParenthesizedExpression(SyntaxNode node, out SyntaxToken openParen, out SyntaxNode expression, out SyntaxToken closeParen);
        public abstract void GetPartsOfPrefixUnaryExpression(SyntaxNode node, out SyntaxToken operatorToken, out SyntaxNode operand);
        public abstract void GetPartsOfQualifiedName(SyntaxNode node, out SyntaxNode left, out SyntaxToken dotToken, out SyntaxNode right);
        public abstract void GetPartsOfUsingAliasDirective(SyntaxNode node, out SyntaxToken globalKeyword, out SyntaxToken alias, out SyntaxNode name);
        public abstract SyntaxNode GetExpressionOfAwaitExpression(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfExpressionStatement(SyntaxNode node);
        public abstract SyntaxNode? GetExpressionOfReturnStatement(SyntaxNode node);
        public abstract SyntaxNode GetExpressionOfThrowExpression(SyntaxNode node);
        public abstract SyntaxNode? GetValueOfEqualsValueClause(SyntaxNode? node);
    }
}
