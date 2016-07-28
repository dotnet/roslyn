// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Windows;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class TableEntriesSnapshot : WpfTableEntriesSnapshotBase
        {
            private readonly int _versionNumber;

            private readonly ImmutableList<ReferenceEntry> _referenceEntries;

            public TableEntriesSnapshot(ImmutableList<ReferenceEntry> referenceEntries, int versionNumber)
            {
                _referenceEntries = referenceEntries;
                _versionNumber = versionNumber;
            }

            public override int VersionNumber => _versionNumber;

            public override int Count => _referenceEntries.Count;

            public override int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot)
            {
                // We only add items to the end of our list, and we never reorder.
                // As such, any index in us will map to the same index in any newer snapshot.
                return currentIndex;
            }

            public override bool TryGetValue(int index, string keyName, out object content)
            {
                return _referenceEntries[index].TryGetValue(keyName, out content);
            }

            //public override bool TryCreateToolTip(int index, string columnName, out object toolTip)
            //{
            //    return this._referenceEntries[index].TryCreateToolTip(columnName, out toolTip);
            //}

            public override bool TryCreateColumnContent(
                int index, string columnName, bool singleColumnView, out FrameworkElement content)
            {
                return this._referenceEntries[index].TryCreateColumnContent(columnName, out content);
            }
        }
    }
}