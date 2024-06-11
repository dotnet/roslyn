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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification;

[ExportLanguageService(typeof(ISyntaxClassificationService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSyntaxClassificationService() : AbstractSyntaxClassificationService
{
    private static readonly ImmutableArray<ISyntaxClassifier> s_defaultSyntaxClassifiers = ImmutableArray.Create<ISyntaxClassifier>(
        new NameSyntaxClassifier(),
        new OperatorOverloadSyntaxClassifier(),
        new SyntaxTokenClassifier(),
        new UsingDirectiveSyntaxClassifier(),
        new DiscardSyntaxClassifier(),
        new FunctionPointerUnmanagedCallingConventionClassifier());

    public override ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers()
        => s_defaultSyntaxClassifiers;

    public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        => ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken);

    public override void AddSyntacticClassifications(SyntaxNode root, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        => Worker.CollectClassifiedSpans(root, textSpan, result, cancellationToken);

    public override ClassifiedSpan FixClassification(SourceText rawText, ClassifiedSpan classifiedSpan)
        => ClassificationHelpers.AdjustStaleClassification(rawText, classifiedSpan);

    public override string? GetSyntacticClassificationForIdentifier(SyntaxToken identifier)
        => ClassificationHelpers.GetSyntacticClassificationForIdentifier(identifier);
}
