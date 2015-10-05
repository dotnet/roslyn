// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private class TableDataSource : AbstractRoslynTableDataSource<TodoItem>
        {
            private readonly Workspace _workspace;
            private readonly string _identifier;
            private readonly ITodoListProvider _todoListProvider;

            public TableDataSource(Workspace workspace, ITodoListProvider todoListProvider, string identifier) :
                base(workspace)
            {
                _workspace = workspace;
                _identifier = identifier;

                _todoListProvider = todoListProvider;
                _todoListProvider.TodoListUpdated += OnTodoListUpdated;

                PopulateInitialData(workspace, _todoListProvider);
            }

            public override string DisplayName => ServicesVSResources.TodoTableSourceName;
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

                var documents = args.Solution.GetRelatedDocumentIds(args.DocumentId);
                return key == documents;
            }

            private object CreateAggregationKey(object data)
            {
                var args = data as TodoItemsUpdatedArgs;
                if (args?.Solution == null)
                {
                    return GetItemKey(data);
                }

                return args.Solution.GetRelatedDocumentIds(args.DocumentId);
            }

            public override ImmutableArray<TableItem<TodoItem>> Deduplicate(IEnumerable<IList<TableItem<TodoItem>>> groupedItems)
            {
                return groupedItems.MergeDuplicatesOrderedBy(Order);
            }

            public override ITrackingPoint CreateTrackingPoint(TodoItem data, ITextSnapshot snapshot)
            {
                return snapshot.CreateTrackingPoint(data.OriginalLine, data.OriginalColumn);
            }

            public override AbstractTableEntriesSnapshot<TodoItem> CreateSnapshot(AbstractTableEntriesSource<TodoItem> source, int version, ImmutableArray<TableItem<TodoItem>> items, ImmutableArray<ITrackingPoint> trackingPoints)
            {
                return new TableEntriesSnapshot(source, version, items, trackingPoints);
            }

            private static IEnumerable<TableItem<TodoItem>> Order(IEnumerable<TableItem<TodoItem>> groupedItems)
            {
                return groupedItems.OrderBy(d => d.Primary.OriginalLine)
                                   .ThenBy(d => d.Primary.OriginalColumn);
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

                Contract.Requires(e.DocumentId != null);

                if (e.TodoItems.Length == 0)
                {
                    OnDataRemoved(e);
                    return;
                }

                OnDataAddedOrChanged(e);
            }

            public override AbstractTableEntriesSource<TodoItem> CreateTableEntriesSource(object data)
            {
                var item = (UpdatedEventArgs)data;
                return new TableEntriesSource(this, item.Workspace, item.DocumentId);
            }

            private class TableEntriesSource : AbstractTableEntriesSource<TodoItem>
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

                public override ImmutableArray<TableItem<TodoItem>> GetItems()
                {
                    var provider = _source._todoListProvider;

                    return provider.GetTodoItems(_workspace, _documentId, CancellationToken.None)
                                   .Select(i => new TableItem<TodoItem>(i, GenerateDeduplicationKey))
                                   .ToImmutableArray();
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<TodoItem>> items)
                {
                    return _workspace.CreateTrackingPoints(_documentId, items, _source.CreateTrackingPoint);
                }

                private int GenerateDeduplicationKey(TodoItem item)
                {
                    return Hash.Combine(item.OriginalColumn, item.OriginalLine);
                }
            }

            private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<TodoItem>
            {
                private readonly AbstractTableEntriesSource<TodoItem> _source;

                public TableEntriesSnapshot(
                    AbstractTableEntriesSource<TodoItem> source, int version, ImmutableArray<TableItem<TodoItem>> items, ImmutableArray<ITrackingPoint> trackingPoints) :
                    base(version, items, trackingPoints)
                {
                    _source = source;
                }

                public override bool TryGetValue(int index, string columnName, out object content)
                {
                    // REVIEW: this method is too-chatty to make async, but otherwise, how one can implement it async?
                    //         also, what is cancellation mechanism?
                    var item = GetItem(index);

                    var data = item?.Primary;
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
                            content = GetLineColumn(data).Line;
                            return true;
                        case StandardTableKeyNames.Column:
                            content = GetLineColumn(data).Character;
                            return true;
                        case StandardTableKeyNames.TaskCategory:
                            content = ValueTypeCache.GetOrCreate(VSTASKCATEGORY.CAT_COMMENTS);
                            return content != null;
                        case StandardTableKeyNames.ProjectName:
                            content = item.ProjectName;
                            return content != null;
                        case ProjectNames:
                            content = item.ProjectNames;
                            return ((string[])content).Length > 0;
                        case StandardTableKeyNames.ProjectGuid:
                            content = ValueTypeCache.GetOrCreate(item.ProjectGuid);
                            return (Guid)content != Guid.Empty;
                        case ProjectGuids:
                            content = item.ProjectGuids;
                            return ((Guid[])content).Length > 0;
                        default:
                            content = null;
                            return false;
                    }
                }

                private LinePosition GetLineColumn(TodoItem item)
                {
                    return VisualStudioVenusSpanMappingService.GetAdjustedLineColumn(
                        item.Workspace,
                        item.DocumentId,
                        item.OriginalLine,
                        item.OriginalColumn,
                        item.MappedLine,
                        item.MappedColumn);
                }

                public override bool TryNavigateTo(int index, bool previewTab)
                {
                    var item = GetItem(index)?.Primary;
                    if (item == null)
                    {
                        return false;
                    }

                    var trackingLinePosition = GetTrackingLineColumn(item.Workspace, item.DocumentId, index);
                    if (trackingLinePosition != LinePosition.Zero)
                    {
                        return TryNavigateTo(item.Workspace, item.DocumentId, trackingLinePosition.Line, trackingLinePosition.Character, previewTab);
                    }

                    return TryNavigateTo(item.Workspace, item.DocumentId, item.OriginalLine, item.OriginalColumn, previewTab);
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
