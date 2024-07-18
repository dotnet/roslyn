// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed partial class CpsDiagnosticItemSource : BaseDiagnosticAndGeneratorItemSource, INotifyPropertyChanged
{
    private readonly IVsHierarchyItem _item;
    private readonly string _projectDirectoryPath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CpsDiagnosticItemSource(
        Workspace workspace,
        string projectPath,
        ProjectId projectId,
        IVsHierarchyItem item,
        IAnalyzersCommandHandler commandHandler,
        IDiagnosticAnalyzerService analyzerService,
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(workspace, projectId, commandHandler, analyzerService, listenerProvider)
    {
        _item = item;
        _projectDirectoryPath = Path.GetDirectoryName(projectPath);

        this.AnalyzerReference = TryGetAnalyzerReference(Workspace.CurrentSolution);
        if (this.AnalyzerReference == null)
        {
            // The ProjectId that was given to us was found by enumerating the list of projects in the solution,
            // thus the project must have already been added to the workspace at some point. As long as the project
            // is still there, we're going to assume the reason we don't have the reference yet is because while we
            // have a project, we don't have all the references added yet. We'll wait until we see the reference and
            // then connect to it.
            if (workspace.CurrentSolution.ContainsProject(projectId))
            {
                Workspace.WorkspaceChanged += OnWorkspaceChangedLookForAnalyzer;
                item.PropertyChanged += IVsHierarchyItem_PropertyChanged;

                // Now that we've subscribed, check once more in case we missed the event
                var analyzerReference = TryGetAnalyzerReference(Workspace.CurrentSolution);

                if (analyzerReference != null)
                {
                    this.AnalyzerReference = analyzerReference;
                    UnsubscribeFromEvents();
                }
            }
        }
    }

    private void UnsubscribeFromEvents()
    {
        Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
        _item.PropertyChanged -= IVsHierarchyItem_PropertyChanged;
    }

    private void IVsHierarchyItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // IVsHierarchyItem implements ISupportDisposalNotification, which allows us to know when it's been removed
        if (e.PropertyName == nameof(ISupportDisposalNotification.IsDisposed))
        {
            UnsubscribeFromEvents();
        }
    }

    public IContextMenuController DiagnosticItemContextMenuController => CommandHandler.DiagnosticContextMenuController;

    public override object SourceItem => _item;

    private void OnWorkspaceChangedLookForAnalyzer(object sender, WorkspaceChangeEventArgs e)
    {
        // If the project has gone away in this change, it's not coming back, so we can stop looking at this point
        if (!e.NewSolution.ContainsProject(ProjectId))
        {
            UnsubscribeFromEvents();
            return;
        }

        // Was this a change to our project, or a global change?
        if (e.ProjectId == ProjectId ||
            e.Kind == WorkspaceChangeKind.SolutionChanged)
        {
            var analyzerReference = TryGetAnalyzerReference(e.NewSolution);
            if (analyzerReference != null)
            {
                this.AnalyzerReference = analyzerReference;
                UnsubscribeFromEvents();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
            }
        }
    }

    private AnalyzerReference? TryGetAnalyzerReference(Solution solution)
    {
        var project = solution.GetProject(ProjectId);

        if (project == null)
        {
            return null;
        }

        var canonicalName = _item.CanonicalName;
        var analyzerFilePath = CpsUtilities.ExtractAnalyzerFilePath(_projectDirectoryPath, canonicalName);

        if (string.IsNullOrEmpty(analyzerFilePath))
        {
            return null;
        }

        return project.AnalyzerReferences.FirstOrDefault(r => string.Equals(r.FullPath, analyzerFilePath, StringComparison.OrdinalIgnoreCase));
    }
}
