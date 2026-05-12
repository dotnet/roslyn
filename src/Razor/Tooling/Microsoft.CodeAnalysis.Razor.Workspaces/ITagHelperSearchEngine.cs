// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.GoToDefinition;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal interface ITagHelperSearchEngine
{
    Task<LspLocation[]?> TryLocateTagHelperDefinitionsAsync(ImmutableArray<BoundTagHelperResult> boundTagHelpers, IDocumentSnapshot documentSnapshot, ISolutionQueryOperations solutionQueryOperations, CancellationToken cancellationToken);
}
