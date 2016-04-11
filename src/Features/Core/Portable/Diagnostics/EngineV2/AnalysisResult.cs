// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// This holds onto diagnostics for a specific version of project snapshot
    /// in a way each kind of diagnostics can be queried fast.
    /// </summary>
    internal struct AnalysisResult
    {
        public readonly ProjectId ProjectId;
        public readonly VersionStamp Version;

        // set of documents that has any kind of diagnostics on it
        public readonly ImmutableHashSet<DocumentId> DocumentIds;
        public readonly bool IsEmpty;

        // map for each kind of diagnostics
        // syntax locals and semantic locals are self explanatory.
        // non locals means diagnostics that belong to a tree that are produced by analyzing other files.
        // others means diagnostics that doesnt have locations.
        private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> _syntaxLocals;
        private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> _semanticLocals;
        private readonly ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> _nonLocals;
        private readonly ImmutableArray<DiagnosticData> _others;

        public AnalysisResult(
            ProjectId projectId, VersionStamp version, ImmutableHashSet<DocumentId> documentIds, bool isEmpty)
        {
            ProjectId = projectId;
            Version = version;
            DocumentIds = documentIds;
            IsEmpty = isEmpty;

            _syntaxLocals = null;
            _semanticLocals = null;
            _nonLocals = null;
            _others = default(ImmutableArray<DiagnosticData>);
        }

        public AnalysisResult(
            ProjectId projectId, VersionStamp version,
            ImmutableHashSet<DocumentId> documentIds,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocals,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocals,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocals,
            ImmutableArray<DiagnosticData> others)
        {
            ProjectId = projectId;
            Version = version;
            DocumentIds = documentIds;

            _syntaxLocals = syntaxLocals;
            _semanticLocals = semanticLocals;
            _nonLocals = nonLocals;
            _others = others;

            IsEmpty = DocumentIds.IsEmpty && _others.IsEmpty;
        }

        // aggregated form means it has aggregated information but no actual data.
        public bool IsAggregatedForm => _syntaxLocals == null;

        // this shouldn't be called for aggregated form.
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SyntaxLocals => ReturnIfNotDefalut(_syntaxLocals);
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SemanticLocals => ReturnIfNotDefalut(_semanticLocals);
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> NonLocals => ReturnIfNotDefalut(_nonLocals);
        public ImmutableArray<DiagnosticData> Others => ReturnIfNotDefalut(_others);

        public ImmutableArray<DiagnosticData> GetResultOrEmpty(ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> map, DocumentId key)
        {
            // this is just a helper method.
            ImmutableArray<DiagnosticData> diagnostics;
            if (map.TryGetValue(key, out diagnostics))
            {
                return diagnostics;
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        public AnalysisResult ToAggregatedForm()
        {
            return new AnalysisResult(ProjectId, Version, DocumentIds, IsEmpty);
        }

        private T ReturnIfNotDefalut<T>(T value)
        {
            if (object.Equals(value, default(T)))
            {
                Contract.Fail("shouldn't be called");
            }

            return value;
        }
    }
}