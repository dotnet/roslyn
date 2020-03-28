// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    [ExportLanguageService(typeof(IClassificationService), LanguageNames.CSharp), Shared]
    internal class CSharpEditorClassificationService : AbstractClassificationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorClassificationService()
        {
        }

        public override void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
            ClassificationHelpers.AddLexicalClassifications(text, textSpan, temp, cancellationToken);
            AddRange(temp, result);
        }

        public override ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan)
            => ClassificationHelpers.AdjustStaleClassification(text, classifiedSpan);
    }
}
