// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{
    internal abstract class SettingsSnapshotFactoryBase<T, TEntriesSnapshot> : TableEntriesSnapshotFactoryBase
        where TEntriesSnapshot : SettingsEntriesSnapshotBase<T>
    {
        private readonly ISettingsProvider<T> _data;

        // State
        private int _currentVersionNumber;
        private int _lastSnapshotVersionNumber = -1;
        private TEntriesSnapshot? _lastSnapshot;

        // Disallow concurrent modification of state
        private readonly object _gate = new();

        public SettingsSnapshotFactoryBase(ISettingsProvider<T> data)
        {
            _data = data;
        }

        public override int CurrentVersionNumber => _currentVersionNumber;

        public override ITableEntriesSnapshot? GetCurrentSnapshot() => GetSnapshot(CurrentVersionNumber);

        internal void NotifyOfUpdate() => Interlocked.Increment(ref _currentVersionNumber);

        public override ITableEntriesSnapshot? GetSnapshot(int versionNumber)
        {
            lock (_gate)
            {
                if (versionNumber == _currentVersionNumber)
                {
                    if (_lastSnapshotVersionNumber == _currentVersionNumber)
                    {
                        return _lastSnapshot;
                    }
                    else
                    {
                        var data = _data.GetCurrentDataSnapshot();
                        var snapshot = CreateSnapshot(data, _currentVersionNumber);

                        _lastSnapshot = snapshot;
                        _lastSnapshotVersionNumber = _currentVersionNumber;

                        return snapshot;
                    }
                }
                else if (versionNumber < _currentVersionNumber)
                {
                    // We can return null from this method.
                    // This will signal to Table Control to request current snapshot.
                    return null;
                }
                else // versionNumber > this.currentVersionNumber
                {
                    throw new InvalidOperationException($"Invalid GetSnapshot request. Requested version: {versionNumber}. Current version: {_currentVersionNumber}");
                }
            }
        }

        protected abstract TEntriesSnapshot CreateSnapshot(ImmutableArray<T> data, int currentVersionNumber);
    }
}
