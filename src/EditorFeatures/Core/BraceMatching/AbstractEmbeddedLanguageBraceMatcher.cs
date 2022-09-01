// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.EmbeddedLanguages;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    // Note: this type could be concrete, but we cannot export IBraceMatcher's for multiple
    // languages at once.  So all logic is contained here.  The derived types only exist for
    // exporting purposes.
    internal abstract class AbstractEmbeddedLanguageBraceMatcher : IBraceMatcher
    {
        public async Task<BraceMatchingResult?> FindBracesAsync(
            Document document, int position, BraceMatchingOptions options, CancellationToken cancellationToken)
        {
            var languagesProvider = document.GetLanguageService<IEmbeddedLanguagesProvider>();
            if (languagesProvider != null)
            {
                foreach (var language in languagesProvider.Languages)
                {
                    var braceMatcher = (language as IEmbeddedLanguageEditorFeatures)?.BraceMatcher;
                    if (braceMatcher != null)
                    {
                        var result = await braceMatcher.FindBracesAsync(
                            document, position, options, cancellationToken).ConfigureAwait(false);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }
    }
}
