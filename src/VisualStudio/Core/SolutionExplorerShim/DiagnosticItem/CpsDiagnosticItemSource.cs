// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal partial class CpsDiagnosticItemSource : BaseDiagnosticItemSource, INotifyPropertyChanged
    {
        private readonly IVsHierarchyItem _item;
        private readonly string _projectDirectoryPath;

        private AnalyzerReference _analyzerReference;

        public event PropertyChangedEventHandler PropertyChanged;

        public CpsDiagnosticItemSource(Workspace workspace, string projectPath, ProjectId projectId, IVsHierarchyItem item, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService analyzerService)
            : base(workspace, projectId, commandHandler, analyzerService)
        {
            _item = item;
            _projectDirectoryPath = Path.GetDirectoryName(projectPath);

            _analyzerReference = TryGetAnalyzerReference(_workspace.CurrentSolution);
            if (_analyzerReference == null)
            {
                // The workspace doesn't know about the project and/or the analyzer yet.
                // Hook up an event handler so we can update when it does.
                _workspace.WorkspaceChanged += OnWorkspaceChangedLookForAnalyzer;
            }
        }

        public IContextMenuController DiagnosticItemContextMenuController => _commandHandler.DiagnosticContextMenuController;
        public Workspace Workspace => _workspace;
        public ProjectId ProjectId => _projectId;

        public override object SourceItem => _item;

        public override AnalyzerReference AnalyzerReference => _analyzerReference;
        protected override BaseDiagnosticItem CreateItem(DiagnosticDescriptor diagnostic, ReportDiagnostic effectiveSeverity)
        {
            return new CpsDiagnosticItem(this, diagnostic, effectiveSeverity);
        }

        private void OnWorkspaceChangedLookForAnalyzer(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionCleared ||
                e.Kind == WorkspaceChangeKind.SolutionReloaded ||
                e.Kind == WorkspaceChangeKind.SolutionRemoved)
            {
                _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
            }
            else if (e.Kind == WorkspaceChangeKind.SolutionAdded)
            {
                _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                if (_analyzerReference != null)
                {
                    _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                }
            }
            else if (e.ProjectId == _projectId)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                {
                    _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
                }
                else if (e.Kind == WorkspaceChangeKind.ProjectAdded
                         || e.Kind == WorkspaceChangeKind.ProjectChanged)
                {
                    _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                    if (_analyzerReference != null)
                    {
                        _workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                    }
                }
            }
        }

        private AnalyzerReference TryGetAnalyzerReference(Solution solution)
        {
            var project = solution.GetProject(_projectId);

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

            return project.AnalyzerReferences.FirstOrDefault(r => r.FullPath.Equals(analyzerFilePath, StringComparison.OrdinalIgnoreCase));
        }
    }
}
