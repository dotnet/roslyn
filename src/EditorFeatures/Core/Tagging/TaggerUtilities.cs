using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal static class TaggerUtilities
    {
        internal static async Task Delegate<TTag>(
            IEnumerable<DocumentSnapshotSpan> snapshotSpans, SnapshotPoint? caretPosition, Action<ITagSpan<TTag>> addTag,
            Func<DocumentSnapshotSpan, int?, Action<ITagSpan<TTag>>, CancellationToken, Task> delegatee, CancellationToken cancellationToken) where TTag : ITag
        {
            foreach (var snapshotSpan in snapshotSpans)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await delegatee(snapshotSpan, GetCaretPosition(caretPosition, snapshotSpan.SnapshotSpan), addTag, cancellationToken).ConfigureAwait(false);
            }
        }

        private static int? GetCaretPosition(SnapshotPoint? caretPosition, SnapshotSpan snapshotSpan)
        {
            return caretPosition.HasValue && caretPosition.Value.Snapshot == snapshotSpan.Snapshot
                ? caretPosition.Value.Position : (int?)null;
        }
    }
}