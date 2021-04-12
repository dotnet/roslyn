﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Simple data holder for local diagnostics for an analyzer
        /// </summary>
        private readonly struct DocumentAnalysisData
        {
            public static readonly DocumentAnalysisData Empty = new(VersionStamp.Default, ImmutableArray<DiagnosticData>.Empty);

            /// <summary>
            /// Version of the diagnostic data.
            /// </summary>
            public readonly VersionStamp Version;

            /// <summary>
            /// Current data that matches the version.
            /// </summary>
            public readonly ImmutableArray<DiagnosticData> Items;

            /// <summary>
            /// Last set of data we broadcasted to outer world, or <see langword="default"/>.
            /// </summary>
            public readonly ImmutableArray<DiagnosticData> OldItems;

            public DocumentAnalysisData(VersionStamp version, ImmutableArray<DiagnosticData> items)
            {
                Debug.Assert(!items.IsDefault);

                Version = version;
                Items = items;
                OldItems = default;
            }

            public DocumentAnalysisData(VersionStamp version, ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems)
                : this(version, newItems)
            {
                Debug.Assert(!oldItems.IsDefault);
                OldItems = oldItems;
            }

            public DocumentAnalysisData ToPersistData()
                => new(Version, Items);

            public bool FromCache
            {
                get { return OldItems.IsDefault; }
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
            /// Version of the Items
            /// </summary>
            public readonly VersionStamp Version;

            /// <summary>
            /// Current data that matches the version
            /// </summary>
            public readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> Result;

            /// <summary>
            /// When present, holds onto last data we broadcasted to outer world.
            /// </summary>
            public readonly ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? OldResult;

            public ProjectAnalysisData(ProjectId projectId, VersionStamp version, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> result)
            {
                ProjectId = projectId;
                Version = version;
                Result = result;

                OldResult = null;
            }

            public ProjectAnalysisData(
                ProjectId projectId,
                VersionStamp version,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> oldResult,
                ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> newResult)
                : this(projectId, version, newResult)
            {
                OldResult = oldResult;
            }

            public DiagnosticAnalysisResult GetResult(DiagnosticAnalyzer analyzer)
                => GetResultOrEmpty(Result, analyzer, ProjectId, Version);

            public static async Task<ProjectAnalysisData> CreateAsync(IPersistentStorageService persistentService, Project project, IEnumerable<StateSet> stateSets, bool avoidLoadingData, CancellationToken cancellationToken)
            {
                VersionStamp? version = null;

                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetOrCreateProjectState(project.Id);
                    var result = await state.GetAnalysisDataAsync(persistentService, project, avoidLoadingData, cancellationToken).ConfigureAwait(false);
                    Contract.ThrowIfFalse(project.Id == result.ProjectId);

                    if (!version.HasValue)
                    {
                        version = result.Version;
                    }
                    else if (version.Value != VersionStamp.Default && version.Value != result.Version)
                    {
                        // if not all version is same, set version as default.
                        // this can happen at the initial data loading or
                        // when document is closed and we put active file state to project state
                        version = VersionStamp.Default;
                    }

                    builder.Add(stateSet.Analyzer, result);
                }

                if (!version.HasValue)
                {
                    // there is no saved data to return.
                    return new ProjectAnalysisData(project.Id, VersionStamp.Default, ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty);
                }

                return new ProjectAnalysisData(project.Id, version.Value, builder.ToImmutable());
            }
        }
    }
}
