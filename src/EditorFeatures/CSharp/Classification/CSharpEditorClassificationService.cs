// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Classification
{
    [ExportLanguageService(typeof(IEditorClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpEditorClassificationService : AbstractEditorClassificationService
    {
        public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, result, cancellationToken);
        }

        public override ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
        {
            return ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan);
        }
    }
}
