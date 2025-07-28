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
    extension(NameSyntax nameSyntax)
    {
        public IList<NameSyntax> GetNameParts()
        => new NameSyntaxIterator(nameSyntax).ToList();

        public NameSyntax GetLastDottedName()
        {
            var parts = nameSyntax.GetNameParts();
            return parts[parts.Count - 1];
        }

        public SyntaxToken GetNameToken()
            => nameSyntax switch
            {
                SimpleNameSyntax simpleName => simpleName.Identifier,
                QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier,
                AliasQualifiedNameSyntax aliasName => aliasName.Name.Identifier,
                _ => throw ExceptionUtilities.Unreachable(),
            };

        public bool CanBeReplacedWithAnyName()
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
