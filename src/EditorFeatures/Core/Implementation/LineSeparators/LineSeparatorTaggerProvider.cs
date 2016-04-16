// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    /// <summary>
    /// This factory is called to create taggers that provide information about where line
    /// separators go.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(LineSeparatorTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal partial class LineSeparatorTaggerProvider : AsynchronousTaggerProvider<LineSeparatorTag>
    {
        protected override IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.LineSeparator);

        [ImportingConstructor]
        public LineSeparatorTaggerProvider(
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.LineSeparators), notificationService)
        {
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.NearImmediate);
        }

        protected override async Task ProduceTagsAsync(TaggerContext<LineSeparatorTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;
            if (document == null)
            {
                return;
            }

            var options = document.Project.Solution.Workspace.Options;
            if (!options.GetOption(FeatureOnOffOptions.LineSeparator, document.Project.Language))
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.Tagger_LineSeparator_TagProducer_ProduceTags, cancellationToken))
            {
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var lineSeparatorService = document.Project.LanguageServices.GetService<ILineSeparatorService>();
                var lineSeparatorSpans = await lineSeparatorService.GetLineSeparatorsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var span in lineSeparatorSpans)
                {
                    context.AddTag(new TagSpan<LineSeparatorTag>(span.ToSnapshotSpan(snapshotSpan.Snapshot), LineSeparatorTag.Instance));
                }
            }
        }
    }
}