// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public struct ProjectChanges
    {
        private readonly Project _newProject;
        private readonly Project _oldProject;

        internal ProjectChanges(Project newProject, Project oldProject)
        {
            _newProject = newProject;
            _oldProject = oldProject;
        }

        public ProjectId ProjectId => _newProject.Id;

        public Project OldProject => _oldProject;

        public Project NewProject => _newProject;

        public IEnumerable<ProjectReference> GetAddedProjectReferences()
        {
            var oldRefs = new HashSet<ProjectReference>(_oldProject.ProjectReferences);
            foreach (var reference in _newProject.ProjectReferences)
            {
                if (!oldRefs.Contains(reference))
                {
                    yield return reference;
                }
            }
        }

        public IEnumerable<ProjectReference> GetRemovedProjectReferences()
        {
            var newRefs = new HashSet<ProjectReference>(_newProject.ProjectReferences);
            foreach (var reference in _oldProject.ProjectReferences)
            {
                if (!newRefs.Contains(reference))
                {
                    yield return reference;
                }
            }
        }

        public IEnumerable<MetadataReference> GetAddedMetadataReferences()
        {
            var oldMetadata = new HashSet<MetadataReference>(_oldProject.MetadataReferences);
            foreach (var metadata in _newProject.MetadataReferences)
            {
                if (!oldMetadata.Contains(metadata))
                {
                    yield return metadata;
                }
            }
        }

        public IEnumerable<MetadataReference> GetRemovedMetadataReferences()
        {
            var newMetadata = new HashSet<MetadataReference>(_newProject.MetadataReferences);
            foreach (var metadata in _oldProject.MetadataReferences)
            {
                if (!newMetadata.Contains(metadata))
                {
                    yield return metadata;
                }
            }
        }

        public IEnumerable<AnalyzerReference> GetAddedAnalyzerReferences()
        {
            var oldAnalyzerReferences = new HashSet<AnalyzerReference>(_oldProject.AnalyzerReferences);
            foreach (var analyzerReference in _newProject.AnalyzerReferences)
            {
                if (!oldAnalyzerReferences.Contains(analyzerReference))
                {
                    yield return analyzerReference;
                }
            }
        }

        public IEnumerable<AnalyzerReference> GetRemovedAnalyzerReferences()
        {
            var newAnalyzerReferences = new HashSet<AnalyzerReference>(_newProject.AnalyzerReferences);
            foreach (var analyzerReference in _oldProject.AnalyzerReferences)
            {
                if (!newAnalyzerReferences.Contains(analyzerReference))
                {
                    yield return analyzerReference;
                }
            }
        }

        public IEnumerable<DocumentId> GetAddedDocuments()
            => _newProject.State.DocumentStates.GetAddedStateIds(_oldProject.State.DocumentStates);

        public IEnumerable<DocumentId> GetAddedAdditionalDocuments()
            => _newProject.State.AdditionalDocumentStates.GetAddedStateIds(_oldProject.State.AdditionalDocumentStates);

        public IEnumerable<DocumentId> GetAddedAnalyzerConfigDocuments()
            => _newProject.State.AnalyzerConfigDocumentStates.GetAddedStateIds(_oldProject.State.AnalyzerConfigDocumentStates);

        /// <summary>
        /// Get Documents with any changes, including textual and non-textual changes
        /// </summary>
        public IEnumerable<DocumentId> GetChangedDocuments()
            => GetChangedDocuments(onlyGetDocumentsWithTextChanges: false, ignoreUnchangeableDocuments: false);

        /// <summary>
        /// Get changed documents.
        /// When <paramref name="onlyGetDocumentsWithTextChanges"/> is true, only get documents with text changes (we only check text source, not actual content);
        /// otherwise get documents with any changes i.e. <see cref="ParseOptions"/>, <see cref="SourceCodeKind"/> and file path.
        /// </summary>
        public IEnumerable<DocumentId> GetChangedDocuments(bool onlyGetDocumentsWithTextChanges)
            => GetChangedDocuments(onlyGetDocumentsWithTextChanges, ignoreUnchangeableDocuments: false);

        internal IEnumerable<DocumentId> GetChangedDocuments(bool onlyGetDocumentsWithTextChanges, bool ignoreUnchangeableDocuments)
        {
            foreach (var newState in _newProject.State.DocumentStates.States)
            {
                var oldState = _oldProject.State.DocumentStates.GetState(newState.Id);
                if (oldState == null)
                {
                    // document was removed
                    continue;
                }

                if (newState == oldState)
                {
                    continue;
                }

                if (onlyGetDocumentsWithTextChanges && !newState.HasTextChanged(oldState, ignoreUnchangeableDocuments))
                {
                    continue;
                }

                yield return newState.Id;
            }
        }

        public IEnumerable<DocumentId> GetChangedAdditionalDocuments()
            => _newProject.State.AdditionalDocumentStates.GetChangedStateIds(_oldProject.State.AdditionalDocumentStates);

        public IEnumerable<DocumentId> GetChangedAnalyzerConfigDocuments()
            => _newProject.State.AnalyzerConfigDocumentStates.GetChangedStateIds(_oldProject.State.AnalyzerConfigDocumentStates);

        public IEnumerable<DocumentId> GetRemovedDocuments()
            => _newProject.State.DocumentStates.GetRemovedStateIds(_oldProject.State.DocumentStates);

        public IEnumerable<DocumentId> GetRemovedAdditionalDocuments()
            => _newProject.State.AdditionalDocumentStates.GetRemovedStateIds(_oldProject.State.AdditionalDocumentStates);

        public IEnumerable<DocumentId> GetRemovedAnalyzerConfigDocuments()
            => _newProject.State.AnalyzerConfigDocumentStates.GetRemovedStateIds(_oldProject.State.AnalyzerConfigDocumentStates);
    }
}
