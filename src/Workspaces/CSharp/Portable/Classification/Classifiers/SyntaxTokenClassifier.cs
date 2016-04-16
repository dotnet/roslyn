// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class SyntaxTokenClassifier : AbstractSyntaxClassifier
    {
        public override IEnumerable<int> SyntaxTokenKinds
        {
            get
            {
                yield return (int)SyntaxKind.LessThanToken;
            }
        }

        private static readonly Func<ITypeSymbol, bool> s_shouldInclude = t => t.TypeKind != TypeKind.Error && t.GetArity() > 0;

        public override IEnumerable<ClassifiedSpan> ClassifyToken(
            SyntaxToken lessThanToken,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var syntaxTree = semanticModel.SyntaxTree;

            SyntaxToken identifier;
            if (syntaxTree.IsInPartiallyWrittenGeneric(lessThanToken.Span.End, cancellationToken, out identifier))
            {
                var types = semanticModel.LookupTypeRegardlessOfArity(identifier, cancellationToken);
                if (types.Any(s_shouldInclude))
                {
                    return SpecializedCollections.SingletonEnumerable(
                        new ClassifiedSpan(identifier.Span, GetClassificationForType(types.First())));
                }
            }

            return null;
        }
    }
}
