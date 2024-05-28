// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

/// <summary>
/// Intentionally not exported.  It is consumed by the <see cref="TotalClassificationTaggerProvider"/> instead.
/// </summary>
internal sealed partial class SyntacticClassificationTaggerProvider(TaggerHost taggerHost, ClassificationTypeMap typeMap)
{
    private readonly TaggerHost _taggerHost = taggerHost;
    private readonly ClassificationTypeMap _typeMap = typeMap;

    private readonly IAsynchronousOperationListener _listener = taggerHost.AsyncListenerProvider.GetListener(FeatureAttribute.Classification);
    private IThreadingContext ThreadingContext => _taggerHost.ThreadingContext;
    private IGlobalOptionService GlobalOptions => _taggerHost.GlobalOptions;

    public EfficientTagger<IClassificationTag>? CreateTagger(ITextBuffer buffer)
    {
        ThreadingContext.ThrowIfNotOnUIThread();
        if (!GlobalOptions.GetOption(SyntacticColorizerOptionsStorage.SyntacticColorizer))
            return null;

        // Note: creating the Tagger must not fail (or we will leak the TagComputer).
        return new Tagger(TagComputer.GetOrCreate(this, (ITextBuffer2)buffer));
    }
}
