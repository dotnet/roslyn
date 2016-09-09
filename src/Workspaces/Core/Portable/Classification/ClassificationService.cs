// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract class ClassificationService : ILanguageService
    {
        // this is really GetQuickClassifications
        public abstract ImmutableArray<ClassifiedSpan> GetLexicalClassifications(SourceText text, TextSpan span, CancellationToken cancellationToken);

        public abstract ClassifiedSpan AdjustClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public abstract Task<ImmutableArray<ClassifiedSpan>> GetSyntacticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
        public abstract Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
    }
}
