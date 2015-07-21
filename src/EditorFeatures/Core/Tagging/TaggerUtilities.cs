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
        internal static async Task Delegate<TTag, TState>(
            AsynchronousTaggerContext<TTag,TState> context,
            Func<AsynchronousTaggerContext<TTag, TState>, DocumentSnapshotSpan, int?, Task> delegatee) where TTag : ITag
        {
            foreach (var snapshotSpan in context.SnapshotSpans)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                await delegatee(context, snapshotSpan, GetCaretPosition(context.CaretPosition, snapshotSpan.SnapshotSpan)).ConfigureAwait(false);
            }
        }

        private static int? GetCaretPosition(SnapshotPoint? caretPosition, SnapshotSpan snapshotSpan)
        {
            return caretPosition.HasValue && caretPosition.Value.Snapshot == snapshotSpan.Snapshot
                ? caretPosition.Value.Position : (int?)null;
        }
    }
}