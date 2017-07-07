// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal abstract class AbstractSyntaxClassifier : ISyntaxClassifier
    {
        protected AbstractSyntaxClassifier()
        {
        }

        protected ClassificationTypeKind? GetClassificationForType(ITypeSymbol type)
            => type.GetClassification();

        public virtual void AddClassifications(SyntaxNode syntax, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpanSlim> result, CancellationToken cancellationToken)
        {
        }

        public virtual void AddClassifications(SyntaxToken syntax, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpanSlim> result, CancellationToken cancellationToken)
        {
        }

        public virtual ImmutableArray<Type> SyntaxNodeTypes
            => ImmutableArray<Type>.Empty;

        public virtual ImmutableArray<int> SyntaxTokenKinds
            => ImmutableArray<int>.Empty;
    }
}
