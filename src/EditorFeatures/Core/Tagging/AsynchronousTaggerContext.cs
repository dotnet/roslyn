using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    internal class AsynchronousTaggerContext<TTag, TState> where TTag : ITag
    {
        public TState State { get; set; }
        public IEnumerable<DocumentSnapshotSpan> SnapshotSpans { get; }
        public SnapshotPoint? CaretPosition { get; }
        public CancellationToken CancellationToken { get; }

        internal ImmutableArray<ITagSpan<TTag>>.Builder tagSpans = ImmutableArray.CreateBuilder<ITagSpan<TTag>>();

        internal AsynchronousTaggerContext(
            TState state,
            IEnumerable<DocumentSnapshotSpan> snapshotSpans,
            SnapshotPoint? caretPosition,
            CancellationToken cancellationToken)
        {
            this.State = state;
            this.SnapshotSpans = snapshotSpans;
            this.CaretPosition = caretPosition;
            this.CancellationToken = cancellationToken;
        }

        public void AddTag(ITagSpan<TTag> tag)
        {
            tagSpans.Add(tag);
        }
    }
}
