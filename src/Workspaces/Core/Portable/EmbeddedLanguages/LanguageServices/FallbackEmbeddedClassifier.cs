// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal partial class FallbackEmbeddedLanguage
    {
        private class FallbackEmbeddedClassifier : IEmbeddedClassifier
        {
            private readonly FallbackEmbeddedLanguage _language;

            public FallbackEmbeddedClassifier(FallbackEmbeddedLanguage language)
            {
                _language = language;
            }

            public void AddClassifications(
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
