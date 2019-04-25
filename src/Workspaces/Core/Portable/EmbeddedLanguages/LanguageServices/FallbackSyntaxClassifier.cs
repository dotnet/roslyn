// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal partial class FallbackEmbeddedLanguage
    {
        private class FallbackSyntaxClassifier : AbstractSyntaxClassifier
        {
            private readonly EmbeddedLanguageInfo _info;

            public override ImmutableArray<int> SyntaxTokenKinds { get; }

            public FallbackSyntaxClassifier(EmbeddedLanguageInfo info)
            {
                _info = info;
                SyntaxTokenKinds = ImmutableArray.Create(info.StringLiteralTokenKind, info.InterpolatedTextTokenKind);
            }

            public override void AddClassifications(
                Workspace workspace, SyntaxToken token, SemanticModel semanticModel,
                ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
            {
                if (_info.StringLiteralTokenKind != token.RawKind &&
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
}
