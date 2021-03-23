// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal static class ImportCompletionProviderHelper
    {
        public static ImmutableArray<string> GetImportedNamespaces(
            SyntaxNode location,
            SemanticModel semanticModel)
            => semanticModel.GetUsingNamespacesInScope(location)
                .SelectAsArray(namespaceSymbol => namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat));

        public static async Task<SyntaxContext> CreateContextAsync(Document document, int position, bool usePartialSemantic, CancellationToken cancellationToken)
        {
            // Need regular semantic model because we will use it to get imported namespace symbols. Otherwise we will try to 
            // reach outside of the span and ended up with "node not within syntax tree" error from the speculative model.
            // Also we use partial model unless full model is explictly request (e.g. in tests) so that we don't have to wait for all semantics to be computed.
            var semanticModel = usePartialSemantic
                ? await document.GetPartialSemanticModelAsync(cancellationToken).ConfigureAwait(false)
                : await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(semanticModel);
            return CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);
        }
    }
}
