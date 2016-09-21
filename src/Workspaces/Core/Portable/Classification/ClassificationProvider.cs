// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// Extend this class if you want to provide classifications to a <see cref="ClassificationServiceWithProviders"/> service.
    /// Use the <see cref="ExportClassificationProviderAttribute"/> on your declaration in order for the service to find your provider.
    /// </summary>
    internal abstract class ClassificationProvider
    {
        /// <summary>
        /// Override this method to provide quick lexical classifications.
        /// </summary>
        public virtual void AddLexicalClassifications(SourceText text, TextSpan span, ClassificationContext context, CancellationToken cancellationToken)
        {
        }

        /// <summary>
        /// Override this method to provide adjustments to existing classifications after text changes due to typing.
        /// </summary>
        public virtual ClassifiedSpan AdjustClassification(SourceText changedText, ClassifiedSpan classifiedSpan)
        {
            return classifiedSpan;
        }

        /// <summary>
        /// Override this method to provide classifications based on syntax nodes and tokens.
        /// If your classifications need access to symbols override <see cref="AddSemanticClassificationsAsync"/> instead.
        /// </summary>
        public virtual Task AddSyntacticClassificationsAsync(Document document, TextSpan span, ClassificationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Override this method to provide classifications based on symbol references.
        /// </summary>
        public virtual Task AddSemanticClassificationsAsync(Document document, TextSpan span, ClassificationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }
}
