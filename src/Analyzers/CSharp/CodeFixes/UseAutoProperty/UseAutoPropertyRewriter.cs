// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty;

using static SyntaxFactory;

internal sealed partial class CSharpUseAutoPropertyCodeFixProvider
{
    private sealed class UseAutoPropertyRewriter(
        IdentifierNameSyntax propertyIdentifierName,
        ISet<IdentifierNameSyntax> identifierNames) : CSharpSyntaxRewriter
    {
        private readonly IdentifierNameSyntax _propertyIdentifierName = propertyIdentifierName;
        private readonly ISet<IdentifierNameSyntax> _identifierNames = identifierNames;

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name is IdentifierNameSyntax identifierName &&
                _identifierNames.Contains(identifierName))
            {
                if (node.Expression.IsKind(SyntaxKind.ThisExpression))
                {
                    // `this.fieldName` gets rewritten to `field`.
                    return FieldExpression().WithTriviaFrom(node);
                }
                else
                {
                    // `obj.fieldName` gets rewritten to `obj.PropName`
                    return node.WithName(_propertyIdentifierName.WithTriviaFrom(identifierName));
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_identifierNames.Contains(node))
            {
                if (node.Parent is AssignmentExpressionSyntax
                    {
                        Parent: InitializerExpressionSyntax { RawKind: (int)SyntaxKind.ObjectInitializerExpression }
                    } assignment && assignment.Left == node)
                {
                    // `new X { fieldName = ... }` gets rewritten to `new X { propName = ... }`
                    return _propertyIdentifierName.WithTriviaFrom(node);
                }

                // Any other naked reference to fieldName within the property gets updated to `field`.
                return FieldExpression().WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }
}
