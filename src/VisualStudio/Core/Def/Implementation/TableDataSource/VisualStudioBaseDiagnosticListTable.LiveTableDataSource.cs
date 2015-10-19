// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV1;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract partial class VisualStudioBaseDiagnosticListTable
    {
        protected class LiveTableDataSource : AbstractRoslynTableDataSource<DiagnosticData>
        {
            private readonly string _identifier;
            private readonly IDiagnosticService _diagnosticService;
            private readonly IServiceProvider _serviceProvider;
            private readonly Workspace _workspace;
            private readonly OpenDocumentTracker<DiagnosticData> _tracker;

            public LiveTableDataSource(IServiceProvider serviceProvider, Workspace workspace, IDiagnosticService diagnosticService, string identifier) :
                base(workspace)
            {
                _workspace = workspace;
                _serviceProvider = serviceProvider;
                _identifier = identifier;

                _tracker = new OpenDocumentTracker<DiagnosticData>(_workspace);

                _diagnosticService = diagnosticService;
                _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;

                PopulateInitialData(workspace, diagnosticService);
            }

            public override string DisplayName => ServicesVSResources.DiagnosticsTableSourceName;
            public override string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
            public override string Identifier => _identifier;
            public override object GetItemKey(object data) => ((UpdatedEventArgs)data).Id;

            public override ImmutableArray<TableItem<DiagnosticData>> Deduplicate(IEnumerable<IList<TableItem<DiagnosticData>>> groupedItems)
            {
                return groupedItems.MergeDuplicatesOrderedBy(Order);
            }

            public override ITrackingPoint CreateTrackingPoint(DiagnosticData data, ITextSnapshot snapshot)
            {
                return snapshot.CreateTrackingPoint(data.DataLocation?.OriginalStartLine ?? 0, data.DataLocation?.OriginalStartColumn ?? 0);
            }

            public override AbstractTableEntriesSnapshot<DiagnosticData> CreateSnapshot(
                AbstractTableEntriesSource<DiagnosticData> source,
                int version,
                ImmutableArray<TableItem<DiagnosticData>> items,
                ImmutableArray<ITrackingPoint> trackingPoints)
            {
                var diagnosticSource = (DiagnosticTableEntriesSource)source;
                var snapshot = new TableEntriesSnapshot(diagnosticSource, version, items, trackingPoints);

                if (diagnosticSource.SupportSpanTracking && !trackingPoints.IsDefaultOrEmpty)
                {
                    // track the open document so that we can throw away tracking points on document close properly
                    _tracker.TrackOpenDocument(diagnosticSource.TrackingDocumentId, diagnosticSource.Key, snapshot);
                }

                return snapshot;
            }

            protected override object GetOrUpdateAggregationKey(object data)
            {
                var key = TryGetAggregateKey(data);
                if (key == null)
                {
                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                    return key;
                }

                if (!CheckAggregateKey(key as AggregatedKey, data as DiagnosticsUpdatedArgs))
                {
                    RemoveStaledData(data);

                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                }

                return key;
            }

            private bool CheckAggregateKey(AggregatedKey key, DiagnosticsUpdatedArgs args)
            {
                if (key == null)
                {
                    return true;
                }

                if (args?.DocumentId == null || args?.Solution == null)
                {
                    return true;
                }

                var documents = args.Solution.GetRelatedDocumentIds(args.DocumentId);
                return key.DocumentIds == documents;
            }

            private object CreateAggregationKey(object data)
            {
                var args = data as DiagnosticsUpdatedArgs;
                if (args?.DocumentId == null || args?.Solution == null)
                {
                    return GetItemKey(data);
                }

                var argumentKey = args.Id as DiagnosticIncrementalAnalyzer.ArgumentKey;
                if (argumentKey == null)
                {
                    return GetItemKey(data);
                }

                var documents = args.Solution.GetRelatedDocumentIds(args.DocumentId);
                return new AggregatedKey(documents, argumentKey.Analyzer, argumentKey.StateType);
            }

            private void PopulateInitialData(Workspace workspace, IDiagnosticService diagnosticService)
            {
                foreach (var args in diagnosticService.GetDiagnosticsUpdatedEventArgs(workspace, projectId: null, documentId: null, cancellationToken: CancellationToken.None))
                {
                    OnDataAddedOrChanged(args);
                }
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                if (_workspace != e.Workspace)
                {
                    return;
                }

                if (e.Diagnostics.Length == 0)
                {
                    OnDataRemoved(e);
                    return;
                }

                var count = e.Diagnostics.Where(ShouldInclude).Count();
                if (count <= 0)
                {
                    OnDataRemoved(e);
                    return;
                }

                OnDataAddedOrChanged(e);
            }

            public override AbstractTableEntriesSource<DiagnosticData> CreateTableEntriesSource(object data)
            {
                var item = (UpdatedEventArgs)data;
                return new TableEntriesSource(this, item.Workspace, item.ProjectId, item.DocumentId, item.Id);
            }

            private static bool ShouldInclude(DiagnosticData diagnostic)
            {
                if (diagnostic == null)
                {
                    // guard us from wrong provider that gives null diagnostic
                    Contract.Requires(false, "Let's see who does this");
                    return false;
                }

                return diagnostic?.Severity != DiagnosticSeverity.Hidden;
            }

            private static IEnumerable<TableItem<DiagnosticData>> Order(IEnumerable<TableItem<DiagnosticData>> groupedItems)
            {
                // this should make order of result always deterministic. we only need these 6 values since data with all these same will merged to one.
                return groupedItems.OrderBy(d => d.Primary.DataLocation?.OriginalStartLine ?? 0)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalStartColumn ?? 0)
                                   .ThenBy(d => d.Primary.Id)
                                   .ThenBy(d => d.Primary.Message)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalEndLine ?? 0)
                                   .ThenBy(d => d.Primary.DataLocation?.OriginalEndColumn ?? 0);
            }

            private class TableEntriesSource : DiagnosticTableEntriesSource
            {
                private readonly LiveTableDataSource _source;
                private readonly Workspace _workspace;
                private readonly ProjectId _projectId;
                private readonly DocumentId _documentId;
                private readonly object _id;
                private readonly string _buildTool;

                public TableEntriesSource(LiveTableDataSource source, Workspace workspace, ProjectId projectId, DocumentId documentId, object id)
                {
                    _source = source;
                    _workspace = workspace;
                    _projectId = projectId;
                    _documentId = documentId;
                    _id = id;
                    _buildTool = (id as BuildToolId)?.BuildTool ?? string.Empty;
                }

                public override object Key => _id;
                public override string BuildTool => _buildTool;
                public override bool SupportSpanTracking => _documentId != null;
                public override DocumentId TrackingDocumentId => _documentId;

                public override ImmutableArray<TableItem<DiagnosticData>> GetItems()
                {
                    var provider = _source._diagnosticService;
                    var items = provider.GetDiagnostics(_workspace, _projectId, _documentId, _id, includeSuppressedDiagnostics: true, cancellationToken: CancellationToken.None)
                                        .Where(ShouldInclude).Select(d => new TableItem<DiagnosticData>(d, GenerateDeduplicationKey));

                    return items.ToImmutableArrayOrEmpty();
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<DiagnosticData>> items)
                {
                    return _workspace.CreateTrackingPoints(_documentId, items, _source.CreateTrackingPoint);
                }

                private int GenerateDeduplicationKey(DiagnosticData diagnostic)
                {
                    if (diagnostic.DataLocation == null)
                    {
                        return diagnostic.GetHashCode();
                    }

                    return Hash.Combine(diagnostic.DataLocation.OriginalStartColumn,
                           Hash.Combine(diagnostic.DataLocation.OriginalStartLine,
                           Hash.Combine(diagnostic.DataLocation.OriginalEndColumn,
                           Hash.Combine(diagnostic.DataLocation.OriginalEndLine,
                           Hash.Combine(diagnostic.IsSuppressed,
                           Hash.Combine(diagnostic.Id.GetHashCode(), diagnostic.Message.GetHashCode()))))));
                }
            }

            private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticData>, IWpfTableEntriesSnapshot
            {
                private readonly DiagnosticTableEntriesSource _source;
                private FrameworkElement[] _descriptions;

                public TableEntriesSnapshot(
                DiagnosticTableEntriesSource source, int version, ImmutableArray<TableItem<DiagnosticData>> items, ImmutableArray<ITrackingPoint> trackingPoints) :
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
                        case StandardTableKeyNames.ErrorRank:
                            content = ValueTypeCache.GetOrCreate(GetErrorRank(data));
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
                            content = ValueTypeCache.GetOrCreate(GetErrorSource(_source.BuildTool));
                            return content != null;
                        case StandardTableKeyNames.BuildTool:
                            content = GetBuildTool(_source.BuildTool);
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
                            content = data.IsSuppressed ? ServicesVSResources.SuppressionStateSuppressed : ServicesVSResources.SuppressionStateActive;
                            return true;
                        default:
                            content = null;
                            return false;
                    }
                }

                private string GetBuildTool(string buildTool)
                {
                    // for build tool, regardless where error is coming from ("build" or "live"), 
                    // we show "compiler" to users.
                    if (buildTool == PredefinedBuildTools.Live)
                    {
                        return PredefinedBuildTools.Build;
                    }

                    return _source.BuildTool;
                }

                private ErrorSource GetErrorSource(string buildTool)
                {
                    if (buildTool == PredefinedBuildTools.Build)
                    {
                        return ErrorSource.Build;
                    }

                    return ErrorSource.Other;
                }

                private ErrorRank GetErrorRank(DiagnosticData item)
                {
                    string value;
                    if (!item.Properties.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, out value))
                    {
                        return ErrorRank.Other;
                    }

                    switch (value)
                    {

                        case WellKnownDiagnosticTags.Build:
                            // any error from build is highest priority
                            return ErrorRank.Lexical;
                        case nameof(ErrorRank.Lexical):
                            return ErrorRank.Lexical;
                        case nameof(ErrorRank.Syntactic):
                            return ErrorRank.Syntactic;
                        case nameof(ErrorRank.Declaration):
                            return ErrorRank.Declaration;
                        case nameof(ErrorRank.Semantic):
                            return ErrorRank.Semantic;
                        case nameof(ErrorRank.Emit):
                            return ErrorRank.Emit;
                        case nameof(ErrorRank.PostBuild):
                            return ErrorRank.PostBuild;
                        default:
                            return ErrorRank.Other;
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

                    var trackingLinePosition = GetTrackingLineColumn(item.Workspace, item.DocumentId, index);
                    if (trackingLinePosition != LinePosition.Zero)
                    {
                        return TryNavigateTo(item.Workspace, item.DocumentId, trackingLinePosition.Line, trackingLinePosition.Character, previewTab);
                    }

                    return TryNavigateTo(item.Workspace, item.DocumentId,
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

                #region IWpfTableEntriesSnapshot

                public bool CanCreateDetailsContent(int index)
                {
                    var item = GetItem(index)?.Primary;
                    if (item == null)
                    {
                        return false;
                    }

                    return !string.IsNullOrWhiteSpace(item.Description);
                }

                public bool TryCreateDetailsContent(int index, out FrameworkElement expandedContent)
                {
                    var item = GetItem(index)?.Primary;
                    if (item == null)
                    {
                        expandedContent = default(FrameworkElement);
                        return false;
                    }

                    expandedContent = GetOrCreateTextBlock(ref _descriptions, this.Count, index, item, i => GetDescriptionTextBlock(i));
                    return true;
                }

                public bool TryCreateDetailsStringContent(int index, out string content)
                {
                    var item = GetItem(index)?.Primary;
                    if (item == null)
                    {
                        content = default(string);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(item.Description))
                    {
                        content = default(string);
                        return false;
                    }

                    content = item.Description;
                    return true;
                }

                private static FrameworkElement GetDescriptionTextBlock(DiagnosticData item)
                {
                    return new TextBlock()
                    {
                        Background = null,
                        Padding = new Thickness(10, 6, 10, 8),
                        TextWrapping = TextWrapping.Wrap,
                        Text = item.Description
                    };
                }

                private static FrameworkElement GetOrCreateTextBlock(
                    ref FrameworkElement[] caches, int count, int index, DiagnosticData item, Func<DiagnosticData, FrameworkElement> elementCreator)
                {
                    if (caches == null)
                    {
                        caches = new FrameworkElement[count];
                    }

                    if (caches[index] == null)
                    {
                        caches[index] = elementCreator(item);
                    }

                    return caches[index];
                }

                // unused ones                    
                public bool TryCreateColumnContent(int index, string columnName, bool singleColumnView, out FrameworkElement content)
                {
                    content = default(FrameworkElement);
                    return false;
                }

                public bool TryCreateImageContent(int index, string columnName, bool singleColumnView, out ImageMoniker content)
                {
                    content = default(ImageMoniker);
                    return false;
                }

                public bool TryCreateStringContent(int index, string columnName, bool truncatedText, bool singleColumnView, out string content)
                {
                    content = default(string);
                    return false;
                }

                public bool TryCreateToolTip(int index, string columnName, out object toolTip)
                {
                    toolTip = default(object);
                    return false;
                }

                // remove this once we moved to new drop
                public bool TryCreateStringContent(int index, string columnName, bool singleColumnView, out string content)
                {
                    content = default(string);
                    return false;
                }

                #endregion
            }
        }
    }
}
