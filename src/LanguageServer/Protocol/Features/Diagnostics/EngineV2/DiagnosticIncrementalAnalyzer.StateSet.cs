// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
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

            public void OnRemoved()
            {
                // ths stateset is being removed.
                // TODO: we do this since InMemoryCache is static type. we might consider making it instance object
                //       of something.
                InMemoryStorage.DropCache(Analyzer);
            }
        }
    }
}
