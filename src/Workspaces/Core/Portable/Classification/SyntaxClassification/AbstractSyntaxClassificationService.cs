// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        public abstract void AddSyntacticClassifications(SyntaxTree syntaxTree, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        public abstract ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers();
        public abstract ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public async Task AddSemanticClassificationsAsync(
            Document document, TextSpan textSpan,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            try
            {
                var semanticModel = await document.GetSemanticModelForSpanAsync(textSpan, cancellationToken).ConfigureAwait(false);
                AddSemanticClassifications(semanticModel, textSpan, document.Project.Solution.Workspace, getNodeClassifiers, getTokenClassifiers, result, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public void AddSemanticClassifications(
            SemanticModel semanticModel, TextSpan textSpan, Workspace workspace,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            Worker.Classify(workspace, semanticModel, textSpan, result, getNodeClassifiers, getTokenClassifiers, cancellationToken);
        }
    }
}
