// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Base type of all asynchronous tagger providers (<see cref="ITaggerProvider"/> and <see cref="IViewTaggerProvider"/>). 
    /// </summary>
    internal abstract partial class AsynchronousTaggerProvider<TTag> : IAsynchronousTaggerDataSource<TTag>, ITaggerProvider, IViewTaggerProvider
        where TTag : ITag
    {
        private readonly object uniqueKey = new object();
        private readonly IAsynchronousOperationListener asyncListener;
        private readonly IForegroundNotificationService notificationService;

        public virtual TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.None;
        public virtual TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.None;
        public virtual SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeExclusive;

        public virtual IEqualityComparer<TTag> TagComparer => null;

        public virtual IEnumerable<Option<bool>> Options => null;
        public virtual IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => null;

        protected AsynchronousTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            this.asyncListener = asyncListener;
            this.notificationService = notificationService;
        }

        private TagSource CreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            var options = this.Options ?? SpecializedCollections.EmptyEnumerable<Option<bool>>();
            var perLanguageOptions = this.PerLanguageOptions ?? SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

            if (options.Any(option => !subjectBuffer.GetOption(option)) ||
                perLanguageOptions.Any(option => !subjectBuffer.GetOption(option)))
            {
                return null;
            }

            return new TagSource(textViewOpt, subjectBuffer, this, asyncListener, notificationService);
        }

        private IAccurateTagger<T> GetOrCreateTagger<T>(ITextView textViewOpt, ITextBuffer subjectBuffer) where T : ITag
        {
            if (!subjectBuffer.GetOption(EditorComponentOnOffOptions.Tagger))
            {
                return null;
            }

            var tagSource = GetOrCreateTagSource(textViewOpt, subjectBuffer);
            return tagSource == null
                ? null
                : new Tagger(this.asyncListener, this.notificationService, tagSource, subjectBuffer) as IAccurateTagger<T>;
        }

        private TagSource GetOrCreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            TagSource tagSource;
            if (!this.TryRetrieveTagSource(textViewOpt, subjectBuffer, out tagSource))
            {
                tagSource = this.CreateTagSource(textViewOpt, subjectBuffer);
                if (tagSource == null)
                {
                    return null;
                }

                this.StoreTagSource(textViewOpt, subjectBuffer, tagSource);
                tagSource.Disposed += (s, e) => this.RemoveTagSource(textViewOpt, subjectBuffer);
            }

            return tagSource;
        }

        private bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out TagSource tagSource)
        {
            return textViewOpt != null
                ? textViewOpt.TryGetPerSubjectBufferProperty(subjectBuffer, uniqueKey, out tagSource)
                : subjectBuffer.Properties.TryGetProperty(uniqueKey, out tagSource);
        }

        private void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            if (textViewOpt != null)
            {
                textViewOpt.RemovePerSubjectBufferProperty<TagSource, ITextView>(subjectBuffer, uniqueKey);
            }
            else
            {
                subjectBuffer.Properties.RemoveProperty(uniqueKey);
            }
        }

        private void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, TagSource tagSource)
        {
            if (textViewOpt != null)
            {
                textViewOpt.AddPerSubjectBufferProperty(subjectBuffer, uniqueKey, tagSource);
            }
            else
            {
                subjectBuffer.Properties.AddProperty(uniqueKey, tagSource);
            }
        }

        public virtual SnapshotPoint? GetCaretPoint(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // Use 'null' to indicate that the tagger should get the default caret position.
            return null;
        }

        public virtual IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // Use 'null' to indicate that the tagger should tag the default set of spans.
            return null;
        }

        public abstract ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer);

        public virtual async Task ProduceTagsAsync(TaggerContext<TTag> context)
        {
            foreach (var spanToTag in context.SpansToTag)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                await ProduceTagsAsync(context, spanToTag, GetCaretPosition(context.CaretPosition, spanToTag.SnapshotSpan)).ConfigureAwait(false);
            }
        }

        private static int? GetCaretPosition(SnapshotPoint? caretPosition, SnapshotSpan snapshotSpan)
        {
            return caretPosition.HasValue && caretPosition.Value.Snapshot == snapshotSpan.Snapshot
                ? caretPosition.Value.Position : (int?)null;
        }

        public virtual Task ProduceTagsAsync(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            return SpecializedTasks.EmptyTask;
        }

        public IAccurateTagger<T> CreateTagger<T>(ITextBuffer subjectBuffer) where T : ITag
        {
            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            return this.GetOrCreateTagger<T>(null, subjectBuffer);
        }

        ITagger<T> ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
        {
            return CreateTagger<T>(buffer);
        }

        public IAccurateTagger<T> CreateTagger<T>(ITextView textView, ITextBuffer subjectBuffer) where T : ITag
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (subjectBuffer == null)
            {
                throw new ArgumentNullException(nameof(subjectBuffer));
            }

            return this.GetOrCreateTagger<T>(textView, subjectBuffer);
        }

        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        {
            return this.CreateTagger<T>(textView, buffer);
        }
    }
}