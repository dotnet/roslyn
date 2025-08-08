// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SyncNamespaces;

internal interface ISyncNamespacesService : ILanguageService
{
    /// <summary>
    /// This will update documents in the specified projects so that their namespace matches the RootNamespace
    /// and their relative folder path.
    /// </summary>
    Task<Solution> SyncNamespacesAsync(
        ImmutableArray<Project> projects, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken);
}
