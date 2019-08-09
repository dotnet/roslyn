// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal sealed partial class CSharpNullableAnnotationReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpNullableAnnotationReducer() : base(s_pool)
        {
        }

        private static readonly Func<NullableTypeSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyNullableType = SimplifyNullableType;

        private static SyntaxNode SimplifyNullableType(
            NullableTypeSyntax node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            // If annotations are enabled, there's no further simplification to do
            var context = semanticModel.GetNullableContext(node.Span.End);

            // Work around https://github.com/dotnet/roslyn/issues/37809
            if (semanticModel.IsSpeculativeSemanticModel && context.AnnotationsInherited())
            {
                // Work around bug where GetNullableContext() on a speculative model doesn't inherit automatically
                context = semanticModel.ParentModel.GetNullableContext(semanticModel.OriginalPositionForSpeculation);
            }

            if (context.AnnotationsEnabled())
            {
                return node;
            }

            // If it's not a reference type, also leave the ?
            var type = semanticModel.GetTypeInfo(node, cancellationToken).Type;
            if (type == null || !type.IsReferenceType)
            {
                return node;
            }

            // Drop the ?
            return node.ElementType.WithAppendedTrailingTrivia(node.QuestionToken.GetAllTrivia());
        }
    }
}
