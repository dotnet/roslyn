// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class DiscardSyntaxClassifier : AbstractNameSyntaxClassifier
    {
        public override ImmutableArray<int> SyntaxTokenKinds => ImmutableArray.Create((int)SyntaxKind.Argument);

        protected override int? GetRightmostNameArity(SyntaxNode node)
        {
            return null;
        }

        protected override bool IsParentAnAttribute(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.Attribute);
        }

        public override void AddClassifications(
           Workspace workspace,
           SyntaxNode syntax,
           SemanticModel semanticModel,
           ArrayBuilder<ClassifiedSpan> result,
           CancellationToken cancellationToken)
        {
            if (syntax is ArgumentSyntax argument)
            {
                if (!argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    return;
                }

                var operation = semanticModel.GetOperation(argument.Expression, cancellationToken);

                if (operation is IDiscardOperation discardOperation)
                {
                    var span = discardOperation.Syntax switch
                    {
                        DeclarationExpressionSyntax decl => decl.Designation.Span,  // the case vor out var _
                        IdentifierNameSyntax discard => discard.Span,               // the case for out _
                        _ => default
                    };

                    if (span != default)
                    {
                        result.Add(new ClassifiedSpan(span, ClassificationTypeNames.Keyword));
                    }
                }
            }
        }
    }
}
