// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class FieldKeywordRecommender()
    : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.FieldKeyword)
{
    // interfaces don't have members that you can put a [field:] attribute on
    private static readonly ISet<SyntaxKind> s_validTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.StructDeclaration,
        SyntaxKind.ClassDeclaration,
        SyntaxKind.RecordDeclaration,
        SyntaxKind.RecordStructDeclaration,
        SyntaxKind.EnumDeclaration,
    };

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsMemberAttributeContext(s_validTypeDeclarations, cancellationToken))
            return true;

        if (context.IsAnyExpressionContext || context.IsStatementContext)
        {
            var token = context.TargetToken;
            if (IsInPropertyAccessor(token.Parent))
                return true;
        }

        return false;
    }

    private static bool IsInPropertyAccessor(SyntaxNode? node)
    {
        while (node != null)
        {
            if (node is ArrowExpressionClauseSyntax { Parent: PropertyDeclarationSyntax })
                return true;

            if (node is AccessorDeclarationSyntax { Parent: AccessorListSyntax { Parent: PropertyDeclarationSyntax } })
                return true;

            node = node.Parent;
        }

        return false;
    }
}
