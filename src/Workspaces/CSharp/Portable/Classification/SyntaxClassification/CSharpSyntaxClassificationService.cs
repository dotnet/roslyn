// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification;

[ExportLanguageServiceFactory(typeof(ISyntaxClassificationService), LanguageNames.CSharp), Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSyntaxClassificationServiceFactory() : ILanguageServiceFactory
{
    public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
        => new CSharpSyntaxClassificationService(languageServices.LanguageServices.SolutionServices);

    private sealed class CSharpSyntaxClassificationService(SolutionServices solutionServices)
        : AbstractSyntaxClassificationService
    {
        private readonly ImmutableArray<ISyntaxClassifier> _defaultClassifiers =
            [
                new NameSyntaxClassifier(),
                new OperatorOverloadSyntaxClassifier(),
                new SyntaxTokenClassifier(),
                new UsingDirectiveSyntaxClassifier(),
                new DiscardSyntaxClassifier(),
                new FunctionPointerUnmanagedCallingConventionClassifier(),
                new DocCommentCodeBlockClassifier(solutionServices),
            ];

        public override ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
            => _defaultClassifiers;

        public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
            => ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken);

        public override void AddSyntacticClassifications(SyntaxNode root, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            foreach (var textSpan in textSpans)
            {
                Worker.CollectClassifiedSpans(root, textSpan, result, cancellationToken);
            }
        }

        public override ClassifiedSpan FixClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
            => ClassificationHelpers.AdjustStaleClassification(rawText, classifiedSpan);

        public override string? GetSyntacticClassificationForIdentifier(SyntaxToken identifier)
            => ClassificationHelpers.GetSyntacticClassificationForIdentifier(identifier);
    }
}
