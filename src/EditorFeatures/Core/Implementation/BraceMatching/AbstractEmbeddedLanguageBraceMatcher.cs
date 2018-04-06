// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
{
    // Note: this type could be concrete, but we cannot export IBraceMatcher's for multiple
    // languages at once.  So all logic is contained here.  The derived types only exist for
    // exporting purposes.
    internal abstract class AbstractEmbeddedLanguageBraceMatcher : IBraceMatcher
    {
        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var languageProvider = document.GetLanguageService<IEmbeddedLanguageProvider>();
            foreach (var language in languageProvider.GetEmbeddedLanguages())
            {
                var braceMatcher = language.BraceMatcher;
                if (braceMatcher != null)
                {
                    var result = await braceMatcher.FindBracesAsync(
                        document, position, cancellationToken).ConfigureAwait(false);
                    if (result != null)
                    {
                        return new BraceMatchingResult(result.Value.LeftSpan, result.Value.RightSpan);
                    }
                }
            }

            return null;
        }
    }
}
