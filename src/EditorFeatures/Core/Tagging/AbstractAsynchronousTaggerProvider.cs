// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
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
    internal abstract class AbstractAsynchronousTaggerProvider<TTag> : AsynchronousTaggerDataSource<TTag>
        where TTag : ITag
    {
        private readonly object uniqueKey = new object();
        private readonly IAsynchronousOperationListener asyncListener;
        private readonly IForegroundNotificationService notificationService;

        public AbstractAsynchronousTaggerProvider(
            IAsynchronousOperationListener asyncListener,
            IForegroundNotificationService notificationService)
        {
            this.asyncListener = asyncListener;
            this.notificationService = notificationService;
        }

        private TagSource<TTag> CreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            var options = this.Options ?? SpecializedCollections.EmptyEnumerable<Option<bool>>();
            var perLanguageOptions = this.PerLanguageOptions ?? SpecializedCollections.EmptyEnumerable<PerLanguageOption<bool>>();

            if (options.Any(option => !subjectBuffer.GetOption(option)) ||
                perLanguageOptions.Any(option => !subjectBuffer.GetOption(option)))
            {
                return null;
            }

            return new TagSource<TTag>(textViewOpt, subjectBuffer, this, asyncListener, notificationService);
        }

        protected ITagger<T> GetOrCreateTagger<T>(ITextView textViewOpt, ITextBuffer subjectBuffer) where T : ITag
        {
            if (!subjectBuffer.GetOption(EditorComponentOnOffOptions.Tagger))
            {
                return null;
            }

            var tagSource = GetOrCreateTagSource(textViewOpt, subjectBuffer);
            return tagSource == null
                ? null
                : new AsynchronousTagger<TTag>(this.asyncListener, this.notificationService, tagSource, subjectBuffer) as ITagger<T>;
        }

        protected TagSource<TTag> GetOrCreateTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            TagSource<TTag> tagSource;
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

        private bool TryRetrieveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, out TagSource<TTag> tagSource)
        {
            return textViewOpt != null
                ? textViewOpt.TryGetPerSubjectBufferProperty(subjectBuffer, uniqueKey, out tagSource)
                : subjectBuffer.Properties.TryGetProperty(uniqueKey, out tagSource);
        }

        private void RemoveTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            if (textViewOpt != null)
            {
                textViewOpt.RemovePerSubjectBufferProperty<TagSource<TTag>, ITextView>(subjectBuffer, uniqueKey);
            }
            else
            {
                subjectBuffer.Properties.RemoveProperty(uniqueKey);
            }
        }

        private void StoreTagSource(ITextView textViewOpt, ITextBuffer subjectBuffer, TagSource<TTag> tagSource)
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
    }
}
