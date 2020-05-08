// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTime.LanguageServices
{
    internal class DateAndTimeEmbeddedLanguage : IEmbeddedLanguage
    {
        public readonly EmbeddedLanguageInfo Info;

        public ISyntaxClassifier Classifier { get; }

        public DateAndTimeEmbeddedLanguage(EmbeddedLanguageInfo info)
        {
            Info = info;
            Classifier = new FallbackSyntaxClassifier(info);
        }

        internal async Task<SyntaxToken?> TryGetDateAndTimeTokenAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (!DateAndTimePatternDetector.IsPossiblyDateAndTimeToken(token, syntaxFacts, out _, out _))
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = DateAndTimePatternDetector.TryGetOrCreate(semanticModel, this.Info);
            return detector != null && detector.IsDateAndTimeToken(token, cancellationToken)
                ? token : (SyntaxToken?)null;
        }
    }
}
