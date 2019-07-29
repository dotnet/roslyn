// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.Diagnostics
{
    /// <summary>
    /// This holds onto diagnostics for a specific version of project snapshot
    /// in a way each kind of diagnostics can be queried fast.
    /// </summary>
    internal struct DiagnosticAnalysisResult
    {
        public readonly bool FromBuild;
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

        private DiagnosticAnalysisResult(
            ProjectId projectId, VersionStamp version, ImmutableHashSet<DocumentId> documentIds, bool isEmpty, bool fromBuild)
        {
            ProjectId = projectId;
            Version = version;
            DocumentIds = documentIds;
            IsEmpty = isEmpty;
            FromBuild = fromBuild;

            _syntaxLocals = null;
            _semanticLocals = null;
            _nonLocals = null;
            _others = default;
        }

        private DiagnosticAnalysisResult(
            ProjectId projectId, VersionStamp version,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocals,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocals,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocals,
            ImmutableArray<DiagnosticData> others,
            ImmutableHashSet<DocumentId> documentIds,
            bool fromBuild)
        {
            ProjectId = projectId;
            Version = version;
            FromBuild = fromBuild;

            _syntaxLocals = syntaxLocals;
            _semanticLocals = semanticLocals;
            _nonLocals = nonLocals;
            _others = others;

            DocumentIds = documentIds;
            IsEmpty = false;

            // do after all fields are assigned.
            DocumentIds ??= CreateDocumentIds();
            IsEmpty = DocumentIds.IsEmpty && _others.IsEmpty;
        }

        public static DiagnosticAnalysisResult CreateEmpty(ProjectId projectId, VersionStamp version)
        {
            return new DiagnosticAnalysisResult(
                projectId,
                version,
                documentIds: ImmutableHashSet<DocumentId>.Empty,
                syntaxLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                others: ImmutableArray<DiagnosticData>.Empty,
                fromBuild: false);
        }

        public static DiagnosticAnalysisResult CreateInitialResult(ProjectId projectId)
        {
            return new DiagnosticAnalysisResult(
                projectId,
                version: VersionStamp.Default,
                documentIds: null,
                isEmpty: true,
                fromBuild: false);
        }

        public static DiagnosticAnalysisResult CreateFromBuild(Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            // we can't distinguish locals and non locals from build diagnostics nor determine right snapshot version for the build.
            // so we put everything in as semantic local with default version. this lets us to replace those to live diagnostics when needed easily.
            var version = VersionStamp.Default;

            // filter out any document that doesn't support diagnostics.
            // g.Key == null means diagnostics on the project which assigned to "others" error category
            var group = diagnostics.GroupBy(d => d.DocumentId).Where(g => g.Key == null || project.GetDocument(g.Key).SupportsDiagnostics()).ToList();

            var result = new DiagnosticAnalysisResult(
                project.Id,
                version,
                documentIds: group.Where(g => g.Key != null).Select(g => g.Key).ToImmutableHashSet(),
                syntaxLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                semanticLocals: group.Where(g => g.Key != null).ToImmutableDictionary(g => g.Key, g => g.ToImmutableArray()),
                nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                others: group.Where(g => g.Key == null).SelectMany(g => g).ToImmutableArrayOrEmpty(),
                fromBuild: true);

            return result;
        }

        public static DiagnosticAnalysisResult CreateFromSerialization(
            Project project,
            VersionStamp version,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> syntaxLocalMap,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> semanticLocalMap,
            ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> nonLocalMap,
            ImmutableArray<DiagnosticData> others,
            ImmutableHashSet<DocumentId> documentIds = null)
        {
            VerifyDocumentMap(project, syntaxLocalMap);
            VerifyDocumentMap(project, semanticLocalMap);
            VerifyDocumentMap(project, nonLocalMap);

            return new DiagnosticAnalysisResult(
                project.Id,
                version,
                syntaxLocalMap,
                semanticLocalMap,
                nonLocalMap,
                others,
                documentIds,
                fromBuild: false);
        }

        public static DiagnosticAnalysisResult CreateFromBuilder(DiagnosticAnalysisResultBuilder builder)
        {
            return CreateFromSerialization(
                builder.Project,
                builder.Version,
                builder.SyntaxLocals,
                builder.SemanticLocals,
                builder.NonLocals,
                builder.Others,
                builder.DocumentIds);
        }

        // aggregated form means it has aggregated information but no actual data.
        public bool IsAggregatedForm => _syntaxLocals == null;

        // default analysis result
        public bool IsDefault => DocumentIds == null;

        // make sure we don't return null
        public ImmutableHashSet<DocumentId> DocumentIdsOrEmpty => DocumentIds ?? ImmutableHashSet<DocumentId>.Empty;

        // this shouldn't be called for aggregated form.
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SyntaxLocals => ReturnIfNotDefault(_syntaxLocals);
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> SemanticLocals => ReturnIfNotDefault(_semanticLocals);
        public ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> NonLocals => ReturnIfNotDefault(_nonLocals);
        public ImmutableArray<DiagnosticData> Others => ReturnIfNotDefault(_others);

        public ImmutableArray<DiagnosticData> GetResultOrEmpty(ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> map, DocumentId key)
        {
            // this is just a helper method.
            if (map.TryGetValue(key, out var diagnostics))
            {
                Contract.ThrowIfFalse(DocumentIds.Contains(key));
                return diagnostics;
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        public DiagnosticAnalysisResult ToAggregatedForm()
        {
            return new DiagnosticAnalysisResult(ProjectId, Version, DocumentIds, IsEmpty, FromBuild);
        }

        public DiagnosticAnalysisResult UpdateAggregatedResult(VersionStamp version, DocumentId documentId, bool fromBuild)
        {
            return new DiagnosticAnalysisResult(ProjectId, version, DocumentIdsOrEmpty.Add(documentId), isEmpty: false, fromBuild: fromBuild);
        }

        public DiagnosticAnalysisResult Reset()
        {
            return new DiagnosticAnalysisResult(ProjectId, VersionStamp.Default, DocumentIds, IsEmpty, FromBuild);
        }

        public DiagnosticAnalysisResult DropExceptSyntax()
        {
            // quick bail out
            if (_syntaxLocals?.Count == 0)
            {
                return CreateEmpty(ProjectId, Version);
            }

            // keep only syntax errors
            return new DiagnosticAnalysisResult(
               ProjectId,
               Version,
               SyntaxLocals,
               semanticLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
               nonLocals: ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
               others: ImmutableArray<DiagnosticData>.Empty,
               documentIds: null,
               fromBuild: false);
        }

        private T ReturnIfNotDefault<T>(T value)
        {
            if (object.Equals(value, default))
            {
                Contract.Fail("shouldn't be called");
            }

            return value;
        }

        private ImmutableHashSet<DocumentId> CreateDocumentIds()
        {
            // quick bail out
            var allEmpty = _syntaxLocals ?? _semanticLocals ?? _nonLocals;
            if (allEmpty == null)
            {
                return ImmutableHashSet<DocumentId>.Empty;
            }

            var documents = SpecializedCollections.EmptyEnumerable<DocumentId>();
            if (_syntaxLocals != null)
            {
                documents = documents.Concat(_syntaxLocals.Keys);
            }

            if (_semanticLocals != null)
            {
                documents = documents.Concat(_semanticLocals.Keys);
            }

            if (_nonLocals != null)
            {
                documents = documents.Concat(_nonLocals.Keys);
            }

            return ImmutableHashSet.CreateRange(documents);
        }

        [Conditional("DEBUG")]
        private static void VerifyDocumentMap(Project project, ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>> map)
        {
            foreach (var documentId in map.Keys)
            {
                Debug.Assert(project.GetDocument(documentId)?.SupportsDiagnostics() == true);
            }
        }
    }
}
