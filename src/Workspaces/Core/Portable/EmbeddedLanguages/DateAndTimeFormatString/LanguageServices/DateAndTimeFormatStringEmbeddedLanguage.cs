// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.DateAndTimeFormatString.LanguageServices
{
    internal class DateAndTimeFormatStringEmbeddedLanguage : IEmbeddedLanguage
    {
        public readonly EmbeddedLanguageInfo Info;

        public ISyntaxClassifier Classifier { get; }

        public DateAndTimeFormatStringEmbeddedLanguage(EmbeddedLanguageInfo info)
        {
            Info = info;
            Classifier = new FallbackSyntaxClassifier(info);
        }

        internal async Task<SyntaxToken?> TryGetDateTimeStringTokenAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (DateAndTimeFormatStringPatternDetector.IsDefinitelyNotDateTimeStringToken(token, syntaxFacts, out _, out _))
                return null;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var detector = DateAndTimeFormatStringPatternDetector.TryGetOrCreate(semanticModel, this.Info);
            return detector != null && detector.IsDateTimeStringToken(token, cancellationToken)
                ? token : default(SyntaxToken?);
        }
    }
}
