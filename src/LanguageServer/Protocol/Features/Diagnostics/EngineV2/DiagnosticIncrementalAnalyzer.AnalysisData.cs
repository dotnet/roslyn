// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Data holder for all diagnostics for a project for an analyzer
        /// </summary>
        private readonly struct ProjectAnalysisData(
            ProjectId projectId,
            VersionStamp version,
            ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
        {
            /// <summary>
            /// ProjectId of this data
            /// </summary>
            public readonly ProjectId ProjectId = projectId;

            /// <summary>
            /// Version of the Items
            /// </summary>
            public readonly VersionStamp Version = version;

            /// <summary>
            /// Current data that matches the version
            /// </summary>
            public readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> Result = result;

            public DiagnosticAnalysisResult GetResult(DiagnosticAnalyzer analyzer)
                => GetResultOrEmpty(Result, analyzer, ProjectId, Version);

            public bool TryGetResult(DiagnosticAnalyzer analyzer, out DiagnosticAnalysisResult result)
                => Result.TryGetValue(analyzer, out result);
        }
    }
}
