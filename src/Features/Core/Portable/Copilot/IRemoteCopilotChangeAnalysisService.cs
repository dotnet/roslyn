// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>Remote version of <see cref="ICopilotChangeAnalysisService"/></summary>
internal interface IRemoteCopilotChangeAnalysisService : IWorkspaceService
{
    /// <inheritdoc cref="ICopilotChangeAnalysisService.AnalyzeChangeAsync"/>
    ValueTask<CopilotChangeAnalysis> AnalyzeChangeAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken);
}
