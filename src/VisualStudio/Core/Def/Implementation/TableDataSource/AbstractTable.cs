// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Base implementation of new platform table. this knows how to create various ITableDataSource and connect
    /// them to ITableManagerProvider
    /// </summary>
    internal abstract class AbstractTable
    {
        protected AbstractTable(Workspace workspace, ITableManagerProvider provider, string tableIdentifier)
        {
            Workspace = workspace;
            this.TableManager = provider.GetTableManager(tableIdentifier);
        }

        protected Workspace Workspace { get; }

        protected abstract void AddTableSourceIfNecessary(Solution solution);
        protected abstract void RemoveTableSourceIfNecessary(Solution solution);
        protected abstract void ShutdownSource();

        protected void ConnectWorkspaceEvents()
            => Workspace.WorkspaceChanged += OnWorkspaceChanged;

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.ProjectAdded:
                    AddTableSourceIfNecessary(e.NewSolution);
                    break;
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.ProjectRemoved:
                    ShutdownSourceIfNecessary(e.NewSolution);
                    RemoveTableSourceIfNecessary(e.NewSolution);
                    break;
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.AdditionalDocumentAdded:
                case WorkspaceChangeKind.AdditionalDocumentRemoved:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AdditionalDocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(e.Kind);
            }
        }

        private void ShutdownSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count > 0)
            {
                return;
            }

            ShutdownSource();
        }

        protected void AddInitialTableSource(Solution solution, ITableDataSource source)
        {
            if (solution.ProjectIds.Count == 0)
            {
                return;
            }

            AddTableSource(source);
        }

        protected void AddTableSource(ITableDataSource source)
            => this.TableManager.AddSource(source, Columns);

        internal ITableManager TableManager { get; }

        internal abstract ImmutableArray<string> Columns { get; }
    }
}
