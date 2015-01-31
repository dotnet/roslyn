using Microsoft.VisualStudio.Text;

namespace Roslyn.Editor.InteractiveWindow
{
    public sealed class RestoreHistoryEditTag
    {
        /// <summary>
        /// The original submission where this history item is from.
        /// </summary>
        public SnapshotSpan OriginalSpan { get; private set; }

        internal RestoreHistoryEditTag(SnapshotSpan originalSpan)
        {
            OriginalSpan = originalSpan;
        }
    }
}
