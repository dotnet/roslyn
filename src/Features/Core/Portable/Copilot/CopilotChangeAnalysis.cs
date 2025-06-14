// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Copilot;

/// <param name="TotalAnalysisTime">Total time to do all analysis (including diagnostics, code fixes, and application).</param>
/// <param name="TotalDiagnosticComputationTime">Total time to do all diagnostic computation over all diagnostic kinds.</param>
[DataContract]
internal readonly record struct CopilotChangeAnalysis(
    [property: DataMember(Order = 0)] bool Succeeded,
    [property: DataMember(Order = 1)] int OldDocumentLength,
    [property: DataMember(Order = 2)] int NewDocumentLength,
    [property: DataMember(Order = 3)] int TextChangeDelta,
    [property: DataMember(Order = 4)] int ProjectDocumentCount,
    [property: DataMember(Order = 5)] int ProjectSourceGeneratedDocumentCount,
    [property: DataMember(Order = 6)] int ProjectConeCount,
    [property: DataMember(Order = 7)] TimeSpan TotalAnalysisTime,
    [property: DataMember(Order = 8)] TimeSpan ForkingTime,
    [property: DataMember(Order = 9)] TimeSpan TotalDiagnosticComputationTime,
    [property: DataMember(Order = 10)] ImmutableArray<CopilotDiagnosticAnalysis> DiagnosticAnalyses,
    [property: DataMember(Order = 11)] CopilotCodeFixAnalysis CodeFixAnalysis);

/// <param name="Kind">What diagnostic kind this is analysis data for.</param>
/// <param name="ComputationTime">How long it took to produce the diagnostics for this diagnostic kind.</param>
/// <param name="IdToCount">Mapping from <see cref="Diagnostic.Id"/> to the number of diagnostics produced for that id.</param>
/// <param name="CategoryToCount">Mapping from <see cref="Diagnostic.Category"/> to the number of diagnostics produced for that category.</param>
/// <param name="SeverityToCount">Mapping from <see cref="Diagnostic.Severity"/> to the number of diagnostics produced for that severity.</param>
[DataContract]
internal readonly record struct CopilotDiagnosticAnalysis(
    [property: DataMember(Order = 0)] DiagnosticKind Kind,
    [property: DataMember(Order = 1)] TimeSpan ComputationTime,
    [property: DataMember(Order = 2)] Dictionary<string, int> IdToCount,
    [property: DataMember(Order = 3)] Dictionary<string, int> CategoryToCount,
    [property: DataMember(Order = 4)] Dictionary<DiagnosticSeverity, int> SeverityToCount);

/// <param name="TotalComputationTime">Total time to compute code fixes for the changed regions.</param>
/// <param name="TotalApplicationTime">Total time to apply code fixes for the changed regions.</param>
/// <param name="DiagnosticIdToCount">Mapping from diagnostic id to to how many diagnostics with that id had fixes.</param>
/// <param name="DiagnosticIdToApplicationTime">Mapping from diagnostic id to the total time taken to fix diagnostics with that id.</param>
/// <param name="DiagnosticIdToProviderName">Mapping from diagnostic id to the name of the provider that provided the fix.</param>
/// <param name="ProviderNameToApplicationTime">Mapping from provider name to the total time taken to fix diagnostics with that provider.</param>
/// <param name="ProviderNameToHasConflict">Mapping from provider name to whether or not that provider conflicted with another provider in producing a fix.</param>
[DataContract]
internal readonly record struct CopilotCodeFixAnalysis(
    [property: DataMember(Order = 0)] TimeSpan TotalComputationTime,
    [property: DataMember(Order = 1)] TimeSpan TotalApplicationTime,
    [property: DataMember(Order = 2)] Dictionary<string, int> DiagnosticIdToCount,
    [property: DataMember(Order = 3)] Dictionary<string, TimeSpan> DiagnosticIdToApplicationTime,
    [property: DataMember(Order = 4)] Dictionary<string, HashSet<string>> DiagnosticIdToProviderName,
    [property: DataMember(Order = 5)] Dictionary<string, TimeSpan> ProviderNameToApplicationTime,
    [property: DataMember(Order = 6)] Dictionary<string, bool> ProviderNameToHasConflict,
    [property: DataMember(Order = 7)] Dictionary<string, int> ProviderNameToTotalCount,
    [property: DataMember(Order = 8)] Dictionary<string, int> ProviderNameToSuccessCount);
