// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification;

[ExportLanguageService(typeof(IClassificationService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpEditorClassificationService(
    CSharpSyntaxClassificationService syntaxClassificationService) : AbstractClassificationService(syntaxClassificationService)
{
    public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken)
        => ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken);

    public override ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
        => ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan);
}
