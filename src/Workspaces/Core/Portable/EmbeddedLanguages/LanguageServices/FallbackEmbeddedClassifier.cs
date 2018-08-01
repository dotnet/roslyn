// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    internal class FallbackEmbeddedClassifier : IEmbeddedClassifier
    {
        private readonly AbstractEmbeddedLanguageProvider _provider;
        private readonly FallbackEmbeddedLanguage _language;

        public FallbackEmbeddedClassifier(
            AbstractEmbeddedLanguageProvider provider,
            FallbackEmbeddedLanguage language)
        {
            _provider = provider;
            _language = language;
        }

        public void AddClassifications(
            Workspace workspace, SyntaxToken token, SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (_language.StringLiteralToken != token.RawKind &&
                _language.InterpolatedTextToken != token.RawKind)
            {
                return;
            }

            var virtualChars = _language.VirtualCharService.TryConvertToVirtualChars(token);
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
