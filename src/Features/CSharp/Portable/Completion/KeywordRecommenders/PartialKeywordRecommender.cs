// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class PartialKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.PartialKeyword)
{
    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsGlobalStatementContext ||
            IsMemberDeclarationContext(context, cancellationToken) ||
            IsTypeDeclarationContext(context, cancellationToken);
    }

    private static bool IsMemberDeclarationContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMemberDeclarationContext(
            validModifiers: SyntaxKindSet.AllMemberModifiers,
            validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
            canBePartial: false,
            cancellationToken: cancellationToken))
        {
            var token = context.LeftToken;
            var decl = token.GetRequiredAncestor<TypeDeclarationSyntax>();

            // partial methods must be in partial types
            if (!decl.Modifiers.Any(t => t.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword)))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    private static bool IsTypeDeclarationContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsTypeDeclarationContext(
            validModifiers: SyntaxKindSet.AllTypeModifiers,
            validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
            canBePartial: false,
            cancellationToken: cancellationToken);
    }
}
