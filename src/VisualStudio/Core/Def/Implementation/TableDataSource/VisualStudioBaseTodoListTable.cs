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
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class VisualStudioBaseTodoListTable : AbstractTable<TaskListEventArgs, TodoTaskItem>
    {
        private static readonly string[] s_columns = new string[]
        {
            StandardTableColumnDefinitions.Priority,
            StandardTableColumnDefinitions.Text,
            StandardTableColumnDefinitions.ProjectName,
            StandardTableColumnDefinitions.DocumentName,
            StandardTableColumnDefinitions.Line,
            StandardTableColumnDefinitions.Column,
            StandardTableColumnDefinitions.TaskCategory
        };

        protected VisualStudioBaseTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, Guid identifier, ITableManagerProvider provider) :
            base(workspace, provider, StandardTables.TasksTable, new TableDataSource(workspace, todoListProvider, identifier))
        {
        }

        internal override IReadOnlyCollection<string> Columns { get { return s_columns; } }

        private class TableDataSource : AbstractRoslynTableDataSource<TaskListEventArgs, TodoTaskItem>
        {
            private readonly Workspace _workspace;
            private readonly Guid _identifier;
            private readonly ITodoListProvider _todoListProvider;

            public TableDataSource(Workspace workspace, ITodoListProvider todoListProvider, Guid identifier)
            {
                _workspace = workspace;
                _identifier = identifier;
                _todoListProvider = todoListProvider;
                _todoListProvider.TodoListUpdated += OnTodoListUpdated;

                ConnectToSolutionCrawlerService(_workspace);
            }

            public override string DisplayName
            {
                get
                {
                    return ServicesVSResources.TodoTableSourceName;
                }
            }

            public override Guid SourceTypeIdentifier
            {
                get
                {
                    return StandardTableDataSources.CommentTableDataSource;
                }
            }

            public override Guid Identifier
            {
                get
                {
                    return _identifier;
                }
            }

            private void OnTodoListUpdated(object sender, TaskListEventArgs e)
            {
                if (_workspace != e.Workspace)
                {
                    return;
                }

                Contract.Requires(e.DocumentId != null);

                if (e.TaskItems.Length == 0)
                {
                    OnDataRemoved(e.DocumentId);
                    return;
                }

                OnDataAddedOrChanged(e.DocumentId, e, e.TaskItems.Length);
            }

            protected override AbstractTableEntriesFactory<TodoTaskItem> CreateTableEntryFactory(object key, TaskListEventArgs data)
            {
                var documentId = (DocumentId)key;
                Contract.Requires(documentId == data.DocumentId);

                return new TableEntriesFactory(this, data.Workspace, data.DocumentId);
            }

            private class TableEntriesFactory : AbstractTableEntriesFactory<TodoTaskItem>
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

                protected override ImmutableArray<TodoTaskItem> GetItems()
                {
                    var provider = _source._todoListProvider;

                    // TODO: remove this wierd cast once we completely move off legacy task list. we, for now, need this since we share data
                    //       between old and new API.
                    return provider.GetTodoItems(_workspace, _documentId, CancellationToken.None).Cast<TodoTaskItem>().ToImmutableArray();
                }

                protected override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TodoTaskItem> items)
                {
                    return CreateTrackingPoints(_workspace, _documentId, items, (d, s) => CreateTrackingPoint(s, d.OriginalLine, d.OriginalColumn));
                }

                protected override AbstractTableEntriesSnapshot<TodoTaskItem> CreateSnapshot(int version, ImmutableArray<TodoTaskItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    return new TableEntriesSnapshot(this, version, items, trackingPoints);
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<TodoTaskItem>
                {
                    private readonly TableEntriesFactory _factory;

                    public TableEntriesSnapshot(
                        TableEntriesFactory factory, int version, ImmutableArray<TodoTaskItem> items, ImmutableArray<ITrackingPoint> trackingPoints) :
                        base(version, GetProjectGuid(factory._workspace, factory._documentId.ProjectId), items, trackingPoints)
                    {
                        _factory = factory;
                    }

                    public override object SnapshotIdentity
                    {
                        get
                        {
                            return _factory;
                        }
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
                            case ProjectGuidKey:
                                content = ProjectGuid;
                                return ProjectGuid != Guid.Empty;
                            case StandardTableKeyNames.Project:
                                // TODO: remove this once moved to new drop
                                content = GetHierarchy(_factory._workspace, _factory._documentId.ProjectId);
                                return content != null;
                            case StandardTableKeyNames.TaskCategory:
                                content = VSTASKCATEGORY.CAT_COMMENTS;
                                return true;
                            default:
                                content = null;
                                return false;
                        }
                    }

                    private LinePosition GetLineColumn(TodoTaskItem item)
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

                    protected override bool IsEquivalent(TodoTaskItem item1, TodoTaskItem item2)
                    {
                        // everything same except location
                        return item1.DocumentId == item2.DocumentId && item1.Message == item2.Message;
                    }
                }
            }
        }
    }
}
