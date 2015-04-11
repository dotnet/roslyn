// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTable<TArgs, TData>
    {
        private readonly Workspace _workspace;
        private readonly ITableManagerProvider _provider;

        protected readonly AbstractRoslynTableDataSource<TArgs, TData> Source;

        protected AbstractTable(Workspace workspace, ITableManagerProvider provider, Guid tableIdentifier, AbstractRoslynTableDataSource<TArgs, TData> source)
        {
            _workspace = workspace;
            _provider = provider;
            this.TableManager = provider.GetTableManager(tableIdentifier);

            this.Source = source;

            AddInitialTableSource(workspace);
        }

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
                    AddTableSourceIfNecessary();
                    SolutionOrProjectChanged(e);
                    break;
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.ProjectRemoved:
                    RemoveTableSourceIfNecessary();
                    SolutionOrProjectChanged(e);
                    break;
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.ProjectReloaded:
                    SolutionOrProjectChanged(e);
                    break;
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

        protected virtual void SolutionOrProjectChanged(WorkspaceChangeEventArgs e)
        {
            // do nothing in base implementation
        }

        private void AddTableSourceIfNecessary()
        {
            if (_workspace.CurrentSolution.ProjectIds.Count == 0 || this.TableManager.Sources.Any(s => s == this.Source))
            {
                return;
            }

            AddTableSource();
        }

        private void RemoveTableSourceIfNecessary()
        {
            if (_workspace.CurrentSolution.ProjectIds.Count > 0 || !this.TableManager.Sources.Any(s => s == this.Source))
            {
                return;
            }

            this.TableManager.RemoveSource(this.Source);
        }

        private void AddInitialTableSource(Workspace workspace)
        {
            if (workspace.CurrentSolution.ProjectIds.Count == 0)
            {
                return;
            }

            AddTableSource();
        }

        protected void AddTableSource()
        {
            this.TableManager.AddSource(this.Source, Columns);
        }

        internal ITableManager TableManager { get; private set; }

        internal abstract IReadOnlyCollection<string> Columns { get; }
    }
}
