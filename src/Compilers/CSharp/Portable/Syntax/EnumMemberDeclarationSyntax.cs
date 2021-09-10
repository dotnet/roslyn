// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class EnumMemberDeclarationSyntax
    {
        public EnumMemberDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken identifier, EqualsValueClauseSyntax equalsValue)
            => this.Update(attributeLists, this.Modifiers, identifier, equalsValue);
    }
}
