// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class RefTypeSyntax
    {
        public RefTypeSyntax Update(SyntaxToken refKeyword, TypeSyntax type)
        {
            return Update(refKeyword, default(SyntaxToken), type);
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
            return RefType(refKeyword, default(SyntaxToken), type);
        }
    }
}
