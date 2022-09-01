// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal partial class VisualStudioDiagnosticListTableWorkspaceEventListener
    {
        internal partial class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
        {
            /// <summary>
            /// Error list diagnostic source for "Build only" setting.
            /// See <see cref="VisualStudioBaseDiagnosticListTable.LiveTableDataSource"/>
            /// for error list diagnostic source for "Build + Intellisense" setting.
            /// </summary>
            private class BuildTableDataSource : AbstractTableDataSource<DiagnosticTableItem, object>
            {
                private readonly object _key = new();

                private readonly ExternalErrorDiagnosticUpdateSource _buildErrorSource;

                public BuildTableDataSource(Workspace workspace, IThreadingContext threadingContext, ExternalErrorDiagnosticUpdateSource errorSource)
                    : base(workspace, threadingContext)
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
                    => data;

                public override AbstractTableEntriesSource<DiagnosticTableItem> CreateTableEntriesSource(object data)
                    => new TableEntriesSource(this);

                public override AbstractTableEntriesSnapshot<DiagnosticTableItem> CreateSnapshot(AbstractTableEntriesSource<DiagnosticTableItem> source, int version, ImmutableArray<DiagnosticTableItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    // Build doesn't support tracking point.
                    return new TableEntriesSnapshot(ThreadingContext, (DiagnosticTableEntriesSource)source, version, items);
                }

                public override IEqualityComparer<DiagnosticTableItem> GroupingComparer
                    => DiagnosticTableItem.GroupingComparer.Instance;

                public override IEnumerable<DiagnosticTableItem> Order(IEnumerable<DiagnosticTableItem> groupedItems)
                {
                    // errors are already given in order. use it as it is.
                    return groupedItems;
                }

                private class TableEntriesSource : DiagnosticTableEntriesSource
                {
                    private readonly BuildTableDataSource _source;

                    public TableEntriesSource(BuildTableDataSource source)
                        => _source = source;

                    public override object Key => _source._key;
                    public override string BuildTool => PredefinedBuildTools.Build;
                    public override bool SupportSpanTracking => false;
                    public override DocumentId TrackingDocumentId => throw ExceptionUtilities.Unreachable;

                    public override ImmutableArray<DiagnosticTableItem> GetItems()
                    {
                        return _source.AggregateItems(
                            _source._buildErrorSource.GetBuildErrors().
                            GroupBy(d => d, d => DiagnosticTableItem.Create(_source.Workspace, d), DiagnosticTableItem.GroupingComparer.Instance));
                    }

                    public override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<DiagnosticTableItem> items)
                        => ImmutableArray<ITrackingPoint>.Empty;
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticTableItem>
                {
                    private readonly DiagnosticTableEntriesSource _source;

                    public TableEntriesSnapshot(
                        IThreadingContext threadingContext,
                        DiagnosticTableEntriesSource source, int version, ImmutableArray<DiagnosticTableItem> items)
                        : base(threadingContext, version, items, ImmutableArray<ITrackingPoint>.Empty)
                    {
                        _source = source;
                    }

                    public override bool TryGetValue(int index, string columnName, [NotNullWhen(true)] out object? content)
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
                                content = ValueTypeCache.GetOrCreate(ErrorSource.Build);
                                return content != null;
                            case StandardTableKeyNames.BuildTool:
                                content = _source.BuildTool;
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
                                // Build doesn't support suppression.
                                Contract.ThrowIfTrue(data.IsSuppressed);
                                content = SuppressionState.NotApplicable;
                                return true;
                            default:
                                content = null;
                                return false;
                        }
                    }

                    public override bool TryNavigateTo(int index, NavigationOptions options, CancellationToken cancellationToken)
                    {
                        var item = GetItem(index);
                        if (item is null)
                            return false;

                        var documentId = GetProperDocumentId(ThreadingContext, item, cancellationToken);
                        if (documentId is null)
                            return false;

                        return TryNavigateTo(item.Workspace, documentId, item.GetOriginalPosition(), options, cancellationToken);
                    }

                    private static DocumentId? GetProperDocumentId(IThreadingContext threadingContext, DiagnosticTableItem item, CancellationToken cancellationToken)
                    {
                        var documentId = item.DocumentId;
                        var projectId = item.ProjectId;

                        // check whether documentId still exist. it might have changed if project it belong to has reloaded.
                        var solution = item.Workspace.CurrentSolution;
                        if (solution.GetDocument(documentId) != null)
                        {
                            return documentId;
                        }

                        if (solution.GetProject(projectId) is { } project)
                        {
                            // We couldn't find a document matching a known ID when the item was created, so it may be a
                            // source generator output.
                            var documents = threadingContext.JoinableTaskFactory.Run(() => project.GetSourceGeneratedDocumentsAsync(cancellationToken).AsTask());
                            if (documentId is not null)
                            {
                                if (documents.Any(document => document.Id == documentId))
                                    return documentId;
                            }
                            else
                            {
                                var projectDirectory = Path.GetDirectoryName(project.FilePath);
                                documentId = documents.FirstOrDefault(document => Path.Combine(projectDirectory, document.FilePath) == item.GetOriginalFilePath())?.Id;
                                if (documentId is not null)
                                    return documentId;
                            }
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
}
