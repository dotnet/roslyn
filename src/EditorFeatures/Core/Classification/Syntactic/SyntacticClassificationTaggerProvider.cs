// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

/// <summary>
/// Intentionally not exported.  It is consumed by the <see cref="TotalClassificationTaggerProvider"/> instead.
/// </summary>
internal sealed partial class SyntacticClassificationTaggerProvider(
    IThreadingContext threadingContext,
    ClassificationTypeMap typeMap,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider) : ITaggerProvider
{
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.Classification);
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly ClassificationTypeMap _typeMap = typeMap;
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    ITagger<T>? ITaggerProvider.CreateTagger<T>(ITextBuffer buffer)
    {
        var tagger = CreateTagger(buffer);
        if (tagger is ITagger<T> typedTagger)
            return typedTagger;

        // Oops, we can't actually return this tagger, so just clean up
        tagger?.Dispose();
        return null;
    }

    public Tagger? CreateTagger(ITextBuffer buffer)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (!_globalOptions.GetOption(SyntacticColorizerOptionsStorage.SyntacticColorizer))
            return null;

        // Note: creating the Tagger must not fail (or we will leak the TagComputer).
        return new Tagger(TagComputer.GetOrCreate(this, (ITextBuffer2)buffer));
    }
}
