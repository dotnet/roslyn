// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal class FallbackSyntaxClassifier : AbstractSyntaxClassifier
    {
        private readonly EmbeddedLanguageInfo _info;

        public override ImmutableArray<int> SyntaxTokenKinds { get; }

        public FallbackSyntaxClassifier(EmbeddedLanguageInfo info)
        {
            _info = info;
            SyntaxTokenKinds = ImmutableArray.Create(
                info.SyntaxKinds.CharacterLiteralToken,
                info.SyntaxKinds.StringLiteralToken,
                info.SyntaxKinds.InterpolatedStringTextToken);
        }

        public override void AddClassifications(
            SyntaxToken token, SemanticModel semanticModel, ClassificationOptions options,
            ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (!SyntaxTokenKinds.Contains(token.RawKind))
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
                    result.Add(new ClassifiedSpan(ClassificationTypeNames.StringEscapeCharacter, vc.Span));
            }
        }
    }
}
