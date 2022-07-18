// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract partial class AbstractSyntaxClassificationService : ISyntaxClassificationService
    {
        protected AbstractSyntaxClassificationService()
        {
        }

        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract void AddSyntacticClassifications(SyntaxNode root, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        public abstract ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers();
        public abstract ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan);
        public abstract string? GetSyntacticClassificationForIdentifier(SyntaxToken identifier);

        public async Task AddSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            try
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                AddSemanticClassifications(semanticModel, textSpan, getNodeClassifiers, getTokenClassifiers, result, options, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public void AddSemanticClassifications(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            ClassificationOptions options,
            CancellationToken cancellationToken)
        {
            Worker.Classify(semanticModel, textSpan, result, getNodeClassifiers, getTokenClassifiers, options, cancellationToken);
        }

        public TextChangeRange? ComputeSyntacticChangeRange(SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
            => SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, cancellationToken);
    }
}
