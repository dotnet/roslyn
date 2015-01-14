// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.CodeAnalysis.Classification.Classifiers
{
    internal interface ISyntaxClassifier
    {
        /// <summary>
        /// The syntax node types this classifier is able to classify
        /// </summary>
        IEnumerable<Type> SyntaxNodeTypes { get; }

        /// <summary>
        /// The syntax token kinds this classifier is able to classify
        /// </summary>
        IEnumerable<int> SyntaxTokenKinds { get; }

        /// <summary>
        /// This method will be called for all nodes that match the types specified by the SyntaxNodeTypes property.
        /// Implementations should return null (instead of an empty enumerable) if they have no classifications for the provided node.
        /// </summary>
        IEnumerable<ClassifiedSpan> ClassifyNode(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken);

        /// <summary>
        /// This method will be called for all nodes that match the types specified by the SyntaxTokenKinds property.
        /// Implementations should return null (instead of an empty enumerable) if they have no classifications for the provided token.
        /// </summary>
        IEnumerable<ClassifiedSpan> ClassifyToken(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken);
    }
}
