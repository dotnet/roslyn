// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public struct ProjectChanges
    {
        private readonly Project newProject;
        private readonly Project oldProject;

        internal ProjectChanges(Project newProject, Project oldProject)
        {
            this.newProject = newProject;
            this.oldProject = oldProject;
        }

        public ProjectId ProjectId
        {
            get { return this.newProject.Id; }
        }

        public Project OldProject
        {
            get { return this.oldProject; }
        }

        public Project NewProject
        {
            get { return this.newProject; }
        }

        public IEnumerable<ProjectReference> GetAddedProjectReferences()
        {
            var oldRefs = new HashSet<ProjectReference>(this.oldProject.ProjectReferences);
            foreach (var reference in this.newProject.ProjectReferences)
            {
                if (!oldRefs.Contains(reference))
                {
                    yield return reference;
                }
            }
        }

        public IEnumerable<ProjectReference> GetRemovedProjectReferences()
        {
            var newRefs = new HashSet<ProjectReference>(this.newProject.ProjectReferences);
            foreach (var reference in this.oldProject.ProjectReferences)
            {
                if (!newRefs.Contains(reference))
                {
                    yield return reference;
                }
            }
        }

        public IEnumerable<MetadataReference> GetAddedMetadataReferences()
        {
            var oldMetadata = new HashSet<MetadataReference>(this.oldProject.MetadataReferences);
            foreach (var metadata in this.newProject.MetadataReferences)
            {
                if (!oldMetadata.Contains(metadata))
                {
                    yield return metadata;
                }
            }
        }

        public IEnumerable<MetadataReference> GetRemovedMetadataReferences()
        {
            var newMetadata = new HashSet<MetadataReference>(this.newProject.MetadataReferences);
            foreach (var metadata in this.oldProject.MetadataReferences)
            {
                if (!newMetadata.Contains(metadata))
                {
                    yield return metadata;
                }
            }
        }

        public IEnumerable<AnalyzerReference> GetAddedAnalyzerReferences()
        {
            var oldAnalyzerReferences = new HashSet<AnalyzerReference>(this.oldProject.AnalyzerReferences);
            foreach (var analyzerReference in this.newProject.AnalyzerReferences)
            {
                if (!oldAnalyzerReferences.Contains(analyzerReference))
                {
                    yield return analyzerReference;
                }
            }
        }

        public IEnumerable<AnalyzerReference> GetRemovedAnalyzerReferences()
        {
            var newAnalyzerReferences = new HashSet<AnalyzerReference>(this.newProject.AnalyzerReferences);
            foreach (var analyzerReference in this.oldProject.AnalyzerReferences)
            {
                if (!newAnalyzerReferences.Contains(analyzerReference))
                {
                    yield return analyzerReference;
                }
            }
        }

        public IEnumerable<DocumentId> GetAddedDocuments()
        {
            foreach (var id in this.newProject.DocumentIds)
            {
                if (!this.oldProject.ContainsDocument(id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetAddedAdditionalDocuments()
        {
            foreach (var id in this.newProject.AdditionalDocumentIds)
            {
                if (!this.oldProject.ContainsAdditionalDocument(id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetChangedDocuments()
        {
            // if the document states are different then there is a change.
            foreach (var id in this.newProject.DocumentIds)
            {
                var newState = this.newProject.GetDocumentState(id);
                var oldState = this.oldProject.GetDocumentState(id);
                if (oldState != null && newState != oldState)
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetChangedAdditionalDocuments()
        {
            // if the document states are different then there is a change.
            foreach (var id in this.newProject.AdditionalDocumentIds)
            {
                var newState = this.newProject.GetAdditionalDocumentState(id);
                var oldState = this.oldProject.GetAdditionalDocumentState(id);
                if (oldState != null && newState != oldState)
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetRemovedDocuments()
        {
            foreach (var id in this.oldProject.DocumentIds)
            {
                if (!this.newProject.ContainsDocument(id))
                {
                    yield return id;
                }
            }
        }

        public IEnumerable<DocumentId> GetRemovedAdditionalDocuments()
        {
            foreach (var id in this.oldProject.AdditionalDocumentIds)
            {
                if (!this.newProject.ContainsAdditionalDocument(id))
                {
                    yield return id;
                }
            }
        }
    }
}
