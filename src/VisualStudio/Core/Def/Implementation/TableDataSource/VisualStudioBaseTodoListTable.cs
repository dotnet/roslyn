// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class VisualStudioBaseTodoListTable : AbstractTable
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

        protected VisualStudioBaseTodoListTable(Workspace workspace, ITodoListProvider todoListProvider, string identifier, ITableManagerProvider provider)
            : base(workspace, provider, StandardTables.TasksTable)
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

        private class TableDataSource : AbstractRoslynTableDataSource<TodoTableItem>
        {
            private readonly Workspace _workspace;
            private readonly string _identifier;
            private readonly ITodoListProvider _todoListProvider;

            public TableDataSource(Workspace workspace, ITodoListProvider todoListProvider, string identifier)
                : base(workspace)
            {
                _workspace = workspace;
                _identifier = identifier;

                _todoListProvider = todoListProvider;
                _todoListProvider.TodoListUpdated += OnTodoListUpdated;

                PopulateInitialData(workspace, _todoListProvider);
            }

            public override string DisplayName => ServicesVSResources.CSharp_VB_Todo_List_Table_Data_Source;
            public override string SourceTypeIdentifier => StandardTableDataSources.CommentTableDataSource;
            public override string Identifier => _identifier;
            public override object GetItemKey(object data) => ((UpdatedEventArgs)data).DocumentId;

            protected override object GetOrUpdateAggregationKey(object data)
            {
                var key = TryGetAggregateKey(data);
                if (key == null)
                {
                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                    return key;
                }

                if (!(key is ImmutableArray<DocumentId>))
                {
                    return key;
                }

                if (!CheckAggregateKey((ImmutableArray<DocumentId>)key, data as TodoItemsUpdatedArgs))
                {
                    RemoveStaledData(data);

                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                }

                return key;
            }

            private bool CheckAggregateKey(ImmutableArray<DocumentId> key, TodoItemsUpdatedArgs args)
            {
                if (args?.DocumentId == null || args?.Solution == null)
                {
                    return true;
                }

                var documents = GetDocumentsWithSameFilePath(args.Solution, args.DocumentId);
                return key == documents;
            }

            private object CreateAggregationKey(object data)
            {
                var args = data as TodoItemsUpdatedArgs;
                if (args?.Solution == null)
                {
                    return GetItemKey(data);
                }

                return GetDocumentsWithSameFilePath(args.Solution, args.DocumentId);
            }

            public override AbstractTableEntriesSnapshot<TodoTableItem> CreateSnapshot(AbstractTableEntriesSource<TodoTableItem> source, int version, ImmutableArray<TodoTableItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
            {
                return new TableEntriesSnapshot(version, items, trackingPoints);
            }

            public override IEqualityComparer<TodoTableItem> GroupingComparer
                => TodoTableItem.GroupingComparer.Instance;

            public override IEnumerable<TodoTableItem> Order(IEnumerable<TodoTableItem> groupedItems)
            {
                return groupedItems.OrderBy(d => d.Data.OriginalLine)
                                   .ThenBy(d => d.Data.OriginalColumn);
            }

            private void PopulateInitialData(Workspace workspace, ITodoListProvider todoListService)
            {
                foreach (var args in todoListService.GetTodoItemsUpdatedEventArgs(workspace, cancellationToken: CancellationToken.None))
                {
                    OnDataAddedOrChanged(args);
                }
            }

            private void OnTodoListUpdated(object sender, TodoItemsUpdatedArgs e)
            {
                if (_workspace != e.Workspace)
                {
                    return;
                }

                Debug.Assert(e.DocumentId != null);

                if (e.TodoItems.Length == 0)
                {
                    OnDataRemoved(e);
                    return;
                }

                OnDataAddedOrChanged(e);
            }

            public override AbstractTableEntriesSource<TodoTableItem> CreateTableEntriesSource(object data)
            {
                var item = (UpdatedEventArgs)data;
                return new TableEntriesSource(this, item.Workspace, item.DocumentId);
            }

            private sealed class TableEntriesSource : AbstractTableEntriesSource<TodoTableItem>
            {
                private readonly TableDataSource _source;
                private readonly Workspace _workspace;
                private readonly DocumentId _documentId;

                public TableEntriesSource(TableDataSource source, Workspace workspace, DocumentId documentId)
                {
                    _source = source;
                    _workspace = workspace;
                    _documentId = documentId;
                }

                public override object Key => _documentId;

                public override ImmutableArray<TodoTableItem> GetItems()
                {
                    return _source._todoListProvider.GetTodoItems(_workspace, _documentId, CancellationToken.None)
                                   .Select(data => TodoTableItem.Create(_workspace, data))
                                   .ToImmutableArray();
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TodoTableItem> items)
                {
                    return _workspace.CreateTrackingPoints(_documentId, items);
                }
            }

            private sealed class TableEntriesSnapshot : AbstractTableEntriesSnapshot<TodoTableItem>
            {
                public TableEntriesSnapshot(int version, ImmutableArray<TodoTableItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
                    : base(version, items, trackingPoints)
                {
                }

                public override bool TryGetValue(int index, string columnName, out object content)
                {
                    // REVIEW: this method is too-chatty to make async, but otherwise, how one can implement it async?
                    //         also, what is cancellation mechanism?
                    var item = GetItem(index);

                    var data = item?.Data;
                    if (data == null)
                    {
                        content = null;
                        return false;
                    }

                    switch (columnName)
                    {
                        case StandardTableKeyNames.Priority:
                            content = ValueTypeCache.GetOrCreate((VSTASKPRIORITY)data.Priority);
                            return content != null;
                        case StandardTableKeyNames.Text:
                            content = data.Message;
                            return content != null;
                        case StandardTableKeyNames.DocumentName:
                            content = GetFileName(data.OriginalFilePath, data.MappedFilePath);
                            return content != null;
                        case StandardTableKeyNames.Line:
                            content = GetLineColumn(item).Line;
                            return true;
                        case StandardTableKeyNames.Column:
                            content = GetLineColumn(item).Character;
                            return true;
                        case StandardTableKeyNames.TaskCategory:
                            content = ValueTypeCache.GetOrCreate(VSTASKCATEGORY.CAT_COMMENTS);
                            return content != null;
                        case StandardTableKeyNames.ProjectName:
                            content = item.ProjectName;
                            return content != null;
                        case ProjectNames:
                            var names = item.ProjectNames;
                            content = names;
                            return names.Length > 0;
                        case StandardTableKeyNames.ProjectGuid:
                            content = ValueTypeCache.GetOrCreate(item.ProjectGuid);
                            return (Guid)content != Guid.Empty;
                        case ProjectGuids:
                            var guids = item.ProjectGuids;
                            content = guids;
                            return guids.Length > 0;
                        default:
                            content = null;
                            return false;
                    }
                }

                // TODO: Apply location mapping when creating the TODO item (https://github.com/dotnet/roslyn/issues/36217)
                private LinePosition GetLineColumn(TodoTableItem item)
                {
                    return VisualStudioVenusSpanMappingService.GetAdjustedLineColumn(
                        item.Workspace,
                        item.Data.DocumentId,
                        item.Data.OriginalLine,
                        item.Data.OriginalColumn,
                        item.Data.MappedLine,
                        item.Data.MappedColumn);
                }

                public override bool TryNavigateTo(int index, bool previewTab)
                    => TryNavigateToItem(index, previewTab);
            }
        }
    }
}
