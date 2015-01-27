// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    internal partial class LineSeparatorTaggerProvider
    {
        private class TagProducer :
            AbstractSingleDocumentTagProducer<LineSeparatorTag>
        {
            public async override Task<IEnumerable<ITagSpan<LineSeparatorTag>>> ProduceTagsAsync(
                Document document,
                SnapshotSpan snapshotSpan,
                int? caretPosition,
                CancellationToken cancellationToken)
            {
                if (document == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<LineSeparatorTag>>();
                }

                var options = document.Project.Solution.Workspace.Options;
                if (!options.GetOption(FeatureOnOffOptions.LineSeparator, document.Project.Language))
                {
                    return SpecializedCollections.EmptyEnumerable<ITagSpan<LineSeparatorTag>>();
                }

                // note: we are not directly using the syntax tree root here, we are holding onto it so that the 
                // line separator service won't block trying to get it.

                using (Logger.LogBlock(FunctionId.Tagger_LineSeparator_TagProducer_ProduceTags, cancellationToken))
                {
                    var lineSeparatorService = document.Project.LanguageServices.GetService<ILineSeparatorService>();
                    var lineSeparatorSpans = await lineSeparatorService.GetLineSeparatorsAsync(document, snapshotSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    var tagSpans = lineSeparatorSpans.Select(span =>
                        new TagSpan<LineSeparatorTag>(span.ToSnapshotSpan(snapshotSpan.Snapshot), LineSeparatorTag.Instance));

                    return tagSpans.ToList();
                }
            }
        }
    }
}
