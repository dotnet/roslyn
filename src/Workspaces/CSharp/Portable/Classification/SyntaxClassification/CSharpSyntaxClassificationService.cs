﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class CSharpSyntaxClassificationService : AbstractSyntaxClassificationService
    {
        private readonly ImmutableArray<ISyntaxClassifier> s_defaultSyntaxClassifiers;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public CSharpSyntaxClassificationService(HostLanguageServices languageServices)
        {
            var syntaxClassifiers = ImmutableArray<ISyntaxClassifier>.Empty;
            var embeddedLanguagesProvider = languageServices.GetService<IEmbeddedLanguagesProvider>();
            if (embeddedLanguagesProvider != null)
            {
                syntaxClassifiers = syntaxClassifiers.Add(new EmbeddedLanguagesClassifier(embeddedLanguagesProvider));
            }

            s_defaultSyntaxClassifiers = syntaxClassifiers.AddRange(
                new ISyntaxClassifier[]
                {
                    new NameSyntaxClassifier(),
                    new OperatorOverloadSyntaxClassifier(),
                    new SyntaxTokenClassifier(),
                    new UsingDirectiveSyntaxClassifier(),
                    new DiscardSyntaxClassifier()
                });
        }

        public override ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
            => s_defaultSyntaxClassifiers;

        public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
            => ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken);

        public override void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
            => Worker.CollectClassifiedSpans(syntaxTree.GetRoot(cancellationToken), textSpan, result, cancellationToken);

        public override ClassifiedSpan FixClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
            => ClassificationHelpers.AdjustStaleClassification(rawText, classifiedSpan);
    }
}
