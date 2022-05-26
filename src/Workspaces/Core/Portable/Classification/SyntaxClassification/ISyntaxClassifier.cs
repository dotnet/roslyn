// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal interface ISyntaxClassifier
    {
        /// <summary>
        /// The syntax node types this classifier is able to classify
        /// </summary>
        ImmutableArray<Type> SyntaxNodeTypes { get; }

        /// <summary>
        /// The syntax token kinds this classifier is able to classify
        /// </summary>
        ImmutableArray<int> SyntaxTokenKinds { get; }

        /// <summary>
        /// This method will be called for all nodes that match the types specified by the <see cref="SyntaxNodeTypes"/> property.
        /// </summary>
        void AddClassifications(SyntaxNode node, SemanticModel semanticModel, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// This method will be called for all tokens that match the kinds specified by the <see cref="SyntaxTokenKinds"/> property.
        /// </summary>
        void AddClassifications(SyntaxToken token, SemanticModel semanticModel, ClassificationOptions options, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);
    }
}
