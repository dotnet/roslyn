// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal partial class CpsDiagnosticItemSource : BaseDiagnosticAndGeneratorItemSource, INotifyPropertyChanged
    {
        private readonly IVsHierarchyItem _item;

        private AnalyzerReference? _analyzerReference;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CpsDiagnosticItemSource(Workspace workspace, ProjectId projectId, IVsHierarchyItem item, IAnalyzersCommandHandler commandHandler, IDiagnosticAnalyzerService analyzerService)
            : base(workspace, projectId, commandHandler, analyzerService)
        {
            _item = item;

            _analyzerReference = TryGetAnalyzerReference(Workspace.CurrentSolution);
            if (_analyzerReference == null)
            {
                // The workspace doesn't know about the project and/or the analyzer yet.
                // Hook up an event handler so we can update when it does.
                Workspace.WorkspaceChanged += OnWorkspaceChangedLookForAnalyzer;
            }
        }

        public IContextMenuController DiagnosticItemContextMenuController => CommandHandler.DiagnosticContextMenuController;

        public override object SourceItem => _item;

        public override AnalyzerReference? AnalyzerReference => _analyzerReference;

        private void OnWorkspaceChangedLookForAnalyzer(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind is WorkspaceChangeKind.SolutionCleared or
                WorkspaceChangeKind.SolutionReloaded or
                WorkspaceChangeKind.SolutionRemoved)
            {
                Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
            }
            else if (e.Kind == WorkspaceChangeKind.SolutionAdded)
            {
                _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                if (_analyzerReference != null)
                {
                    Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                }
            }
            else if (e.ProjectId == ProjectId)
            {
                if (e.Kind == WorkspaceChangeKind.ProjectRemoved)
                {
                    Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;
                }
                else if (e.Kind is WorkspaceChangeKind.ProjectAdded
                         or WorkspaceChangeKind.ProjectChanged)
                {
                    _analyzerReference = TryGetAnalyzerReference(e.NewSolution);
                    if (_analyzerReference != null)
                    {
                        Workspace.WorkspaceChanged -= OnWorkspaceChangedLookForAnalyzer;

                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasItems)));
                    }
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

            if (!ErrorHandler.Succeeded(_item.HierarchyIdentity.Hierarchy.GetProperty(_item.HierarchyIdentity.ItemID, (int)__VSHPROPID.VSHPROPID_BrowseObject, out var browseObject)) ||
                browseObject is not ICustomTypeDescriptor typeDescriptor)
            {
                return null;
            }

            var property = typeDescriptor.GetProperties().OfType<PropertyDescriptor>()
                                                         .FirstOrDefault(p => p.Name == "ResolvedPath");
            if (property is null)
            {
                return null;
            }

            var resolvedPath = property.GetValue(browseObject) as string;
            if (resolvedPath is null)
            {
                return null;
            }

            // The path we get here isn't normalized, so do so
            var analyzerFilePath = FileUtilities.NormalizeAbsolutePath(resolvedPath);

            return project.AnalyzerReferences.FirstOrDefault(r => string.Equals(r.FullPath, analyzerFilePath, StringComparison.OrdinalIgnoreCase));
        }
    }
}
