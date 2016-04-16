// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        private class BuildTableDataSource : AbstractTableDataSource<DiagnosticData>
        {
            private readonly object _key = new object();

            private readonly Workspace _workspace;
            private readonly ExternalErrorDiagnosticUpdateSource _buildErrorSource;

            public BuildTableDataSource(Workspace workspace, ExternalErrorDiagnosticUpdateSource errorSource) :
                base(workspace)
            {
                _workspace = workspace;
                _buildErrorSource = errorSource;

                ConnectToBuildUpdateSource(errorSource);
            }

            private void ConnectToBuildUpdateSource(ExternalErrorDiagnosticUpdateSource errorSource)
            {
                if (errorSource == null)
                {
                    return;
                }

                SetStableState(errorSource.IsInProgress);

                errorSource.BuildStarted += OnBuildStarted;
            }

            private void OnBuildStarted(object sender, bool started)
            {
                SetStableState(started);

                if (!started)
                {
                    OnDataAddedOrChanged(_key);
                }
            }

            private void SetStableState(bool started)
            {
                IsStable = !started;
                ChangeStableState(IsStable);
            }

            public override string DisplayName => ServicesVSResources.BuildTableSourceName;
            public override string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
            public override string Identifier => IdentifierString;
            public override object GetItemKey(object data) => data;

            protected override object GetOrUpdateAggregationKey(object data)
            {
                return data;
            }

            public override AbstractTableEntriesSource<DiagnosticData> CreateTableEntriesSource(object data)
            {
                return new TableEntriesSource(this, _workspace);
            }

            public override ImmutableArray<TableItem<DiagnosticData>> Deduplicate(IEnumerable<IList<TableItem<DiagnosticData>>> groupedItems)
            {
                return groupedItems.MergeDuplicatesOrderedBy(Order);
            }

            public override ITrackingPoint CreateTrackingPoint(DiagnosticData data, ITextSnapshot snapshot)
            {
                return snapshot.CreateTrackingPoint(data.DataLocation?.OriginalStartLine ?? 0, data.DataLocation?.OriginalStartColumn ?? 0);
            }

            public override AbstractTableEntriesSnapshot<DiagnosticData> CreateSnapshot(AbstractTableEntriesSource<DiagnosticData> source, int version, ImmutableArray<TableItem<DiagnosticData>> items, ImmutableArray<ITrackingPoint> trackingPoints)
            {
                // Build doesn't support tracking point.
                return new TableEntriesSnapshot((DiagnosticTableEntriesSource)source, version, items);
            }

            private static IEnumerable<TableItem<DiagnosticData>> Order(IEnumerable<TableItem<DiagnosticData>> groupedItems)
            {
                // this should make order of result always deterministic.
                return groupedItems.OrderBy(d => d.Primary.ProjectId?.Id ?? Guid.Empty)
                                   .ThenBy(d => d.Primary.DocumentId?.Id ?? Guid.Empty)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalStartLine ?? 0)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalStartColumn ?? 0)
                                   .ThenBy(d => d.Primary.Id)
                                   .ThenBy(d => d.Primary.Message)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalEndLine ?? 0)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalEndColumn ?? 0);
            }

            private class TableEntriesSource : DiagnosticTableEntriesSource
            {
                private readonly BuildTableDataSource _source;
                private readonly Workspace _workspace;

                public TableEntriesSource(BuildTableDataSource source, Workspace workspace)
                {
                    _source = source;
                    _workspace = workspace;
                }

                public override object Key => _source._key;
                public override string BuildTool => PredefinedBuildTools.Build;
                public override bool SupportSpanTracking => false;
                public override DocumentId TrackingDocumentId => Contract.FailWithReturn<DocumentId>("This should never be called");

                public override ImmutableArray<TableItem<DiagnosticData>> GetItems()
                {
                    var groupedItems = _source._buildErrorSource
                                               .GetBuildErrors()
                                               .Select(d => new TableItem<DiagnosticData>(d, GenerateDeduplicationKey))
                                               .GroupBy(d => d.DeduplicationKey)
                                               .Select(g => (IList<TableItem<DiagnosticData>>)g)
                                               .ToImmutableArray();

                    return _source.Deduplicate(groupedItems);
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<DiagnosticData>> items)
                {
                    return ImmutableArray<ITrackingPoint>.Empty;
                }

                private int GenerateDeduplicationKey(DiagnosticData diagnostic)
                {
                    if (diagnostic.DataLocation == null ||
                        diagnostic.DataLocation.OriginalFilePath == null)
                    {
                        return diagnostic.GetHashCode();
                    }

                    return Hash.Combine(diagnostic.DataLocation.OriginalStartColumn,
                           Hash.Combine(diagnostic.DataLocation.OriginalStartLine,
                           Hash.Combine(diagnostic.DataLocation.OriginalEndColumn,
                           Hash.Combine(diagnostic.DataLocation.OriginalEndLine,
                           Hash.Combine(diagnostic.DataLocation.OriginalFilePath,
                           Hash.Combine(diagnostic.IsSuppressed,
                           Hash.Combine(diagnostic.Id.GetHashCode(), diagnostic.Message.GetHashCode())))))));
                }
            }

            private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticData>
            {
                private readonly DiagnosticTableEntriesSource _source;

                public TableEntriesSnapshot(
                    DiagnosticTableEntriesSource source, int version, ImmutableArray<TableItem<DiagnosticData>> items) :
                    base(version, items, ImmutableArray<ITrackingPoint>.Empty)
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
                        case StandardTableKeyNames.ErrorRank:
                            // build error gets highest rank
                            content = ValueTypeCache.GetOrCreate(ErrorRank.Lexical);
                            return content != null;
                        case StandardTableKeyNames.ErrorSeverity:
                            content = ValueTypeCache.GetOrCreate(GetErrorCategory(data.Severity));
                            return content != null;
                        case StandardTableKeyNames.ErrorCode:
                            content = data.Id;
                            return content != null;
                        case StandardTableKeyNames.ErrorCodeToolTip:
                            content = GetHelpLinkToolTipText(data);
                            return content != null;
                        case StandardTableKeyNames.HelpKeyword:
                            content = data.Id;
                            return content != null;
                        case StandardTableKeyNames.HelpLink:
                            content = GetHelpLink(data);
                            return content != null;
                        case StandardTableKeyNames.ErrorCategory:
                            content = data.Category;
                            return content != null;
                        case StandardTableKeyNames.ErrorSource:
                            content = ValueTypeCache.GetOrCreate(ErrorSource.Build);
                            return content != null;
                        case StandardTableKeyNames.BuildTool:
                            content = _source.BuildTool;
                            return content != null;
                        case StandardTableKeyNames.Text:
                            content = data.Message;
                            return content != null;
                        case StandardTableKeyNames.DocumentName:
                            content = GetFileName(data.DataLocation?.OriginalFilePath, data.DataLocation?.MappedFilePath);
                            return content != null;
                        case StandardTableKeyNames.Line:
                            content = data.DataLocation?.MappedStartLine ?? 0;
                            return true;
                        case StandardTableKeyNames.Column:
                            content = data.DataLocation?.MappedStartColumn ?? 0;
                            return true;
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
                        case SuppressionStateColumnDefinition.ColumnName:
                            // Build doesn't report suppressed diagnostics.
                            Contract.ThrowIfTrue(data.IsSuppressed);
                            content = ServicesVSResources.SuppressionStateActive;
                            return true;
                        default:
                            content = null;
                            return false;
                    }
                }

                public override bool TryNavigateTo(int index, bool previewTab)
                {
                    var item = GetItem(index)?.Primary;
                    if (item == null)
                    {
                        return false;
                    }

                    // this item is not navigatable
                    if (item.DocumentId == null)
                    {
                        return false;
                    }

                    return TryNavigateTo(item.Workspace, GetProperDocumentId(item),
                                         item.DataLocation?.OriginalStartLine ?? 0, item.DataLocation?.OriginalStartColumn ?? 0, previewTab);
                }

                private DocumentId GetProperDocumentId(DiagnosticData data)
                {
                    // check whether documentId still exist. it might have changed if project it belong to has reloaded.
                    var solution = data.Workspace.CurrentSolution;
                    if (solution.GetDocument(data.DocumentId) != null)
                    {
                        return data.DocumentId;
                    }

                    // okay, documentId no longer exist in current solution, find it by file path.
                    if (string.IsNullOrWhiteSpace(data.DataLocation?.OriginalFilePath))
                    {
                        // we don't have filepath
                        return null;
                    }

                    var documentIds = solution.GetDocumentIdsWithFilePath(data.DataLocation.OriginalFilePath);
                    foreach (var id in documentIds)
                    {
                        // found right documentId;
                        if (id.ProjectId == data.ProjectId)
                        {
                            return id;
                        }
                    }

                    // okay, there is no right one, take the first one if there is any
                    return documentIds.FirstOrDefault();
                }

                protected override bool IsEquivalent(DiagnosticData item1, DiagnosticData item2)
                {
                    // everything same except location
                    return item1.Id == item2.Id &&
                           item1.ProjectId == item2.ProjectId &&
                           item1.DocumentId == item2.DocumentId &&
                           item1.Category == item2.Category &&
                           item1.Severity == item2.Severity &&
                           item1.WarningLevel == item2.WarningLevel &&
                           item1.Message == item2.Message;
                }
            }
        }
    }
}
