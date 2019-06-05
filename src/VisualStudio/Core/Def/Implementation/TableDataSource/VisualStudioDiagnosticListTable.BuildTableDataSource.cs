﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private class BuildTableDataSource : AbstractTableDataSource<DiagnosticTableItem>
        {
            private readonly object _key = new object();

            private readonly ExternalErrorDiagnosticUpdateSource _buildErrorSource;

            public BuildTableDataSource(Workspace workspace, ExternalErrorDiagnosticUpdateSource errorSource)
                : base(workspace)
            {
                _buildErrorSource = errorSource;

                ConnectToBuildUpdateSource(errorSource);
            }

            private void ConnectToBuildUpdateSource(ExternalErrorDiagnosticUpdateSource errorSource)
            {
                if (errorSource == null)
                {
                    // it can be null in unit test
                    return;
                }

                SetStableState(errorSource.IsInProgress);

                errorSource.BuildProgressChanged += OnBuildProgressChanged;
            }

            private void OnBuildProgressChanged(object sender, ExternalErrorDiagnosticUpdateSource.BuildProgress progress)
            {
                SetStableState(progress == ExternalErrorDiagnosticUpdateSource.BuildProgress.Done);

                if (progress != ExternalErrorDiagnosticUpdateSource.BuildProgress.Started)
                {
                    OnDataAddedOrChanged(_key);
                }
            }

            private void SetStableState(bool done)
            {
                IsStable = done;
                ChangeStableState(IsStable);
            }

            public override string DisplayName => ServicesVSResources.CSharp_VB_Build_Table_Data_Source;
            public override string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
            public override string Identifier => IdentifierString;
            public override object GetItemKey(object data) => data;

            protected override object GetOrUpdateAggregationKey(object data)
            {
                return data;
            }

            public override AbstractTableEntriesSource<DiagnosticTableItem> CreateTableEntriesSource(object data)
            {
                return new TableEntriesSource(this);
            }

            public override AbstractTableEntriesSnapshot<DiagnosticTableItem> CreateSnapshot(AbstractTableEntriesSource<DiagnosticTableItem> source, int version, ImmutableArray<DiagnosticTableItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
            {
                // Build doesn't support tracking point.
                return new TableEntriesSnapshot((DiagnosticTableEntriesSource)source, version, items);
            }

            public override IEnumerable<DiagnosticTableItem> Order(IEnumerable<DiagnosticTableItem> groupedItems)
            {
                // errors are already given in order. use it as it is.
                return groupedItems;
            }

            private class TableEntriesSource : DiagnosticTableEntriesSource
            {
                private readonly BuildTableDataSource _source;

                public TableEntriesSource(BuildTableDataSource source)
                {
                    _source = source;
                }

                public override object Key => _source._key;
                public override string BuildTool => PredefinedBuildTools.Build;
                public override bool SupportSpanTracking => false;
                public override DocumentId TrackingDocumentId => Contract.FailWithReturn<DocumentId>("This should never be called");

                public override ImmutableArray<DiagnosticTableItem> GetItems()
                {
                    var groupedItems = _source._buildErrorSource
                                               .GetBuildErrors()
                                               .Select(data => new DiagnosticTableItem(_source.Workspace, cache: null, data))
                                               .GroupBy(d => d.DeduplicationKey)
                                               .Select(g => (IList<DiagnosticTableItem>)g)
                                               .ToImmutableArray();

                    return _source.Deduplicate(groupedItems);
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<DiagnosticTableItem> items)
                {
                    return ImmutableArray<ITrackingPoint>.Empty;
                }
            }

            private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticTableItem>
            {
                private readonly DiagnosticTableEntriesSource _source;

                public TableEntriesSnapshot(
                    DiagnosticTableEntriesSource source, int version, ImmutableArray<DiagnosticTableItem> items) :
                    base(version, items, ImmutableArray<ITrackingPoint>.Empty)
                {
                    _source = source;
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

                    var data = item.Data;
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
                            content = GetHelpLinkToolTipText(item.Workspace, data);
                            return content != null;
                        case StandardTableKeyNames.HelpKeyword:
                            content = data.Id;
                            return content != null;
                        case StandardTableKeyNames.HelpLink:
                            content = GetHelpLink(item.Workspace, data);
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
                            // Build doesn't support suppression.
                            Contract.ThrowIfTrue(data.IsSuppressed);
                            content = ServicesVSResources.NotApplicable;
                            return true;
                        default:
                            content = null;
                            return false;
                    }
                }

                public override bool TryNavigateTo(int index, bool previewTab)
                {
                    var item = GetItem(index);
                    if (item?.DocumentId == null)
                    {
                        return false;
                    }

                    var documentId = GetProperDocumentId(item);
                    var solution = item.Workspace.CurrentSolution;

                    return solution.ContainsDocument(documentId) &&
                        TryNavigateTo(item.Workspace, documentId, item.GetOriginalPosition(), previewTab);
                }

                private DocumentId GetProperDocumentId(DiagnosticTableItem item)
                {
                    var documentId = item.DocumentId;
                    var projectId = item.ProjectId;

                    // check whether documentId still exist. it might have changed if project it belong to has reloaded.
                    var solution = item.Workspace.CurrentSolution;
                    if (solution.GetDocument(documentId) != null)
                    {
                        return documentId;
                    }

                    // okay, documentId no longer exist in current solution, find it by file path.
                    var filePath = item.GetOriginalFilePath();
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        return null;
                    }

                    var documentIds = solution.GetDocumentIdsWithFilePath(filePath);
                    foreach (var id in documentIds)
                    {
                        // found right project
                        if (id.ProjectId == projectId)
                        {
                            return id;
                        }
                    }

                    // okay, there is no right one, take the first one if there is any
                    return documentIds.FirstOrDefault();
                }
            }
        }
    }
}
