// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
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
        {
            while (true)
            {
                if (nameSyntax.Kind() == SyntaxKind.IdentifierName)
                {
                    return ((IdentifierNameSyntax)nameSyntax).Identifier;
                }
                else if (nameSyntax.Kind() == SyntaxKind.QualifiedName)
                {
                    nameSyntax = ((QualifiedNameSyntax)nameSyntax).Right;
                }
                else if (nameSyntax.Kind() == SyntaxKind.GenericName)
                {
                    return ((GenericNameSyntax)nameSyntax).Identifier;
                }
                else if (nameSyntax.Kind() == SyntaxKind.AliasQualifiedName)
                {
                    nameSyntax = ((AliasQualifiedNameSyntax)nameSyntax).Name;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

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
}
