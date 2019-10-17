// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export, Shared]
    internal partial class AnalyzerConfigDocumentAsSolutionItemHandler : IDisposable
    {
        private static readonly string LocalRegistryPath = $@"Roslyn\Internal\{nameof(AnalyzerConfigDocumentAsSolutionItemHandler)}\";
        private static readonly Option<bool> NeverShowAgain = new Option<bool>(nameof(AnalyzerConfigDocumentAsSolutionItemHandler), nameof(NeverShowAgain),
            defaultValue: false, storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + nameof(NeverShowAgain)));

        private readonly VisualStudioWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;

        private DTE? _dte;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerConfigDocumentAsSolutionItemHandler(
            VisualStudioWorkspace workspace,
            IThreadingContext threadingContext)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;

            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            _dte = (DTE)serviceProvider.GetService(typeof(DTE));
        }

        void IDisposable.Dispose()
        {
            _workspace.WorkspaceChanged -= OnWorkspaceChanged;
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // Check if a new analyzer config document was added and we have a non-null DTE instance.
            if (e.Kind != WorkspaceChangeKind.AnalyzerConfigDocumentAdded ||
                _dte == null)
            {
                return;
            }

            // Check if added analyzer config document is at the root of the current solution.
            var analyzerConfigDocumentFilePath = e.NewSolution.GetAnalyzerConfigDocument(e.DocumentId)?.FilePath;
            var analyzerConfigDirectory = PathUtilities.GetDirectoryName(analyzerConfigDocumentFilePath);
            var solutionDirectory = PathUtilities.GetDirectoryName(e.NewSolution?.FilePath);
            if (analyzerConfigDocumentFilePath == null ||
                analyzerConfigDirectory == null ||
                analyzerConfigDirectory != solutionDirectory)
            {
                return;
            }

            // Check if user has explicitly disabled the suggestion to add newly added analyzer config document as solution item.
            if (_workspace.Options.GetOption(NeverShowAgain))
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
                    ServicesVSResources.A_new_editorconfig_file_was_detected_at_the_root_of_your_solution_Would_you_like_to_make_it_a_solution_item,
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
                    action: () => { },
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
    }
}
