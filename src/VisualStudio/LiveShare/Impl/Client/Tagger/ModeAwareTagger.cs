// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attempt to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    internal sealed class ModeAwareTagger : ITagger<IClassificationTag>, IDisposable
    {
        private readonly SyntacticClassificationModeSelector _modeSelector;

        private readonly Lazy<ITagger<IClassificationTag>> _textMateTagger;
        private readonly Lazy<ITagger<IClassificationTag>> _serverTagger;

        public ModeAwareTagger(
            Func<ITagger<IClassificationTag>> createTextMateTagger,
            Func<ITagger<IClassificationTag>> createServerTagger,
            SyntacticClassificationModeSelector modeSelector)
        {
            this._modeSelector = modeSelector;

            this._textMateTagger = new Lazy<ITagger<IClassificationTag>>(() => CreateTagger(createTextMateTagger));
            this._serverTagger = new Lazy<ITagger<IClassificationTag>>(() => CreateTagger(createServerTagger));
        }

        private ITagger<IClassificationTag> CreateTagger(Func<ITagger<IClassificationTag>> createTagger)
        {
            var tagger = createTagger();
            tagger.TagsChanged += RaiseTagsChanged;
            return tagger;
        }

        private void DisposeTagger(Lazy<ITagger<IClassificationTag>> tagger)
        {
            if (tagger.IsValueCreated)
            {
                tagger.Value.TagsChanged -= RaiseTagsChanged;
                (tagger.Value as IDisposable)?.Dispose();
            }
        }

        private void RaiseTagsChanged(object sender, SnapshotSpanEventArgs e)
        {
            if (ReferenceEquals(sender, this.CurrentTagger))
            {
                this.TagsChanged?.Invoke(sender, e);
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> /*ITagger<IClassificationTag>.*/TagsChanged;

        private ITagger<IClassificationTag> CurrentTagger
        {
            get
            {
                var mode = this._modeSelector.GetMode();
                switch (mode)
                {
                    case SyntacticClassificationMode.TextMate:
                        return this._textMateTagger.Value;
                    case SyntacticClassificationMode.SyntaxLsp:
                        return this._serverTagger.Value;
                    case SyntacticClassificationMode.None:
                        return null;
                    default:
                        // shouldn't happen but don't crash. just use text mate
                        return this._textMateTagger.Value;
                }
            }
        }

        IEnumerable<ITagSpan<IClassificationTag>> ITagger<IClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans) =>
            this.CurrentTagger?.GetTags(spans) ?? SpecializedCollections.EmptyEnumerable<ITagSpan<IClassificationTag>>();

        void IDisposable.Dispose()
        {
            DisposeTagger(this._textMateTagger);
            DisposeTagger(this._serverTagger);
        }
    }
}
