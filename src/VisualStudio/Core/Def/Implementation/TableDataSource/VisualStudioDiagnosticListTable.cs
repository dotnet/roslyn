// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    [Export(typeof(VisualStudioDiagnosticListTable))]
    internal class VisualStudioDiagnosticListTable : VisualStudioBaseDiagnosticListTable
    {
        internal const string IdentifierString = nameof(VisualStudioDiagnosticListTable);

        private readonly IErrorList _errorList;
        private readonly LiveTableDataSource _liveTableSource;
        private readonly BuildTableDataSource _buildTableSource;

        [ImportingConstructor]
        public VisualStudioDiagnosticListTable(
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            ExternalErrorDiagnosticUpdateSource errorSource,
            ITableManagerProvider provider) :
            this(serviceProvider, (Workspace)workspace, diagnosticService, errorSource, provider)
        {
            ConnectWorkspaceEvents();

            _errorList = serviceProvider.GetService(typeof(SVsErrorList)) as IErrorList;
            if (_errorList == null)
            {
                AddInitialTableSource(workspace.CurrentSolution, _liveTableSource);
                return;
            }

            _errorList.PropertyChanged += OnErrorListPropertyChanged;

            AddInitialTableSource(workspace.CurrentSolution, GetCurrentDataSource());
        }

        private ITableDataSource GetCurrentDataSource()
        {
            return _errorList.AreOtherErrorSourceEntriesShown ? (ITableDataSource)_liveTableSource : _buildTableSource;
        }

        /// this is for test only
        internal VisualStudioDiagnosticListTable(Workspace workspace, IDiagnosticService diagnosticService, ITableManagerProvider provider) :
            this(null, workspace, diagnosticService, null, provider)
        {
            AddInitialTableSource(workspace.CurrentSolution, _liveTableSource);
        }

        private VisualStudioDiagnosticListTable(
            SVsServiceProvider serviceProvider,
            Workspace workspace,
            IDiagnosticService diagnosticService,
            ExternalErrorDiagnosticUpdateSource errorSource,
            ITableManagerProvider provider) :
            base(serviceProvider, workspace, diagnosticService, provider)
        {
            _liveTableSource = new LiveTableDataSource(serviceProvider, workspace, diagnosticService, IdentifierString);
            _buildTableSource = new BuildTableDataSource(workspace, errorSource);
        }

        protected override void AddTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count == 0)
            {
                return;
            }

            RemoveTableSourcesIfNecessary();
            AddTableSource(GetCurrentDataSource());
        }

        protected override void RemoveTableSourceIfNecessary(Solution solution)
        {
            if (solution.ProjectIds.Count > 0)
            {
                return;
            }

            RemoveTableSourcesIfNecessary();
        }

        private void RemoveTableSourcesIfNecessary()
        {
            RemoveTableSourceIfNecessary(_buildTableSource);
            RemoveTableSourceIfNecessary(_liveTableSource);
        }

        private void RemoveTableSourceIfNecessary(ITableDataSource source)
        {
            if (!this.TableManager.Sources.Any(s => s == source))
            {
                return;
            }

            this.TableManager.RemoveSource(source);
        }

        protected override void ShutdownSource()
        {
            _liveTableSource.Shutdown();
            _buildTableSource.Shutdown();
        }

        private void OnErrorListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IErrorList.AreOtherErrorSourceEntriesShown))
            {
                AddTableSourceIfNecessary(this.Workspace.CurrentSolution);
            }
        }

        private class BuildTableDataSource : AbstractTableDataSource<DiagnosticData>
        {
            private readonly Workspace _workspace;
            private readonly ExternalErrorDiagnosticUpdateSource _buildErrorSource;

            public BuildTableDataSource(Workspace workspce, ExternalErrorDiagnosticUpdateSource errorSource)
            {
                _workspace = workspce;
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
                    OnDataAddedOrChanged(this, _buildErrorSource.GetBuildErrors().Length);
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

            protected void OnDataAddedOrChanged(object key, int itemCount)
            {
                // reuse factory. it is okay to re-use factory since we make sure we remove the factory before
                // adding it back
                bool newFactory = false;
                ImmutableArray<SubscriptionWithoutLock> snapshot;
                AbstractTableEntriesFactory<DiagnosticData> factory;

                lock (Gate)
                {
                    snapshot = Subscriptions;
                    if (!Map.TryGetValue(key, out factory))
                    {
                        factory = new TableEntriesFactory(this, _workspace);
                        Map.Add(key, factory);
                        newFactory = true;
                    }
                }

                factory.OnUpdated(itemCount);

                for (var i = 0; i < snapshot.Length; i++)
                {
                    snapshot[i].AddOrUpdate(factory, newFactory);
                }
            }

            private class TableEntriesFactory : AbstractTableEntriesFactory<DiagnosticData>
            {
                private readonly BuildTableDataSource _source;
                private readonly Workspace _workspace;

                public TableEntriesFactory(BuildTableDataSource source, Workspace workspace) :
                    base(source)
                {
                    _source = source;
                    _workspace = workspace;
                }

                protected override ImmutableArray<DiagnosticData> GetItems()
                {
                    return _source._buildErrorSource.GetBuildErrors();
                }

                protected override ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<DiagnosticData> items)
                {
                    return ImmutableArray<ITrackingPoint>.Empty;
                }

                protected override AbstractTableEntriesSnapshot<DiagnosticData> CreateSnapshot(
                    int version, ImmutableArray<DiagnosticData> items, ImmutableArray<ITrackingPoint> trackingPoints)
                {
                    return new TableEntriesSnapshot(this, version, items);
                }

                private class TableEntriesSnapshot : AbstractTableEntriesSnapshot<DiagnosticData>
                {
                    private readonly TableEntriesFactory _factory;

                    public TableEntriesSnapshot(
                        TableEntriesFactory factory, int version, ImmutableArray<DiagnosticData> items) :
                        base(version, Guid.Empty, items, ImmutableArray<ITrackingPoint>.Empty)
                    {
                        _factory = factory;
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
                                content = GetFileName(item.OriginalFilePath, item.MappedFilePath);
                                return true;
                            case StandardTableKeyNames.Line:
                                content = item.MappedStartLine;
                                return true;
                            case StandardTableKeyNames.Column:
                                content = item.MappedStartColumn;
                                return true;
                            case StandardTableKeyNames.ProjectName:
                                content = GetProjectName(_factory._workspace, item.ProjectId);
                                return content != null;
                            case StandardTableKeyNames.ProjectGuid:
                                var guid = GetProjectGuid(_factory._workspace, item.ProjectId);
                                content = guid;
                                return guid != Guid.Empty;
                            default:
                                content = null;
                                return false;
                        }
                    }

                    public override bool TryNavigateTo(int index, bool previewTab)
                    {
                        var item = GetItem(index);
                        if (item == null)
                        {
                            return false;
                        }

                        // this item is not navigatable
                        if (item.DocumentId == null)
                        {
                            return false;
                        }

                        return TryNavigateTo(_factory._workspace, item.DocumentId, item.OriginalStartLine, item.OriginalStartColumn, previewTab);
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
