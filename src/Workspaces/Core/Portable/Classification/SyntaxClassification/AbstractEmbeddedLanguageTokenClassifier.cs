// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal abstract class AbstractEmbeddedLanguageTokenClassifier : AbstractSyntaxClassifier
    {
        private readonly IEmbeddedLanguageProvider _languageProvider;

        protected AbstractEmbeddedLanguageTokenClassifier(IEmbeddedLanguageProvider languageProvider)
        {
            _languageProvider = languageProvider;
        }

        public sealed override void AddClassifications(Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            foreach (var language in _languageProvider.GetEmbeddedLanguages())
            {
                var classifier = language.Classifier;
                if (classifier != null)
                {
                    var count = result.Count;
                    classifier.AddClassifications(workspace, token, semanticModel, result, cancellationToken);
                    if (result.Count != count)
                    {
                        // This classifier added values.  No need to check the other ones.
                        return;
                    }
                }
            }
        }
    }
}
