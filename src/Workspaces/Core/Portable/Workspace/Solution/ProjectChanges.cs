// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis;

public readonly struct ProjectChanges
{
    private readonly Project _oldProject;

    internal ProjectChanges(Project newProject, Project oldProject)
    {
        NewProject = newProject;
        _oldProject = oldProject;
    }

    public ProjectId ProjectId => NewProject.Id;

    public Project OldProject => _oldProject;

    public Project NewProject { get; }

    public IEnumerable<ProjectReference> GetAddedProjectReferences()
    {
        var oldRefs = new HashSet<ProjectReference>(_oldProject.ProjectReferences);
        foreach (var reference in NewProject.ProjectReferences)
        {
            if (!oldRefs.Contains(reference))
            {
                yield return reference;
            }
        }
    }

    public IEnumerable<ProjectReference> GetRemovedProjectReferences()
    {
        var newRefs = new HashSet<ProjectReference>(NewProject.ProjectReferences);
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
        foreach (var metadata in NewProject.MetadataReferences)
        {
            if (!oldMetadata.Contains(metadata))
            {
                yield return metadata;
            }
        }
    }

    public IEnumerable<MetadataReference> GetRemovedMetadataReferences()
    {
        var newMetadata = new HashSet<MetadataReference>(NewProject.MetadataReferences);
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
        foreach (var analyzerReference in NewProject.AnalyzerReferences)
        {
            if (!oldAnalyzerReferences.Contains(analyzerReference))
            {
                yield return analyzerReference;
            }
        }
    }

    public IEnumerable<AnalyzerReference> GetRemovedAnalyzerReferences()
    {
        var newAnalyzerReferences = new HashSet<AnalyzerReference>(NewProject.AnalyzerReferences);
        foreach (var analyzerReference in _oldProject.AnalyzerReferences)
        {
            if (!newAnalyzerReferences.Contains(analyzerReference))
            {
                yield return analyzerReference;
            }
        }
    }

    /// <summary>
    /// Get <see cref="DocumentId"/>s of added documents in the order they appear in <see cref="Project.DocumentIds"/> of the <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetAddedDocuments()
        => NewProject.State.DocumentStates.GetAddedStateIds(_oldProject.State.DocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of added dditional documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetAddedAdditionalDocuments()
        => NewProject.State.AdditionalDocumentStates.GetAddedStateIds(_oldProject.State.AdditionalDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of added analyzer config documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetAddedAnalyzerConfigDocuments()
        => NewProject.State.AnalyzerConfigDocumentStates.GetAddedStateIds(_oldProject.State.AnalyzerConfigDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of documents with any changes (textual and non-textual)
    /// in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetChangedDocuments()
        => GetChangedDocuments(onlyGetDocumentsWithTextChanges: false);

    /// <summary>
    /// Get changed documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// When <paramref name="onlyGetDocumentsWithTextChanges"/> is true, only get documents with text changes (we only check text source, not actual content);
    /// otherwise get documents with any changes i.e. <see cref="ParseOptions"/>, <see cref="SourceCodeKind"/> and file path.
    /// </summary>
    public IEnumerable<DocumentId> GetChangedDocuments(bool onlyGetDocumentsWithTextChanges)
        => GetChangedDocuments(onlyGetDocumentsWithTextChanges, ignoreUnchangeableDocuments: false);

    internal IEnumerable<DocumentId> GetChangedDocuments(bool onlyGetDocumentsWithTextChanges, bool ignoreUnchangeableDocuments)
        => NewProject.State.DocumentStates.GetChangedStateIds(_oldProject.State.DocumentStates, onlyGetDocumentsWithTextChanges, ignoreUnchangeableDocuments);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of additional documents with any changes (textual and non-textual)
    /// in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetChangedAdditionalDocuments()
        => NewProject.State.AdditionalDocumentStates.GetChangedStateIds(_oldProject.State.AdditionalDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of analyzer config documents with any changes (textual and non-textual)
    /// in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetChangedAnalyzerConfigDocuments()
        => NewProject.State.AnalyzerConfigDocumentStates.GetChangedStateIds(_oldProject.State.AnalyzerConfigDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of removed documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="OldProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedDocuments()
        => NewProject.State.DocumentStates.GetRemovedStateIds(_oldProject.State.DocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of removed additional documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="OldProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedAdditionalDocuments()
        => NewProject.State.AdditionalDocumentStates.GetRemovedStateIds(_oldProject.State.AdditionalDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of removed analyzer config documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="OldProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedAnalyzerConfigDocuments()
        => NewProject.State.AnalyzerConfigDocumentStates.GetRemovedStateIds(_oldProject.State.AnalyzerConfigDocumentStates);
}
