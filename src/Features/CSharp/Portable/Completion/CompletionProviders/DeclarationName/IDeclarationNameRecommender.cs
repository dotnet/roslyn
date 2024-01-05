// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName
{
    internal interface IDeclarationNameRecommender
    {
        Task<ImmutableArray<(string name, Glyph glyph)>> ProvideRecommendedNamesAsync(
            CompletionContext completionContext,
            Document document,
            CSharpSyntaxContext context,
            NameDeclarationInfo nameInfo,
            CancellationToken cancellationToken);
    }
}
