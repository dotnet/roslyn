// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    /// <summary>
    /// clean up this code when we do selection validator work.
    /// </summary>
    internal abstract class SelectionResult
    {
        protected SelectionResult(OperationStatus status)
        {
            Contract.ThrowIfNull(status);

            Status = status;
        }

        protected SelectionResult(
            OperationStatus status,
            TextSpan originalSpan,
            TextSpan finalSpan,
            OptionSet options,
            bool selectionInExpression,
            SemanticDocument document,
            SyntaxAnnotation firstTokenAnnotation,
            SyntaxAnnotation lastTokenAnnotation)
        {
            Status = status;

            OriginalSpan = originalSpan;
            FinalSpan = finalSpan;

            SelectionInExpression = selectionInExpression;
            Options = options;

            FirstTokenAnnotation = firstTokenAnnotation;
            LastTokenAnnotation = lastTokenAnnotation;

            SemanticDocument = document;
        }

        protected abstract bool UnderAnonymousOrLocalMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken);

        public abstract bool ContainingScopeHasAsyncKeyword();

        public abstract SyntaxNode GetContainingScope();
        public abstract ITypeSymbol GetContainingScopeType();

        public OperationStatus Status { get; }
        public TextSpan OriginalSpan { get; }
        public TextSpan FinalSpan { get; }
        public OptionSet Options { get; }
        public bool SelectionInExpression { get; }
        public SemanticDocument SemanticDocument { get; private set; }
        public SyntaxAnnotation FirstTokenAnnotation { get; }
        public SyntaxAnnotation LastTokenAnnotation { get; }

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

        public bool ContainsValidContext
        {
            get
            {
                return SemanticDocument != null;
            }
        }

        public SyntaxToken GetFirstTokenInSelection()
        {
            return SemanticDocument.GetTokenWithAnnotation(FirstTokenAnnotation);
        }

        public SyntaxToken GetLastTokenInSelection()
        {
            return SemanticDocument.GetTokenWithAnnotation(LastTokenAnnotation);
        }

        public TNode GetContainingScopeOf<TNode>() where TNode : SyntaxNode
        {
            var containingScope = GetContainingScope();
            return containingScope.GetAncestorOrThis<TNode>();
        }

        protected T GetFirstStatement<T>() where T : SyntaxNode
        {
            Contract.ThrowIfTrue(SelectionInExpression);

            var token = GetFirstTokenInSelection();
            return token.GetAncestor<T>();
        }

        protected T GetLastStatement<T>() where T : SyntaxNode
        {
            Contract.ThrowIfTrue(SelectionInExpression);

            var token = GetLastTokenInSelection();
            return token.GetAncestor<T>();
        }

        public bool ShouldPutAsyncModifier()
        {
            var firstToken = GetFirstTokenInSelection();
            var lastToken = GetLastTokenInSelection();

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
                if (SemanticDocument.Project.LanguageServices.GetService<ISyntaxFactsService>().IsAwaitKeyword(currentToken)
                    && !UnderAnonymousOrLocalMethod(currentToken, firstToken, lastToken))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ShouldCallConfigureAwaitFalse()
        {
            var firstToken = GetFirstTokenInSelection();
            var lastToken = GetLastTokenInSelection();

            var span = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);

            foreach (var node in SemanticDocument.Root.DescendantNodesAndSelf())
            {
                if (!node.Span.OverlapsWith(span))
                {
                    continue;
                }

                if (IsConfigureAwaitFalse(node) && !UnderAnonymousOrLocalMethod(node.GetFirstToken(), firstToken, lastToken))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsConfigureAwaitFalse(SyntaxNode node)
        {
            var syntaxFacts = SemanticDocument.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (!syntaxFacts.IsInvocationExpression(node))
            {
                return false;
            }

            var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(node);
            if (!syntaxFacts.IsSimpleMemberAccessExpression(invokedExpression))
            {
                return false;
            }

            var name = syntaxFacts.GetNameOfMemberAccessExpression(invokedExpression);
            var identifier = syntaxFacts.GetIdentifierOfSimpleName(name);
            if (!syntaxFacts.StringComparer.Equals(identifier.ValueText, nameof(Task.ConfigureAwait)))
            {
                return false;
            }

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(node);
            if (arguments.Count != 1)
            {
                return false;
            }

            var argument = arguments[0];
            var expression = syntaxFacts.GetExpressionOfArgument(argument);
            return syntaxFacts.IsFalseLiteralExpression(expression);
        }

        public bool AllowMovingDeclaration
        {
            get
            {
                return Options.GetOption(ExtractMethodOptions.AllowMovingDeclaration, SemanticDocument.Project.Language);
            }
        }

        public bool DontPutOutOrRefOnStruct
        {
            get
            {
                return Options.GetOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, SemanticDocument.Project.Language);
            }
        }
    }
}
