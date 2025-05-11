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
using Microsoft.CodeAnalysis.PooledObjects;
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

        private bool? _containsAwaitExpression;
        private bool? _containsConfigureAwaitFalse;

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

        public abstract TExecutableStatementSyntax GetFirstStatementUnderContainer();
        public abstract TExecutableStatementSyntax GetLastStatementUnderContainer();

        public abstract bool ContainingScopeHasAsyncKeyword();

        public abstract SyntaxNode GetContainingScope();
        public abstract SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken);

        protected abstract (ITypeSymbol? returnType, bool returnsByRef) GetReturnTypeInfoWorker(CancellationToken cancellationToken);

        public abstract ImmutableArray<TExecutableStatementSyntax> GetOuterReturnStatements(SyntaxNode commonRoot, ImmutableArray<SyntaxNode> jumpsOutOfRegion);
        public abstract bool IsFinalSpanSemanticallyValidSpan(ImmutableArray<TExecutableStatementSyntax> returnStatements, CancellationToken cancellationToken);
        public abstract bool ContainsUnsupportedExitPointsStatements(ImmutableArray<SyntaxNode> exitPoints);

        protected abstract OperationStatus ValidateLanguageSpecificRules(CancellationToken cancellationToken);

        public (ITypeSymbol returnType, bool returnsByRef) GetReturnTypeInfo(CancellationToken cancellationToken)
        {
            var (returnType, returnsByRef) = GetReturnTypeInfoWorker(cancellationToken);
            return (returnType ?? this.SemanticDocument.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Object), returnsByRef);
        }

        public ITypeSymbol GetReturnType(CancellationToken cancellationToken)
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

        /// <summary>
        /// Checks all of the nodes within the user's selection to see if any of them satisfy the supplied <paramref
        /// name="predicate"/>. Will not descend into local functions or lambdas.
        /// </summary>
        /// <param name="predicate"></param>
        private bool CheckNodesInSelection(Func<ISyntaxFacts, SyntaxNode, bool> predicate)
        {
            var firstToken = this.GetFirstTokenInSelection();
            var lastToken = this.GetLastTokenInSelection();
            var span = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var stack);
            stack.Push(this.GetContainingScope());

            var syntaxFacts = this.SemanticDocument.GetRequiredLanguageService<ISyntaxFactsService>();

            while (stack.TryPop(out var current))
            {
                // Don't dive into lambdas and local functions.  They reset the async/await context.
                if (syntaxFacts.IsAnonymousOrLocalFunction(current))
                    continue;

                if (predicate(syntaxFacts, current))
                    return true;

                // Only dive into child nodes within the span being extracted.
                foreach (var childNode in current.ChildNodes())
                {
                    if (childNode.Span.OverlapsWith(span))
                        stack.Push(childNode);
                }
            }

            return false;
        }

        public bool ContainsAwaitExpression()
        {
            return _containsAwaitExpression ??= CheckNodesInSelection(
                static (syntaxFacts, node) => syntaxFacts.IsAwaitExpression(node));
        }

        public bool ContainsConfigureAwaitFalse()
        {
            return _containsConfigureAwaitFalse ??= CheckNodesInSelection(
                static (syntaxFacts, node) => IsConfigureAwaitFalse(syntaxFacts, node));

            static bool IsConfigureAwaitFalse(ISyntaxFacts syntaxFacts, SyntaxNode node)
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

        /// <summary>f
        /// convert text span to node range for the flow analysis API
        /// </summary>
        private (TExecutableStatementSyntax firstStatement, TExecutableStatementSyntax lastStatement) GetFlowAnalysisNodeRange()
        {
            if (this.IsExtractMethodOnSingleStatement)
            {
                var first = this.GetFirstStatement();
                return (first, first);
            }
            else
            {
                // multiple statement case
                return (this.GetFirstStatementUnderContainer(), this.GetLastStatementUnderContainer());
            }
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
            if (ContainsUnsupportedExitPointsStatements(controlFlowAnalysisData.ExitPoints))
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
