// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal static class ImportCompletionProviderHelper
    {
        public static async Task<ImmutableArray<string>> GetImportedNamespacesAsync(SyntaxContext context, CancellationToken token)
        {
            // The location is the containing node of the LeftToken, or the compilation unit itself if LeftToken
            // indicates the beginning of the document (i.e. no parent).
            var location = context.LeftToken.Parent ?? context.SyntaxTree.GetRoot(token);
            var usings = context.SemanticModel.GetUsingNamespacesInScope(location);

            // Since we don't have a compiler API to easily get all global usings yet,
            // hardcode the the name of SDK auto-generated global using file for now 
            // as a temporary workaround.
            var fileName = context.Document.Project.Name + ".GlobalUsings.g.cs";
            var globalUsingDocument = context.Document.Project.Documents.FirstOrDefault(d => d.Name.Equals(fileName));
            if (globalUsingDocument != null)
            {
                var root = await globalUsingDocument.GetRequiredSyntaxRootAsync(token).ConfigureAwait(false);
                var model = await globalUsingDocument.GetRequiredSemanticModelAsync(token).ConfigureAwait(false);
                var globalUsings = model.GetUsingNamespacesInScope(root);
                usings.UnionWith(globalUsings);
            }

            return usings.SelectAsArray(namespaceSymbol => namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat));
        }
    }
}
