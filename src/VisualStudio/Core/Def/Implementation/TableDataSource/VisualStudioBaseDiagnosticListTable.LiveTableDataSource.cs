// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
            }

            public override string DisplayName => ServicesVSResources.DiagnosticsTableSourceName;
            public override string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
            public override string Identifier => _identifier;
            public override object GetItemKey(object data) => ((DiagnosticsUpdatedArgs)data).Id;

            public override ImmutableArray<TableItem<DiagnosticData>> Deduplicate(IEnumerable<IList<TableItem<DiagnosticData>>> groupedItems)
            {
                return groupedItems.MergeDuplicatesOrderedBy(Order);
            }

            public override ITrackingPoint CreateTrackingPoint(DiagnosticData data, ITextSnapshot snapshot)
            {
                return snapshot.CreateTrackingPoint(data.DataLocation?.OriginalStartLine ?? 0, data.DataLocation?.OriginalStartColumn ?? 0);
            }

            protected override object GetAggregationKey(object data)
            {
                return ((DiagnosticsUpdatedArgs)data).Id;
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

            public override AbstractTableEntriesSource<DiagnosticData> CreateTableEntrySource(object data)
            {
                var item = (DiagnosticsUpdatedArgs)data;
                return new TableEntriesSource(this, item.Workspace, item.ProjectId, item.DocumentId, item.Id);
            }

            private static bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.Severity != DiagnosticSeverity.Hidden;
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

            private class TableEntriesSource : AbstractTableEntriesSource<DiagnosticData>
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

                public override ImmutableArray<TableItem<DiagnosticData>> GetItems()
                {
                    var provider = _source._diagnosticService;
                    var items = provider.GetDiagnostics(_workspace, _projectId, _documentId, _id, CancellationToken.None)
                                        .Where(ShouldInclude).Select(d => new TableItem<DiagnosticData>(d, GenerateDeduplicationKey));

                    return items.ToImmutableArrayOrEmpty();
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<DiagnosticData>> items)
                {
                    return _workspace.CreateTrackingPoints(_documentId, items, _source.CreateTrackingPoint);
                }

                public override AbstractTableEntriesSnapshot<DiagnosticData> CreateSnapshot(
                    int version, ImmutableArray<TableItem<DiagnosticData>> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    var snapshot = new TableEntriesSnapshot(this, version, items, trackingPoints);

                    if (_documentId != null && !trackingPoints.IsDefaultOrEmpty)
                    {
                        // track the open document so that we can throw away tracking points on document close properly
                        _source._tracker.TrackOpenDocument(_documentId, _id, snapshot);
                    }

                    return snapshot;
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
                           Hash.Combine(diagnostic.Id.GetHashCode(), diagnostic.Message.GetHashCode())))));
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticData>, IWpfTableEntriesSnapshot
                {
                    private readonly TableEntriesSource _factorySource;
                    private FrameworkElement[] _descriptions;

                    public TableEntriesSnapshot(
                        TableEntriesSource factorySource, int version, ImmutableArray<TableItem<DiagnosticData>> items, ImmutableArray<ITrackingPoint> trackingPoints) :
                        base(version, GetProjectGuid(factorySource._workspace, factorySource._projectId), items, trackingPoints)
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
                                content = GetErrorRank(item);
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
                                content = GetErrorSource(_factorySource._buildTool);
                                return true;
                            case StandardTableKeyNames.BuildTool:
                                content = GetBuildTool(_factorySource._buildTool);
                                return content != null;
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
                                // TODO: need to change projectId from item
                                content = GetProjectName(_factorySource._workspace, _factorySource._projectId);
                                return content != null;
                            case StandardTableKeyNames.ProjectGuid:
                                // TODO: need to change projectGuid from item
                                content = ProjectGuid;
                                return ProjectGuid != Guid.Empty;
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

                        return _factorySource._buildTool;
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
                        // this item is not navigatable
                        if (_factorySource._documentId == null)
                        {
                            return false;
                        }

                        var item = GetItem(index).Primary;
                        if (item == null)
                        {
                            return false;
                        }

                        var trackingLinePosition = GetTrackingLineColumn(_factorySource._workspace, _factorySource._documentId, index);
                        if (trackingLinePosition != LinePosition.Zero)
                        {
                            return TryNavigateTo(_factorySource._workspace, _factorySource._documentId, trackingLinePosition.Line, trackingLinePosition.Character, previewTab);
                        }

<<<<<<< HEAD
                        return TryNavigateTo(_factory._workspace, _factory._documentId, 
                            item.DataLocation?.OriginalStartLine ?? 0, item.DataLocation?.OriginalStartColumn ?? 0, previewTab);
=======
                        return TryNavigateTo(_factorySource._workspace, _factorySource._documentId, item.OriginalStartLine, item.OriginalStartColumn, previewTab);
>>>>>>> 8e58be7... introduced tableEntrySource which takes part of responsibility of TableEntryFactory
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
                        var item = GetItem(index).Primary;
                        if (item == null)
                        {
                            return false;
                        }

                        return !string.IsNullOrWhiteSpace(item.Description);
                    }

                    public bool TryCreateDetailsContent(int index, out FrameworkElement expandedContent)
                    {
                        var item = GetItem(index).Primary;
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
                        var item = GetItem(index).Primary;
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
}
