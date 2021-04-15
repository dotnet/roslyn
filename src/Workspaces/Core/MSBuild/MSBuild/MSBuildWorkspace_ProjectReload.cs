// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public sealed partial class MSBuildWorkspace
    {
        private async Task<ImmutableArray<Project>> UpdateProjectsAsync(ImmutableArray<ProjectInfo> newProjectInfos)
        {
            using (_serializationLock.DisposableWait())
            {
                var serialization = this.Services.GetRequiredService<ISerializerService>();
                var updatedProjectIds = TemporaryArray<ProjectId>.Empty;
                var oldSolution = this.CurrentSolution;
                var solution = oldSolution;
                foreach (var projectInfo in newProjectInfos)
                {
                    var projectToUpdate = solution.GetProject(projectInfo.Id);
                    RoslynDebug.AssertNotNull(projectToUpdate);
                    var oldCheckSum = await projectToUpdate.State.GetStateChecksumsAsync(default).ConfigureAwait(false);
                    var newCheckSum = projectInfo.GetCheckSum(serialization);
                    var newProject = await UpdateProjectAsync(projectToUpdate, projectInfo, oldCheckSum, newCheckSum).ConfigureAwait(false);
                    solution = AdjustReloadedProject(projectToUpdate, newProject).Solution;
                    updatedProjectIds.Add(newProject.Id);
                }

                this.SetCurrentSolution(solution);
                using var _1 = ArrayBuilder<Project>.GetInstance(out var updatedProjects);
                foreach (var projectId in updatedProjectIds)
                {
                    _ = this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectReloaded, oldSolution, solution, projectId);
                    updatedProjects.Add(solution.GetProject(projectId)!);
                }

                return updatedProjects.ToImmutableAndFree();
            }
        }

        private static async Task<Project> UpdateProjectAsync(Project project, ProjectInfo newProjectInfo, ProjectStateChecksums oldCheckSum, ProjectStateChecksums newCheckSum)
        {
            // changed info
            if (oldCheckSum.Info != newCheckSum.Info)
            {
                project = UpdateProjectAttributes(project, newProjectInfo.Attributes);
            }

            // changed compilation options
            if (oldCheckSum.CompilationOptions != newCheckSum.CompilationOptions)
            {
                project = project.WithCompilationOptions(newProjectInfo.CompilationOptions!);
            }

            // changed parse options
            if (oldCheckSum.ParseOptions != newCheckSum.ParseOptions)
            {
                project = project.WithParseOptions(newProjectInfo.ParseOptions!);
            }

            // changed project references
            if (oldCheckSum.ProjectReferences.Checksum != newCheckSum.ProjectReferences.Checksum)
            {
                project = project.WithProjectReferences(newProjectInfo.ProjectReferences);
            }

            // changed metadata references
            if (oldCheckSum.MetadataReferences.Checksum != newCheckSum.MetadataReferences.Checksum)
            {
                project = project.WithMetadataReferences(newProjectInfo.MetadataReferences);
            }

            // changed analyzer references
            if (oldCheckSum.AnalyzerReferences.Checksum != newCheckSum.AnalyzerReferences.Checksum)
            {
                project = project.WithAnalyzerReferences(newProjectInfo.AnalyzerReferences);
            }

            // changed documents
            if (oldCheckSum.Documents.Checksum != newCheckSum.Documents.Checksum)
            {
                project = await UpdateDocumentsAsync(
                    project,
                    newProjectInfo.Documents,
                    project.State.DocumentStates.States,
                    oldCheckSum.Documents,
                    newCheckSum.Documents,
                    (solution, documents) => solution.AddDocuments(documents),
                    (solution, documentId) => solution.RemoveDocument(documentId)).ConfigureAwait(false);
            }

            // changed additional documents
            if (oldCheckSum.AdditionalDocuments.Checksum != newCheckSum.AdditionalDocuments.Checksum)
            {
                project = await UpdateDocumentsAsync(
                    project,
                    newProjectInfo.AdditionalDocuments,
                    project.State.AdditionalDocumentStates.States,
                    oldCheckSum.AdditionalDocuments,
                    newCheckSum.AdditionalDocuments,
                    (solution, documents) => solution.AddAdditionalDocuments(documents),
                    (solution, documentId) => solution.RemoveAdditionalDocument(documentId)).ConfigureAwait(false);
            }

            // changed analyzer config documents
            if (oldCheckSum.AnalyzerConfigDocuments.Checksum != newCheckSum.AnalyzerConfigDocuments.Checksum)
            {
                project = await UpdateDocumentsAsync(
                    project,
                    newProjectInfo.AnalyzerConfigDocuments,
                    project.State.AnalyzerConfigDocumentStates.States,
                    oldCheckSum.AnalyzerConfigDocuments,
                    newCheckSum.AnalyzerConfigDocuments,
                    (solution, documents) => solution.AddAnalyzerConfigDocuments(documents),
                    (solution, documentId) => solution.RemoveAnalyzerConfigDocument(documentId)).ConfigureAwait(false);
            }

            return project;
        }

        private static Project UpdateProjectAttributes(Project project, ProjectInfo.ProjectAttributes attributes)
        {
            // there is no API to change these once project is created
            Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Id == attributes.Id);
            Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.Language == attributes.Language);
            Contract.ThrowIfFalse(project.State.ProjectInfo.Attributes.IsSubmission == attributes.IsSubmission);
            var projectId = project.Id;

            if (project.State.ProjectInfo.Attributes.Name != attributes.Name)
            {
                project = project.Solution.WithProjectName(projectId, attributes.Name).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.AssemblyName != attributes.AssemblyName)
            {
                project = project.Solution.WithProjectAssemblyName(projectId, attributes.AssemblyName).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.FilePath != attributes.FilePath)
            {
                project = project.Solution.WithProjectFilePath(projectId, attributes.FilePath).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.OutputFilePath != attributes.OutputFilePath)
            {
                project = project.Solution.WithProjectOutputFilePath(projectId, attributes.OutputFilePath).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.OutputRefFilePath != attributes.OutputRefFilePath)
            {
                project = project.Solution.WithProjectOutputRefFilePath(projectId, attributes.OutputRefFilePath).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.CompilationOutputInfo != attributes.CompilationOutputInfo)
            {
                project = project.Solution.WithProjectCompilationOutputInfo(project.Id, attributes.CompilationOutputInfo).GetProject(project.Id)!;
            }

            if (project.State.ProjectInfo.Attributes.DefaultNamespace != attributes.DefaultNamespace)
            {
                project = project.Solution.WithProjectDefaultNamespace(projectId, attributes.DefaultNamespace).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.HasAllInformation != attributes.HasAllInformation)
            {
                project = project.Solution.WithHasAllInformation(projectId, attributes.HasAllInformation).GetProject(projectId)!;
            }

            if (project.State.ProjectInfo.Attributes.RunAnalyzers != attributes.RunAnalyzers)
            {
                project = project.Solution.WithRunAnalyzers(projectId, attributes.RunAnalyzers).GetProject(projectId)!;
            }

            return project;
        }

        private static async Task<Project> UpdateDocumentsAsync(
                Project project,
                IReadOnlyList<DocumentInfo> documentInfos,
                IEnumerable<TextDocumentState> existingTextDocumentStates,
                ChecksumCollection oldChecksums,
                ChecksumCollection newChecksums,
                Func<Solution, ImmutableArray<DocumentInfo>, Solution> addDocuments,
                Func<Solution, DocumentId, Solution> removeDocument)
        {
            using var olds = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
            using var news = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();

            olds.Object.UnionWith(oldChecksums);
            news.Object.UnionWith(newChecksums);

            // remove documents that exist in both side
            olds.Object.ExceptWith(newChecksums);
            news.Object.ExceptWith(oldChecksums);

            var oldMap = await GetDocumentMapAsync(existingTextDocumentStates, olds.Object).ConfigureAwait(false);
            var newMap = GetDocumentMap(documentInfos);

            // added document
            ImmutableArray<DocumentInfo>.Builder? lazyDocumentsToAdd = null;
            foreach (var (documentId, (documentInfo, newDocumentChecksums)) in newMap)
            {
                if (!oldMap.ContainsKey(documentId))
                {
                    lazyDocumentsToAdd ??= ImmutableArray.CreateBuilder<DocumentInfo>();

                    // we have new document added
                    lazyDocumentsToAdd.Add(documentInfo);
                }
            }

            if (lazyDocumentsToAdd != null)
            {
                project = addDocuments(project.Solution, lazyDocumentsToAdd.ToImmutable()).GetProject(project.Id)!;
            }

            // changed document
            foreach (var (documentId, (documentInfo, newDocumentChecksums)) in newMap)
            {
                if (!oldMap.TryGetValue(documentId, out var oldDocumentChecksums))
                {
                    continue;
                }

                var document = project.GetDocument(documentId) ?? project.GetAdditionalDocument(documentId) ?? project.GetAnalyzerConfigDocument(documentId);
                Contract.ThrowIfNull(document);

                project = UpdateDocument(document, documentInfo, oldDocumentChecksums, newDocumentChecksums);
            }

            // removed document
            foreach (var (documentId, _) in oldMap)
            {
                if (!newMap.ContainsKey(documentId))
                {
                    // we have a document removed
                    project = removeDocument(project.Solution, documentId).GetProject(project.Id)!;
                }
            }

            return project;
        }

        private static Project UpdateDocument(TextDocument document, DocumentInfo newDocumentInfo, DocumentStateChecksums oldDocumentChecksums, DocumentStateChecksums newDocumentChecksums)
        {
            // changed info
            if (oldDocumentChecksums.Info != newDocumentChecksums.Info)
            {
                // there is no api to change these once document is created
                Contract.ThrowIfFalse(document.State.Attributes.Id == newDocumentInfo.Id);
                Contract.ThrowIfFalse(document.State.Attributes.Name == newDocumentInfo.Name);
                Contract.ThrowIfFalse(document.State.Attributes.FilePath == newDocumentInfo.FilePath);
                Contract.ThrowIfFalse(document.State.Attributes.IsGenerated == newDocumentInfo.IsGenerated);

                if (document.State.Attributes.Folders != newDocumentInfo.Folders)
                {
                    // additional document can't change folder once created
                    Contract.ThrowIfFalse(document is Document);
                    document = document.Project.Solution.WithDocumentFolders(document.Id, newDocumentInfo.Folders).GetDocument(document.Id)!;
                }

                if (document.State.Attributes.SourceCodeKind != newDocumentInfo.SourceCodeKind)
                {
                    // additional document can't change sourcecode kind once created
                    Contract.ThrowIfFalse(document is Document);
                    document = document.Project.Solution.WithDocumentSourceCodeKind(document.Id, newDocumentInfo.SourceCodeKind).GetDocument(document.Id)!;
                }
            }

            // we don't handle text changes here

            return document.Project;
        }

        private static Dictionary<DocumentId, (DocumentInfo, DocumentStateChecksums)> GetDocumentMap(IReadOnlyList<DocumentInfo> documentInfos)
        {
            var map = new Dictionary<DocumentId, (DocumentInfo, DocumentStateChecksums)>();
            foreach (var documentInfo in documentInfos)
            {
                map.Add(documentInfo.Id, (documentInfo, new DocumentStateChecksums(documentInfo.Attributes.GetCheckSum())));
            }

            return map;
        }

        private static async Task<Dictionary<DocumentId, DocumentStateChecksums>> GetDocumentMapAsync(IEnumerable<TextDocumentState> states, HashSet<Checksum> documents)
        {
            var map = new Dictionary<DocumentId, DocumentStateChecksums>();
            foreach (var state in states)
            {
                var documentChecksums = await state.GetStateChecksumsAsync(default).ConfigureAwait(false);
                if (documents.Contains(documentChecksums.Checksum))
                {
                    map.Add(state.Id, documentChecksums);
                }
            }

            return map;
        }
    }
}
