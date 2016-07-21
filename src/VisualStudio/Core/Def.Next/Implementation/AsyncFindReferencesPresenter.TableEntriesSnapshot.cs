using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class AsyncFindReferencesPresenter
    {
        private class TableEntriesSnapshot : ITableEntriesSnapshot
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
        }
    }
}