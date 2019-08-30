// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal class EmbeddedLanguagesClassifier : AbstractSyntaxClassifier
    {
        private readonly IEmbeddedLanguagesProvider _languagesProvider;

        public override ImmutableArray<int> SyntaxTokenKinds { get; }

        public EmbeddedLanguagesClassifier(IEmbeddedLanguagesProvider languagesProvider)
        {
            _languagesProvider = languagesProvider;
            SyntaxTokenKinds =
                languagesProvider.Languages.SelectMany(p => p.Classifier.SyntaxTokenKinds).Distinct().ToImmutableArray();
        }

        public override void AddClassifications(Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            foreach (var language in _languagesProvider.Languages)
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
