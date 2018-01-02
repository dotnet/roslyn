// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
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
        /// This method will be called for all nodes that match the types specified by the SyntaxNodeTypes property.
        /// Implementations should return null (instead of an empty enumerable) if they have no classifications for the provided node.
        /// </summary>
        void AddClassifications(Workspace workspace, SyntaxNode node, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// This method will be called for all nodes that match the types specified by the SyntaxTokenKinds property.
        /// Implementations should return null (instead of an empty enumerable) if they have no classifications for the provided token.
        /// </summary>
        void AddClassifications(Workspace workspace, SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken);
    }
}
