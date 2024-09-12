// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

/// <summary>
/// clean up this code when we do selection validator work.
/// </summary>
internal abstract class SelectionResult<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
{
    protected SelectionResult(
        TextSpan originalSpan,
        TextSpan finalSpan,
        ExtractMethodOptions options,
        bool selectionInExpression,
        SemanticDocument document,
        SyntaxAnnotation firstTokenAnnotation,
        SyntaxAnnotation lastTokenAnnotation,
        bool selectionChanged)
    {
        OriginalSpan = originalSpan;
        FinalSpan = finalSpan;

        SelectionInExpression = selectionInExpression;
        Options = options;

        FirstTokenAnnotation = firstTokenAnnotation;
        LastTokenAnnotation = lastTokenAnnotation;

        SemanticDocument = document;
        SelectionChanged = selectionChanged;
    }

    protected abstract ISyntaxFacts SyntaxFacts { get; }
    protected abstract bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken);

    public abstract TStatementSyntax GetFirstStatementUnderContainer();
    public abstract TStatementSyntax GetLastStatementUnderContainer();

    public abstract bool ContainingScopeHasAsyncKeyword();

    public abstract SyntaxNode GetContainingScope();
    public abstract SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken);

    public abstract (ITypeSymbol returnType, bool returnsByRef) GetReturnType();

    public ITypeSymbol GetContainingScopeType()
    {
        var (typeSymbol, _) = GetReturnType();
        return typeSymbol;
    }

    public virtual SyntaxNode GetNodeForDataFlowAnalysis() => GetContainingScope();

    public TextSpan OriginalSpan { get; }
    public TextSpan FinalSpan { get; }
    public ExtractMethodOptions Options { get; }
    public bool SelectionInExpression { get; }
    public SemanticDocument SemanticDocument { get; private set; }
    public SyntaxAnnotation FirstTokenAnnotation { get; }
    public SyntaxAnnotation LastTokenAnnotation { get; }
    public bool SelectionChanged { get; }

    public SelectionResult<TStatementSyntax> With(SemanticDocument document)
    {
        if (SemanticDocument == document)
        {
            return this;
        }

        var clone = (SelectionResult<TStatementSyntax>)MemberwiseClone();
        clone.SemanticDocument = document;

        return clone;
    }

    public SyntaxToken GetFirstTokenInSelection()
        => SemanticDocument.GetTokenWithAnnotation(FirstTokenAnnotation);

    public SyntaxToken GetLastTokenInSelection()
        => SemanticDocument.GetTokenWithAnnotation(LastTokenAnnotation);

    public TNode GetContainingScopeOf<TNode>() where TNode : SyntaxNode
    {
        var containingScope = GetContainingScope();
        return containingScope.GetAncestorOrThis<TNode>();
    }

    public bool IsExtractMethodOnSingleStatement()
    {
        var firstStatement = this.GetFirstStatement();
        var lastStatement = this.GetLastStatement();

        return firstStatement == lastStatement || firstStatement.Span.Contains(lastStatement.Span);
    }

    public bool IsExtractMethodOnMultipleStatements()
    {
        var first = this.GetFirstStatement();
        var last = this.GetLastStatement();

        if (first != last)
        {
            var firstUnderContainer = this.GetFirstStatementUnderContainer();
            var lastUnderContainer = this.GetLastStatementUnderContainer();
            Contract.ThrowIfFalse(this.SyntaxFacts.AreStatementsInSameContainer(firstUnderContainer, lastUnderContainer));
            return true;
        }

        return false;
    }

    public TStatementSyntax GetFirstStatement()
    {
        Contract.ThrowIfTrue(SelectionInExpression);

        var token = GetFirstTokenInSelection();
        return token.GetAncestor<TStatementSyntax>();
    }

    public TStatementSyntax GetLastStatement()
    {
        Contract.ThrowIfTrue(SelectionInExpression);

        var token = GetLastTokenInSelection();
        return token.GetAncestor<TStatementSyntax>();
    }

    public bool ShouldPutAsyncModifier()
    {
        var firstToken = GetFirstTokenInSelection();
        var lastToken = GetLastTokenInSelection();
        var syntaxFacts = SemanticDocument.Project.Services.GetService<ISyntaxFactsService>();

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

    public bool ShouldCallConfigureAwaitFalse()
    {
        var syntaxFacts = SemanticDocument.Project.Services.GetService<ISyntaxFactsService>();

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
