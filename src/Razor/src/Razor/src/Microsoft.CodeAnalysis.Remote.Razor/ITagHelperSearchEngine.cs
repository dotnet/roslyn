// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal interface ITagHelperSearchEngine
{
    Task<LspLocation[]?> TryLocateTagHelperDefinitionsAsync(ImmutableArray<BoundTagHelperResult> boundTagHelpers, RemoteDocumentSnapshot documentSnapshot, RemoteSolutionSnapshot solutionSnapshot, CancellationToken cancellationToken);
}

internal sealed record BoundTagHelperResult(TagHelperDescriptor ElementDescriptor, BoundAttributeDescriptor? AttributeDescriptor);
