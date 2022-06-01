// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractFallbackEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
    {
        private readonly EmbeddedLanguageInfo _info;
        private readonly ImmutableArray<int> _supportedKinds;

        protected AbstractFallbackEmbeddedLanguageClassifier(EmbeddedLanguageInfo info)
        {
            _info = info;
            _supportedKinds = ImmutableArray.Create(
                info.SyntaxKinds.CharacterLiteralToken,
                info.SyntaxKinds.StringLiteralToken,
                info.SyntaxKinds.InterpolatedStringTextToken);
        }

        public void RegisterClassifications(EmbeddedLanguageClassificationContext context)
        {
            var token = context.SyntaxToken;
            if (!_supportedKinds.Contains(token.RawKind))
                return;

            var virtualChars = _info.VirtualCharService.TryConvertToVirtualChars(token);
            if (virtualChars.IsDefaultOrEmpty)
                return;

            // Can avoid any work if we got the same number of virtual characters back as characters in the string. In
            // that case, there are clearly no escaped characters.
            if (virtualChars.Length == token.Text.Length)
                return;

            foreach (var vc in virtualChars)
            {
                if (vc.Span.Length > 1)
                    context.AddClassification(ClassificationTypeNames.StringEscapeCharacter, vc.Span);
            }
        }
    }
}
