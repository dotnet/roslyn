// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Classification;

[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(IClassificationTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
internal sealed class TotalClassificationTaggerProvider : IViewTaggerProvider
{
    private readonly SyntacticClassificationTaggerProvider _syntacticTaggerProvider;
    private readonly SemanticClassificationViewTaggerProvider _semanticTaggerProvider;
    private readonly EmbeddedLanguageClassificationViewTaggerProvider _embeddedTaggerProvider;

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public TotalClassificationTaggerProvider(
        IThreadingContext threadingContext,
        ClassificationTypeMap typeMap,
        IGlobalOptionService globalOptions,
        [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _syntacticTaggerProvider = new(threadingContext, typeMap, globalOptions, listenerProvider);
        _semanticTaggerProvider = new(threadingContext, typeMap, globalOptions, visibilityTracker, listenerProvider);
        _embeddedTaggerProvider = new(threadingContext, typeMap, globalOptions, visibilityTracker, listenerProvider);
    }

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
    {
        using var taggers = TemporaryArray<ITagger<T>?>.Empty;

        taggers.Add(_syntacticTaggerProvider.CreateTagger<T>(buffer));
        taggers.Add(_semanticTaggerProvider.CreateTagger<T>(textView, buffer));
        taggers.Add(_embeddedTaggerProvider.CreateTagger<T>(textView, buffer));

        // If any child tagger failed to create, then we fail entirely.
        if (taggers.Any(t => t is null))
        {
            foreach (var tagger in taggers)
                (tagger as IDisposable)?.Dispose();

            return null;
        }

        var finalTagger = new TotalClassificationAggregateTagger<T>(taggers[0]!, taggers[1]!, taggers[2]!);
        return finalTagger;
    }

    private sealed class TotalClassificationAggregateTagger<TTag>(
        ITagger<TTag> syntacticTagger,
        ITagger<TTag> semanticTagger,
        ITagger<TTag> embeddedTagger)
        : AbstractAggregateTagger<TTag>(taggers)
        where TTag : ITag
    {
        public override IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // First, get all the syntactic tags.  While they are generally overridden by semantic tags (since semantics
            // allows us to understand better what things like identifiers mean), they do take precedence for certain
            // tags like 'Comments' and 'Excluded Code'.  In those cases we want the classification to 'snap' instantly to
            // the syntactic state, and we do not want things like semantic classifications showing up over that.
            using var _ = ArrayBuilder<ITagSpan<TTag>>.GetInstance(out var syntacticTagSpans);
            syntacticTagSpans.AddRange()
        }
    }
}
