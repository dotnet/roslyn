// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// this contains all states regarding a <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        private sealed class StateSet
        {
            public readonly string Language;
            public readonly DiagnosticAnalyzer Analyzer;
            public readonly bool IsHostAnalyzer;

            private readonly ConcurrentDictionary<DocumentId, ActiveFileState> _activeFileStates;
            private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectStates;

            public StateSet(string language, DiagnosticAnalyzer analyzer, bool isHostAnalyzer)
            {
                Language = language;
                Analyzer = analyzer;
                IsHostAnalyzer = isHostAnalyzer;

                _activeFileStates = new ConcurrentDictionary<DocumentId, ActiveFileState>(concurrencyLevel: 2, capacity: 10);
                _projectStates = new ConcurrentDictionary<ProjectId, ProjectState>(concurrencyLevel: 2, capacity: 1);
            }

            public bool IsActiveFile(DocumentId documentId)
                => _activeFileStates.ContainsKey(documentId);

            public bool TryGetActiveFileState(DocumentId documentId, [NotNullWhen(true)] out ActiveFileState? state)
                => _activeFileStates.TryGetValue(documentId, out state);

            public bool TryGetProjectState(ProjectId projectId, [NotNullWhen(true)] out ProjectState? state)
                => _projectStates.TryGetValue(projectId, out state);

            public ActiveFileState GetOrCreateActiveFileState(DocumentId documentId)
                => _activeFileStates.GetOrAdd(documentId, id => new ActiveFileState(id));

            public ProjectState GetOrCreateProjectState(ProjectId projectId)
                => _projectStates.GetOrAdd(projectId, static (id, self) => new ProjectState(self, id), this);

            public async Task<bool> OnDocumentOpenedAsync(TextDocument document)
            {
                // can not be cancelled
                if (!TryGetProjectState(document.Project.Id, out var projectState) ||
                    projectState.IsEmpty(document.Id))
                {
                    // nothing to do
                    return false;
                }

                var result = await projectState.GetAnalysisDataAsync(document, avoidLoadingData: false, CancellationToken.None).ConfigureAwait(false);
                var text = await document.GetValueTextAsync(CancellationToken.None).ConfigureAwait(false);

                // store analysis result to active file state:
                var activeFileState = GetOrCreateActiveFileState(document.Id);

                activeFileState.Save(AnalysisKind.Syntax, new DocumentAnalysisData(result.Checksum, text.Lines.Count, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Syntax)));
                activeFileState.Save(AnalysisKind.Semantic, new DocumentAnalysisData(result.Checksum, text.Lines.Count, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Semantic)));

                return true;
            }
        }
    }
}
