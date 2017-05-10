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
            var analyzerFilePath = ExtractAnalyzerFilePath(canonicalName);

            return project.AnalyzerReferences.FirstOrDefault(r => r.FullPath.Equals(analyzerFilePath, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Given the canonical name of a node representing an analyzer assembly in the
        /// CPS-based project system extracts out the full path to the assembly.
        /// </summary>
        /// <remarks>
        /// The canonical name takes the following form:
        /// 
        ///   [{path to project directory}\]{target framework}\analyzerdependency\{path to assembly}
        ///   
        /// e.g.:
        /// 
        ///   C:\projects\solutions\MyProj\netstandard2.0\analyzerdependency\C:\users\me\.packages\somePackage\lib\someAnalyzer.dll
        ///   
        /// This method exists solely to extract out the "path to assembly" part, i.e.
        /// "C:\users\me\.packages\somePackage\lib\someAnalyzer.dll". We don't need the
        /// other parts.
        /// 
        /// Note that the path to the project directory is optional. It's not clear if
        /// this is intentional or a bug in the project system, but either way it
        /// doesn't really matter.
        /// </remarks>
        private string ExtractAnalyzerFilePath(string canonicalName)
        {
            // The canonical name may or may not start with the path to the project's directory.
            if (canonicalName.StartsWith(_projectDirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                // Extract the rest of the string, taking into account the "\" separating the directory
                // path from the rest of the canonical name
                canonicalName = canonicalName.Substring(_projectDirectoryPath.Length + 1);
            }

            // Find the slash after the target framework
            var backslashIndex = canonicalName.IndexOf('\\');
            // Find the slash after "analyzerdependency"
            backslashIndex = canonicalName.IndexOf('\\', backslashIndex + 1);

            // The rest of the string is the path.
            return canonicalName.Substring(backslashIndex + 1);
        }
    }
}