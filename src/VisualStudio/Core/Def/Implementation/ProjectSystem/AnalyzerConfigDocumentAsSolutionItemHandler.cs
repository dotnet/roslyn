// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(AnalyzerConfigDocumentAsSolutionItemHandler)), Shared]
    internal partial class AnalyzerConfigDocumentAsSolutionItemHandler : IVsSolutionEvents, IVsSolutionLoadEvents, IDisposable
    {
        private static readonly string LocalRegistryPath = $@"Roslyn\Internal\{nameof(AnalyzerConfigDocumentAsSolutionItemHandler)}\";
        private static readonly Option<bool> NeverShowAgain = new Option<bool>(nameof(AnalyzerConfigDocumentAsSolutionItemHandler), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));

        private readonly VisualStudioWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;
        private readonly SolutionUserOptionsProvider _solutionUserOptionsProvider;
        private readonly DTE _dte;
        private readonly IVsSolution _vsSolution;
        private uint _solutionEventsCookie;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerConfigDocumentAsSolutionItemHandler(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IThreadingContext threadingContext,
            SolutionUserOptionsProvider solutionUserOptionsProvider)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;
            _solutionUserOptionsProvider = solutionUserOptionsProvider;
            _dte = (DTE)serviceProvider.GetService(typeof(DTE));

            _vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            _vsSolution?.AdviseSolutionEvents(this, out _solutionEventsCookie);

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        void IDisposable.Dispose()
        {
            _vsSolution?.UnadviseSolutionEvents(_solutionEventsCookie);
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind != WorkspaceChangeKind.AnalyzerConfigDocumentAdded)
            {
                return;
            }

            var analyzerConfigDocumentFilePath = e.NewSolution.GetAnalyzerConfigDocument(e.DocumentId)?.FilePath;
            if (analyzerConfigDocumentFilePath == null)
            {
                return;
            }

            var analyzerConfigDirectory = PathUtilities.GetDirectoryName(analyzerConfigDocumentFilePath);
            var solutionDirectory = PathUtilities.GetDirectoryName(e.NewSolution?.FilePath);
            if (analyzerConfigDirectory == null ||
                analyzerConfigDirectory != solutionDirectory)
            {
                return;
            }

            ProcessAnalyzerConfigDocumentFilePath(analyzerConfigDocumentFilePath);
        }

        private void OnSolutionLoadComplete()
        {
            var solutionDirectory = PathUtilities.GetDirectoryName(_workspace.CurrentSolution?.FilePath);
            if (solutionDirectory == null)
            {
                return;
            }

            var analyzerConfigDocumentFilePath = PathUtilities.CombinePathsUnchecked(solutionDirectory, ".editorconfig");
            ProcessAnalyzerConfigDocumentFilePath(analyzerConfigDocumentFilePath);
        }

        private void ProcessAnalyzerConfigDocumentFilePath(string analyzerConfigDocumentFilePath)
        {
            if (!File.Exists(analyzerConfigDocumentFilePath) ||
                _workspace.Options.GetOption(NeverShowAgain) ||
                _solutionUserOptionsProvider.GetOption(SolutionUserOptionNames.DoNotAddEditorConfigAsSlnItem) == true)
            {
                return;
            }

            // Kick off a task to show info bar to make it a solution item.
            Task.Run(async () =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                var solution = (Solution2)_dte.Solution;
                if (VisualStudioAddSolutionItemService.TryGetExistingSolutionItemsFolder(solution, analyzerConfigDocumentFilePath, out _, out var hasExistingSolutionItem) &&
                    hasExistingSolutionItem)
                {
                    return;
                }

                var infoBarService = _workspace.Services.GetRequiredService<IInfoBarService>();
                infoBarService.ShowInfoBarInGlobalView(
                    ServicesVSResources.Visual_Studio_detected_an_editorconfig_file_at_the_root_of_your_solution_Would_you_like_to_add_it_as_a_solution_item,
                    GetInfoBarUIItems().ToArray());
            });

            return;

            // Local functions
            IEnumerable<InfoBarUI> GetInfoBarUIItems()
            {
                // Yes - add editorconfig solution item.
                yield return new InfoBarUI(
                    title: ServicesVSResources.Yes,
                    kind: InfoBarUI.UIKind.Button,
                    action: AddEditorconfigSolutionItem,
                    closeAfterAction: true);

                // No - do not add editorconfig solution item.
                yield return new InfoBarUI(
                    title: ServicesVSResources.No,
                    kind: InfoBarUI.UIKind.Button,
                    action: () => _solutionUserOptionsProvider.SetOption(SolutionUserOptionNames.DoNotAddEditorConfigAsSlnItem, true),
                    closeAfterAction: true);

                // Don't show the InfoBar again link
                yield return new InfoBarUI(title: ServicesVSResources.Never_show_this_again,
                    kind: InfoBarUI.UIKind.Button,
                    action: () => _workspace.Options = _workspace.Options.WithChangedOption(NeverShowAgain, true),
                    closeAfterAction: true);
            }

            void AddEditorconfigSolutionItem()
            {
                var addSolutionItemService = _workspace.Services.GetRequiredService<IAddSolutionItemService>();
                addSolutionItemService.AddSolutionItemAsync(analyzerConfigDocumentFilePath, CancellationToken.None).Wait();
            }
        }

        #region Interface implementations

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
        {
            OnSolutionLoadComplete();
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionLoadEvents.OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionLoadEvents.OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionLoadEvents.OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.E_NOTIMPL;
        }

        #endregion
    }
}
