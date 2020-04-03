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
                info.CharLiteralTokenKind,
                info.StringLiteralTokenKind,
                info.InterpolatedTextTokenKind);
        }

        public override void AddClassifications(
            Workspace workspace, SyntaxToken token, SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (_info.CharLiteralTokenKind != token.RawKind &&
                _info.StringLiteralTokenKind != token.RawKind &&
                _info.InterpolatedTextTokenKind != token.RawKind)
            {
                return;
            }

            var virtualChars = _info.VirtualCharService.TryConvertToVirtualChars(token);
            if (virtualChars.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var vc in virtualChars)
            {
                if (vc.Span.Length > 1)
                {
                    result.Add(new ClassifiedSpan(ClassificationTypeNames.StringEscapeCharacter, vc.Span));
                }
            }
        }
    }
}
