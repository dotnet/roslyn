// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
{
    internal abstract class AbstractSyntaxContext
    {
        private ISet<INamedTypeSymbol> _outerTypes;

        protected AbstractSyntaxContext(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            SyntaxToken leftToken,
            SyntaxToken targetToken,
            bool isTypeContext,
            bool isNamespaceContext,
            bool isPreProcessorDirectiveContext,
            bool isRightOfNameSeparator,
            bool isStatementContext,
            bool isAnyExpressionContext,
            bool isAttributeNameContext,
            bool isEnumTypeMemberAccessContext,
            bool isNameOfContext,
            bool isInQuery,
            bool isInImportsDirective)
        {
            this.Workspace = workspace;
            this.SemanticModel = semanticModel;
            this.SyntaxTree = semanticModel.SyntaxTree;
            this.Position = position;
            this.LeftToken = leftToken;
            this.TargetToken = targetToken;
            this.IsTypeContext = isTypeContext;
            this.IsNamespaceContext = isNamespaceContext;
            this.IsPreProcessorDirectiveContext = isPreProcessorDirectiveContext;
            this.IsRightOfNameSeparator = isRightOfNameSeparator;
            this.IsStatementContext = isStatementContext;
            this.IsAnyExpressionContext = isAnyExpressionContext;
            this.IsAttributeNameContext = isAttributeNameContext;
            this.IsEnumTypeMemberAccessContext = isEnumTypeMemberAccessContext;
            this.IsNameOfContext = isNameOfContext;
            this.IsInQuery = isInQuery;
            this.IsInImportsDirective = isInImportsDirective;
        }

        public Workspace Workspace { get; }
        public SemanticModel SemanticModel { get; }
        public SyntaxTree SyntaxTree { get; }
        public int Position { get; }

        public SyntaxToken LeftToken { get; }
        public SyntaxToken TargetToken { get; }

        public bool IsTypeContext { get; }
        public bool IsNamespaceContext { get; }

        public bool IsPreProcessorDirectiveContext { get; }

        public bool IsRightOfNameSeparator { get; }
        public bool IsStatementContext { get; }
        public bool IsAnyExpressionContext { get; }
        public bool IsAttributeNameContext { get; }
        public bool IsEnumTypeMemberAccessContext { get; }
        public bool IsNameOfContext { get; }

        public bool IsInQuery { get; }
        public bool IsInImportsDirective { get; }

        private ISet<INamedTypeSymbol> ComputeOuterTypes(CancellationToken cancellationToken)
        {
            var enclosingSymbol = this.SemanticModel.GetEnclosingSymbol(this.LeftToken.SpanStart, cancellationToken);
            if (enclosingSymbol != null)
            {
                var containingType = enclosingSymbol.GetContainingTypeOrThis();
                if (containingType != null)
                {
                    return containingType.GetContainingTypes().ToSet();
                }
            }

            return SpecializedCollections.EmptySet<INamedTypeSymbol>();
        }

        public ISet<INamedTypeSymbol> GetOuterTypes(CancellationToken cancellationToken)
        {
            if (_outerTypes == null)
            {
                Interlocked.CompareExchange(ref _outerTypes, ComputeOuterTypes(cancellationToken), null);
            }

            return _outerTypes;
        }

        public TService GetLanguageService<TService>() where TService : class, ILanguageService
        {
            return this.Workspace.Services.GetLanguageServices(this.SemanticModel.Language).GetService<TService>();
        }

        public TService GetWorkspaceService<TService>() where TService : class, IWorkspaceService
        {
            return this.Workspace.Services.GetService<TService>();
        }
    }
}
