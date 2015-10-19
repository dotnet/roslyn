// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
        private readonly Workspace _workspace;
        private readonly ITableManagerProvider _provider;

        protected AbstractTable(Workspace workspace, ITableManagerProvider provider, string tableIdentifier)
        {
            _workspace = workspace;
            _provider = provider;

            this.TableManager = provider.GetTableManager(tableIdentifier);
        }

        protected Workspace Workspace => _workspace;

        protected abstract void AddTableSourceIfNecessary(Solution solution);
        protected abstract void RemoveTableSourceIfNecessary(Solution solution);
        protected abstract void ShutdownSource();

        protected void ConnectWorkspaceEvents()
        {
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

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
                    break;
                default:
                    Contract.Fail("Can't reach here");
                    return;
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
        {
            this.TableManager.AddSource(source, Columns);
        }

        internal ITableManager TableManager { get; }

        internal abstract IReadOnlyCollection<string> Columns { get; }
    }
}
