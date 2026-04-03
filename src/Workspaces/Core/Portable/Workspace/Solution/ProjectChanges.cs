// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis;

public readonly struct ProjectChanges
{
    internal ProjectChanges(Project newProject, Project oldProject)
    {
        NewProject = newProject;
        OldProject = oldProject;
    }

    public ProjectId ProjectId => NewProject.Id;

    public Project OldProject { get; }

    public Project NewProject { get; }

    public IEnumerable<ProjectReference> GetAddedProjectReferences()
        => GetChangedProjectReferences(NewProject, OldProject);

    public IEnumerable<ProjectReference> GetRemovedProjectReferences()
        => GetChangedProjectReferences(OldProject, NewProject);

    public IEnumerable<MetadataReference> GetAddedMetadataReferences()
        => GetChangedItems(NewProject.MetadataReferences, OldProject.MetadataReferences);

    public IEnumerable<MetadataReference> GetRemovedMetadataReferences()
        => GetChangedItems(OldProject.MetadataReferences, NewProject.MetadataReferences);

    public IEnumerable<AnalyzerReference> GetAddedAnalyzerReferences()
        => GetChangedItems(NewProject.AnalyzerReferences, OldProject.AnalyzerReferences);

    public IEnumerable<AnalyzerReference> GetRemovedAnalyzerReferences()
        => GetChangedItems(OldProject.AnalyzerReferences, NewProject.AnalyzerReferences);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of added documents in the order they appear in <see cref="Project.DocumentIds"/> of the <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetAddedDocuments()
        => NewProject.State.DocumentStates.GetAddedStateIds(OldProject.State.DocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of added dditional documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetAddedAdditionalDocuments()
        => NewProject.State.AdditionalDocumentStates.GetAddedStateIds(OldProject.State.AdditionalDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of added analyzer config documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetAddedAnalyzerConfigDocuments()
        => NewProject.State.AnalyzerConfigDocumentStates.GetAddedStateIds(OldProject.State.AnalyzerConfigDocumentStates);

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
        => NewProject.State.DocumentStates.GetChangedStateIds(OldProject.State.DocumentStates, onlyGetDocumentsWithTextChanges, ignoreUnchangeableDocuments);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of additional documents with any changes (textual and non-textual)
    /// in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetChangedAdditionalDocuments()
        => NewProject.State.AdditionalDocumentStates.GetChangedStateIds(OldProject.State.AdditionalDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of analyzer config documents with any changes (textual and non-textual)
    /// in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="NewProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetChangedAnalyzerConfigDocuments()
        => NewProject.State.AnalyzerConfigDocumentStates.GetChangedStateIds(OldProject.State.AnalyzerConfigDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of removed documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="OldProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedDocuments()
        => NewProject.State.DocumentStates.GetRemovedStateIds(OldProject.State.DocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of removed additional documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="OldProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedAdditionalDocuments()
        => NewProject.State.AdditionalDocumentStates.GetRemovedStateIds(OldProject.State.AdditionalDocumentStates);

    /// <summary>
    /// Get <see cref="DocumentId"/>s of removed analyzer config documents in the order they appear in <see cref="Project.DocumentIds"/> of <see cref="OldProject"/>.
    /// </summary>
    public IEnumerable<DocumentId> GetRemovedAnalyzerConfigDocuments()
        => NewProject.State.AnalyzerConfigDocumentStates.GetRemovedStateIds(OldProject.State.AnalyzerConfigDocumentStates);

    private static IEnumerable<T> GetChangedItems<T>(IEnumerable<T> newItems, IEnumerable<T> oldItems)
        => newItems == oldItems ? [] : newItems.Except(oldItems);

    private static IEnumerable<ProjectReference> GetChangedProjectReferences(Project newProject, Project oldProject)
    {
        // Fast path: if the set of projects in the solution and the underlying project references
        // collection are identical, then no project references (within the solution) have changed.
        if (newProject.Solution.ProjectIds == oldProject.Solution.ProjectIds &&
            newProject.State.ProjectReferences == oldProject.State.ProjectReferences)
        {
            return [];
        }

        // Compute the diff based on ProjectReferences, which only includes references to projects
        // contained in the solution.
        return newProject.ProjectReferences.Except(oldProject.ProjectReferences);
    }
}
