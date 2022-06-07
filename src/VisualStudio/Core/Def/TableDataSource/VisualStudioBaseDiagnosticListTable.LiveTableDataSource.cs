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
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract partial class VisualStudioBaseDiagnosticListTable
    {
        /// <summary>
        /// Used by the editor to signify that errors added to the error list
        /// should not be copied to the guest.  Instead they will be published via LSP.
        /// </summary>
        private const string DoNotPropogateToGuestProperty = "DoNotPropagateToGuests";

        /// <summary>
        /// Error list diagnostic source for "Build + Intellisense" setting.
        /// See <see cref="VisualStudioDiagnosticListTableWorkspaceEventListener.VisualStudioDiagnosticListTable.BuildTableDataSource"/>
        /// for error list diagnostic source for "Build only" setting.
        /// </summary>
        protected class LiveTableDataSource : AbstractRoslynTableDataSource<DiagnosticTableItem, DiagnosticsUpdatedArgs>
        {
            private readonly string _identifier;
            private readonly IDiagnosticService _diagnosticService;
            private readonly Workspace _workspace;
            private readonly OpenDocumentTracker<DiagnosticTableItem> _tracker;

            /// <summary>
            /// Flag indicating if a build inside Visual Studio is in progress.
            /// We get build progress updates from <see cref="ExternalErrorDiagnosticUpdateSource.BuildProgressChanged"/>.
            /// Build progress events are guaranteed to be invoked in a serial fashion during build.
            /// </summary>
            private bool _isBuildRunning;

            public LiveTableDataSource(Workspace workspace, IThreadingContext threadingContext, IDiagnosticService diagnosticService, string identifier, ExternalErrorDiagnosticUpdateSource? buildUpdateSource = null)
                : base(workspace, threadingContext)
            {
                _workspace = workspace;
                _identifier = identifier;

                _tracker = new OpenDocumentTracker<DiagnosticTableItem>(_workspace);

                _diagnosticService = diagnosticService;
                ConnectToDiagnosticService(workspace, diagnosticService);

                ConnectToBuildUpdateSource(buildUpdateSource);
            }

            public IGlobalOptionService GlobalOptions => _diagnosticService.GlobalOptions;
            public override string DisplayName => ServicesVSResources.CSharp_VB_Diagnostics_Table_Data_Source;
            public override string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;
            public override string Identifier => _identifier;
            public override object GetItemKey(DiagnosticsUpdatedArgs data) => data.Id;

            private void ConnectToBuildUpdateSource(ExternalErrorDiagnosticUpdateSource? buildUpdateSource)
            {
                if (buildUpdateSource == null)
                {
                    // it can be null in unit test
                    return;
                }

                OnBuildProgressChanged(buildUpdateSource.IsInProgress);
                buildUpdateSource.BuildProgressChanged +=
                    (_, progress) => OnBuildProgressChanged(running: progress != ExternalErrorDiagnosticUpdateSource.BuildProgress.Done);
            }

            private void OnBuildProgressChanged(bool running)
            {
                _isBuildRunning = running;

                // We mark error list "Build + Intellisense" setting as stable,
                // i.e. shown as "Error List" without the trailing "...", if both the following are true:
                //  1. User invoked build is not running inside Visual Studio AND
                //  2. Solution crawler is not running in background to compute intellisense diagnostics.
                ChangeStableStateIfRequired(newIsStable: !_isBuildRunning && !IsSolutionCrawlerRunning);
            }

            public override AbstractTableEntriesSnapshot<DiagnosticTableItem> CreateSnapshot(
                AbstractTableEntriesSource<DiagnosticTableItem> source,
                int version,
                ImmutableArray<DiagnosticTableItem> items,
                ImmutableArray<ITrackingPoint> trackingPoints)
            {
                var diagnosticSource = (DiagnosticTableEntriesSource)source;
                var snapshot = new TableEntriesSnapshot(ThreadingContext, diagnosticSource, version, items, trackingPoints);

                if (diagnosticSource.SupportSpanTracking && !trackingPoints.IsDefaultOrEmpty)
                {
                    // track the open document so that we can throw away tracking points on document close properly
                    _tracker.TrackOpenDocument(diagnosticSource.TrackingDocumentId, diagnosticSource.Key, snapshot);
                }

                return snapshot;
            }

            protected override object GetOrUpdateAggregationKey(DiagnosticsUpdatedArgs data)
            {
                var key = TryGetAggregateKey(data);
                if (key == null)
                {
                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                    return key;
                }

                if (!CheckAggregateKey(key as AggregatedKey, data))
                {
                    RemoveStaledData(data);

                    key = CreateAggregationKey(data);
                    AddAggregateKey(data, key);
                }

                return key;
            }

            private bool CheckAggregateKey(AggregatedKey? key, DiagnosticsUpdatedArgs? args)
            {
                if (key == null)
                {
                    return true;
                }

                if (args?.DocumentId == null || args?.Solution == null)
                {
                    return true;
                }

                var documents = GetDocumentsWithSameFilePath(args.Solution, args.DocumentId);
                return key.DocumentIds == documents;
            }

            private object CreateAggregationKey(DiagnosticsUpdatedArgs data)
            {
                if (data.DocumentId == null || data.Solution == null)
                    return GetItemKey(data);

                if (data.Id is not LiveDiagnosticUpdateArgsId liveArgsId)
                    return GetItemKey(data);

                var documents = GetDocumentsWithSameFilePath(data.Solution, data.DocumentId);
                return new AggregatedKey(documents, liveArgsId.Analyzer, liveArgsId.Kind);
            }

            private void PopulateInitialData(Workspace workspace, IDiagnosticService diagnosticService)
            {
                var diagnosticMode = GlobalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode);
                var diagnostics = diagnosticService.GetPushDiagnosticBuckets(
                    workspace, projectId: null, documentId: null, diagnosticMode, cancellationToken: CancellationToken.None);

                foreach (var bucket in diagnostics)
                {
                    // We only need to issue an event to VS that these docs have diagnostics associated with them.  So
                    // we create a dummy notification for this.  It doesn't matter that it is 'DiagnosticsRemoved' as
                    // this doesn't actually change any data.  All that will happen now is that VS will call back into
                    // us for these IDs and we'll fetch the diagnostics at that point.
                    OnDataAddedOrChanged(DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                        bucket.Id, bucket.Workspace, solution: null, bucket.ProjectId, bucket.DocumentId));
                }
            }

            private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
            {
                using (Logger.LogBlock(FunctionId.LiveTableDataSource_OnDiagnosticsUpdated, a => GetDiagnosticUpdatedMessage(GlobalOptions, a), e, CancellationToken.None))
                {
                    if (_workspace != e.Workspace)
                    {
                        return;
                    }

                    var diagnostics = e.GetPushDiagnostics(GlobalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                    if (diagnostics.Length == 0)
                    {
                        OnDataRemoved(e);
                        return;
                    }

                    var count = diagnostics.Where(ShouldInclude).Count();
                    if (count <= 0)
                    {
                        OnDataRemoved(e);
                        return;
                    }

                    OnDataAddedOrChanged(e);
                }
            }

            public override AbstractTableEntriesSource<DiagnosticTableItem> CreateTableEntriesSource(object data)
            {
                var item = (UpdatedEventArgs)data;
                return new TableEntriesSource(this, item.Workspace, GlobalOptions, item.ProjectId, item.DocumentId, item.Id);
            }

            private void ConnectToDiagnosticService(Workspace workspace, IDiagnosticService diagnosticService)
            {
                if (diagnosticService == null)
                {
                    // it can be null in unit test
                    return;
                }

                _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;

                PopulateInitialData(workspace, diagnosticService);
            }

            private static bool ShouldInclude(DiagnosticData diagnostic)
            {
                if (diagnostic == null)
                {
                    // guard us from wrong provider that gives null diagnostic
                    Debug.Assert(false, "Let's see who does this");
                    return false;
                }

                // If this diagnostic is for LSP only, then we won't show it here
                if (diagnostic.Properties.ContainsKey(nameof(DocumentPropertiesService.DiagnosticsLspClientName)))
                {
                    return false;
                }

                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Info:
                    case DiagnosticSeverity.Warning:
                    case DiagnosticSeverity.Error:
                        return true;
                    case DiagnosticSeverity.Hidden:
                    default:
                        return false;
                }
            }

            public override IEqualityComparer<DiagnosticTableItem> GroupingComparer
                => DiagnosticTableItem.GroupingComparer.Instance;

            public override IEnumerable<DiagnosticTableItem> Order(IEnumerable<DiagnosticTableItem> groupedItems)
            {
                // this should make order of result always deterministic. we only need these 6 values since data with 
                // all these same will merged to one.
                return groupedItems.OrderBy(d => d.Data.DataLocation?.OriginalStartLine ?? 0)
                                   .ThenBy(d => d.Data.DataLocation?.OriginalStartColumn ?? 0)
                                   .ThenBy(d => d.Data.Id)
                                   .ThenBy(d => d.Data.Message)
                                   .ThenBy(d => d.Data.DataLocation?.OriginalEndLine ?? 0)
                                   .ThenBy(d => d.Data.DataLocation?.OriginalEndColumn ?? 0);
            }

            private class TableEntriesSource : DiagnosticTableEntriesSource
            {
                private readonly LiveTableDataSource _source;
                private readonly Workspace _workspace;
                private readonly IGlobalOptionService _globalOptions;
                private readonly ProjectId? _projectId;
                private readonly DocumentId? _documentId;
                private readonly object _id;
                private readonly string _buildTool;

                public TableEntriesSource(LiveTableDataSource source, Workspace workspace, IGlobalOptionService globalOptions, ProjectId? projectId, DocumentId? documentId, object id)
                {
                    _source = source;
                    _workspace = workspace;
                    _globalOptions = globalOptions;
                    _projectId = projectId;
                    _documentId = documentId;
                    _id = id;
                    _buildTool = (id as BuildToolId)?.BuildTool ?? string.Empty;
                }

                public override object Key => _id;
                public override string BuildTool => _buildTool;
                public override bool SupportSpanTracking => _documentId != null;
                public override DocumentId? TrackingDocumentId => _documentId;

                public override ImmutableArray<DiagnosticTableItem> GetItems()
                {
                    var diagnosticMode = _globalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode);
                    var provider = _source._diagnosticService;
                    var items = provider.GetPushDiagnosticsAsync(_workspace, _projectId, _documentId, _id, includeSuppressedDiagnostics: true, diagnosticMode, cancellationToken: CancellationToken.None)
                        .AsTask()
                        .WaitAndGetResult_CanCallOnBackground(CancellationToken.None)
                                        .Where(ShouldInclude)
                                        .Select(data => DiagnosticTableItem.Create(_workspace, data));

                    return items.ToImmutableArray();
                }

                public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<DiagnosticTableItem> items)
                    => _workspace.CreateTrackingPoints(_documentId, items);
            }

            private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticTableItem>, IWpfTableEntriesSnapshot
            {
                private readonly DiagnosticTableEntriesSource _source;
                private FrameworkElement[]? _descriptions;

                public TableEntriesSnapshot(
                    IThreadingContext threadingContext,
                    DiagnosticTableEntriesSource source,
                    int version,
                    ImmutableArray<DiagnosticTableItem> items,
                    ImmutableArray<ITrackingPoint> trackingPoints)
                    : base(threadingContext, version, items, trackingPoints)
                {
                    _source = source;
                }

                public override bool TryGetValue(int index, string columnName, [NotNullWhen(returnValue: true)] out object? content)
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
                            content = ValueTypeCache.GetOrCreate(GetErrorRank(data));
                            return content != null;
                        case StandardTableKeyNames.ErrorSeverity:
                            content = ValueTypeCache.GetOrCreate(GetErrorCategory(data.Severity));
                            return content != null;
                        case StandardTableKeyNames.ErrorCode:
                            content = data.Id;
                            return content != null;
                        case StandardTableKeyNames.ErrorCodeToolTip:
                            content = (data.GetValidHelpLinkUri() != null) ? string.Format(EditorFeaturesResources.Get_help_for_0, data.Id) : null;
                            return content != null;
                        case StandardTableKeyNames.HelpKeyword:
                            content = data.Id;
                            return content != null;
                        case StandardTableKeyNames.HelpLink:
                            content = data.GetValidHelpLinkUri()?.AbsoluteUri;
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
                            content = data.DataLocation?.GetFilePath();
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
                        case StandardTableKeyNames.SuppressionState:
                            content = data.IsSuppressed ? SuppressionState.Suppressed : SuppressionState.Active;
                            return true;
                        case DoNotPropogateToGuestProperty:
                            content = true;
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

                private static ErrorSource GetErrorSource(string buildTool)
                {
                    if (buildTool == PredefinedBuildTools.Build)
                    {
                        return ErrorSource.Build;
                    }

                    return ErrorSource.Other;
                }

                private static ErrorRank GetErrorRank(DiagnosticData item)
                {
                    if (!item.Properties.TryGetValue(WellKnownDiagnosticPropertyNames.Origin, out var value))
                    {
                        return ErrorRank.Other;
                    }

                    switch (value)
                    {
                        case WellKnownDiagnosticTags.Build:
                            // any error from build gets lowest priority
                            // see https://github.com/dotnet/roslyn/issues/28807
                            //
                            // this is only used when intellisense (live) errors are involved.
                            // with "build only" filter on, we use order of errors came in from build for ordering
                            // and doesn't use ErrorRank for ordering (by giving same rank for all errors)
                            //
                            // when live errors are involved, by default, error list will use the following to sort errors
                            // error rank > project rank > project name > file name > line > column
                            // which will basically make syntax errors show up before declaration error and method body semantic errors
                            // among same type of errors, leaf project's error will show up first and then projects that depends on the leaf projects
                            //
                            // any build errors mixed with live errors will show up at the end. when live errors are on, some of errors
                            // still left as build errors such as errors produced after CompilationStages.Compile or ones listed here
                            // http://source.roslyn.io/#Microsoft.CodeAnalysis.CSharp/Compilation/CSharpCompilerDiagnosticAnalyzer.cs,23 or similar ones for VB
                            // and etc.
                            return ErrorRank.PostBuild;
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

                public override bool TryNavigateTo(int index, NavigationOptions options, CancellationToken cancellationToken)
                    => TryNavigateToItem(index, options, cancellationToken);

                #region IWpfTableEntriesSnapshot

                public bool CanCreateDetailsContent(int index)
                {
                    var item = GetItem(index)?.Data;
                    if (item == null)
                    {
                        return false;
                    }

                    return !string.IsNullOrWhiteSpace(item.Description);
                }

                public bool TryCreateDetailsContent(int index, [NotNullWhen(returnValue: true)] out FrameworkElement? expandedContent)
                {
                    var item = GetItem(index)?.Data;
                    if (item == null)
                    {
                        expandedContent = null;
                        return false;
                    }

                    expandedContent = GetOrCreateTextBlock(ref _descriptions, this.Count, index, item, i => GetDescriptionTextBlock(i));
                    return true;
                }

                public bool TryCreateDetailsStringContent(int index, [NotNullWhen(returnValue: true)] out string? content)
                {
                    var item = GetItem(index)?.Data;
                    if (item == null)
                    {
                        content = null;
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(item.Description))
                    {
                        content = null;
                        return false;
                    }

                    content = item.Description;
                    return content != null;
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
                    [NotNull] ref FrameworkElement[]? caches, int count, int index, DiagnosticData item, Func<DiagnosticData, FrameworkElement> elementCreator)
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
                public bool TryCreateColumnContent(int index, string columnName, bool singleColumnView, [NotNullWhen(returnValue: true)] out FrameworkElement? content)
                {
                    content = null;
                    return false;
                }

                public bool TryCreateImageContent(int index, string columnName, bool singleColumnView, out ImageMoniker content)
                {
                    content = default;
                    return false;
                }

                public bool TryCreateStringContent(int index, string columnName, bool truncatedText, bool singleColumnView, [NotNullWhen(returnValue: true)] out string? content)
                {
                    content = null;
                    return false;
                }

                public bool TryCreateToolTip(int index, string columnName, [NotNullWhen(returnValue: true)] out object? toolTip)
                {
                    toolTip = null;
                    return false;
                }

#pragma warning disable IDE0060 // Remove unused parameter - TODO: remove this once we moved to new drop 
                public static bool TryCreateStringContent(int index, string columnName, bool singleColumnView, [NotNullWhen(returnValue: true)] out string? content)
#pragma warning restore IDE0060 // Remove unused parameter
                {
                    content = null;
                    return false;
                }

                #endregion
            }

            private static string GetDiagnosticUpdatedMessage(IGlobalOptionService globalOptions, DiagnosticsUpdatedArgs e)
            {
                var id = e.Id.ToString();
                if (e.Id is LiveDiagnosticUpdateArgsId live)
                {
                    id = $"{live.Analyzer}/{live.Kind}";
                }
                else if (e.Id is AnalyzerUpdateArgsId analyzer)
                {
                    id = analyzer.Analyzer.ToString();
                }

                var diagnostics = e.GetPushDiagnostics(globalOptions, InternalDiagnosticsOptions.NormalDiagnosticMode);
                return $"Kind:{e.Workspace.Kind}, Analyzer:{id}, Update:{e.Kind}, {(object?)e.DocumentId ?? e.ProjectId}, ({string.Join(Environment.NewLine, diagnostics)})";
            }
        }
    }
}
