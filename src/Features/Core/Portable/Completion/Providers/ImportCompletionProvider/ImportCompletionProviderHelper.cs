// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal static class ImportCompletionProviderHelper
    {
        public static ImmutableArray<string> GetImportedNamespaces(SyntaxContext context, CancellationToken cancellationToken)
        {
            var position = context.Position;

            var trivia = context.SyntaxTree.FindTriviaAndAdjustForEndOfFile(position, cancellationToken, findInsideTrivia: true);
            var triviaToken = trivia.Token;

            // If we are inside of leading trivia of a token adjust position to be the start of that token.
            // This is a workaround for an issue, when immediately after a `using` directive it is not included into the import scope.
            if (triviaToken.SpanStart > context.Position)
                position = triviaToken.SpanStart;

            var scopes = context.SemanticModel.GetImportScopes(position, cancellationToken);

            using var _ = ArrayBuilder<string>.GetInstance(out var usingsBuilder);

            foreach (var scope in scopes)
            {
                foreach (var import in scope.Imports)
                {
                    if (import.NamespaceOrType is INamespaceSymbol @namespace)
                    {
                        usingsBuilder.Add(GetNamespaceName(@namespace));
                    }
                }
            }

            return usingsBuilder.ToImmutable();

            static string GetNamespaceName(INamespaceSymbol symbol)
                => symbol.ToDisplayString(SymbolDisplayFormats.NameFormat);
        }
    }
}
