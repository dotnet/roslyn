﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportLanguageService(typeof(ISyntaxClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxClassificationService : AbstractSyntaxClassificationService
    {
        private readonly IEnumerable<ISyntaxClassifier> s_defaultSyntaxClassifiers =
            ImmutableArray.Create<ISyntaxClassifier>(
                new NameSyntaxClassifier(),
                new SyntaxTokenClassifier(),
                new UsingDirectiveSyntaxClassifier());

        public override IEnumerable<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
        {
            return s_defaultSyntaxClassifiers;
        }

        public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken);
        }

        public override void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var root = syntaxTree.GetRoot(cancellationToken);
            Worker.CollectClassifiedSpans(root, textSpan, result, cancellationToken);
        }

        public override ClassifiedSpan FixClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
        {
            return ClassificationHelpers.AdjustStaleClassification(rawText, classifiedSpan);
        }
    }
}
