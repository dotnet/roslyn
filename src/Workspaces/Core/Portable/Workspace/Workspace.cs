// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A workspace provides access to a active set of source code projects and documents and their
    /// associated syntax trees, compilations and semantic models. A workspace has a current solution
    /// that is an immutable snapshot of the projects and documents. This property may change over time
    /// as the workspace is updated either from live interactions in the environment or via call to the
    /// workspace's <see cref="TryApplyChanges(Solution)"/> method.
    /// </summary>
    public abstract partial class Workspace : IDisposable
    {
        private readonly string? _workspaceKind;
        private readonly HostWorkspaceServices _services;
        private readonly BranchId _primaryBranchId;

        private readonly IWorkspaceOptionService? _workspaceOptionService;

        // forces serialization of mutation calls from host (OnXXX methods). Must take this lock before taking stateLock.
        private readonly SemaphoreSlim _serializationLock = new SemaphoreSlim(initialCount: 1);

        // this lock guards all the mutable fields (do not share lock with derived classes)
        private readonly NonReentrantLock _stateLock = new NonReentrantLock(useThisInstanceForSynchronization: true);

        // Current solution.
        private Solution _latestSolution;

        private readonly IWorkspaceTaskScheduler _taskQueue;

        // test hooks.
        internal static bool TestHookStandaloneProjectsDoNotHoldReferences = false;

        internal bool TestHookPartialSolutionsDisabled { get; set; }

        private Action<string>? _testMessageLogger;

        /// <summary>
        /// Constructs a new workspace instance.
        /// </summary>
        /// <param name="host">The <see cref="HostServices"/> this workspace uses</param>
        /// <param name="workspaceKind">A string that can be used to identify the kind of workspace. Usually this matches the name of the class.</param>
        protected Workspace(HostServices host, string? workspaceKind)
        {
            _primaryBranchId = BranchId.GetNextId();
            _workspaceKind = workspaceKind;

            _services = host.CreateWorkspaceServices(this);

            _workspaceOptionService = _services.GetService<IOptionService>() as IWorkspaceOptionService;

            // queue used for sending events
            var workspaceTaskSchedulerFactory = _services.GetRequiredService<IWorkspaceTaskSchedulerFactory>();
            _taskQueue = workspaceTaskSchedulerFactory.CreateEventingTaskQueue();

            // initialize with empty solution
            _latestSolution = CreateSolution(SolutionId.CreateNewId());
        }

        internal void LogTestMessage(string message)
        {
            _testMessageLogger?.Invoke(message);
        }

        /// <summary>
        /// Sets an internal logger that will receive some messages.
        /// </summary>
        /// <param name="writeLineMessageLogger">An action called to write a single line to the log.</param>
        internal void SetTestLogger(Action<string>? writeLineMessageLogger)
        {
            _testMessageLogger = writeLineMessageLogger;
        }

        /// <summary>
        /// Services provider by the host for implementing workspace features.
        /// </summary>
        public HostWorkspaceServices Services => _services;

        /// <summary>
        /// primary branch id that current solution has
        /// </summary>
        internal BranchId PrimaryBranchId => _primaryBranchId;

        /// <summary>
        /// Override this property if the workspace supports partial semantics for documents.
        /// </summary>
        protected internal virtual bool PartialSemanticsEnabled => false;

        /// <summary>
        /// The kind of the workspace.
        /// This is generally <see cref="WorkspaceKind.Host"/> if originating from the host environment, but may be
        /// any other name used for a specific kind of workspace.
        /// </summary>
        // TODO (https://github.com/dotnet/roslyn/issues/37110): decide if Kind should be non-null
        public string? Kind => _workspaceKind;

        /// <summary>
        /// Create a new empty solution instance associated with this workspace.
        /// </summary>
        protected internal Solution CreateSolution(SolutionInfo solutionInfo)
        {
            return new Solution(this, solutionInfo.Attributes);
        }

        /// <summary>
        /// Create a new empty solution instance associated with this workspace.
        /// </summary>
        protected internal Solution CreateSolution(SolutionId id)
        {
            return CreateSolution(SolutionInfo.Create(id, VersionStamp.Create()));
        }

        /// <summary>
        /// The current solution.
        ///
        /// The solution is an immutable model of the current set of projects and source documents.
        /// It provides access to source text, syntax trees and semantics.
        ///
        /// This property may change as the workspace reacts to changes in the environment or
        /// after <see cref="TryApplyChanges(Solution)"/> is called.
        /// </summary>
        public Solution CurrentSolution
        {
            get
            {
                return Volatile.Read(ref _latestSolution);
            }
        }

        /// <summary>
        /// Sets the <see cref="CurrentSolution"/> of this workspace. This method does not raise a <see cref="WorkspaceChanged"/> event.
        /// </summary>
        /// <remarks>
        /// This method does not guarantee that linked files will have the same contents. Callers
        /// should enforce that policy before passing in the new solution.
        /// </remarks>
        protected Solution SetCurrentSolution(Solution solution)
        {
            if (solution is null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            var currentSolution = Volatile.Read(ref _latestSolution);
            if (solution == currentSolution)
            {
                // No change
                return solution;
            }

            while (true)
            {
                var newSolution = solution.WithNewWorkspace(this, currentSolution.WorkspaceVersion + 1);
                var replacedSolution = Interlocked.CompareExchange(ref _latestSolution, newSolution, currentSolution);
                if (replacedSolution == currentSolution)
                {
                    return newSolution;
                }

                currentSolution = replacedSolution;
            }
        }

        /// <summary>
        /// Gets or sets the set of all global options.
        /// </summary>
        public OptionSet Options
        {
            get
            {
                return _services.GetRequiredService<IOptionService>().GetOptions();
            }

            set
            {
                _services.GetRequiredService<IOptionService>().SetOptions(value);
            }
        }

        /// <summary>
        /// Executes an action as a background task, as part of a sequential queue of tasks.
        /// </summary>
        protected internal Task ScheduleTask(Action action, string taskName = "Workspace.Task")
        {
            return _taskQueue.ScheduleTask(action, taskName);
        }

        /// <summary>
        /// Execute a function as a background task, as part of a sequential queue of tasks.
        /// </summary>
        protected internal Task<T> ScheduleTask<T>(Func<T> func, string taskName = "Workspace.Task")
        {
            return _taskQueue.ScheduleTask(func, taskName);
        }

        /// <summary>
        /// Override this method to act immediately when the text of a document has changed, as opposed
        /// to waiting for the corresponding workspace changed event to fire asynchronously.
        /// </summary>
        protected virtual void OnDocumentTextChanged(Document document)
        {
        }

        /// <summary>
        /// Override this method to act immediately when a document is closing, as opposed
        /// to waiting for the corresponding workspace changed event to fire asynchronously.
        /// </summary>
        protected virtual void OnDocumentClosing(DocumentId documentId)
        {
        }

        /// <summary>
        /// Clears all solution data and empties the current solution.
        /// </summary>
        protected void ClearSolution()
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                this.ClearSolutionData();

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionCleared, oldSolution, this.CurrentSolution);
            }
        }

        /// <summary>
        /// This method is called when a solution is cleared.
        ///
        /// Override this method if you want to do additional work when a solution is cleared.
        /// Call the base method at the end of your method.
        /// </summary>
        protected virtual void ClearSolutionData()
        {
            // clear any open documents
            this.ClearOpenDocuments();

            this.SetCurrentSolution(this.CreateSolution(this.CurrentSolution.Id));
        }

        /// <summary>
        /// This method is called when an individual project is removed.
        ///
        /// Override this method if you want to do additional work when a project is removed.
        /// Call the base method at the end of your method.
        /// </summary>
        protected virtual void ClearProjectData(ProjectId projectId)
        {
            this.ClearOpenDocuments(projectId);
        }

        /// <summary>
        /// This method is called to clear an individual document is removed.
        ///
        /// Override this method if you want to do additional work when a document is removed.
        /// Call the base method at the end of your method.
        /// </summary>
        protected internal virtual void ClearDocumentData(DocumentId documentId)
        {
            this.ClearOpenDocument(documentId);
        }

        /// <summary>
        /// Disposes this workspace. The workspace can longer be used after it is disposed.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(finalize: false);
        }

        /// <summary>
        /// Call this method when the workspace is disposed.
        ///
        /// Override this method to do additional work when the workspace is disposed.
        /// Call this method at the end of your method.
        /// </summary>
        protected virtual void Dispose(bool finalize)
        {
            if (!finalize)
            {
                this.ClearSolutionData();

                this.Services.GetService<IWorkspaceEventListenerService>()?.Stop();
            }

            _workspaceOptionService?.OnWorkspaceDisposed(this);
        }

        #region Host API
        /// <summary>
        /// Call this method to respond to a solution being opened in the host environment.
        /// </summary>
        protected internal void OnSolutionAdded(SolutionInfo solutionInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var solutionId = solutionInfo.Id;

                CheckSolutionIsEmpty();
                this.SetCurrentSolution(this.CreateSolution(solutionInfo));

                solutionInfo.Projects.Do(p => OnProjectAdded_NoLock(p, silent: true));

                var newSolution = this.CurrentSolution;
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionAdded, oldSolution, newSolution);
            }
        }

        /// <summary>
        /// Call this method to respond to a solution being reloaded in the host environment.
        /// </summary>
        protected internal void OnSolutionReloaded(SolutionInfo reloadedSolutionInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(this.CreateSolution(reloadedSolutionInfo));

                reloadedSolutionInfo.Projects.Do(pi => OnProjectAdded_NoLock(pi, silent: true));

                newSolution = this.AdjustReloadedSolution(oldSolution, this.CurrentSolution);
                newSolution = this.SetCurrentSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionReloaded, oldSolution, newSolution);
            }
        }

        /// <summary>
        /// This method is called when the solution is removed from the workspace.
        ///
        /// Override this method if you want to do additional work when the solution is removed.
        /// Call the base method at the end of your method.
        /// Call this method to respond to a solution being removed/cleared/closed in the host environment.
        /// </summary>
        protected internal void OnSolutionRemoved()
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;

                this.ClearSolutionData();

                // reset to new empty solution
                this.SetCurrentSolution(this.CreateSolution(SolutionId.CreateNewId()));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionRemoved, oldSolution, this.CurrentSolution);
            }
        }

        /// <summary>
        /// Call this method to respond to a project being added/opened in the host environment.
        /// </summary>
        protected internal void OnProjectAdded(ProjectInfo projectInfo)
        {
            this.OnProjectAdded(projectInfo, silent: false);
        }

        private void OnProjectAdded(ProjectInfo projectInfo, bool silent)
        {
            using (_serializationLock.DisposableWait())
            {
                this.OnProjectAdded_NoLock(projectInfo, silent);
            }
        }

        private void OnProjectAdded_NoLock(ProjectInfo projectInfo, bool silent)
        {
            var projectId = projectInfo.Id;

            CheckProjectIsNotInCurrentSolution(projectId);

            var oldSolution = this.CurrentSolution;
            var newSolution = this.SetCurrentSolution(oldSolution.AddProject(projectInfo));

            if (!silent)
            {
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectAdded, oldSolution, newSolution, projectId);
            }
        }

        /// <summary>
        /// Call this method to respond to a project being reloaded in the host environment.
        /// </summary>
        protected internal virtual void OnProjectReloaded(ProjectInfo reloadedProjectInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var projectId = reloadedProjectInfo.Id;

                CheckProjectIsInCurrentSolution(projectId);

                var oldSolution = this.CurrentSolution;
                var newSolution = oldSolution.RemoveProject(projectId).AddProject(reloadedProjectInfo);

                newSolution = this.AdjustReloadedProject(oldSolution.GetProject(projectId), newSolution.GetProject(projectId)).Solution;
                newSolution = this.SetCurrentSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectReloaded, oldSolution, newSolution, projectId);
            }
        }

        /// <summary>
        /// Call this method to respond to a project being removed from the host environment.
        /// </summary>
        protected internal virtual void OnProjectRemoved(ProjectId projectId)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                this.CheckProjectCanBeRemoved(projectId);

                var oldSolution = this.CurrentSolution;

                this.ClearProjectData(projectId);
                var newSolution = this.SetCurrentSolution(oldSolution.RemoveProject(projectId));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectRemoved, oldSolution, newSolution, projectId);
            }
        }

        /// <summary>
        /// Currently projects can always be removed, but this method still exists because it's protected and we don't
        /// want to break people who may have derived from <see cref="Workspace"/> and either called it, or overridden it.
        /// </summary>
        protected virtual void CheckProjectCanBeRemoved(ProjectId projectId)
        {
        }

        private void HandleProjectChange(ProjectId projectId, Func<Solution, Solution> getSolutionWithChangedProject)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);

                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(getSolutionWithChangedProject(oldSolution));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution, projectId);
            }
        }

        /// <summary>
        /// Call this method when a project's assembly name is changed in the host environment.
        /// </summary>
        protected internal void OnAssemblyNameChanged(ProjectId projectId, string assemblyName)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectAssemblyName(projectId, assemblyName));
        }

        /// <summary>
        /// Call this method when a project's output file path is changed in the host environment.
        /// </summary>
        protected internal void OnOutputFilePathChanged(ProjectId projectId, string? outputFilePath)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectOutputFilePath(projectId, outputFilePath));
        }

        /// <summary>
        /// Call this method when a project's output ref file path is changed in the host environment.
        /// </summary>
        protected internal void OnOutputRefFilePathChanged(ProjectId projectId, string? outputFilePath)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectOutputRefFilePath(projectId, outputFilePath));
        }

        /// <summary>
        /// Call this method when a project's name is changed in the host environment.
        /// </summary>
        // TODO (https://github.com/dotnet/roslyn/issues/37124): decide if we want to allow "name" to be nullable.
        // As of this writing you can pass null, but rather than updating the project to null it seems it does nothing.
        // I'm leaving this marked as "non-null" so as not to say we actually support that behavior. The underlying
        // requirement is ProjectInfo.ProjectAttributes holds a non-null name, so you can't get a null into this even if you tried.
        protected internal void OnProjectNameChanged(ProjectId projectId, string name, string? filePath)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectName(projectId, name).WithProjectFilePath(projectId, filePath));
        }

        /// <summary>
        /// Call this method when a project's default namespace is changed in the host environment.
        /// </summary>
        internal void OnDefaultNamespaceChanged(ProjectId projectId, string? defaultNamespace)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectDefaultNamespace(projectId, defaultNamespace));
        }

        /// <summary>
        /// Call this method when a project's compilation options are changed in the host environment.
        /// </summary>
        protected internal void OnCompilationOptionsChanged(ProjectId projectId, CompilationOptions options)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectCompilationOptions(projectId, options));
        }

        /// <summary>
        /// Call this method when a project's parse options are changed in the host environment.
        /// </summary>
        protected internal void OnParseOptionsChanged(ProjectId projectId, ParseOptions options)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithProjectParseOptions(projectId, options));
        }

        /// <summary>
        /// Call this method when a project reference is added to a project in the host environment.
        /// </summary>
        protected internal void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            this.HandleProjectChange(projectId, oldSolution =>
            {
                CheckProjectIsInCurrentSolution(projectReference.ProjectId);
                CheckProjectDoesNotHaveProjectReference(projectId, projectReference);

                // Can only add this P2P reference if it would not cause a circularity.
                CheckProjectDoesNotHaveTransitiveProjectReference(projectId, projectReference.ProjectId);

                return oldSolution.AddProjectReference(projectId, projectReference);
            });
        }

        /// <summary>
        /// Call this method when a project reference is removed from a project in the host environment.
        /// </summary>
        protected internal void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            this.HandleProjectChange(projectId, oldSolution =>
            {
                CheckProjectIsInCurrentSolution(projectReference.ProjectId);
                CheckProjectHasProjectReference(projectId, projectReference);

                return oldSolution.RemoveProjectReference(projectId, projectReference);
            });
        }

        /// <summary>
        /// Call this method when a metadata reference is added to a project in the host environment.
        /// </summary>
        protected internal void OnMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            this.HandleProjectChange(projectId, oldSolution =>
            {
                CheckProjectDoesNotHaveMetadataReference(projectId, metadataReference);
                return oldSolution.AddMetadataReference(projectId, metadataReference);
            });
        }

        /// <summary>
        /// Call this method when a metadata reference is removed from a project in the host environment.
        /// </summary>
        protected internal void OnMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            this.HandleProjectChange(projectId, oldSolution =>
            {
                CheckProjectHasMetadataReference(projectId, metadataReference);
                return oldSolution.RemoveMetadataReference(projectId, metadataReference);
            });
        }

        /// <summary>
        /// Call this method when an analyzer reference is added to a project in the host environment.
        /// </summary>
        protected internal void OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            this.HandleProjectChange(projectId, oldSolution =>
            {
                CheckProjectDoesNotHaveAnalyzerReference(projectId, analyzerReference);
                return oldSolution.AddAnalyzerReference(projectId, analyzerReference);
            });
        }

        /// <summary>
        /// Call this method when an analyzer reference is removed from a project in the host environment.
        /// </summary>
        protected internal void OnAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            this.HandleProjectChange(projectId, oldSolution =>
            {
                CheckProjectHasAnalyzerReference(projectId, analyzerReference);
                return oldSolution.RemoveAnalyzerReference(projectId, analyzerReference);
            });
        }

        /// <summary>
        /// Call this method when status of project has changed to incomplete.
        /// See <see cref="ProjectInfo.HasAllInformation"/> for more information.
        /// </summary>
        // TODO: make it public
        internal void OnHasAllInformationChanged(ProjectId projectId, bool hasAllInformation)
        {
            this.HandleProjectChange(projectId, oldSolution => oldSolution.WithHasAllInformation(projectId, hasAllInformation));
        }

        /// <summary>
        /// Call this method when a document is added to a project in the host environment.
        /// </summary>
        protected internal void OnDocumentAdded(DocumentInfo documentInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var documentId = documentInfo.Id;

                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.AddDocument(documentInfo));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentAdded, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when multiple document are added to one or more projects in the host environment.
        /// </summary>
        protected internal void OnDocumentsAdded(ImmutableArray<DocumentInfo> documentInfos)
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.AddDocuments(documentInfos));

                // Raise ProjectChanged as the event type here. DocumentAdded is presumed by many callers to have a
                // DocumentId associated with it, and we don't want to be raising multiple events.
                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.ProjectChanged, oldSolution, newSolution);
            }
        }

        /// <summary>
        /// Call this method when a document is reloaded in the host environment.
        /// </summary>
        protected internal void OnDocumentReloaded(DocumentInfo newDocumentInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var documentId = newDocumentInfo.Id;

                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.RemoveDocument(documentId).AddDocument(newDocumentInfo));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentReloaded, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when a document is removed from a project in the host environment.
        /// </summary>
        protected internal void OnDocumentRemoved(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);

                this.CheckDocumentCanBeRemoved(documentId);

                var oldSolution = this.CurrentSolution;

                this.ClearDocumentData(documentId);

                var newSolution = this.SetCurrentSolution(oldSolution.RemoveDocument(documentId));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentRemoved, oldSolution, newSolution, documentId: documentId);
            }
        }

        protected virtual void CheckDocumentCanBeRemoved(DocumentId documentId)
        {
        }

        /// <summary>
        /// Call this method when the text of a document is changed on disk.
        /// </summary>
        protected internal void OnDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;

                var newSolution = oldSolution.WithDocumentTextLoader(documentId, loader, PreservationMode.PreserveValue);
                newSolution = this.SetCurrentSolution(newSolution);

                var newDocument = newSolution.GetDocument(documentId)!;
                this.OnDocumentTextChanged(newDocument);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when the text of a additional document is changed on disk.
        /// </summary>
        protected internal void OnAdditionalDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckAdditionalDocumentIsInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;

                var newSolution = oldSolution.WithAdditionalDocumentTextLoader(documentId, loader, PreservationMode.PreserveValue);
                newSolution = this.SetCurrentSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentChanged, oldSolution, newSolution, documentId: documentId);
            }
        }


        /// <summary>
        /// Call this method when the text of a analyzer config document is changed on disk.
        /// </summary>
        protected internal void OnAnalyzerConfigDocumentTextLoaderChanged(DocumentId documentId, TextLoader loader)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckAnalyzerConfigDocumentIsInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;

                var newSolution = oldSolution.WithAnalyzerConfigDocumentTextLoader(documentId, loader, PreservationMode.PreserveValue);
                newSolution = this.SetCurrentSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AnalyzerConfigDocumentChanged, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when the document info changes, such as the name, folders or file path.
        /// </summary>
        protected internal void OnDocumentInfoChanged(DocumentId documentId, DocumentInfo newInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;

                var newSolution = oldSolution;
                var oldAttributes = oldSolution.GetDocument(documentId)!.State.Attributes;

                if (oldAttributes.Name != newInfo.Name)
                {
                    newSolution = newSolution.WithDocumentName(documentId, newInfo.Name);
                }

                if (oldAttributes.Folders != newInfo.Folders)
                {
                    newSolution = newSolution.WithDocumentFolders(documentId, newInfo.Folders);
                }

                if (oldAttributes.FilePath != newInfo.FilePath)
                {
                    newSolution = newSolution.WithDocumentFilePath(documentId, newInfo.FilePath);
                }

                if (oldAttributes.SourceCodeKind != newInfo.SourceCodeKind)
                {
                    newSolution = newSolution.WithDocumentSourceCodeKind(documentId, newInfo.SourceCodeKind);
                }

                if (newSolution != oldSolution)
                {
                    SetCurrentSolution(newSolution);

                    this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentInfoChanged, oldSolution, newSolution, documentId: documentId);
                }
            }
        }

        /// <summary>
        /// Call this method when the text of a document is updated in the host environment.
        /// </summary>
        protected internal void OnDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode)
        {
            OnAnyDocumentTextChanged(
                documentId,
                newText,
                mode,
                CheckDocumentIsInCurrentSolution,
                (solution, docId) => solution.GetRelatedDocumentIds(docId),
                (solution, docId, text, preservationMode) => solution.WithDocumentText(docId, text, preservationMode),
                WorkspaceChangeKind.DocumentChanged,
                isCodeDocument: true);
        }

        /// <summary>
        /// Call this method when the text of an additional document is updated in the host environment.
        /// </summary>
        protected internal void OnAdditionalDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode)
        {
            OnAnyDocumentTextChanged(
                documentId,
                newText,
                mode,
                CheckAdditionalDocumentIsInCurrentSolution,
                (solution, docId) => ImmutableArray.Create(docId), // We do not support the concept of linked additional documents
                (solution, docId, text, preservationMode) => solution.WithAdditionalDocumentText(docId, text, preservationMode),
                WorkspaceChangeKind.AdditionalDocumentChanged,
                isCodeDocument: false);
        }

        /// <summary>
        /// Call this method when the text of an analyzer config document is updated in the host environment.
        /// </summary>
        protected internal void OnAnalyzerConfigDocumentTextChanged(DocumentId documentId, SourceText newText, PreservationMode mode)
        {
            OnAnyDocumentTextChanged(
                documentId,
                newText,
                mode,
                CheckAnalyzerConfigDocumentIsInCurrentSolution,
                (solution, docId) => ImmutableArray.Create(docId), // We do not support the concept of linked additional documents
                (solution, docId, text, preservationMode) => solution.WithAnalyzerConfigDocumentText(docId, text, preservationMode),
                WorkspaceChangeKind.AnalyzerConfigDocumentChanged,
                isCodeDocument: false);
        }

        /// <summary>
        /// When a <see cref="Document"/>s text is changed, we need to make sure all of the linked
        /// files also have their content updated in the new solution before applying it to the
        /// workspace to avoid the workspace having solutions with linked files where the contents
        /// do not match.
        /// </summary>
        private void OnAnyDocumentTextChanged(
            DocumentId documentId,
            SourceText newText,
            PreservationMode mode,
            Action<DocumentId> checkIsInCurrentSolution,
            Func<Solution, DocumentId, ImmutableArray<DocumentId>> getRelatedDocuments,
            Func<Solution, DocumentId, SourceText, PreservationMode, Solution> updateSolutionWithText,
            WorkspaceChangeKind changeKind,
            bool isCodeDocument)
        {
            using (_serializationLock.DisposableWait())
            {
                checkIsInCurrentSolution(documentId);

                var originalSolution = CurrentSolution;
                var updatedSolution = CurrentSolution;
                var previousSolution = updatedSolution;

                var linkedDocuments = getRelatedDocuments(updatedSolution, documentId);
                var updatedDocumentIds = new List<DocumentId>();

                foreach (var linkedDocument in linkedDocuments)
                {
                    previousSolution = updatedSolution;
                    updatedSolution = updateSolutionWithText(updatedSolution, linkedDocument, newText, mode);
                    if (previousSolution != updatedSolution)
                    {
                        updatedDocumentIds.Add(linkedDocument);
                    }
                }

                // In the case of linked files, we may have already updated all of the linked
                // documents during an earlier call to this method. We may have no work to do here.
                if (updatedDocumentIds.Count > 0)
                {
                    var newSolution = SetCurrentSolution(updatedSolution);

                    // Prior to the unification of the callers of this method, the
                    // OnAdditionalDocumentTextChanged method did not fire any sort of synchronous
                    // update notification event, so we preserve that behavior here.
                    if (isCodeDocument)
                    {
                        foreach (var updatedDocumentId in updatedDocumentIds)
                        {
                            var newDocument = newSolution.GetDocument(updatedDocumentId);
                            Contract.ThrowIfNull(newDocument);
                            OnDocumentTextChanged(newDocument);
                        }
                    }

                    foreach (var updatedDocumentInfo in updatedDocumentIds)
                    {
                        RaiseWorkspaceChangedEventAsync(
                            changeKind,
                            originalSolution,
                            newSolution,
                            documentId: updatedDocumentInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Call this method when the SourceCodeKind of a document changes in the host environment.
        /// </summary>
        protected internal void OnDocumentSourceCodeKindChanged(DocumentId documentId, SourceCodeKind sourceCodeKind)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.WithDocumentSourceCodeKind(documentId, sourceCodeKind));

                var newDocument = newSolution.GetDocument(documentId)!;
                this.OnDocumentTextChanged(newDocument);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.DocumentChanged, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when an additional document is added to a project in the host environment.
        /// </summary>
        protected internal void OnAdditionalDocumentAdded(DocumentInfo documentInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var documentId = documentInfo.Id;

                CheckProjectIsInCurrentSolution(documentId.ProjectId);
                CheckAdditionalDocumentIsNotInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.AddAdditionalDocument(documentInfo));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentAdded, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when an additional document is removed from a project in the host environment.
        /// </summary>
        protected internal void OnAdditionalDocumentRemoved(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckAdditionalDocumentIsInCurrentSolution(documentId);

                this.CheckDocumentCanBeRemoved(documentId);

                var oldSolution = this.CurrentSolution;

                this.ClearDocumentData(documentId);

                var newSolution = this.SetCurrentSolution(oldSolution.RemoveAdditionalDocument(documentId));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AdditionalDocumentRemoved, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when an analyzer config document is added to a project in the host environment.
        /// </summary>
        protected internal void OnAnalyzerConfigDocumentAdded(DocumentInfo documentInfo)
        {
            using (_serializationLock.DisposableWait())
            {
                var documentId = documentInfo.Id;

                CheckProjectIsInCurrentSolution(documentId.ProjectId);
                CheckAnalyzerConfigDocumentIsNotInCurrentSolution(documentId);

                var oldSolution = this.CurrentSolution;
                var newSolution = this.SetCurrentSolution(oldSolution.AddAnalyzerConfigDocuments(ImmutableArray.Create(documentInfo)));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AnalyzerConfigDocumentAdded, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Call this method when an analyzer config document is removed from a project in the host environment.
        /// </summary>
        protected internal void OnAnalyzerConfigDocumentRemoved(DocumentId documentId)
        {
            using (_serializationLock.DisposableWait())
            {
                CheckAnalyzerConfigDocumentIsInCurrentSolution(documentId);

                this.CheckDocumentCanBeRemoved(documentId);

                var oldSolution = this.CurrentSolution;

                this.ClearDocumentData(documentId);

                var newSolution = this.SetCurrentSolution(oldSolution.RemoveAnalyzerConfigDocument(documentId));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.AnalyzerConfigDocumentRemoved, oldSolution, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Updates all projects to properly reference other projects as project references instead of metadata references.
        /// </summary>
        protected void UpdateReferencesAfterAdd()
        {
            using (_serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var newSolution = UpdateReferencesAfterAdd(oldSolution);

                if (newSolution != oldSolution)
                {
                    newSolution = this.SetCurrentSolution(newSolution);
                    var ignore = this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
                }
            }
        }

        [System.Diagnostics.Contracts.Pure]
        private static Solution UpdateReferencesAfterAdd(Solution solution)
        {
            // Build map from output assembly path to ProjectId
            // Use explicit loop instead of ToDictionary so we don't throw if multiple projects have same output assembly path.
            var outputAssemblyToProjectIdMap = new Dictionary<string, ProjectId>();
            foreach (var p in solution.Projects)
            {
                if (!string.IsNullOrEmpty(p.OutputFilePath))
                {
                    outputAssemblyToProjectIdMap[p.OutputFilePath!] = p.Id;
                }

                if (!string.IsNullOrEmpty(p.OutputRefFilePath))
                {
                    outputAssemblyToProjectIdMap[p.OutputRefFilePath!] = p.Id;
                }
            }

            // now fix each project if necessary
            foreach (var pid in solution.ProjectIds)
            {
                var project = solution.GetProject(pid)!;

                // convert metadata references to project references if the metadata reference matches some project's output assembly.
                foreach (var meta in project.MetadataReferences)
                {
                    if (meta is PortableExecutableReference pemeta)
                    {

                        // check both Display and FilePath. FilePath points to the actually bits, but Display should match output path if
                        // the metadata reference is shadow copied.
                        if ((!string.IsNullOrEmpty(pemeta.Display) && outputAssemblyToProjectIdMap.TryGetValue(pemeta.Display, out var matchingProjectId)) ||
                            (!string.IsNullOrEmpty(pemeta.FilePath) && outputAssemblyToProjectIdMap.TryGetValue(pemeta.FilePath, out matchingProjectId)))
                        {
                            var newProjRef = new ProjectReference(matchingProjectId, pemeta.Properties.Aliases, pemeta.Properties.EmbedInteropTypes);

                            if (!project.ProjectReferences.Contains(newProjRef))
                            {
                                project = project.AddProjectReference(newProjRef);
                            }

                            project = project.RemoveMetadataReference(meta);
                        }
                    }
                }

                solution = project.Solution;
            }

            return solution;
        }

        #endregion

        #region Apply Changes

        internal virtual bool CanRenameFilesDuringCodeActions(Project project) => true;

        /// <summary>
        /// Determines if the specific kind of change is supported by the <see cref="TryApplyChanges(Solution)"/> method.
        /// </summary>
        public virtual bool CanApplyChange(ApplyChangesKind feature)
        {
            return false;
        }

        /// <summary>
        /// Returns <see langword="true"/> if a reference to referencedProject can be added to
        /// referencingProject.  <see langword="false"/> otherwise.
        /// </summary>
        internal virtual bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
        {
            return false;
        }

        internal async Task<Solution> ExcludeDisallowedDocumentTextChangesAsync(Solution newSolution, CancellationToken cancellationToken)
        {
            var oldSolution = this.CurrentSolution;
            var solutionChanges = newSolution.GetChanges(oldSolution);

            foreach (var projectChange in solutionChanges.GetProjectChanges())
            {
                foreach (var changedDocumentId in projectChange.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                {
                    var oldDocument = oldSolution.GetDocument(changedDocumentId)!;
                    if (oldDocument.CanApplyChange())
                    {
                        continue;
                    }

                    var oldRoot = await oldDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var newDocument = newSolution.GetDocument(changedDocumentId)!;
                    var revertedDocument = newDocument.WithSyntaxRoot(oldRoot);

                    newSolution = revertedDocument.Project.Solution;
                }
            }

            return newSolution;
        }

        /// <summary>
        /// Apply changes made to a solution back to the workspace.
        ///
        /// The specified solution must be one that originated from this workspace. If it is not, or the workspace
        /// has been updated since the solution was obtained from the workspace, then this method returns false. This method
        /// will still throw if the solution contains changes that are not supported according to the <see cref="CanApplyChange(ApplyChangesKind)"/>
        /// method.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if the solution contains changes not supported according to the
        /// <see cref="CanApplyChange(ApplyChangesKind)"/> method.</exception>
        public virtual bool TryApplyChanges(Solution newSolution)
        {
            return TryApplyChanges(newSolution, new ProgressTracker());
        }

        internal virtual bool TryApplyChanges(Solution newSolution, IProgressTracker progressTracker)
        {
            using (Logger.LogBlock(FunctionId.Workspace_ApplyChanges, CancellationToken.None))
            {
                // If solution did not originate from this workspace then fail
                if (newSolution.Workspace != this)
                {
                    return false;
                }

                var oldSolution = this.CurrentSolution;

                // If the workspace has already accepted an update, then fail
                if (newSolution.WorkspaceVersion != oldSolution.WorkspaceVersion)
                {
                    return false;
                }

                // make sure that newSolution is a branch of the current solution

                // the given solution must be a branched one.
                // otherwise, there should be no change to apply.
                if (oldSolution.BranchId == newSolution.BranchId)
                {
                    CheckNoChanges(oldSolution, newSolution);
                    return true;
                }

                var solutionChanges = newSolution.GetChanges(oldSolution);
                this.CheckAllowedSolutionChanges(solutionChanges);

                var solutionWithLinkedFileChangesMerged = newSolution.WithMergedLinkedFileChangesAsync(oldSolution, solutionChanges, cancellationToken: CancellationToken.None).Result;
                solutionChanges = solutionWithLinkedFileChangesMerged.GetChanges(oldSolution);

                // added projects
                foreach (var proj in solutionChanges.GetAddedProjects())
                {
                    this.ApplyProjectAdded(this.CreateProjectInfo(proj));
                }

                // changed projects
                var projectChangesList = solutionChanges.GetProjectChanges().ToList();
                progressTracker.AddItems(projectChangesList.Count);

                foreach (var projectChanges in projectChangesList)
                {
                    this.ApplyProjectChanges(projectChanges);
                    progressTracker.ItemCompleted();
                }

                // removed projects
                foreach (var proj in solutionChanges.GetRemovedProjects())
                {
                    this.ApplyProjectRemoved(proj.Id);
                }

                return true;
            }
        }

        private void CheckAllowedSolutionChanges(SolutionChanges solutionChanges)
        {
            if (solutionChanges.GetRemovedProjects().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveProject))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_projects_is_not_supported);
            }

            if (solutionChanges.GetAddedProjects().Any() && !this.CanApplyChange(ApplyChangesKind.AddProject))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_projects_is_not_supported);
            }

            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                this.CheckAllowedProjectChanges(projectChanges);
            }
        }

        private void CheckAllowedProjectChanges(ProjectChanges projectChanges)
        {
            // It's OK to use the null-suppression operator when calling CanApplyCompilationOptionChange: if they were both null,
            // we'd bail right away since they didn't change. Thus, at least one is non-null, and once you have a non-null CompilationOptions and ParseOptions
            // you can't ever make it null again, and it'll be non-null as long as the language supported it in the first place.
            if (projectChanges.OldProject.CompilationOptions != projectChanges.NewProject.CompilationOptions
                && !this.CanApplyChange(ApplyChangesKind.ChangeCompilationOptions)
                && !this.CanApplyCompilationOptionChange(
                    projectChanges.OldProject.CompilationOptions!, projectChanges.NewProject.CompilationOptions!, projectChanges.NewProject))
            {
                throw new NotSupportedException(WorkspacesResources.Changing_compilation_options_is_not_supported);
            }

            if (projectChanges.OldProject.ParseOptions != projectChanges.NewProject.ParseOptions
                && !this.CanApplyChange(ApplyChangesKind.ChangeParseOptions)
                && !this.CanApplyParseOptionChange(
                    projectChanges.OldProject.ParseOptions!, projectChanges.NewProject.ParseOptions!, projectChanges.NewProject))
            {
                throw new NotSupportedException(WorkspacesResources.Changing_parse_options_is_not_supported);
            }

            if (projectChanges.GetAddedDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.AddDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_documents_is_not_supported);
            }

            if (projectChanges.GetRemovedDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_documents_is_not_supported);
            }

            if (!this.CanApplyChange(ApplyChangesKind.ChangeDocumentInfo)
                && projectChanges.GetChangedDocuments().Any(id => projectChanges.NewProject.GetDocument(id)!.HasInfoChanged(projectChanges.OldProject.GetDocument(id))))
            {
                throw new NotSupportedException(WorkspacesResources.Changing_document_property_is_not_supported);
            }

            if (!this.CanApplyChange(ApplyChangesKind.ChangeDocument)
                && projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).Any())
            {
                throw new NotSupportedException(WorkspacesResources.Changing_documents_is_not_supported);
            }

            if (!this.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                var documentsWithTextChanges = projectChanges.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).ToImmutableArray();
                if (!documentsWithTextChanges.IsEmpty)
                {
                    throw new NotSupportedException(WorkspacesResources.Changing_documents_is_not_supported);
                }

                foreach (var changedDocumentId in documentsWithTextChanges)
                {
                    var document = projectChanges.OldProject.GetDocumentState(changedDocumentId) ?? projectChanges.NewProject.GetDocumentState(changedDocumentId)!;
                    if (!document.CanApplyChange())
                    {
                        throw new NotSupportedException(string.Format(WorkspacesResources.Changing_document_0_is_not_supported, document.FilePath ?? document.Name));
                    }
                }
            }

            if (projectChanges.GetAddedAdditionalDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.AddAdditionalDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_additional_documents_is_not_supported);
            }

            if (projectChanges.GetRemovedAdditionalDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveAdditionalDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_additional_documents_is_not_supported);
            }

            if (projectChanges.GetChangedAdditionalDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.ChangeAdditionalDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Changing_additional_documents_is_not_supported);
            }

            if (projectChanges.GetAddedAnalyzerConfigDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.AddAnalyzerConfigDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_analyzer_config_documents_is_not_supported);
            }

            if (projectChanges.GetRemovedAnalyzerConfigDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveAnalyzerConfigDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_analyzer_config_documents_is_not_supported);
            }

            if (projectChanges.GetChangedAnalyzerConfigDocuments().Any() && !this.CanApplyChange(ApplyChangesKind.ChangeAnalyzerConfigDocument))
            {
                throw new NotSupportedException(WorkspacesResources.Changing_analyzer_config_documents_is_not_supported);
            }

            if (projectChanges.GetAddedProjectReferences().Any() && !this.CanApplyChange(ApplyChangesKind.AddProjectReference))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_project_references_is_not_supported);
            }

            if (projectChanges.GetRemovedProjectReferences().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveProjectReference))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_project_references_is_not_supported);
            }

            if (projectChanges.GetAddedMetadataReferences().Any() && !this.CanApplyChange(ApplyChangesKind.AddMetadataReference))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_project_references_is_not_supported);
            }

            if (projectChanges.GetRemovedMetadataReferences().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveMetadataReference))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_project_references_is_not_supported);
            }

            if (projectChanges.GetAddedAnalyzerReferences().Any() && !this.CanApplyChange(ApplyChangesKind.AddAnalyzerReference))
            {
                throw new NotSupportedException(WorkspacesResources.Adding_analyzer_references_is_not_supported);
            }

            if (projectChanges.GetRemovedAnalyzerReferences().Any() && !this.CanApplyChange(ApplyChangesKind.RemoveAnalyzerReference))
            {
                throw new NotSupportedException(WorkspacesResources.Removing_analyzer_references_is_not_supported);
            }
        }

        protected virtual bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project)
            => false;

        public virtual bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project)
            => false;

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> for each project
        /// that has been added, removed or changed.
        ///
        /// Override this method if you want to modify how project changes are applied.
        /// </summary>
        protected virtual void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            // It's OK to use the null-suppression operator when calling ApplyCompilation/ParseOptionsChanged: the only change that is allowed
            // is going from one non-null value to another which is blocked by the Project.WithCompilationOptions() API directly.

            // changed compilation options
            if (projectChanges.OldProject.CompilationOptions != projectChanges.NewProject.CompilationOptions)
            {
                this.ApplyCompilationOptionsChanged(projectChanges.ProjectId, projectChanges.NewProject.CompilationOptions!);
            }

            // changed parse options
            if (projectChanges.OldProject.ParseOptions != projectChanges.NewProject.ParseOptions)
            {
                this.ApplyParseOptionsChanged(projectChanges.ProjectId, projectChanges.NewProject.ParseOptions!);
            }

            // removed project references
            foreach (var removedProjectReference in projectChanges.GetRemovedProjectReferences())
            {
                this.ApplyProjectReferenceRemoved(projectChanges.ProjectId, removedProjectReference);
            }

            // added project references
            foreach (var addedProjectReference in projectChanges.GetAddedProjectReferences())
            {
                this.ApplyProjectReferenceAdded(projectChanges.ProjectId, addedProjectReference);
            }

            // removed metadata references
            foreach (var metadata in projectChanges.GetRemovedMetadataReferences())
            {
                this.ApplyMetadataReferenceRemoved(projectChanges.ProjectId, metadata);
            }

            // added metadata references
            foreach (var metadata in projectChanges.GetAddedMetadataReferences())
            {
                this.ApplyMetadataReferenceAdded(projectChanges.ProjectId, metadata);
            }

            // removed analyzer references
            foreach (var analyzerReference in projectChanges.GetRemovedAnalyzerReferences())
            {
                this.ApplyAnalyzerReferenceRemoved(projectChanges.ProjectId, analyzerReference);
            }

            // added analyzer references
            foreach (var analyzerReference in projectChanges.GetAddedAnalyzerReferences())
            {
                this.ApplyAnalyzerReferenceAdded(projectChanges.ProjectId, analyzerReference);
            }

            // removed documents
            foreach (var documentId in projectChanges.GetRemovedDocuments())
            {
                this.ApplyDocumentRemoved(documentId);
            }

            // removed additional documents
            foreach (var documentId in projectChanges.GetRemovedAdditionalDocuments())
            {
                this.ApplyAdditionalDocumentRemoved(documentId);
            }

            // removed analyzer config documents
            foreach (var documentId in projectChanges.GetRemovedAnalyzerConfigDocuments())
            {
                this.ApplyAnalyzerConfigDocumentRemoved(documentId);
            }

            // added documents
            foreach (var documentId in projectChanges.GetAddedDocuments())
            {
                var document = projectChanges.NewProject.GetDocument(documentId)!;
                var text = document.GetTextSynchronously(CancellationToken.None);
                var info = this.CreateDocumentInfoWithoutText(document);
                this.ApplyDocumentAdded(info, text);
            }

            // added additional documents
            foreach (var documentId in projectChanges.GetAddedAdditionalDocuments())
            {
                var document = projectChanges.NewProject.GetAdditionalDocument(documentId)!;
                var text = document.GetTextSynchronously(CancellationToken.None);
                var info = this.CreateDocumentInfoWithoutText(document);
                this.ApplyAdditionalDocumentAdded(info, text);
            }

            // added analyzer config documents
            foreach (var documentId in projectChanges.GetAddedAnalyzerConfigDocuments())
            {
                var document = projectChanges.NewProject.GetAnalyzerConfigDocument(documentId)!;
                var text = document.GetTextSynchronously(CancellationToken.None);
                var info = this.CreateDocumentInfoWithoutText(document);
                this.ApplyAnalyzerConfigDocumentAdded(info, text);
            }

            // changed documents
            foreach (var documentId in projectChanges.GetChangedDocuments())
            {
                ApplyChangedDocument(projectChanges, documentId);
            }

            // changed additional documents
            foreach (var documentId in projectChanges.GetChangedAdditionalDocuments())
            {
                var oldDoc = projectChanges.OldProject.GetAdditionalDocument(documentId)!;
                var newDoc = projectChanges.NewProject.GetAdditionalDocument(documentId)!;

                // We don't understand the text of additional documents and so we just replace the entire text.
                var currentText = newDoc.GetTextSynchronously(CancellationToken.None); // needs wait
                this.ApplyAdditionalDocumentTextChanged(documentId, currentText);
            }

            // changed analyzer config documents
            foreach (var documentId in projectChanges.GetChangedAnalyzerConfigDocuments())
            {
                var oldDoc = projectChanges.OldProject.GetAnalyzerConfigDocument(documentId)!;
                var newDoc = projectChanges.NewProject.GetAnalyzerConfigDocument(documentId)!;

                // We don't understand the text of analyzer config documents and so we just replace the entire text.
                var currentText = newDoc.GetTextSynchronously(CancellationToken.None); // needs wait
                this.ApplyAnalyzerConfigDocumentTextChanged(documentId, currentText);
            }
        }

        private void ApplyChangedDocument(
            ProjectChanges projectChanges, DocumentId documentId)
        {
            var oldDoc = projectChanges.OldProject.GetDocument(documentId)!;
            var newDoc = projectChanges.NewProject.GetDocument(documentId)!;

            // update text if changed
            if (newDoc.HasTextChanged(oldDoc))
            {
                // What we'd like to do here is figure out what actual text changes occurred and pass them on to the host.
                // However, since it is likely that the change was done by replacing the syntax tree, getting the actual text changes is non trivial.

                if (!oldDoc.TryGetText(out var oldText))
                {
                    // If we don't have easy access to the old text, then either it was never observed or it was kicked out of memory.
                    // Either way, the new text cannot possibly hold knowledge of the changes, and any new syntax tree will not likely be able to derive them.
                    // So just use whatever new text we have without preserving text changes.
                    var currentText = newDoc.GetTextSynchronously(CancellationToken.None); // needs wait
                    this.ApplyDocumentTextChanged(documentId, currentText);
                }
                else if (!newDoc.TryGetText(out var newText))
                {
                    // We have the old text, but no new text is easily available. This typically happens when the content is modified via changes to the syntax tree.
                    // Ask document to compute equivalent text changes by comparing the syntax trees, and use them to
                    var textChanges = newDoc.GetTextChangesAsync(oldDoc, CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None); // needs wait
                    this.ApplyDocumentTextChanged(documentId, oldText.WithChanges(textChanges));
                }
                else
                {
                    // We have both old and new text, so assume the text was changed manually.
                    // So either the new text already knows the individual changes or we do not have a way to compute them.
                    this.ApplyDocumentTextChanged(documentId, newText);
                }
            }

            // Update document info if changed. Updating the info can cause files to move on disk (or have other side effects),
            // so we do this after any text changes have been applied.
            if (newDoc.HasInfoChanged(oldDoc))
            {
                // ApplyDocumentInfoChanged ignores the loader information, so we can pass null for it
                ApplyDocumentInfoChanged(
                    documentId,
                    new DocumentInfo(newDoc.State.Attributes, loader: null, documentServiceProvider: newDoc.State.Services));
            }
        }

        [Conditional("DEBUG")]
        private void CheckNoChanges(Solution oldSolution, Solution newSolution)
        {
            var changes = newSolution.GetChanges(oldSolution);
            Contract.ThrowIfTrue(changes.GetAddedProjects().Any());
            Contract.ThrowIfTrue(changes.GetRemovedProjects().Any());
            Contract.ThrowIfTrue(changes.GetProjectChanges().Any());
        }

        private ProjectInfo CreateProjectInfo(Project project)
        {
            return ProjectInfo.Create(
                project.Id,
                VersionStamp.Create(),
                project.Name,
                project.AssemblyName,
                project.Language,
                project.FilePath,
                project.OutputFilePath,
                project.CompilationOptions,
                project.ParseOptions,
                project.Documents.Select(d => CreateDocumentInfoWithText(d)),
                project.ProjectReferences,
                project.MetadataReferences,
                project.AnalyzerReferences,
                project.AdditionalDocuments.Select(d => CreateDocumentInfoWithText(d)),
                project.IsSubmission,
                project.State.HostObjectType,
                project.OutputRefFilePath)
                .WithDefaultNamespace(project.DefaultNamespace)
                .WithAnalyzerConfigDocuments(project.AnalyzerConfigDocuments.Select(d => CreateDocumentInfoWithText(d)));
        }

        private DocumentInfo CreateDocumentInfoWithText(TextDocument doc)
        {
            return CreateDocumentInfoWithoutText(doc).WithTextLoader(TextLoader.From(TextAndVersion.Create(doc.GetTextSynchronously(CancellationToken.None), VersionStamp.Create(), doc.FilePath)));
        }

        internal DocumentInfo CreateDocumentInfoWithoutText(TextDocument doc)
        {
            var sourceDoc = doc as Document;
            return DocumentInfo.Create(
                doc.Id,
                doc.Name,
                doc.Folders,
                sourceDoc != null ? sourceDoc.SourceCodeKind : SourceCodeKind.Regular,
                filePath: doc.FilePath);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a project to the current solution.
        ///
        /// Override this method to implement the capability of adding projects.
        /// </summary>
        protected virtual void ApplyProjectAdded(ProjectInfo project)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddProject));
            this.OnProjectAdded(project);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a project from the current solution.
        ///
        /// Override this method to implement the capability of removing projects.
        /// </summary>
        protected virtual void ApplyProjectRemoved(ProjectId projectId)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveProject));
            this.OnProjectRemoved(projectId);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to change the compilation options.
        ///
        /// Override this method to implement the capability of changing compilation options.
        /// </summary>
        protected virtual void ApplyCompilationOptionsChanged(ProjectId projectId, CompilationOptions options)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeCompilationOptions));
            this.OnCompilationOptionsChanged(projectId, options);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to change the parse options.
        ///
        /// Override this method to implement the capability of changing parse options.
        /// </summary>
        protected virtual void ApplyParseOptionsChanged(ProjectId projectId, ParseOptions options)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeParseOptions));
            this.OnParseOptionsChanged(projectId, options);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a project reference to a project.
        ///
        /// Override this method to implement the capability of adding project references.
        /// </summary>
        protected virtual void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddProjectReference));
            this.OnProjectReferenceAdded(projectId, projectReference);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a project reference from a project.
        ///
        /// Override this method to implement the capability of removing project references.
        /// </summary>
        protected virtual void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveProjectReference));
            this.OnProjectReferenceRemoved(projectId, projectReference);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a metadata reference to a project.
        ///
        /// Override this method to implement the capability of adding metadata references.
        /// </summary>
        protected virtual void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddMetadataReference));
            this.OnMetadataReferenceAdded(projectId, metadataReference);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a metadata reference from a project.
        ///
        /// Override this method to implement the capability of removing metadata references.
        /// </summary>
        protected virtual void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveMetadataReference));
            this.OnMetadataReferenceRemoved(projectId, metadataReference);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add an analyzer reference to a project.
        ///
        /// Override this method to implement the capability of adding analyzer references.
        /// </summary>
        protected virtual void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddAnalyzerReference));
            this.OnAnalyzerReferenceAdded(projectId, analyzerReference);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an analyzer reference from a project.
        ///
        /// Override this method to implement the capability of removing analyzer references.
        /// </summary>
        protected virtual void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveAnalyzerReference));
            this.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a new document to a project.
        ///
        /// Override this method to implement the capability of adding documents.
        /// </summary>
        protected virtual void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddDocument));
            this.OnDocumentAdded(info.WithTextLoader(TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()))));
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove a document from a project.
        ///
        /// Override this method to implement the capability of removing documents.
        /// </summary>
        protected virtual void ApplyDocumentRemoved(DocumentId documentId)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveDocument));
            this.OnDocumentRemoved(documentId);
        }

        /// <summary>
        /// This method is called to change the text of a document.
        ///
        /// Override this method to implement the capability of changing document text.
        /// </summary>
        protected virtual void ApplyDocumentTextChanged(DocumentId id, SourceText text)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeDocument));
            this.OnDocumentTextChanged(id, text, PreservationMode.PreserveValue);
        }

        /// <summary>
        /// This method is called to change the info of a document.
        ///
        /// Override this method to implement the capability of changing a document's info.
        /// </summary>
        protected virtual void ApplyDocumentInfoChanged(DocumentId id, DocumentInfo info)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeDocumentInfo));
            this.OnDocumentInfoChanged(id, info);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a new additional document to a project.
        ///
        /// Override this method to implement the capability of adding additional documents.
        /// </summary>
        protected virtual void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddAdditionalDocument));
            this.OnAdditionalDocumentAdded(info.WithTextLoader(TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()))));
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an additional document from a project.
        ///
        /// Override this method to implement the capability of removing additional documents.
        /// </summary>
        protected virtual void ApplyAdditionalDocumentRemoved(DocumentId documentId)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveAdditionalDocument));
            this.OnAdditionalDocumentRemoved(documentId);
        }

        /// <summary>
        /// This method is called to change the text of an additional document.
        ///
        /// Override this method to implement the capability of changing additional document text.
        /// </summary>
        protected virtual void ApplyAdditionalDocumentTextChanged(DocumentId id, SourceText text)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeAdditionalDocument));
            this.OnAdditionalDocumentTextChanged(id, text, PreservationMode.PreserveValue);
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to add a new analyzer config document to a project.
        ///
        /// Override this method to implement the capability of adding analyzer config documents.
        /// </summary>
        protected virtual void ApplyAnalyzerConfigDocumentAdded(DocumentInfo info, SourceText text)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.AddAnalyzerConfigDocument));
            this.OnAnalyzerConfigDocumentAdded(info.WithTextLoader(TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()))));
        }

        /// <summary>
        /// This method is called during <see cref="TryApplyChanges(Solution)"/> to remove an analyzer config document from a project.
        ///
        /// Override this method to implement the capability of removing analyzer config documents.
        /// </summary>
        protected virtual void ApplyAnalyzerConfigDocumentRemoved(DocumentId documentId)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.RemoveAnalyzerConfigDocument));
            this.OnAnalyzerConfigDocumentRemoved(documentId);
        }

        /// <summary>
        /// This method is called to change the text of an analyzer config document.
        ///
        /// Override this method to implement the capability of changing analyzer config document text.
        /// </summary>
        protected virtual void ApplyAnalyzerConfigDocumentTextChanged(DocumentId id, SourceText text)
        {
            Debug.Assert(CanApplyChange(ApplyChangesKind.ChangeAnalyzerConfigDocument));
            this.OnAnalyzerConfigDocumentTextLoaderChanged(id, TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())));
        }

        #endregion

        #region Checks and Asserts
        /// <summary>
        /// Throws an exception is the solution is not empty.
        /// </summary>
        protected void CheckSolutionIsEmpty()
        {
            if (this.CurrentSolution.ProjectIds.Any())
            {
                throw new ArgumentException(WorkspacesResources.Workspace_is_not_empty);
            }
        }

        /// <summary>
        /// Throws an exception if the project is not part of the current solution.
        /// </summary>
        protected void CheckProjectIsInCurrentSolution(ProjectId projectId)
        {
            if (!this.CurrentSolution.ContainsProject(projectId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_not_part_of_the_workspace,
                    this.GetProjectName(projectId)));
            }
        }

        /// <summary>
        /// Throws an exception is the project is part of the current solution.
        /// </summary>
        protected void CheckProjectIsNotInCurrentSolution(ProjectId projectId)
        {
            if (this.CurrentSolution.ContainsProject(projectId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_already_part_of_the_workspace,
                    this.GetProjectName(projectId)));
            }
        }

        /// <summary>
        /// Throws an exception if a project does not have a specific project reference.
        /// </summary>
        protected void CheckProjectHasProjectReference(ProjectId fromProjectId, ProjectReference projectReference)
        {
            if (!this.CurrentSolution.GetProject(fromProjectId)!.ProjectReferences.Contains(projectReference))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_not_referenced,
                    this.GetProjectName(projectReference.ProjectId)));
            }
        }

        /// <summary>
        /// Throws an exception if a project already has a specific project reference.
        /// </summary>
        protected void CheckProjectDoesNotHaveProjectReference(ProjectId fromProjectId, ProjectReference projectReference)
        {
            if (this.CurrentSolution.GetProject(fromProjectId)!.ProjectReferences.Contains(projectReference))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_already_referenced,
                    this.GetProjectName(projectReference.ProjectId)));
            }
        }

        /// <summary>
        /// Throws an exception if project has a transitive reference to another project.
        /// </summary>
        protected void CheckProjectDoesNotHaveTransitiveProjectReference(ProjectId fromProjectId, ProjectId toProjectId)
        {
            var transitiveReferences = this.CurrentSolution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(toProjectId);
            if (transitiveReferences.Contains(fromProjectId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources.Adding_project_reference_from_0_to_1_will_cause_a_circular_reference,
                    this.GetProjectName(fromProjectId), this.GetProjectName(toProjectId)));
            }
        }

        /// <summary>
        /// Throws an exception if a project does not have a specific metadata reference.
        /// </summary>
        protected void CheckProjectHasMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            if (!this.CurrentSolution.GetProject(projectId)!.MetadataReferences.Contains(metadataReference))
            {
                throw new ArgumentException(WorkspacesResources.Metadata_is_not_referenced);
            }
        }

        /// <summary>
        /// Throws an exception if a project already has a specific metadata reference.
        /// </summary>
        protected void CheckProjectDoesNotHaveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            if (this.CurrentSolution.GetProject(projectId)!.MetadataReferences.Contains(metadataReference))
            {
                throw new ArgumentException(WorkspacesResources.Metadata_is_already_referenced);
            }
        }

        /// <summary>
        /// Throws an exception if a project does not have a specific analyzer reference.
        /// </summary>
        protected void CheckProjectHasAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            if (!this.CurrentSolution.GetProject(projectId)!.AnalyzerReferences.Contains(analyzerReference))
            {
                throw new ArgumentException(string.Format(WorkspacesResources._0_is_not_present, analyzerReference));
            }
        }

        /// <summary>
        /// Throws an exception if a project already has a specific analyzer reference.
        /// </summary>
        protected void CheckProjectDoesNotHaveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            if (this.CurrentSolution.GetProject(projectId)!.AnalyzerReferences.Contains(analyzerReference))
            {
                throw new ArgumentException(string.Format(WorkspacesResources._0_is_already_present, analyzerReference));
            }
        }

        /// <summary>
        /// Throws an exception if a document is not part of the current solution.
        /// </summary>
        protected void CheckDocumentIsInCurrentSolution(DocumentId documentId)
        {
            if (this.CurrentSolution.GetDocument(documentId) == null)
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_not_part_of_the_workspace,
                    this.GetDocumentName(documentId)));
            }
        }

        /// <summary>
        /// Throws an exception if an additional document is not part of the current solution.
        /// </summary>
        protected void CheckAdditionalDocumentIsInCurrentSolution(DocumentId documentId)
        {
            if (this.CurrentSolution.GetAdditionalDocument(documentId) == null)
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_not_part_of_the_workspace,
                    this.GetDocumentName(documentId)));
            }
        }

        /// <summary>
        /// Throws an exception if an analyzer config is not part of the current solution.
        /// </summary>
        protected void CheckAnalyzerConfigDocumentIsInCurrentSolution(DocumentId documentId)
        {
            if (!this.CurrentSolution.ContainsAnalyzerConfigDocument(documentId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_not_part_of_the_workspace,
                    this.GetDocumentName(documentId)));
            }
        }

        /// <summary>
        /// Throws an exception if a document is already part of the current solution.
        /// </summary>
        protected void CheckDocumentIsNotInCurrentSolution(DocumentId documentId)
        {
            if (this.CurrentSolution.ContainsDocument(documentId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_already_part_of_the_workspace,
                    this.GetDocumentName(documentId)));
            }
        }

        /// <summary>
        /// Throws an exception if an additional document is already part of the current solution.
        /// </summary>
        protected void CheckAdditionalDocumentIsNotInCurrentSolution(DocumentId documentId)
        {
            if (this.CurrentSolution.ContainsAdditionalDocument(documentId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_already_part_of_the_workspace,
                    this.GetAdditionalDocumentName(documentId)));
            }
        }

        /// <summary>
        /// Throws an exception if the analyzer config document is already part of the current solution.
        /// </summary>
        protected void CheckAnalyzerConfigDocumentIsNotInCurrentSolution(DocumentId documentId)
        {
            if (this.CurrentSolution.ContainsAnalyzerConfigDocument(documentId))
            {
                throw new ArgumentException(string.Format(
                    WorkspacesResources._0_is_already_part_of_the_workspace,
                    this.GetAnalyzerConfigDocumentName(documentId)));
            }
        }

        /// <summary>
        /// Gets the name to use for a project in an error message.
        /// </summary>
        protected virtual string GetProjectName(ProjectId projectId)
        {
            var project = this.CurrentSolution.GetProject(projectId);
            var name = project != null ? project.Name : "<Project" + projectId.Id + ">";
            return name;
        }

        /// <summary>
        /// Gets the name to use for a document in an error message.
        /// </summary>
        protected virtual string GetDocumentName(DocumentId documentId)
        {
            var document = this.CurrentSolution.GetTextDocument(documentId);
            var name = document != null ? document.Name : "<Document" + documentId.Id + ">";
            return name;
        }

        /// <summary>
        /// Gets the name to use for an additional document in an error message.
        /// </summary>
        protected virtual string GetAdditionalDocumentName(DocumentId documentId)
            => GetDocumentName(documentId);

        /// <summary>
        /// Gets the name to use for an analyzer document in an error message.
        /// </summary>
        protected virtual string GetAnalyzerConfigDocumentName(DocumentId documentId)
            => GetDocumentName(documentId);

        #endregion
    }
}
