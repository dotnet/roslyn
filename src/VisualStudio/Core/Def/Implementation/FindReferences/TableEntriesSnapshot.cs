// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal partial class StreamingFindUsagesPresenter
    {
        // Name of the key used to retireve the whole entry object.
        internal const string SelfKeyName = "self";

        private class TableEntriesSnapshot : WpfTableEntriesSnapshotBase
        {
            private readonly int _versionNumber;

            private readonly ImmutableList<Entry> _entries;

            public TableEntriesSnapshot(ImmutableList<Entry> entries, int versionNumber)
            {
                _entries = entries;
                _versionNumber = versionNumber;
            }

            public override int VersionNumber => _versionNumber;

            public override int Count => _entries.Count;

            public override int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot)
            {
                // We only add items to the end of our list, and we never reorder.
                // As such, any index in us will map to the same index in any newer snapshot.
                return currentIndex;
            }

            public override bool TryGetValue(int index, string keyName, out object content)
            {
                if (keyName == SelfKeyName)
                {
                    content = _entries[index];
                    return true;
                }

                return _entries[index].TryGetValue(keyName, out content);
            }

            public override bool TryCreateColumnContent(
                int index, string columnName, bool singleColumnView, out FrameworkElement content)
            {
                return this._entries[index].TryCreateColumnContent(columnName, out content);
            }
        }
    }
}
