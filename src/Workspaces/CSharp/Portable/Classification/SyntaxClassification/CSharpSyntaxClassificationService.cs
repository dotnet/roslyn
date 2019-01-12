// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportLanguageService(typeof(ISyntaxClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxClassificationService : AbstractSyntaxClassificationService
    {
        private readonly ImmutableArray<ISyntaxClassifier> s_defaultSyntaxClassifiers =
            ImmutableArray.Create<ISyntaxClassifier>(
                new EmbeddedLanguagesClassifier(),
                new NameSyntaxClassifier(),
                new OperatorOverloadSyntaxClassifier(),
                new SyntaxTokenClassifier(),
                new UsingDirectiveSyntaxClassifier());

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
