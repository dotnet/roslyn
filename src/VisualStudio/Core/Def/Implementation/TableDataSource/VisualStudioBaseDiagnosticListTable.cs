// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class VisualStudioBaseDiagnosticListTable : AbstractTable<DiagnosticsUpdatedArgs, DiagnosticData>
    {
        // predefined name of diagnostic property which shows in what compilation stage the diagnostic is created.
        private const string Origin = "Origin";

        // key for new errorrank data. we will remove this once we get official vs base drop update.
        private const string ErrorRankKey = "errorrank";

        // predefined error ranks. we are going to start with this predefined error ranks for now and can be extended to support additional languages
        // this predefined ranks will be moved from roslyn to table control in next base drop update.
        private static class ErrorRank
        {
            public const int Lexical = 0;
            public const int Syntactic = 100;
            public const int Declaration = 200;
            public const int Semantic = 300;
            public const int Emit = 400;
            public const int PostBuild = 500;
            public const int Other = int.MaxValue;
        }

        private static readonly string[] s_columns = new string[]
        {
            ShimTableColumnDefinitions.ErrorSeverity,
            ShimTableColumnDefinitions.ErrorCode,
            StandardTableColumnDefinitions.Text,
            ShimTableColumnDefinitions.ErrorCategory,
            ShimTableColumnDefinitions.ProjectName,
            StandardTableColumnDefinitions.DocumentName,
            StandardTableColumnDefinitions.Line,
            StandardTableColumnDefinitions.Column,
            StandardTableColumnDefinitions.DetailsExpander
        };

        protected VisualStudioBaseDiagnosticListTable(
            SVsServiceProvider serviceProvider, Workspace workspace, IDiagnosticService diagnosticService, Guid identifier, ITableManagerProvider provider) :
            base(workspace, provider, StandardTables.ErrorsTable, new TableDataSource(serviceProvider, workspace, diagnosticService, identifier))
        {
        }

        internal override IReadOnlyCollection<string> Columns { get { return s_columns; } }

        protected override void SolutionOrProjectChanged(WorkspaceChangeEventArgs e)
        {
            if (e.ProjectId == null)
            {
                // solution level change
                this.Source.OnProjectDependencyChanged(e.NewSolution);
                return;
            }

            var oldProject = e.OldSolution.GetProject(e.ProjectId);
            var newProject = e.NewSolution.GetProject(e.ProjectId);

            if (oldProject == null || newProject == null)
            {
                // project added or removed
                this.Source.OnProjectDependencyChanged(e.NewSolution);
                return;
            }

            if (!object.ReferenceEquals(newProject.AllProjectReferences, oldProject.AllProjectReferences) &&
                !newProject.ProjectReferences.SetEquals(oldProject.ProjectReferences))
            {
                // reference has changed
                this.Source.OnProjectDependencyChanged(e.NewSolution);
                return;
            }
        }

        private class TableDataSource : AbstractTableDataSource<DiagnosticsUpdatedArgs, DiagnosticData>
        {
            private readonly Guid _identifier;
            private readonly IDiagnosticService _diagnosticService;
            private readonly IServiceProvider _serviceProvider;
            private readonly Workspace _workspace;
            private readonly OpenDocumentTracker _tracker;

            private ImmutableDictionary<ProjectId, int> _projectRanks;

            public TableDataSource(IServiceProvider serviceProvider, Workspace workspace, IDiagnosticService diagnosticService, Guid identifier)
            {
                _workspace = workspace;
                _serviceProvider = serviceProvider;
                _identifier = identifier;
                _projectRanks = ImmutableDictionary<ProjectId, int>.Empty;

                _tracker = new OpenDocumentTracker(_workspace);

                _diagnosticService = diagnosticService;
                _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;

                ConnectToSolutionCrawlerService(_workspace);
            }

            public override void OnProjectDependencyChanged(Solution solution)
            {
                var rankList = solution.GetProjectDependencyGraph().GetTopologicallySortedProjects(CancellationToken.None);
                Contract.ThrowIfNull(rankList);

                // rank is acsending order
                var rank = 0;
                var builder = ImmutableDictionary.CreateBuilder<ProjectId, int>();
                foreach (var projectId in rankList)
                {
                    builder.Add(projectId, rank++);
                }

                _projectRanks = builder.ToImmutable();

                // project rank has changed, refresh all factories.
                this.RefreshAllFactories();
            }

            public override string DisplayName
            {
                get
                {
                    return ServicesVSResources.DiagnosticsTableSourceName;
                }
            }

            public override Guid SourceTypeIdentifier
            {
                get
                {
                    return StandardTableDataSources.ErrorTableDataSource;
                }
            }

            public override Guid Identifier
            {
                get
                {
                    return _identifier;
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
                    OnDataRemoved(e.Id);
                    return;
                }

                var count = e.Diagnostics.Where(ShouldInclude).Count();
                if (count <= 0)
                {
                    OnDataRemoved(e.Id);
                    return;
                }

                OnDataAddedOrChanged(e.Id, e, count);
            }

            private static bool ShouldInclude(DiagnosticData diagnostic)
            {
                return diagnostic.Severity != DiagnosticSeverity.Hidden;
            }

            protected override AbstractTableEntriesFactory<DiagnosticData> CreateTableEntryFactory(object key, DiagnosticsUpdatedArgs data)
            {
                return new TableEntriesFactory(this, data.Workspace, data.ProjectId, data.DocumentId, data.Id);
            }

            private class TableEntriesFactory : AbstractTableEntriesFactory<DiagnosticData>
            {
                private readonly TableDataSource _source;
                private readonly Workspace _workspace;
                private readonly ProjectId _projectId;
                private readonly DocumentId _documentId;
                private readonly object _id;

                public TableEntriesFactory(TableDataSource source, Workspace workspace, ProjectId projectId, DocumentId documentId, object id)
                {
                    _source = source;
                    _workspace = workspace;
                    _projectId = projectId;
                    _documentId = documentId;
                    _id = id;
                }

                protected override ImmutableArray<DiagnosticData> GetItems()
                {
                    var provider = _source._diagnosticService;
                    var items = provider.GetDiagnostics(_workspace, _projectId, _documentId, _id, CancellationToken.None)
                                        .Where(ShouldInclude);

                    return items.ToImmutableArrayOrEmpty();
                }

                protected override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<DiagnosticData> items)
                {
                    return CreateTrackingPoints(
                        _workspace, _documentId, items, (d, s) => CreateTrackingPoint(s, d.OriginalStartLine, d.OriginalStartColumn));
                }

                protected override AbstractTableEntriesSnapshot<DiagnosticData> CreateSnapshot(
                    int version, ImmutableArray<DiagnosticData> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    var snapshot = new TableEntriesSnapshot(this, version, GetProjectRank(_projectId), items, trackingPoints);

                    if (_documentId != null && !trackingPoints.IsDefaultOrEmpty)
                    {
                        // track the open document so that we can throw away tracking points on document close properly
                        _source._tracker.TrackOpenDocument(_documentId, _id, snapshot);
                    }

                    return snapshot;
                }

                private int GetProjectRank(ProjectId projectId)
                {
                    var rank = 0;
                    if (projectId != null && _source._projectRanks.TryGetValue(projectId, out rank))
                    {
                        return rank;
                    }

                    return _source._projectRanks.Count;
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticData>, IWpfTableEntriesSnapshot
                {
                    private readonly TableEntriesFactory _factory;
                    private readonly int _projectRank;

                    private FrameworkElement[] _descriptions;
                    private FrameworkElement[] _errorCodes;

                    public TableEntriesSnapshot(
                        TableEntriesFactory factory, int version,
                        int projectRank, ImmutableArray<DiagnosticData> items, ImmutableArray<ITrackingPoint> trackingPoints) :
                        base(version, items, trackingPoints)
                    {
                        _projectRank = projectRank;
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
                            case ShimTableKeyNames.ProjectRank:
                                content = _projectRank;
                                return true;
                            case ErrorRankKey:
                                content = GetErrorRank(item);
                                return true;
                            case ShimTableKeyNames.ErrorSeverity:
                                content = GetErrorCategory(item.Severity);
                                return true;
                            case ShimTableKeyNames.ErrorCode:
                                content = item.Id;
                                return true;
                            case StandardTableKeyNames.HelpLink:
                                content = GetHelpLink(item);
                                return content != null;
                            case ShimTableKeyNames.ErrorCategory:
                                content = item.Category;
                                return true;
                            case StandardTableKeyNames.Text:
                                content = item.Message;
                                return true;
                            case StandardTableKeyNames.DocumentName:
                                content = GetFileName(item.OriginalFilePath, item.MappedFilePath);
                                return true;
                            case StandardTableKeyNames.Line:
                                content = item.MappedStartLine;
                                return true;
                            case StandardTableKeyNames.Column:
                                content = item.MappedStartColumn;
                                return true;
                            case ShimTableKeyNames.ProjectName:
                                content = GetProjectName(_factory._workspace, _factory._projectId);
                                return content != null;
                            case ShimTableKeyNames.Project:
                                content = GetHierarchy(_factory._workspace, _factory._projectId);
                                return content != null;
                            default:
                                content = null;
                                return false;
                        }
                    }

                    private int GetErrorRank(DiagnosticData item)
                    {
                        string value;
                        if (!item.Properties.TryGetValue(Origin, out value))
                        {
                            return ErrorRank.Other;
                        }

                        switch (value)
                        {
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
                        if (_factory._documentId == null)
                        {
                            return false;
                        }

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

                        return TryNavigateTo(_factory._workspace, _factory._documentId, item.OriginalStartLine, item.OriginalStartColumn, previewTab);
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

                    private __VSERRORCATEGORY GetErrorCategory(DiagnosticSeverity severity)
                    {
                        // REVIEW: why is it using old interface for new API?
                        switch (severity)
                        {
                            case DiagnosticSeverity.Error:
                                return __VSERRORCATEGORY.EC_ERROR;
                            case DiagnosticSeverity.Warning:
                                return __VSERRORCATEGORY.EC_WARNING;
                            case DiagnosticSeverity.Info:
                                return __VSERRORCATEGORY.EC_MESSAGE;
                            default:
                                return Contract.FailWithReturn<__VSERRORCATEGORY>();
                        }
                    }

                    #region IWpfTableEntriesSnapshot

                    public bool CanCreateDetailsContent(int index)
                    {
                        var item = GetItem(index);
                        if (item == null)
                        {
                            return false;
                        }

                        return !string.IsNullOrWhiteSpace(item.Description);
                    }

                    public bool TryCreateDetailsContent(int index, out FrameworkElement expandedContent)
                    {
                        var item = GetItem(index);
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
                        var item = GetItem(index);
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

                    private string GetHelpLink(DiagnosticData item)
                    {
                        Uri link;
                        if (BrowserHelper.TryGetUri(item.HelpLink, out link))
                        {
                            return link.AbsoluteUri;
                        }

                        if (!string.IsNullOrWhiteSpace(item.Id))
                        {
                            // TODO: once we link descriptor with diagnostic, get en-us message for Uri creation
                            return BrowserHelper.CreateBingQueryUri(item.Id, item.MessageFormat).AbsoluteUri;
                        }

                        return null;
                    }

                    public bool TryCreateColumnContent(int index, string columnName, bool singleColumnView, out FrameworkElement content)
                    {
                        content = default(FrameworkElement);
                        if (columnName != ShimTableColumnDefinitions.ErrorCode)
                        {
                            return false;
                        }

                        var item = GetItem(index);
                        if (item == null)
                        {
                            return false;
                        }

                        Uri unused;
                        if (BrowserHelper.TryGetUri(item.HelpLink, out unused))
                        {
                            content = GetOrCreateTextBlock(ref _errorCodes, this.Count, index, item, i => GetHyperLinkTextBlock(i, new Uri(i.HelpLink, UriKind.Absolute), bingLink: false));
                            return true;
                        }

                        if (!string.IsNullOrWhiteSpace(item.Id))
                        {
                            // TODO: once we link descriptor with diagnostic, get en-us message for Uri creation
                            content = GetOrCreateTextBlock(ref _errorCodes, this.Count, index, item, i => GetHyperLinkTextBlock(i, BrowserHelper.CreateBingQueryUri(item.Id, item.MessageFormat), bingLink: true));
                            return true;
                        }

                        return false;
                    }

                    private FrameworkElement GetHyperLinkTextBlock(DiagnosticData item, Uri uri, bool bingLink)
                    {
                        // currently, we can't do pooling since there is no event saying when this got out of view.
                        var content = new TextBlock()
                        {
                            Background = null,
                            ToolTip = item.Id,
                        };

                        var hyperlink = new Hyperlink();

                        hyperlink.Inlines.Add(item.Id);
                        hyperlink.NavigateUri = uri;
                        content.Inlines.Add(hyperlink);

                        // hyperlink will go away as soon as it goes out of view or updated.
                        hyperlink.Tag = item;

                        // use small event handler singleton object so that leaking ui doesnt make snapshot to leak.
                        UriNavigator.AttachRequestNaviateEventHandler(hyperlink, _factory._source._serviceProvider);
                        return content;
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
