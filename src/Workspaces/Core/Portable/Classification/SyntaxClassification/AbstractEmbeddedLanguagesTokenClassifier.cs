// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal abstract class AbstractEmbeddedLanguagesTokenClassifier : ISyntaxClassifier
    {
        private readonly IEmbeddedLanguagesProvider _languagesProvider;

        public ImmutableArray<Type> SyntaxNodeTypes { get; }
        public ImmutableArray<int> SyntaxTokenKinds { get; }

        protected AbstractEmbeddedLanguagesTokenClassifier(IEmbeddedLanguagesProvider languagesProvider)
        {
            _languagesProvider = languagesProvider;
            SyntaxNodeTypes = languagesProvider.GetEmbeddedLanguages()
                                               .SelectMany(p => p.Classifier.SyntaxNodeTypes)
                                               .Distinct()
                                               .ToImmutableArray();
            SyntaxTokenKinds = languagesProvider.GetEmbeddedLanguages()
                                                .SelectMany(p => p.Classifier.SyntaxTokenKinds)
                                                .Distinct()
                                                .ToImmutableArray();
        }

        public void AddClassifications(Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            foreach (var language in _languagesProvider.GetEmbeddedLanguages())
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

        public void AddClassifications(Workspace workspace, SyntaxNode node, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            foreach (var language in _languagesProvider.GetEmbeddedLanguages())
            {
                var classifier = language.Classifier;
                if (classifier != null)
                {
                    var count = result.Count;
                    classifier.AddClassifications(workspace, node, semanticModel, result, cancellationToken);
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
