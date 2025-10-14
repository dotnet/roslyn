// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class StructKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.StructKeyword)
{
    private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.InternalKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.RefKeyword,
            SyntaxKind.ReadOnlyKeyword,
            SyntaxKind.FileKeyword,
        };

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsGlobalStatementContext ||
            context.IsTypeDeclarationContext(
                validModifiers: s_validModifiers,
                validTypeDeclarations: SyntaxKindSet.NonEnumTypeDeclarations,
                canBePartial: true,
                cancellationToken: cancellationToken) ||
            context.IsRecordDeclarationContext(s_validModifiers, cancellationToken) ||
            IsConstraintContext(context);
    }

    private static bool IsConstraintContext(CSharpSyntaxContext context)
    {
        //    where T : |
        if (context.SyntaxTree.IsTypeParameterConstraintStartContext(context.Position, context.LeftToken))
        {
            return true;
        }

        // cases:
        //    where T : allows ref |
        //    where T : struct, allows ref |
        //    where T : class, allows ref |
        //    where T : new(), allows ref |
        //    where T : Goo, allows ref |

        var token = context.TargetToken;

        if (token.Kind() == SyntaxKind.RefKeyword &&
            token.Parent is RefStructConstraintSyntax refStructConstraint && refStructConstraint.RefKeyword == token &&
            refStructConstraint.Parent is AllowsConstraintClauseSyntax allowsClause &&
            allowsClause.Parent is TypeParameterConstraintClauseSyntax)
        {
            return true;
        }

        return false;
    }
}
