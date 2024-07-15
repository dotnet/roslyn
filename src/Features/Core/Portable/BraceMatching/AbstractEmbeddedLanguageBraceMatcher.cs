// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.BraceMatching;

/// <summary>
/// Brace matcher that analyzes string literals (for C#/VB) and then dispatches out to embedded brace matchers for
/// particular embedded languages (like JSON/Regex).
/// </summary>
internal abstract class AbstractEmbeddedLanguageBraceMatcher :
    AbstractEmbeddedLanguageFeatureService<IEmbeddedLanguageBraceMatcher>, IBraceMatcher
{
    protected AbstractEmbeddedLanguageBraceMatcher(
        string languageName,
        EmbeddedLanguageInfo info,
        ISyntaxKinds syntaxKinds,
        IEnumerable<Lazy<IEmbeddedLanguageBraceMatcher, EmbeddedLanguageMetadata>> allServices)
        : base(languageName, info, syntaxKinds, allServices)
    {
    }

    public async Task<BraceMatchingResult?> FindBracesAsync(
        Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(position);

        if (!this.SyntaxTokenKinds.Contains(token.RawKind))
            return null;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var braceMatchers = GetServices(semanticModel, token, cancellationToken);
        foreach (var braceMatcher in braceMatchers)
        {
            // If this service added values then need to check the other ones.
            var result = braceMatcher.Value.FindBraces(document.Project, semanticModel, token, position, options, cancellationToken);
            if (result.HasValue)
                return result;
        }

        return null;
    }
}
