// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class EmbeddedLanguageTokenClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<int> SyntaxTokenKinds { get; } = ImmutableArray.Create((int)SyntaxKind.StringLiteralToken);

        public override void AddClassifications(Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);

            var languageProvider = CSharpEmbeddedLanguageProvider.Instance;
            foreach (var language in languageProvider.GetEmbeddedLanguages())
            {
                var classifier = language.Classifier;
                if (classifier != null)
                {
                    classifier.AddClassifications(workspace, token, semanticModel, result, cancellationToken);
                }
            }
        }
    }
}
