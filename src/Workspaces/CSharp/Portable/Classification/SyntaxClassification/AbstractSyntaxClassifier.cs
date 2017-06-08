// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal abstract class AbstractSyntaxClassifier : ISyntaxClassifier
    {
        protected AbstractSyntaxClassifier()
        {
        }

        protected string GetClassificationForType(ITypeSymbol type)
        {
            return type.GetClassification();
        }

        public virtual IEnumerable<ClassifiedSpan> ClassifyNode(SyntaxNode syntax, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return null;
        }

        public virtual IEnumerable<ClassifiedSpan> ClassifyToken(SyntaxToken syntax, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            return null;
        }

        public virtual IEnumerable<Type> SyntaxNodeTypes
        {
            get
            {
                return null;
            }
        }

        public virtual IEnumerable<int> SyntaxTokenKinds
        {
            get
            {
                return null;
            }
        }
    }
}
