// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(IClassificationTag))]
[Microsoft.VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class TotalClassificationTaggerProvider(
    IThreadingContext threadingContext,
    ClassificationTypeMap typeMap,
    IGlobalOptionService globalOptions,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider listenerProvider) : IViewTaggerProvider
{
    private readonly SyntacticClassificationTaggerProvider _syntacticTaggerProvider = new(threadingContext, typeMap, globalOptions, listenerProvider);
    private readonly SemanticClassificationViewTaggerProvider _semanticTaggerProvider = new(threadingContext, typeMap, globalOptions, visibilityTracker, listenerProvider);
    private readonly EmbeddedLanguageClassificationViewTaggerProvider _embeddedTaggerProvider = new(threadingContext, typeMap, globalOptions, visibilityTracker, listenerProvider);

    ITagger<T>? IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
    {
        var tagger = CreateTagger(textView, buffer);
        if (tagger is not ITagger<T> typedTagger)
        {
            tagger?.Dispose();
            return null;
        }

        return typedTagger;
    }

    public TotalClassificationAggregateTagger? CreateTagger(ITextView textView, ITextBuffer buffer)
    {
        var syntacticTagger = _syntacticTaggerProvider.CreateTagger(buffer);
        var semanticTagger = _semanticTaggerProvider.CreateTagger(textView, buffer);
        var embeddedTagger = _embeddedTaggerProvider.CreateTagger(textView, buffer);

        if (syntacticTagger is null || semanticTagger is null || embeddedTagger is null)
        {
            syntacticTagger?.Dispose();
            semanticTagger?.Dispose();
            embeddedTagger?.Dispose();
            return null;
        }

        return new TotalClassificationAggregateTagger(syntacticTagger, semanticTagger, embeddedTagger);
    }
}
