// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class VisualStudioBaseTodoListTable : AbstractTable<TodoListEventArgs, TodoItem>
    {
        private static readonly string[] s_columns = new string[]
        {
            StandardTableColumnDefinitions.Priority,
            StandardTableColumnDefinitions.Text,
            StandardTableColumnDefinitions.ProjectName,
            StandardTableColumnDefinitions.DocumentName,
            StandardTableColumnDefinitions.Line,
            StandardTableColumnDefinitions.Column
        };

        private readonly TableDataSource _source;

        protected VisualStudioBaseTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, string identifier, ITableManagerProvider provider) :
            base(workspace, provider, StandardTables.TasksTable)
        {
            _source = new TableDataSource(workspace, todoListProvider, identifier);
            AddInitialTableSource(workspace.CurrentSolution, _source);
        }

        internal override IReadOnlyCollection<string> Columns => s_columns;

        protected override void AddTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count == 0 || this.TableManager.Sources.Any(s => s == _source))
            {
                return;
            }

            AddTableSource(_source);
        }

        protected override void RemoveTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count > 0 || !this.TableManager.Sources.Any(s => s == _source))
            {
                return;
            }

            this.TableManager.RemoveSource(_source);
        }

        protected override void ShutdownSource()
        {
            _source.Shutdown();
        }

        private class TableDataSource : AbstractRoslynTableDataSource<TodoListEventArgs, TodoItem>
        {
            private readonly Workspace _workspace;
            private readonly string _identifier;
            private readonly ITodoListProvider _todoListProvider;

            public TableDataSource(Workspace workspace, ITodoListProvider todoListProvider, string identifier)
            {
                _workspace = workspace;
                _identifier = identifier;
                _todoListProvider = todoListProvider;
                _todoListProvider.TodoListUpdated += OnTodoListUpdated;

                ConnectToSolutionCrawlerService(_workspace);
            }

            public override string DisplayName => ServicesVSResources.TodoTableSourceName;
            public override string SourceTypeIdentifier => StandardTableDataSources.CommentTableDataSource;
            public override string Identifier => _identifier;

            private void OnTodoListUpdated(object sender, TodoListEventArgs e)
            {
                if (_workspace != e.Workspace)
                {
                    return;
                }

                Contract.Requires(e.DocumentId != null);

                if (e.TodoItems.Length == 0)
                {
                    OnDataRemoved(e.DocumentId);
                    return;
                }

                OnDataAddedOrChanged(e.DocumentId, e.Solution, e.ProjectId, e.DocumentId, e, e.TodoItems.Length);
            }

            protected override AbstractTableEntriesFactory<TodoItem> CreateTableEntryFactory(object key, TodoListEventArgs data)
            {
                var documentId = (DocumentId)key;
                Contract.Requires(documentId == data.DocumentId);

                return new TableEntriesFactory(this, data.Workspace, data.DocumentId);
            }

            private class TableEntriesFactory : AbstractTableEntriesFactory<TodoItem>
            {
                private readonly TableDataSource _source;
                private readonly Workspace _workspace;
                private readonly DocumentId _documentId;

                public TableEntriesFactory(TableDataSource source, Workspace workspace, DocumentId documentId) :
                    base(source)
                {
                    _source = source;
                    _workspace = workspace;
                    _documentId = documentId;
                }

                protected override ImmutableArray<TodoItem> GetItems()
                {
                    var provider = _source._todoListProvider;

                    // TODO: remove this wierd cast once we completely move off legacy task list. we, for now, need this since we share data
                    //       between old and new API.
                    return provider.GetTodoItems(_workspace, _documentId, CancellationToken.None).Cast<TodoItem>().ToImmutableArray();
                }

                protected override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TodoItem> items)
                {
                    return CreateTrackingPoints(_workspace, _documentId, items, (d, s) => CreateTrackingPoint(s, d.OriginalLine, d.OriginalColumn));
                }

                protected override AbstractTableEntriesSnapshot<TodoItem> CreateSnapshot(int version, ImmutableArray<TodoItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    return new TableEntriesSnapshot(this, version, items, trackingPoints);
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<TodoItem>
                {
                    private readonly TableEntriesFactory _factory;

                    public TableEntriesSnapshot(
                        TableEntriesFactory factory, int version, ImmutableArray<TodoItem> items, ImmutableArray<ITrackingPoint> trackingPoints) :
                        base(version, GetProjectGuid(factory._workspace, factory._documentId.ProjectId), items, trackingPoints)
                    {
                        _factory = factory;
                    }

                    public override bool TryGetValue(int index, string columnName, out object content)
                    {
                        // REVIEW: this method is too-chatty to make async, but otherwise, how one can implement it async?
                        //         also, what is cancellation mechanism?
                        var item = GetItem(index);
                        if (item == null)
                        {
                            content = null;
                            return false;
                        }

                        switch (columnName)
                        {
                            case StandardTableKeyNames.Priority:
                                content = (VSTASKPRIORITY)item.Priority;
                                return true;
                            case StandardTableKeyNames.Text:
                                content = item.Message;
                                return true;
                            case StandardTableKeyNames.DocumentName:
                                content = GetFileName(item.OriginalFilePath, item.MappedFilePath);
                                return true;
                            case StandardTableKeyNames.Line:
                                content = GetLineColumn(item).Line;
                                return true;
                            case StandardTableKeyNames.Column:
                                content = GetLineColumn(item).Character;
                                return true;
                            case StandardTableKeyNames.ProjectName:
                                content = GetProjectName(_factory._workspace, _factory._documentId.ProjectId);
                                return content != null;
                            case StandardTableKeyNames.ProjectGuid:
                                content = ProjectGuid;
                                return ProjectGuid != Guid.Empty;
                            case StandardTableKeyNames.TaskCategory:
                                content = VSTASKCATEGORY.CAT_COMMENTS;
                                return true;
                            default:
                                content = null;
                                return false;
                        }
                    }

                    private LinePosition GetLineColumn(TodoItem item)
                    {
                        return VisualStudioVenusSpanMappingService.GetAdjustedLineColumn(
                            _factory._workspace,
                            _factory._documentId,
                            item.OriginalLine,
                            item.OriginalColumn,
                            item.MappedLine,
                            item.MappedColumn);
                    }

                    public override bool TryNavigateTo(int index, bool previewTab)
                    {
                        var item = GetItem(index);
                        if (item == null)
                        {
                            return false;
                        }

                        var trackingLinePosition = GetTrackingLineColumn(_factory._workspace, _factory._documentId, index);
                        if (trackingLinePosition != LinePosition.Zero)
                        {
                            return TryNavigateTo(_factory._workspace, _factory._documentId, trackingLinePosition.Line, trackingLinePosition.Character, previewTab);
                        }

                        return TryNavigateTo(_factory._workspace, _factory._documentId, item.OriginalLine, item.OriginalColumn, previewTab);
                    }

                    protected override bool IsEquivalent(TodoItem item1, TodoItem item2)
                    {
                        // everything same except location
                        return item1.DocumentId == item2.DocumentId && item1.Message == item2.Message;
                    }
                }
            }
        }
    }
}
