// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal interface IRazorComponentSearchEngine
{
    Task<IDocumentSnapshot?> TryLocateComponentAsync(
        TagHelperDescriptor tagHelper,
        ISolutionQueryOperations solutionQueryOperations,
        CancellationToken cancellationToken);
}
