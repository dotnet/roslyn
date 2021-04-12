// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class MemberDeclarationSyntaxExtensions
    {
        public static SyntaxList<AttributeListSyntax> GetAttributes(this MemberDeclarationSyntax member)
        {
            if (member != null)
            {
                return member.AttributeLists;
            }

            return SyntaxFactory.List<AttributeListSyntax>();
        }
    }
}
