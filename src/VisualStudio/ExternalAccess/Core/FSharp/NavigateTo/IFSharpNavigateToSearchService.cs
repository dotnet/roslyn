// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.NavigateTo;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;
#endif

internal interface IFSharpNavigateToSearchService
{
    IImmutableSet<string> KindsProvided { get; }

    bool CanFilter { get; }

    Task<ImmutableArray<FSharpNavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
    Task<ImmutableArray<FSharpNavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
}
