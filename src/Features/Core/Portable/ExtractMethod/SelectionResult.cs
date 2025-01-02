// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class AbstractExtractMethodService<
    TStatementSyntax,
    TExecutableStatementSyntax,
    TExpressionSyntax>
{
    internal abstract class SelectionResult(
        SemanticDocument document,
        SelectionType selectionType,
        TextSpan finalSpan)
    {
        protected static readonly SyntaxAnnotation s_firstTokenAnnotation = new();
        protected static readonly SyntaxAnnotation s_lastTokenAnnotation = new();

        private bool? _createAsyncMethod;

        public SemanticDocument SemanticDocument { get; private set; } = document;
        public TextSpan FinalSpan { get; } = finalSpan;
        public SelectionType SelectionType { get; } = selectionType;

        /// <summary>
        /// Cached data flow analysis result for the selected code.  Valid for both expressions and statements.
        /// </summary>
        private DataFlowAnalysis? _dataFlowAnalysis;

        /// <summary>
        /// Cached information about the control flow of the selected code.  Only valid if the selection covers one or
        /// more statements.
        /// </summary>
        private ControlFlowAnalysis? _statementControlFlowAnalysis;

        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken);

        public abstract TExecutableStatementSyntax GetFirstStatementUnderContainer();
        public abstract TExecutableStatementSyntax GetLastStatementUnderContainer();

        public abstract bool ContainingScopeHasAsyncKeyword();

        public abstract SyntaxNode GetContainingScope();
        public abstract SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken);

        public abstract (ITypeSymbol? returnType, bool returnsByRef) GetReturnTypeInfo(CancellationToken cancellationToken);

        public abstract ImmutableArray<TExecutableStatementSyntax> GetOuterReturnStatements(SyntaxNode commonRoot, ImmutableArray<SyntaxNode> jumpsOutOfRegion);
        public abstract bool IsFinalSpanSemanticallyValidSpan(ImmutableArray<TExecutableStatementSyntax> returnStatements, CancellationToken cancellationToken);
        public abstract bool ContainsNonReturnExitPointsStatements(ImmutableArray<SyntaxNode> jumpsOutOfRegion);

        protected abstract OperationStatus ValidateLanguageSpecificRules(CancellationToken cancellationToken);

        public ITypeSymbol? GetReturnType(CancellationToken cancellationToken)
            => GetReturnTypeInfo(cancellationToken).returnType;

        public bool IsExtractMethodOnExpression => this.SelectionType == SelectionType.Expression;
        public bool IsExtractMethodOnSingleStatement => this.SelectionType == SelectionType.SingleStatement;
        public bool IsExtractMethodOnMultipleStatements => this.SelectionType == SelectionType.MultipleStatements;

        protected virtual SyntaxNode GetNodeForDataFlowAnalysis() => GetContainingScope();

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

        public DataFlowAnalysis GetDataFlowAnalysis()
        {
            return _dataFlowAnalysis ??= ComputeDataFlowAnalysis();

            DataFlowAnalysis ComputeDataFlowAnalysis()
            {
                var semanticModel = this.SemanticDocument.SemanticModel;
                if (this.IsExtractMethodOnExpression)
                    return semanticModel.AnalyzeDataFlow(this.GetNodeForDataFlowAnalysis());

                var (firstStatement, lastStatement) = this.GetFlowAnalysisNodeRange();
                return semanticModel.AnalyzeDataFlow(firstStatement, lastStatement);
            }
        }

        public ControlFlowAnalysis GetStatementControlFlowAnalysis()
        {
            Contract.ThrowIfTrue(IsExtractMethodOnExpression);
            return _statementControlFlowAnalysis ??= ComputeControlFlowAnalysis();

            ControlFlowAnalysis ComputeControlFlowAnalysis()
            {
                var (firstStatement, lastStatement) = this.GetFlowAnalysisNodeRange();
                return this.SemanticDocument.SemanticModel.AnalyzeControlFlow(firstStatement, lastStatement);
            }
        }

        public bool IsEndOfSelectionReachable()
            => this.IsExtractMethodOnExpression || GetStatementControlFlowAnalysis().EndPointIsReachable;

        /// <summary>f
        /// convert text span to node range for the flow analysis API
        /// </summary>
        private (TExecutableStatementSyntax firstStatement, TExecutableStatementSyntax lastStatement) GetFlowAnalysisNodeRange()
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

        public OperationStatus ValidateSelectionResult(CancellationToken cancellationToken)
        {
            if (!this.IsExtractMethodOnExpression)
            {
                if (!IsFinalSpanSemanticallyValidSpan(cancellationToken))
                    return new(succeeded: true, FeaturesResources.Not_all_code_paths_return);

                return ValidateLanguageSpecificRules(cancellationToken);
            }

            return OperationStatus.SucceededStatus;
        }

        protected bool IsFinalSpanSemanticallyValidSpan(CancellationToken cancellationToken)
        {
            var controlFlowAnalysisData = this.GetStatementControlFlowAnalysis();

            // there must be no control in and out of given span
            if (controlFlowAnalysisData.EntryPoints.Any())
                return false;

            // check something like continue, break, yield break, yield return, and etc
            if (ContainsNonReturnExitPointsStatements(controlFlowAnalysisData.ExitPoints))
                return false;

            // okay, there is no branch out, check whether next statement can be executed normally
            var (firstStatement, lastStatement) = this.GetFlowAnalysisNodeRange();
            var returnStatements = GetOuterReturnStatements(firstStatement.GetCommonRoot(lastStatement), controlFlowAnalysisData.ExitPoints);
            if (!returnStatements.Any())
                return true;

            // okay, only branch was return. make sure we have all return in the selection.

            // check for special case, if end point is not reachable, we don't care the selection
            // actually contains all return statements. we just let extract method go through
            // and work like we did in dev10
            if (!controlFlowAnalysisData.EndPointIsReachable)
                return true;

            // there is a return statement, and current position is reachable. let's check whether this is a case where that is okay
            return IsFinalSpanSemanticallyValidSpan(returnStatements, cancellationToken);
        }
    }
}
