// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.BraceMatching
{
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

            using var _1 = ArrayBuilder<IEmbeddedLanguageBraceMatcher>.GetInstance(out var buffer);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // First, see if this is a string annotated with either a comment or [StringSyntax] attribute. If
            // so, delegate to the first matcher we have registered for whatever language ID we find.
            if (this.Detector.IsEmbeddedLanguageToken(token, semanticModel, cancellationToken, out var identifier, out _) &&
                this.IdentifierToServices.TryGetValue(identifier, out var braceMatchers))
            {
                foreach (var braceMatcher in braceMatchers)
                {
                    // keep track of what matchers we've run so we don't call into them multiple times.
                    buffer.Add(braceMatcher.Value);

                    // If this service added values then need to check the other ones.
                    var result = braceMatcher.Value.FindBraces(semanticModel, token, position, options, cancellationToken);
                    if (result.HasValue)
                        return result;
                }
            }

            // It wasn't an annotated API.  See if it's some legacy API our legacy matchers have direct
            // support for (for example, .net APIs prior to Net6).
            foreach (var legacyBraceMatcher in this.LegacyServices)
            {
                // don't bother trying to classify again if we already tried above.
                if (buffer.Contains(legacyBraceMatcher.Value))
                    continue;

                // If this service added values then need to check the other ones.
                var result = legacyBraceMatcher.Value.FindBraces(semanticModel, token, position, options, cancellationToken);
                if (result.HasValue)
                    return result;
            }

            return null;
        }
    }
}
