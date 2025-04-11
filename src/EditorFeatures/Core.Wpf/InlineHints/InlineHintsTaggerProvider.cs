// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

/// <summary>
/// The provider that is used as a middleman to create the tagger so that the data tag
/// can be used to create the UI tag
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[TagType(typeof(IntraTextAdornmentTag))]
[Name(nameof(InlineHintsTaggerProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class InlineHintsTaggerProvider(
    IGlobalOptionService globalOptionService,
    IClassificationFormatMapService classificationFormatMapService,
    IClassificationTypeRegistryService classificationTypeRegistryService,
    IThreadingContext threadingContext,
    IUIThreadOperationExecutor operationExecutor,
    IAsynchronousOperationListenerProvider listenerProvider,
    IToolTipService toolTipService,
    ClassificationTypeMap typeMap,
    Lazy<IStreamingFindUsagesPresenter> streamingFindUsagesPresenter,
    EditorOptionsService editorOptionsService,
    TaggerHost taggerHost,
    [Import(AllowDefault = true)] IInlineHintKeyProcessor inlineHintKeyProcessor) : IViewTaggerProvider
{
    public readonly IGlobalOptionService GlobalOptionService = globalOptionService;
    public readonly IClassificationFormatMapService ClassificationFormatMapService = classificationFormatMapService;
    public readonly IClassificationTypeRegistryService ClassificationTypeRegistryService = classificationTypeRegistryService;
    public readonly IThreadingContext ThreadingContext = threadingContext;
    public readonly IUIThreadOperationExecutor OperationExecutor = operationExecutor;
    public readonly IAsynchronousOperationListener AsynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.InlineHints);
    public readonly IToolTipService ToolTipService = toolTipService;
    public readonly ClassificationTypeMap TypeMap = typeMap;
    public readonly Lazy<IStreamingFindUsagesPresenter> StreamingFindUsagesPresenter = streamingFindUsagesPresenter;
    public readonly EditorOptionsService EditorOptionsService = editorOptionsService;

    /// <summary>
    /// Underlying data tagger that produces the raw data tags.  We defer to it to own the actual low level tagger.
    /// That tagger is responsible for listening to events (like scrolling/editing the buffer) and emitting <see
    /// cref="ITagger{T}.TagsChanged"/> events when the tags change.  We then forward those events along to any
    /// client of us.  When that client then asks us for our adornment tags, we call into the underlying tagger for
    /// its data tags.  Then, on demand, we convert and cache those data tags into adornment tags and pass on the
    /// results.
    /// </summary>
    private readonly InlineHintsDataTaggerProvider<CachedAdornmentTagSpan> _dataTaggerProvider = new(taggerHost, inlineHintKeyProcessor);

    public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer subjectBuffer) where T : ITag
    {
        if (textView.IsNotSurfaceBufferOfTextView(subjectBuffer))
            return null;

        if (textView is not IWpfTextView wpfTextView)
            return null;

        var tagger = new InlineHintsTagger(
            this, wpfTextView, subjectBuffer, _dataTaggerProvider.CreateTagger(textView, subjectBuffer));
        if (tagger is not ITagger<T> typedTagger)
        {
            tagger.Dispose();
            return null;
        }

        return typedTagger;
    }
}
