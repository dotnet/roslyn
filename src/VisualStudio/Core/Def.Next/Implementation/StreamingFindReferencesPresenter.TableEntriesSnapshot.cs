using System;
using System.Collections.Immutable;
using System.Windows;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class TableEntriesSnapshot : ITableEntriesSnapshot, IWpfTableEntriesSnapshot
        {
            public int VersionNumber { get; }
            private readonly ImmutableList<ReferenceEntry> _referenceEntries;

            public TableEntriesSnapshot(ImmutableList<ReferenceEntry> referenceEntries, int versionNumber)
            {
                _referenceEntries = referenceEntries;
                VersionNumber = versionNumber;
            }

            int ITableEntriesSnapshot.Count => _referenceEntries.Count;

            void IDisposable.Dispose()
            {
            }

            int ITableEntriesSnapshot.IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot)
            {
                // We only add items to the end of our list, and we never reorder.
                // As such, any index in us will map to the same index in any newer snapshot.
                return currentIndex;
            }

            void ITableEntriesSnapshot.StartCaching()
            {
            }

            void ITableEntriesSnapshot.StopCaching()
            {
            }

            bool ITableEntriesSnapshot.TryGetValue(int index, string keyName, out object content)
            {
                return _referenceEntries[index].TryGetValue(keyName, out content);
            }

            public bool TryCreateImageContent(int index, string columnName, bool singleColumnView, out ImageMoniker content)
            {
                content = default(ImageMoniker);
                return false;
            }

            public bool TryCreateStringContent(int index, string columnName, bool truncatedText, bool singleColumnView, out string content)
            {
                content = null;
                return false;
            }

            public bool TryCreateColumnContent(int index, string columnName, bool singleColumnView, out FrameworkElement content)
            {
                content = null;
                return false;
            }

            public bool CanCreateDetailsContent(int index)
            {
                return false;
            }

            public bool TryCreateDetailsContent(int index, out FrameworkElement expandedContent)
            {
                expandedContent = null;
                return false;
            }

            public bool TryCreateDetailsStringContent(int index, out string content)
            {
                content = null;
                return false;
            }

            public bool TryCreateToolTip(int index, string columnName, out object toolTip)
            {
                return this._referenceEntries[index].TryCreateToolTip(columnName, out toolTip);
            }
        }
    }
}