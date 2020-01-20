// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
