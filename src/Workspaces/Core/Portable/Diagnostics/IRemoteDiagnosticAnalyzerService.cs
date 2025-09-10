// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal interface IRemoteDiagnosticAnalyzerService
{
    ValueTask<ImmutableArray<DiagnosticData>> ForceRunCodeAnalysisDiagnosticsAsync(
        Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken);

    ValueTask<bool> IsAnyDiagnosticIdDeprioritizedAsync(
        Checksum solutionChecksum, ProjectId projectId, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        TextSpan? range,
        DiagnosticIdFilter diagnosticIdFilter,
        CodeActionRequestPriority? priority,
        DiagnosticKind diagnosticKind,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Checksum solutionChecksum, ProjectId projectId,
        ImmutableArray<DocumentId> documentIds,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        bool includeLocalDocumentDiagnostics,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Checksum solutionChecksum, ProjectId projectId,
        ImmutableHashSet<string>? diagnosticIds,
        AnalyzerFilter analyzerFilter,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<DiagnosticData>> GetSourceGeneratorDiagnosticsAsync(Checksum solutionChecksum, ProjectId projectId, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<DiagnosticDescriptorData>> GetDiagnosticDescriptorsAsync(
        Checksum solutionChecksum, ProjectId projectId, string analyzerReferenceFullPath, string language, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<string>> GetCompilationEndDiagnosticDescriptorIdsAsync(
        Checksum solutionChecksum, CancellationToken cancellationToken);

    ValueTask<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptorData>>> GetDiagnosticDescriptorsPerReferenceAsync(
        Checksum solutionChecksum, ProjectId? projectId, CancellationToken cancellationToken);
}

[DataContract]
internal readonly struct AnalyzerPerformanceInfo(string analyzerId, bool builtIn, TimeSpan timeSpan)
{
    [DataMember(Order = 0)]
    public readonly string AnalyzerId = analyzerId;

    [DataMember(Order = 1)]
    public readonly bool BuiltIn = builtIn;

    [DataMember(Order = 2)]
    public readonly TimeSpan TimeSpan = timeSpan;
}
