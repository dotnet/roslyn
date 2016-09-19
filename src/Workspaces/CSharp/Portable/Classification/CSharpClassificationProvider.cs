// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportClassificationProvider(PredefinedClassificationProviderNames.Default, LanguageNames.CSharp), Shared]
    internal class CSharpClassificationProvider : CommonClassificationProvider
    {
        private static readonly ImmutableArray<ISemanticClassifier> s_defaultClassifiers =
            ImmutableArray.Create<ISemanticClassifier>(
                new NameSyntaxClassifier(),
                new SyntaxTokenClassifier(),
                new UsingDirectiveSyntaxClassifier());

        public override ImmutableArray<ISemanticClassifier> GetDefaultSemanticClassifiers()
        {
            return s_defaultClassifiers;
        }

        public override void AddLexicalClassifications(SourceText text, TextSpan span, ClassificationContext context, CancellationToken cancellationToken)
        {
            ClassificationHelpers.AddLexicalClassifications(text, span, context, cancellationToken);
        }

        protected override void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ClassificationContext context, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken);
            SyntacticClassifier.CollectClassifiedSpans(root, textSpan, context, cancellationToken);
        }

        public override ClassifiedSpan AdjustClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
        {
            return ClassificationHelpers.AdjustStaleClassification(rawText, classifiedSpan);
        }
    }
}
