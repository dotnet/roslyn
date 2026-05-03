// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class NameSyntaxExtensions
{
    public static IList<NameSyntax> GetNameParts(this NameSyntax nameSyntax)
        => new NameSyntaxIterator(nameSyntax).ToList();

    public static NameSyntax GetLastDottedName(this NameSyntax nameSyntax)
    {
        var parts = nameSyntax.GetNameParts();
        return parts[parts.Count - 1];
    }

    public static SyntaxToken GetNameToken(this NameSyntax nameSyntax)
        => nameSyntax switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier,
            AliasQualifiedNameSyntax aliasName => aliasName.Name.Identifier,
            _ => throw ExceptionUtilities.Unreachable(),
        };

    public static bool CanBeReplacedWithAnyName(this NameSyntax nameSyntax)
    {
        if (nameSyntax.Parent?.Kind()
                is SyntaxKind.AliasQualifiedName
                or SyntaxKind.NameColon
                or SyntaxKind.NameEquals
                or SyntaxKind.TypeParameterConstraintClause)
        {
            return false;
        }

        if (nameSyntax.CheckParent<QualifiedNameSyntax>(q => q.Right == nameSyntax) ||
            nameSyntax.CheckParent<MemberAccessExpressionSyntax>(m => m.Name == nameSyntax))
        {
            return false;
        }

        // TODO(cyrusn): Add more cases as the language changes.
        return true;
    }
}
