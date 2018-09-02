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
        private class FallbackEmbeddedClassifier : AbstractSyntaxClassifier
        {
            private readonly FallbackEmbeddedLanguage _language;

            public override ImmutableArray<int> SyntaxTokenKinds { get; }

            public FallbackEmbeddedClassifier(FallbackEmbeddedLanguage language)
            {
                _language = language;
                SyntaxTokenKinds = ImmutableArray.Create(language._stringLiteralTokenKind, language._interpolatedTextTokenKind);
            }

            public override void AddClassifications(
                Workspace workspace, SyntaxToken token, SemanticModel semanticModel,
                ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
            {
                if (_language._stringLiteralTokenKind != token.RawKind &&
                    _language._interpolatedTextTokenKind != token.RawKind)
                {
                    return;
                }

                var virtualChars = _language._virtualCharService.TryConvertToVirtualChars(token);
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
