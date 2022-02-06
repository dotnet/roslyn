// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
    }
}
