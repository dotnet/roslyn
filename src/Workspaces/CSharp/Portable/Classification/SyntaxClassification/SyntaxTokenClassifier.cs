﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class SyntaxTokenClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<int> SyntaxTokenKinds { get; } = ImmutableArray.Create((int)SyntaxKind.LessThanToken);

        private static readonly Func<ITypeSymbol, bool> s_shouldInclude = t => t.TypeKind != TypeKind.Error && t.GetArity() > 0;

        public override void AddClassifications(
            Workspace workspace,
            SyntaxToken lessThanToken,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            if (syntaxTree.IsInPartiallyWrittenGeneric(lessThanToken.Span.End, cancellationToken, out var identifier))
            {
                // IsInPartiallyWrittenGeneric will return true for things that could be 
                // partially generic method calls (as opposed to partially written types).
                //
                // For example: X?.Y<
                //
                // In this case, this could never be a type, and we do not want to try to 
                // resolve it as such as it can lead to inappropriate classifications.
                if (CouldBeGenericType(identifier))
                {
                    var types = semanticModel.LookupTypeRegardlessOfArity(identifier, cancellationToken);
                    if (types.Any(s_shouldInclude))
                    {
#nullable disable // Can 'GetClassificationForType(types.First()' be null here?
                        result.Add(new ClassifiedSpan(identifier.Span, GetClassificationForType(types.First())));
#nullable enable
                    }
                }
            }
        }

        private static bool CouldBeGenericType(SyntaxToken identifier)
        {
            // Look for patterns that indicate that this could never be a partially written 
            // generic *Type* (although it could be a partially written generic method).

            if (!(identifier.Parent is IdentifierNameSyntax identifierName))
            {
                // Definitely not a generic type if this isn't even an identifier name.
                return false;
            }

            if (identifierName.IsParentKind(SyntaxKind.MemberBindingExpression))
            {
                // anything?.Identifier is never a generic type.
                return false;
            }

            // ?.X.Identifier   or  ?.X.Y.Identifier  is never a generic type.
            if (identifierName.IsSimpleMemberAccessExpressionName() ||
                identifierName.IsMemberBindingExpressionName())
            {
                if (identifier.Parent.IsParentKind(SyntaxKind.ConditionalAccessExpression))
                    return false;
            }

            // Add more cases as necessary.
            return true;
        }
    }
}
