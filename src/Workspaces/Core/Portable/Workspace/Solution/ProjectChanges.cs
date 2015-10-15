// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

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

        public ProjectId ProjectId
        {
            get { return _newProject.Id; }
        }

        public Project OldProject
        {
            get { return _oldProject; }
        }

        public Project NewProject
        {
            get { return _newProject; }
        }

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
        {
            foreach (var id in _newProject.DocumentIds)
            {
                if (!_oldProject.ContainsDocument(id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetAddedAdditionalDocuments()
        {
            foreach (var id in _newProject.AdditionalDocumentIds)
            {
                if (!_oldProject.ContainsAdditionalDocument(id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetChangedDocuments()
        {
            // if the document states are different then there is a change.
            foreach (var id in _newProject.DocumentIds)
            {
                var newState = _newProject.GetDocumentState(id);
                var oldState = _oldProject.GetDocumentState(id);
                if (oldState != null && newState != oldState)
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetChangedAdditionalDocuments()
        {
            // if the document states are different then there is a change.
            foreach (var id in _newProject.AdditionalDocumentIds)
            {
                var newState = _newProject.GetAdditionalDocumentState(id);
                var oldState = _oldProject.GetAdditionalDocumentState(id);
                if (oldState != null && newState != oldState)
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetRemovedDocuments()
        {
            foreach (var id in _oldProject.DocumentIds)
            {
                if (!_newProject.ContainsDocument(id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetRemovedAdditionalDocuments()
        {
            foreach (var id in _oldProject.AdditionalDocumentIds)
            {
                if (!_newProject.ContainsAdditionalDocument(id))
                {
                    yield return id;
                }
            }
        }
    }
}
