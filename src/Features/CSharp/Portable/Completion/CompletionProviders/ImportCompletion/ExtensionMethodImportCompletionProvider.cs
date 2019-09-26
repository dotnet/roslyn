// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class ExtensionMethodImportCompletionProvider : AbstractExtensionMethodImportCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => ImportCompletionProviderHelper.IsInsertionTrigger(text, characterPosition, options);

        protected override ImmutableArray<string> GetImportedNamespaces(
            SyntaxNode location,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
            => ImportCompletionProviderHelper.GetImportedNamespaces(location, semanticModel, cancellationToken);

        protected override Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken)
            => ImportCompletionProviderHelper.CreateContextAsync(document, position, cancellationToken);

        protected override Task<bool> IsInImportsDirectiveAsync(Document document, int position, CancellationToken cancellationToken)
            => ImportCompletionProviderHelper.IsInImportsDirectiveAsync(document, position, cancellationToken);

        protected override bool TryGetReceiverTypeSymbol(SyntaxContext syntaxContext, out ITypeSymbol receiverTypeSymbol)
        {
            if (syntaxContext.TargetToken.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                var symbol = syntaxContext.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                switch (symbol?.Kind)
                {
                    case null:
                    case SymbolKind.Field:
                    case SymbolKind.Local:
                    case SymbolKind.Parameter:
                    case SymbolKind.Property:
                        receiverTypeSymbol = syntaxContext.SemanticModel.GetTypeInfo(memberAccess.Expression).Type;
                        return receiverTypeSymbol != null;
                }
            }

            receiverTypeSymbol = null;
            return false;
        }
    }
}
