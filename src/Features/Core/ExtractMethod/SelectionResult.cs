// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            this.Status = status;
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
            this.Status = status;

            this.OriginalSpan = originalSpan;
            this.FinalSpan = finalSpan;

            this.SelectionInExpression = selectionInExpression;
            this.Options = options;

            this.FirstTokenAnnotation = firstTokenAnnotation;
            this.LastTokenAnnotation = lastTokenAnnotation;

            this.SemanticDocument = document;
        }

        protected abstract bool UnderAsyncAnonymousMethod(SyntaxToken token, SyntaxToken firstToken, SyntaxToken lastToken);

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
            if (this.SemanticDocument == document)
            {
                return this;
            }

            var clone = (SelectionResult)this.MemberwiseClone();
            clone.SemanticDocument = document;

            return clone;
        }

        public bool ContainsValidContext
        {
            get
            {
                return this.SemanticDocument != null;
            }
        }

        public SyntaxToken GetFirstTokenInSelection()
        {
            return this.SemanticDocument.GetTokenWithAnnotation(this.FirstTokenAnnotation);
        }

        public SyntaxToken GetLastTokenInSelection()
        {
            return this.SemanticDocument.GetTokenWithAnnotation(this.LastTokenAnnotation);
        }

        public TNode GetContainingScopeOf<TNode>() where TNode : SyntaxNode
        {
            var containingScope = this.GetContainingScope();
            return containingScope.GetAncestorOrThis<TNode>();
        }

        protected T GetFirstStatement<T>() where T : SyntaxNode
        {
            Contract.ThrowIfTrue(this.SelectionInExpression);

            var token = this.GetFirstTokenInSelection();
            return token.GetAncestor<T>();
        }

        protected T GetLastStatement<T>() where T : SyntaxNode
        {
            Contract.ThrowIfTrue(this.SelectionInExpression);

            var token = this.GetLastTokenInSelection();
            return token.GetAncestor<T>();
        }

        public bool ShouldPutAsyncModifier()
        {
            var firstToken = this.GetFirstTokenInSelection();
            var lastToken = this.GetLastTokenInSelection();

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
                    && !UnderAsyncAnonymousMethod(currentToken, firstToken, lastToken))
                {
                    return true;
                }
            }

            return false;
        }

        public bool AllowMovingDeclaration
        {
            get
            {
                return this.Options.GetOption(ExtractMethodOptions.AllowMovingDeclaration, this.SemanticDocument.Project.Language);
            }
        }

        public bool DontPutOutOrRefOnStruct
        {
            get
            {
                return this.Options.GetOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, this.SemanticDocument.Project.Language);
            }
        }
    }
}
