// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// 
        /// </summary>
        private struct DocumentAnalysisData
        {
            public static readonly DocumentAnalysisData Empty = new DocumentAnalysisData(VersionStamp.Default, ImmutableArray<DiagnosticData>.Empty);

            public readonly VersionStamp Version;
            public readonly ImmutableArray<DiagnosticData> OldItems;
            public readonly ImmutableArray<DiagnosticData> Items;

            public DocumentAnalysisData(VersionStamp version, ImmutableArray<DiagnosticData> items)
            {
                this.Version = version;
                this.Items = items;
            }

            public DocumentAnalysisData(VersionStamp version, ImmutableArray<DiagnosticData> oldItems, ImmutableArray<DiagnosticData> newItems) :
                this(version, newItems)
            {
                this.OldItems = oldItems;
            }

            public DocumentAnalysisData ToPersistData()
            {
                return new DocumentAnalysisData(Version, Items);
            }

            public bool FromCache
            {
                get { return this.OldItems.IsDefault; }
            }
        }

        private struct ProjectAnalysisData
        {
            public static readonly ProjectAnalysisData Empty = new ProjectAnalysisData(
                VersionStamp.Default, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult>.Empty);

            public readonly VersionStamp Version;
            public readonly ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> OldResult;
            public readonly ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> Result;


            public ProjectAnalysisData(VersionStamp version, ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> result)
            {
                this.Version = version;
                this.Result = result;

                this.OldResult = null;
            }

            public ProjectAnalysisData(
                VersionStamp version,
                ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> oldResult,
                ImmutableDictionary<DiagnosticAnalyzer, AnalysisResult> newResult) :
                this(version, newResult)
            {
                this.OldResult = oldResult;
            }

            public AnalysisResult GetResult(DiagnosticAnalyzer analyzer)
            {
                return IDictionaryExtensions.GetValueOrDefault(Result, analyzer);
            }

            public bool FromCache
            {
                get { return this.OldResult == null; }
            }

            public static async Task<ProjectAnalysisData> CreateAsync(Project project, IEnumerable<StateSet> stateSets, bool avoidLoadingData, CancellationToken cancellationToken)
            {
                VersionStamp? version = null;

                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalysisResult>();
                foreach (var stateSet in stateSets)
                {
                    var state = stateSet.GetProjectState(project.Id);
                    var result = await state.GetAnalysisDataAsync(project, avoidLoadingData, cancellationToken).ConfigureAwait(false);

                    if (!version.HasValue)
                    {
                        version = result.Version;
                    }
                    else
                    {
                        // all version must be same.
                        Contract.ThrowIfFalse(version == result.Version);
                    }

                    builder.Add(stateSet.Analyzer, result);
                }

                if (!version.HasValue)
                {
                    // there is no saved data to return.
                    return ProjectAnalysisData.Empty;
                }

                return new ProjectAnalysisData(version.Value, builder.ToImmutable());
            }
        }
    }
}