using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    internal partial class AsyncFindReferencesPresenter
    {
        private class TableEntriesSnapshot : ITableEntriesSnapshot
        {
            public int VersionNumber { get; }
            private readonly ImmutableArray<TableEntry> _entries;

            public TableEntriesSnapshot(ImmutableArray<TableEntry> entries, int versionNumber)
            {
                _entries = entries;
                VersionNumber = versionNumber;
            }

            public int Count => _entries.Length;

            public void Dispose()
            {
            }

            public int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot)
            {
                // We only add items to the end of our list, and we never reorder.
                // As such, any index in us will map to the same index in any newer snapshot.
                return currentIndex;
            }

            public void StartCaching()
            {
            }

            public void StopCaching()
            {
            }

            public bool TryGetValue(int index, string keyName, out object content)
            {
                return _entries[index].TryGetValue(keyName, out content);
            }
        }
    }
}
