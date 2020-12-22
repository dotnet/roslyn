' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.IO
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Editor.CodeCleanup
Imports Microsoft.VisualStudio.Language.CodeCleanUp
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.Threading
Imports Roslyn.Utilities
Imports __VSHPROPID8 = Microsoft.VisualStudio.Shell.Interop.__VSHPROPID8
Imports IVsHierarchyItemManager = Microsoft.VisualStudio.Shell.IVsHierarchyItemManager

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.LanguageService

    <Export(GetType(CodeCleanUpFixer))>
    <VisualStudio.Utilities.ContentType(ContentTypeNames.VisualBasicContentType)>
    Partial Friend Class VisualBasicCodeCleanUpFixer
        Inherits CodeCleanUpFixer
        Private Const FormatDocumentFixId As String = NameOf(FormatDocumentFixId)
        Private Const RemoveUnusedImportsFixId As String = NameOf(RemoveUnusedImportsFixId)
        Private Const SortImportsFixId As String = NameOf(SortImportsFixId)
        Private ReadOnly _threadingContext As IThreadingContext
        Private ReadOnly _workspace As VisualStudioWorkspaceImpl
        Private ReadOnly _vsHierarchyItemManager As IVsHierarchyItemManager

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext, workspace As VisualStudioWorkspaceImpl, vsHierarchyItemManager As IVsHierarchyItemManager)
            _threadingContext = threadingContext
            _workspace = workspace
            _vsHierarchyItemManager = vsHierarchyItemManager
        End Sub

        Public Overrides Function FixAsync(scope As ICodeCleanUpScope, context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Select Case True
                Case TypeOf scope Is TextBufferCodeCleanUpScope
                    Dim textBufferScope As TextBufferCodeCleanUpScope = CType(scope, TextBufferCodeCleanUpScope)
                    Return FixTextBufferAsync(textBufferScope, context)
                Case TypeOf scope Is IVsHierarchyCodeCleanupScope
                    Dim hierarchyContentScope As IVsHierarchyCodeCleanupScope = CType(scope, IVsHierarchyCodeCleanupScope)
                    Return FixHierarchyContentAsync(hierarchyContentScope, context)
                Case Else
                    Return Task.FromResult(False)
            End Select
        End Function

        Private Async Function FixHierarchyContentAsync(hierarchyContent As IVsHierarchyCodeCleanupScope, context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Dim hierarchy = hierarchyContent.Hierarchy
            If hierarchy Is Nothing Then
                Return Await FixSolutionAsync(_workspace.CurrentSolution, context).ConfigureAwait(True)
            End If

            ' Map the hierarchy to a ProjectId. For hierarchies mapping to multitargeted projects, we first try to
            ' get the project in the most recent active context, but fall back to the first target framework if no
            ' active context is available.
            Dim hierarchyToProjectMap = _workspace.Services.GetRequiredService(Of IHierarchyItemToProjectIdMap)()

            Await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(context.OperationContext.UserCancellationToken)

            Dim projectId As ProjectId = Nothing
            Dim contextProjectNameObject As Object = Nothing
            Dim contextProjectNameObjectIsStr As Boolean = TypeOf contextProjectNameObject Is String
            Dim contextProjectName As String = ""
            If contextProjectNameObjectIsStr Then
                contextProjectName = CStr(contextProjectNameObject)
            End If

            If ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID.Root, __VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, contextProjectNameObject)) AndAlso contextProjectNameObjectIsStr Then
                projectId = _workspace.GetProjectWithHierarchyAndName(hierarchy, contextProjectName)?.Id
            End If

            If projectId Is Nothing Then
                Dim projectHierarchyItem = _vsHierarchyItemManager.GetHierarchyItem(hierarchyContent.Hierarchy, VSConstants.VSITEMID.Root)
                If Not hierarchyToProjectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker:=Nothing, projectId) Then
                    Return False
                End If
            End If

            Dim itemId = hierarchyContent.ItemId
            Dim path As String = Nothing
            If itemId = VSConstants.VSITEMID.Root Then
                Await TaskScheduler.Default

                Dim project = _workspace.CurrentSolution.GetProject(projectId)
                If project Is Nothing Then
                    Return False
                End If

                Return Await FixProjectAsync(project, context).ConfigureAwait(True)
            ElseIf hierarchy.GetCanonicalName(itemId, path) = 0 Then
                Dim attr As FileAttributes = File.GetAttributes(path)
                If attr.HasFlag(FileAttributes.Directory) Then
                Else
                    ' Handle code cleanup for a single document
                    Await TaskScheduler.Default

                    Dim solution = _workspace.CurrentSolution
                    Dim documentIds = solution.GetDocumentIdsWithFilePath(path)
                    Dim documentId = documentIds.FirstOrDefault(Function(id1) id1.ProjectId = projectId)
                    If documentId Is Nothing Then
                        Return False
                    End If

                    Return Await FixDocumentAsync(solution.GetDocument(documentId), context).ConfigureAwait(True)
                    ' directory
                    ' TODO: this one will be implemented later
                    ' https://github.com/dotnet/roslyn/issues/30165
                End If
            End If

            Return False
        End Function

        Private Function FixSolutionAsync(solution As Solution, context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Dim applyFixAsync = Function(progressTracker1 As ProgressTracker, cancellationToken1 As CancellationToken) As Task(Of Solution)
                                    Return FixSolutionAsync(solution, context.EnabledFixIds, progressTracker1, cancellationToken1)
                                End Function

            Return FixAsync(solution.Workspace, applyFixAsync, context)

        End Function

        Private Function FixProjectAsync(project As Project, context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Dim applyFixAsync = Async Function(progressTracker1 As ProgressTracker, cancellationToken1 As CancellationToken) As Task(Of Solution)
                                    Dim newProject = Await FixProjectAsync(project, context.EnabledFixIds, progressTracker1, addProgressItemsForDocuments:=True, cancellationToken1).ConfigureAwait(True)
                                    Return newProject.Solution
                                End Function

            Return FixAsync(project.Solution.Workspace, applyFixAsync, context)

        End Function

        Private Function FixDocumentAsync(Doc As Document, context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Dim applyFixAsync = Async Function(progressTracker1 As ProgressTracker, cancelToken As CancellationToken) As Task(Of Solution)
                                    Dim newDocument = Await FixDocumentAsync(Doc, context.EnabledFixIds, progressTracker1, cancelToken).ConfigureAwait(True)
                                    Return newDocument.Project.Solution
                                End Function

            Return FixAsync(Doc.Project.Solution.Workspace, applyFixAsync, context)

        End Function

        Private Function FixTextBufferAsync(textBufferScope As TextBufferCodeCleanUpScope, context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Dim buffer = textBufferScope.SubjectBuffer

            ' Let LSP handle code cleanup in the cloud scenario
            If buffer.IsInLspEditorContext() Then
                Return SpecializedTasks.False
            End If

            Dim document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges()
            If document Is Nothing Then
                Return SpecializedTasks.False
            End If

            Dim applyFixAsync = Async Function(progressTrack As ProgressTracker, cancellationToken1 As CancellationToken) As Task(Of Solution)
                                    Dim doc = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges()
                                    Dim newDoc = Await FixDocumentAsync(doc, context.EnabledFixIds, progressTrack, cancellationToken1).ConfigureAwait(True)
                                    Return newDoc.Project.Solution
                                End Function

            Return FixAsync(buffer.GetWorkspace(), applyFixAsync, context)

        End Function

        Private Overloads Async Function FixAsync(
            workspace As Workspace,
            applyFixAsync As Func(Of ProgressTracker, CancellationToken, Task(Of Solution)),
            context As ICodeCleanUpExecutionContext) As Task(Of Boolean)
            Using scope = context.OperationContext.AddScope(allowCancellation:=True, EditorFeaturesResources.Waiting_for_background_work_to_finish)
                Dim workspaceStatusService = workspace.Services.GetService(Of IWorkspaceStatusService)()
                If workspaceStatusService IsNot Nothing Then
                    Await workspaceStatusService.WaitUntilFullyLoadedAsync(context.OperationContext.UserCancellationToken).ConfigureAwait(True)
                End If
            End Using
            Using scope = context.OperationContext.AddScope(allowCancellation:=True, description:=EditorFeaturesResources.Applying_changes)
                Dim cancelToken = context.OperationContext.UserCancellationToken
                Dim progressTracker1 As New ProgressTracker(Sub(description, completed, total)
                                                                If scope IsNot Nothing Then
                                                                    scope.Description = description
                                                                    scope.Progress.Report(New VisualStudio.Utilities.ProgressInfo(completed, total))
                                                                End If
                                                            End Sub)

                Dim solution = Await applyFixAsync(progressTracker1, cancelToken).ConfigureAwait(True)

                Await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancelToken)

                Return workspace.TryApplyChanges(solution, progressTracker1)
            End Using
        End Function

        Private Shared Async Function FixSolutionAsync(
            solution As Solution,
            enabledFixIds As FixIdContainer,
            progressTracker As ProgressTracker,
            cancellationToken1 As CancellationToken) As Task(Of Solution)
            ' Pre-populate the solution progress tracker with the total number of documents to process
            For Each projectId1 In solution.ProjectIds
                Dim project = solution.GetProject(projectId1)
                If Not CanCleanupProject(project) Then
                    Continue For
                End If

                progressTracker.AddItems(project.DocumentIds.Count)
            Next

            For Each projectId1 In solution.ProjectIds
                cancellationToken1.ThrowIfCancellationRequested()

                Dim project = solution.GetProject(projectId1)
                Dim newProject = Await FixProjectAsync(project, enabledFixIds, progressTracker, addProgressItemsForDocuments:=False, cancellationToken1).ConfigureAwait(False)
                solution = newProject.Solution
            Next

            Return solution
        End Function

        Private Shared Async Function FixProjectAsync(
            project As Project,
            enabledFixIds As FixIdContainer,
            progressTracker1 As ProgressTracker,
            addProgressItemsForDocuments As Boolean,
            cancelToken As CancellationToken) As Task(Of Project)
            If Not CanCleanupProject(project) Then
                Return project
            End If

            If addProgressItemsForDocuments Then
                progressTracker1.AddItems(project.DocumentIds.Count)
            End If

            For Each documentId In project.DocumentIds
                cancelToken.ThrowIfCancellationRequested()

                Dim document1 = project.GetDocument(documentId)
                progressTracker1.Description = document1.Name

                ' FixDocumentAsync reports progress within a document, but we limit progress reporting for a project
                ' to the current document.
                Dim documentProgressTracker As New ProgressTracker

                Dim fixedDocument = Await FixDocumentAsync(document1, enabledFixIds, documentProgressTracker, cancelToken).ConfigureAwait(False)
                project = fixedDocument.Project
                progressTracker1.ItemCompleted()
            Next

            Return project
        End Function

        Private Shared Function CanCleanupProject(project As Project) As Boolean
            Return project.LanguageServices.GetService(Of ICodeCleanupService)() IsNot Nothing
        End Function

        Private Shared Async Function FixDocumentAsync(
            document As Document,
            enabledFixIds As FixIdContainer,
            progressTracker As ProgressTracker,
            cancelToken As CancellationToken) As Task(Of Document)
            If document.IsGeneratedCode(cancelToken) Then
                Return document
            End If

            Dim codeCleanupService = document.GetLanguageService(Of ICodeCleanupService)()

            Dim allDiagnostics = codeCleanupService.GetAllDiagnostics()

            Dim enabedDiagnosticSets As ArrayBuilder(Of DiagnosticSet) = ArrayBuilder(Of DiagnosticSet).GetInstance()

            For Each diagnostic In allDiagnostics.Diagnostics
                For Each diagnosticId In diagnostic.DiagnosticIds
                    If enabledFixIds.IsFixIdEnabled(diagnosticId) Then
                        enabedDiagnosticSets.Add(diagnostic)
                        Exit For
                    End If
                Next
            Next

            Dim isFormatDocumentEnabled = enabledFixIds.IsFixIdEnabled(FormatDocumentFixId)
            Dim isRemoveUnusedUsingsEnabled = enabledFixIds.IsFixIdEnabled(RemoveUnusedImportsFixId)
            Dim isSortUsingsEnabled = enabledFixIds.IsFixIdEnabled(SortImportsFixId)
            Dim enabledDiagnostics As New EnabledDiagnosticOptions(
                isFormatDocumentEnabled,
                enabedDiagnosticSets.ToImmutableArray(),
                New OrganizeUsingsSet(isRemoveUnusedUsingsEnabled, isSortUsingsEnabled))

            Return Await codeCleanupService.CleanupAsync(
                document, enabledDiagnostics, progressTracker, cancelToken).ConfigureAwait(False)
        End Function

    End Class

End Namespace
