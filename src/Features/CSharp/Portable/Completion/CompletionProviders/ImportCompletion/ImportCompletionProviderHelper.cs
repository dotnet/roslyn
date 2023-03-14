// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal static class ImportCompletionProviderHelper
    {
        public static ImmutableArray<string> GetImportedNamespaces(SyntaxContext context, CancellationToken cancellationToken)
        {
            var scopes = context.SemanticModel.GetImportScopes(context.Position, cancellationToken);

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
