// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.TaskList;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal sealed class VisualStudioTaskListTable : AbstractTable
    {
        internal const string IdentifierString = nameof(VisualStudioTaskListTable);

        private readonly TableDataSource _source;

        public VisualStudioTaskListTable(
            Workspace workspace,
            IThreadingContext threadingContext,
            ITableManagerProvider provider,
            ITaskListProvider taskProvider)
            : base(workspace, provider, StandardTables.TasksTable)
        {
            _source = new TableDataSource(workspace, threadingContext, taskProvider, IdentifierString);
            AddInitialTableSource(workspace.CurrentSolution, _source);
            ConnectWorkspaceEvents();
        }

        internal override ImmutableArray<string> Columns { get; } = ImmutableArray.Create(
            StandardTableColumnDefinitions.Priority,
            StandardTableColumnDefinitions.Text,
            StandardTableColumnDefinitions.ProjectName,
            StandardTableColumnDefinitions.DocumentName,
            StandardTableColumnDefinitions.Line,
            StandardTableColumnDefinitions.Column);

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
            => _source.Shutdown();

        private class TableDataSource : AbstractRoslynTableDataSource<TaskListTableItem, TaskListUpdatedArgs>
        {
            private readonly Workspace _workspace;
            private readonly string _identifier;
            private readonly ITaskListProvider _taskProvider;

            public TableDataSource(Workspace workspace, IThreadingContext threadingContext, ITaskListProvider taskProvider, string identifier)
                : base(workspace, threadingContext)
            {
                _workspace = workspace;
                _identifier = identifier;

                _taskProvider = taskProvider;
                _taskProvider.TaskListUpdated += OnTaskListUpdated;
            }

            public override string DisplayName => ServicesVSResources.CSharp_VB_Todo_List_Table_Data_Source;
            public override string SourceTypeIdentifier => StandardTableDataSources.CommentTableDataSource;
            public override string Identifier => _identifier;
            public override object GetItemKey(TaskListUpdatedArgs data) => data.DocumentId;

            protected override object GetOrUpdateAggregationKey(TaskListUpdatedArgs data)
            {
                var key = TryGetAggregateKey(data);
                if (key == null)
                {
                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                    return key;
                }

                if (key is not ImmutableArray<DocumentId>)
                {
                    return key;
                }

                if (!CheckAggregateKey((ImmutableArray<DocumentId>)key, data))
                {
                    RemoveStaledData(data);

                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                }

                return key;
            }

            private bool CheckAggregateKey(ImmutableArray<DocumentId> key, TaskListUpdatedArgs args)
            {
                Contract.ThrowIfNull(args.Solution);
                Contract.ThrowIfNull(args.DocumentId);
                var documents = GetDocumentsWithSameFilePath(args.Solution, args.DocumentId);
                return key == documents;
            }

            private object CreateAggregationKey(TaskListUpdatedArgs data)
            {
                Contract.ThrowIfNull(data.Solution);
                return GetDocumentsWithSameFilePath(data.Solution, data.DocumentId);
            }

            public override AbstractTableEntriesSnapshot<TaskListTableItem> CreateSnapshot(AbstractTableEntriesSource<TaskListTableItem> source, int version, ImmutableArray<TaskListTableItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
                => new TableEntriesSnapshot(ThreadingContext, version, items, trackingPoints);

            public override IEqualityComparer<TaskListTableItem> GroupingComparer
                => TaskListTableItem.GroupingComparer.Instance;

            public override IEnumerable<TaskListTableItem> Order(IEnumerable<TaskListTableItem> groupedItems)
                => groupedItems.OrderBy(d => d.Data.Span.StartLinePosition);

            private void OnTaskListUpdated(object sender, TaskListUpdatedArgs e)
            {
                if (_workspace != e.Workspace)
                {
                    return;
                }

                Debug.Assert(e.DocumentId != null);

                if (e.TaskListItems.Length == 0)
                {
                    OnDataRemoved(e);
                    return;
                }

                OnDataAddedOrChanged(e);
            }

            public override AbstractTableEntriesSource<TaskListTableItem> CreateTableEntriesSource(object data)
            {
                var item = (TaskListUpdatedArgs)data;
                return new TableEntriesSource(this, item.Workspace, item.DocumentId);
            }

            private sealed class TableEntriesSource : AbstractTableEntriesSource<TaskListTableItem>
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

                public override ImmutableArray<TaskListTableItem> GetItems()
                {
                    return _source._taskProvider.GetTaskListItems(_workspace, _documentId, CancellationToken.None)
                                   .Select(data => TaskListTableItem.Create(_workspace, data))
                                   .ToImmutableArray();
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TaskListTableItem> items)
                    => _workspace.CreateTrackingPoints(_documentId, items);
            }

            private sealed class TableEntriesSnapshot : AbstractTableEntriesSnapshot<TaskListTableItem>
            {
                public TableEntriesSnapshot(IThreadingContext threadingContext, int version, ImmutableArray<TaskListTableItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
                    : base(threadingContext, version, items, trackingPoints)
                {
                }

                public override bool TryGetValue(int index, string columnName, [NotNullWhen(true)] out object? content)
                {
                    // REVIEW: this method is too-chatty to make async, but otherwise, how one can implement it async?
                    //         also, what is cancellation mechanism?
                    var item = GetItem(index);

                    if (item is not { Data: var data })
                    {
                        content = null;
                        return false;
                    }

                    switch (columnName)
                    {
                        case StandardTableKeyNames.Priority:
                            content = ValueTypeCache.GetOrCreate(data.Priority switch
                            {
                                TaskListItemPriority.Low => VSTASKPRIORITY.TP_LOW,
                                TaskListItemPriority.Medium => VSTASKPRIORITY.TP_NORMAL,
                                TaskListItemPriority.High => VSTASKPRIORITY.TP_HIGH,
                                _ => VSTASKPRIORITY.TP_NORMAL,
                            });
                            return content != null;
                        case StandardTableKeyNames.Text:
                            content = data.Message;
                            return content != null;
                        case StandardTableKeyNames.DocumentName:
                            content = data.MappedSpan.HasMappedPath ? data.MappedSpan.Path : data.Span.Path;
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
                private static LinePosition GetLineColumn(TaskListTableItem item)
                {
                    return VisualStudioVenusSpanMappingService.GetAdjustedLineColumn(
                        item.Data.DocumentId,
                        item.Data.Span.StartLinePosition.Line,
                        item.Data.Span.StartLinePosition.Character,
                        item.Data.MappedSpan.StartLinePosition.Line,
                        item.Data.MappedSpan.StartLinePosition.Character);
                }

                public override bool TryNavigateTo(int index, NavigationOptions options, CancellationToken cancellationToken)
                    => TryNavigateToItem(index, options, cancellationToken);
            }
        }
    }
}
