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
                    OnDataAddedOrChanged(null);
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
            public override object GetItemKey(object data) => this;

            protected override object GetAggregationKey(object data)
            {
                return this;
            }

            public override AbstractTableEntriesSource<DiagnosticData> CreateTableEntrySource(object data)
            {
                return new TableEntriesSource(this, _workspace);
            }

            public override ImmutableArray<TableItem<DiagnosticData>> Deduplicate(IEnumerable<IList<TableItem<DiagnosticData>>> groupedItems)
            {
                return groupedItems.MergeDuplicatesOrderedBy(Order);
            }

            public override ITrackingPoint CreateTrackingPoint(DiagnosticData data, ITextSnapshot snapshot)
            {
                return Contract.FailWithReturn<ITrackingPoint>("Build doesn't support tracking point");
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

            private class TableEntriesSource : AbstractTableEntriesSource<DiagnosticData>
            {
                private readonly BuildTableDataSource _source;
                private readonly Workspace _workspace;

                public TableEntriesSource(BuildTableDataSource source, Workspace workspace)
                {
                    _source = source;
                    _workspace = workspace;
                }

                public override object Key => this;

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

                public override AbstractTableEntriesSnapshot<DiagnosticData> CreateSnapshot(
                    int version, ImmutableArray<TableItem<DiagnosticData>> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    return new TableEntriesSnapshot(this, version, items);
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
                           Hash.Combine(diagnostic.Id.GetHashCode(), diagnostic.Message.GetHashCode()))))));
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticData>
                {
                    private readonly TableEntriesSource _factorySource;

                    public TableEntriesSnapshot(
                        TableEntriesSource factorySource, int version, ImmutableArray<TableItem<DiagnosticData>> items) :
                        base(version, Guid.Empty, items, ImmutableArray<ITrackingPoint>.Empty)
                    {
                        _factorySource = factorySource;
                    }

                    public override bool TryGetValue(int index, string columnName, out object content)
                    {
                        // REVIEW: this method is too-chatty to make async, but otherwise, how one can implement it async?
                        //         also, what is cancellation mechanism?
                        var data = GetItem(index);

                        var item = data.Primary;
                        if (item == null)
                        {
                            content = null;
                            return false;
                        }

                        switch (columnName)
                        {
                            case StandardTableKeyNames.ErrorRank:
                                content = WellKnownDiagnosticTags.Build;
                                return true;
                            case StandardTableKeyNames.ErrorSeverity:
                                content = GetErrorCategory(item.Severity);
                                return true;
                            case StandardTableKeyNames.ErrorCode:
                                content = item.Id;
                                return true;
                            case StandardTableKeyNames.ErrorCodeToolTip:
                                content = GetHelpLinkToolTipText(item);
                                return content != null;
                            case StandardTableKeyNames.HelpLink:
                                content = GetHelpLink(item);
                                return content != null;
                            case StandardTableKeyNames.ErrorCategory:
                                content = item.Category;
                                return true;
                            case StandardTableKeyNames.ErrorSource:
                                content = ErrorSource.Build;
                                return true;
                            case StandardTableKeyNames.BuildTool:
                                content = PredefinedBuildTools.Build;
                                return true;
                            case StandardTableKeyNames.Text:
                                content = item.Message;
                                return true;
                            case StandardTableKeyNames.DocumentName:
                                content = GetFileName(item.DataLocation?.OriginalFilePath, item.DataLocation?.MappedFilePath);
                                return true;
                            case StandardTableKeyNames.Line:
                                content = item.DataLocation?.MappedStartLine ?? 0;
                                return true;
                            case StandardTableKeyNames.Column:
                                content = item.DataLocation?.MappedStartColumn ?? 0;
                                return true;
                            case StandardTableKeyNames.ProjectName:
                                // TODO: make it multiple projectId
                                content = GetProjectName(_factorySource._workspace, item.ProjectId);
                                return content != null;
                            case StandardTableKeyNames.ProjectGuid:
                                // TODO: same here
                                var guid = GetProjectGuid(_factorySource._workspace, item.ProjectId);
                                content = guid;
                                return guid != Guid.Empty;
                            default:
                                content = null;
                                return false;
                        }
                    }

                    public override bool TryNavigateTo(int index, bool previewTab)
                    {
                        var item = GetItem(index).Primary;
                        if (item == null)
                        {
                            return false;
                        }

                        // this item is not navigatable
                        if (item.DocumentId == null)
                        {
                            return false;
                        }

                        return TryNavigateTo(_factorySource._workspace, item.DocumentId,
                                             item.DataLocation?.OriginalStartLine ?? 0, item.DataLocation?.OriginalStartColumn ?? 0, previewTab);
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
}
