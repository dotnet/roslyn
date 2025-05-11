// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal abstract class AbstractEmbeddedLanguageQuickInfoProvider : CommonQuickInfoProvider
{
    private readonly EmbeddedLanguageProviderFeatureService _embeddedLanguageProviderFeature;

    public AbstractEmbeddedLanguageQuickInfoProvider(
        string languageName,
        EmbeddedLanguageInfo info,
        ISyntaxKinds syntaxKinds,
        IEnumerable<Lazy<IEmbeddedLanguageQuickInfoProvider, EmbeddedLanguageMetadata>> allServices)
    {
        _embeddedLanguageProviderFeature = new EmbeddedLanguageProviderFeatureService(languageName, info, syntaxKinds, allServices);
    }

    protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(QuickInfoContext context, SyntaxToken token)
    {
        if (!_embeddedLanguageProviderFeature.SyntaxTokenKinds.Contains(token.RawKind))
            return null;

        var semanticModel = await context.Document.GetRequiredSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        var quickInfoProviders = _embeddedLanguageProviderFeature.GetServices(semanticModel, token, context.CancellationToken);
        foreach (var quickInfoProvider in quickInfoProviders)
        {
            // If this service added values then need to check the other ones.
            var result = quickInfoProvider.Value.GetQuickInfo(context, semanticModel, token);
            if (result != null)
                return result;
        }

        return null;
    }

    protected override Task<QuickInfoItem?> BuildQuickInfoAsync(CommonQuickInfoContext context, SyntaxToken token)
    {
        // Not implemented as this entrypoint appears to be dead code.
        throw new NotImplementedException();
    }

    /// <summary>
    /// A derivation of <see cref="AbstractEmbeddedLanguageFeatureService{TService}"/> so we can fetch providers. Normally, our providers implement an interface,
    /// and the combined provider directly inherits from <see cref="AbstractEmbeddedLanguageFeatureService{TService}"/>. Unfortunately Quick Info is a bit different:
    /// there is a class (not an interface) and a private base class that also defines some logic that we need to reuse. Since we don't
    /// have multiple inheritance, we'll create a separate class here and delegate to the protected methods. We can remove this if we
    /// switch Quick Info over to a pattern like the rest of our features.
    /// </summary>
    private sealed class EmbeddedLanguageProviderFeatureService :
        AbstractEmbeddedLanguageFeatureService<IEmbeddedLanguageQuickInfoProvider>
    {
        public EmbeddedLanguageProviderFeatureService(string languageName, EmbeddedLanguageInfo info, ISyntaxKinds syntaxKinds, IEnumerable<Lazy<IEmbeddedLanguageQuickInfoProvider, EmbeddedLanguageMetadata>> allServices)
            : base(languageName, info, syntaxKinds, allServices)
        {
        }

        public new ImmutableArray<Lazy<IEmbeddedLanguageQuickInfoProvider, EmbeddedLanguageMetadata>> GetServices(
            SemanticModel semanticModel,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            return base.GetServices(semanticModel, token, cancellationToken);
        }

        public new HashSet<int> SyntaxTokenKinds => base.SyntaxTokenKinds;
    }
}
