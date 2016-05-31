// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class IncompleteMemberSyntax
    {
        public IncompleteMemberSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, TypeSyntax type)
        {
            return Update(attributeLists, modifiers, this.RefKeyword, type);
        }
    }
}
