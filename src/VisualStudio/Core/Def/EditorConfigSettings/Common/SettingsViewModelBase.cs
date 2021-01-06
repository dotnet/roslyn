// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{
    internal abstract partial class SettingsViewModelBase<T, TSnapshotFactory, TEntriesSnapshot> : IWpfSettingsEditorViewModel, ITableDataSource
        where TSnapshotFactory : SettingsSnapshotFactoryBase<T, TEntriesSnapshot>
        where TEntriesSnapshot : SettingsEntriesSnapshotBase<T>
    {
        private readonly ISettingsProvider<T> _data;
        private readonly IWpfTableControlProvider _controlProvider;
        private readonly TSnapshotFactory _snapshotFactory;
        private readonly ITableManager _tableManager;

        private List<ITableDataSink> TableSinks { get; } = new List<ITableDataSink>();

        protected SettingsViewModelBase(ISettingsProvider<T> data,
                                        IWpfTableControlProvider controlProvider,
                                        ITableManagerProvider tableMangerProvider)
        {
            _data = data;
            _controlProvider = controlProvider;
            _data.RegisterViewModel(this);
            _tableManager = tableMangerProvider.GetTableManager(Identifier);
            _ = _tableManager.AddSource(this);
            _snapshotFactory = CreateSnapshotFactory(_data);
        }

        public abstract string Identifier { get; }
        protected abstract TSnapshotFactory CreateSnapshotFactory(ISettingsProvider<T> data);
        protected abstract string[] GetFixedColumns();
        protected abstract IEnumerable<ColumnState2> GetInitialColumnStates();

        public string SourceTypeIdentifier => "EditorConfigSettings";
        public string? DisplayName => null;

        public Task NotifyOfUpdateAsync()
        {
            _snapshotFactory.NotifyOfUpdate();

            // Notify the sinks. Generally, VS Table Control will request data 500ms after the last notification.
            foreach (var tableSink in TableSinks)
            {
                // Notify that an update is available
                tableSink.FactorySnapshotChanged(null);
            }

            return Task.CompletedTask;
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            sink.AddFactory(_snapshotFactory);
            TableSinks.Add(sink);
            return new RemoveSinkWhenDisposed(TableSinks, sink);
        }

        public Task<IReadOnlyList<TextChange>?> GetChangesAsync()
            => _data.GetChangedEditorConfigAsync();

        public IWpfTableControl4 GetTableControl()
        {
            var initialColumnStates = GetInitialColumnStates();
            var fixedColumns = GetFixedColumns();
            return (IWpfTableControl4)_controlProvider.CreateControl(
                    _tableManager,
                    true,
                    initialColumnStates,
                    fixedColumns);
        }
    }
}
