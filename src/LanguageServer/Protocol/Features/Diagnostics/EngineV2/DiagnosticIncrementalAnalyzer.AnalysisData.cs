// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2;

internal partial class DiagnosticIncrementalAnalyzer
{
    /// <summary>
    /// Simple data holder for local diagnostics for an analyzer
    /// </summary>
    private readonly struct DocumentAnalysisData
    {
        public static readonly DocumentAnalysisData Empty = new(VersionStamp.Default, lineCount: 0, []);

        /// <summary>
        /// Version of the diagnostic data.
        /// </summary>
        public readonly VersionStamp Version;

        /// <summary>
        /// Number of lines in the document.
        /// </summary>
        public readonly int LineCount;

        /// <summary>
        /// Current data that matches the version.
        /// </summary>
        public readonly ImmutableArray<DiagnosticData> Items;

        /// <summary>
        /// Last set of data we broadcasted to outer world, or <see langword="default"/>.
        /// </summary>
        public readonly ImmutableArray<DiagnosticData> OldItems;

        public DocumentAnalysisData(Checksum checksum, int lineCount, ImmutableArray<DiagnosticData> items)
        {
            Debug.Assert(!items.IsDefault);

            Version = version;
            LineCount = lineCount;
            Items = items;
            OldItems = default;
        }

        public DocumentAnalysisData(VersionStamp version, int lineCount, ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems)
            : this(version, lineCount, newItems)
        {
            Debug.Assert(!oldItems.IsDefault);
            OldItems = oldItems;
        }
    }

    /// <summary>
    /// Data holder for all diagnostics for a project for an analyzer
    /// </summary>
    private readonly struct ProjectAnalysisData
    {
        /// <summary>
        /// ProjectId of this data
        /// </summary>
        public readonly ProjectId ProjectId;

        /// <summary>
        /// Checksum of the project diagnostics were computed for.
        /// </summary>
        public readonly Checksum Checksum;

        /// <summary>
        /// Current data that matches the version
        /// </summary>
        public readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> Result;

        public ProjectAnalysisData(ProjectId projectId, Checksum checksum, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
        {
            ProjectId = projectId;
            Checksum = checksum;
            Result = result;
        }

        public DiagnosticAnalysisResult GetResult(DiagnosticAnalyzer analyzer)
            => GetResultOrEmpty(Result, analyzer, ProjectId, Checksum);

        public bool TryGetResult(DiagnosticAnalyzer analyzer, out DiagnosticAnalysisResult result)
            => Result.TryGetValue(analyzer, out result);

        public static async Task<ProjectAnalysisData> CreateAsync(Project project, ImmutableArray<StateSet> stateSets, bool avoidLoadingData, CancellationToken cancellationToken)
        {
            Checksum? checksum = null;

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
            foreach (var stateSet in stateSets)
            {
                var state = stateSet.GetOrCreateProjectState(project.Id);
                var result = await state.GetAnalysisDataAsync(project, avoidLoadingData, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(project.Id == result.ProjectId);

                if (!checksum.HasValue)
                {
                    checksum = result.Checksum;
                }
                else if (checksum.Value != default && checksum.Value != result.Checksum)
                {
                    // if not all version is same, set version as default.
                    // this can happen at the initial data loading or
                    // when document is closed and we put active file state to project state
                    checksum = default(Checksum);
                }

                builder.Add(stateSet.Analyzer, result);
            }

            if (!checksum.HasValue)
            {
                // there is no saved data to return.
                return new ProjectAnalysisData(project.Id, checksum: default, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty);
            }

            return new ProjectAnalysisData(project.Id, checksum.Value, builder.ToImmutable());
        }
    }
}
