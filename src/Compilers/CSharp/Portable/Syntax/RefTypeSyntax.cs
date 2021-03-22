// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class RefTypeSyntax
    {
        public RefTypeSyntax Update(SyntaxToken refKeyword, TypeSyntax type)
        {
            return Update(refKeyword, ReadOnlyKeyword, type);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new RefTypeSyntax instance.</summary>
        public static RefTypeSyntax RefType(SyntaxToken refKeyword, TypeSyntax type)
        {
            return RefType(refKeyword, readOnlyKeyword: default, type);
        }
    }
}
