// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpAnonymousTypePropertyReducer : AbstractCSharpReducer
    {
        public override IExpressionRewriter CreateExpressionRewriter(OptionSet optionSet, CancellationToken cancellationToken)
        {
            return new Rewriter(optionSet, cancellationToken);
        }

        private static SyntaxNode SimplifyNameEquals(NameEqualsSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            // Reduces "var a = new { C = A.B.C }" to "var a = new { A.B.C }" when possible

            var declarator = node.Parent as AnonymousObjectMemberDeclaratorSyntax;
            if (declarator == null)
            {
                return node;
            }

            var explicitPropertyName = node.Name?.Identifier.Text;
            string implicitPropertyName = null;

            var identifier = declarator.Expression as IdentifierNameSyntax;
            var memberAccess = declarator.Expression as MemberAccessExpressionSyntax;
            if (identifier != null)
            {
                implicitPropertyName = identifier.Identifier.Text;
            }
            else if (memberAccess != null)
            {
                implicitPropertyName = memberAccess.Name?.Identifier.Text;
            }

            if (implicitPropertyName == null || implicitPropertyName != explicitPropertyName)
            {
                return node;
            }

            // The NameEquals is removable
            return null;
        }
    }
}
