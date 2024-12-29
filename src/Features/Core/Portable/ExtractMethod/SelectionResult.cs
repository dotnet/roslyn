// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TValidator,
    TExtractor,
    TSelectionResult,
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract class SelectionResult(
        SemanticDocument document,
        SelectionType selectionType,
        TextSpan originalSpan,
        TextSpan finalSpan,
        bool selectionChanged)
    {
        protected static readonly SyntaxAnnotation s_firstTokenAnnotation = new();
        protected static readonly SyntaxAnnotation s_lastTokenAnnotation = new();

        private bool? _createAsyncMethod;

        public SemanticDocument SemanticDocument { get; private set; } = document;
        public TextSpan OriginalSpan { get; } = originalSpan;
        public TextSpan FinalSpan { get; } = finalSpan;
        public SelectionType SelectionType { get; } = selectionType;
        public bool SelectionChanged { get; } = selectionChanged;

        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken);

        public abstract TExecutableStatementSyntax GetFirstStatementUnderContainer();
        public abstract TExecutableStatementSyntax GetLastStatementUnderContainer();

        public abstract bool ContainingScopeHasAsyncKeyword();

        public abstract SyntaxNode GetContainingScope();
        public abstract SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken);

        public abstract (ITypeSymbol? returnType, bool returnsByRef) GetReturnType();

        public ITypeSymbol? GetContainingScopeType()
        {
            var (typeSymbol, _) = GetReturnType();
            return typeSymbol;
        }

        public bool IsExtractMethodOnExpression => this.SelectionType == SelectionType.Expression;
        public bool IsExtractMethodOnSingleStatement => this.SelectionType == SelectionType.SingleStatement;
        public bool IsExtractMethodOnMultipleStatements => this.SelectionType == SelectionType.MultipleStatements;

        public virtual SyntaxNode GetNodeForDataFlowAnalysis() => GetContainingScope();

        public SelectionResult With(SemanticDocument document)
        {
            if (SemanticDocument == document)
            {
                return this;
            }

            var clone = (SelectionResult)MemberwiseClone();
            clone.SemanticDocument = document;

            return clone;
        }

        public SyntaxToken GetFirstTokenInSelection()
            => SemanticDocument.GetTokenWithAnnotation(s_firstTokenAnnotation);

        public SyntaxToken GetLastTokenInSelection()
            => SemanticDocument.GetTokenWithAnnotation(s_lastTokenAnnotation);

        public TNode? GetContainingScopeOf<TNode>() where TNode : SyntaxNode
        {
            var containingScope = GetContainingScope();
            return containingScope.GetAncestorOrThis<TNode>();
        }

        public TExecutableStatementSyntax GetFirstStatement()
        {
            Contract.ThrowIfTrue(IsExtractMethodOnExpression);

            var token = GetFirstTokenInSelection();
            return token.GetRequiredAncestor<TExecutableStatementSyntax>();
        }

        public TExecutableStatementSyntax GetLastStatement()
        {
            Contract.ThrowIfTrue(IsExtractMethodOnExpression);

            var token = GetLastTokenInSelection();
            return token.GetRequiredAncestor<TExecutableStatementSyntax>();
        }

        public bool CreateAsyncMethod()
        {
            _createAsyncMethod ??= CreateAsyncMethodWorker();
            return _createAsyncMethod.Value;

            bool CreateAsyncMethodWorker()
            {
                var firstToken = GetFirstTokenInSelection();
                var lastToken = GetLastTokenInSelection();
                var syntaxFacts = SemanticDocument.GetRequiredLanguageService<ISyntaxFactsService>();

                for (var currentToken = firstToken;
                    currentToken.Span.End < lastToken.SpanStart;
                    currentToken = currentToken.GetNextToken())
                {
                    // [|
                    //     async () => await ....
                    // |]
                    //
                    // for the case above, even if the selection contains "await", it doesn't belong to the enclosing block
                    // which extract method is applied to
                    if (syntaxFacts.IsAwaitKeyword(currentToken)
                        && !UnderAnonymousOrLocalMethod(currentToken, firstToken, lastToken))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool ShouldCallConfigureAwaitFalse()
        {
            var syntaxFacts = SemanticDocument.GetRequiredLanguageService<ISyntaxFactsService>();

            var firstToken = GetFirstTokenInSelection();
            var lastToken = GetLastTokenInSelection();

            var span = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);

            foreach (var node in SemanticDocument.Root.DescendantNodesAndSelf())
            {
                if (!node.Span.OverlapsWith(span))
                    continue;

                if (IsConfigureAwaitFalse(node) && !UnderAnonymousOrLocalMethod(node.GetFirstToken(), firstToken, lastToken))
                    return true;
            }

            return false;

            bool IsConfigureAwaitFalse(SyntaxNode node)
            {
                if (!syntaxFacts.IsInvocationExpression(node))
                    return false;

                var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(node);
                if (!syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
                    return false;

                var name = syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression);
                var identifier = syntaxFacts.GetIdentifierOfSimpleName(name);
                if (!syntaxFacts.StringComparer.Equals(identifier.ValueText, nameof(Task.ConfigureAwait)))
                    return false;

                var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(node);
                if (arguments.Count != 1)
                    return false;

                var expression = syntaxFacts.GetExpressionOfArgument(arguments[0]);
                return syntaxFacts.IsFalseLiteralExpression(expression);
            }
        }

        /// <summary>f
        /// convert text span to node range for the flow analysis API
        /// </summary>
        public (TExecutableStatementSyntax firstStatement, TExecutableStatementSyntax lastStatement) GetFlowAnalysisNodeRange()
        {
            var first = this.GetFirstStatement();
            var last = this.GetLastStatement();

            // single statement case
            if (first == last ||
                first.Span.Contains(last.Span))
            {
                return (first, first);
            }

            // multiple statement case
            var firstUnderContainer = this.GetFirstStatementUnderContainer();
            var lastUnderContainer = this.GetLastStatementUnderContainer();
            return (firstUnderContainer, lastUnderContainer);
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the tokens
        /// 
        /// tokens should belong to the given root
        /// </summary>
        protected static SyntaxNode AddAnnotations(SyntaxNode root, IEnumerable<(SyntaxToken, SyntaxAnnotation)> pairs)
        {
            Contract.ThrowIfNull(root);

            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceTokens(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }

        /// <summary>
        /// create a new root node from the given root after adding annotations to the nodes
        /// 
        /// nodes should belong to the given root
        /// </summary>
        protected static SyntaxNode AddAnnotations(SyntaxNode root, IEnumerable<(SyntaxNode, SyntaxAnnotation)> pairs)
        {
            Contract.ThrowIfNull(root);

            var tokenMap = pairs.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());
            return root.ReplaceNodes(tokenMap.Keys, (o, n) => o.WithAdditionalAnnotations(tokenMap[o]));
        }
    }
}
