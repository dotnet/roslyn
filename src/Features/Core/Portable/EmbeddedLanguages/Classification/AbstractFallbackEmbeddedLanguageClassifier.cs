// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Configuration;
using System.Text;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class AbstractFallbackEmbeddedLanguageClassifier : IEmbeddedLanguageClassifier
    {
        private readonly EmbeddedLanguageInfo _info;
        private readonly ImmutableArray<int> _supportedKinds;

        protected AbstractFallbackEmbeddedLanguageClassifier(EmbeddedLanguageInfo info)
        {
            _info = info;

            using var array = TemporaryArray<int>.Empty;

            array.Add(info.SyntaxKinds.CharacterLiteralToken);
            array.Add(info.SyntaxKinds.StringLiteralToken);
            array.Add(info.SyntaxKinds.InterpolatedStringTextToken);

            array.AsRef().AddIfNotNull(info.SyntaxKinds.Utf8StringLiteralToken);

            _supportedKinds = array.ToImmutableAndClear();
        }

        protected abstract bool TextStartWithEscapeCharacter(string text);

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
                // non-BMP UTF-16 characters' length can be two
                if (vc.Span.Length > 1)
                {
                    var text = token.SyntaxTree?.GetText(context.CancellationToken).ToString(vc.Span);
                    if (text is not null && TextStartWithEscapeCharacter(text))
                    {
                        context.AddClassification(ClassificationTypeNames.StringEscapeCharacter, vc.Span);
                    }
                }
            }
        }
    }
}
