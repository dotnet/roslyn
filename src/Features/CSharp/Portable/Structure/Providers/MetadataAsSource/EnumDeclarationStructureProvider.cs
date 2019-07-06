// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataEnumDeclarationStructureProvider : AbstractMetadataAsSourceStructureProvider<EnumDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(EnumDeclarationSyntax node)
        {
            return node.Modifiers.Count > 0
                    ? node.Modifiers.First()
                    : node.EnumKeyword;
        }

        protected override SyntaxToken GetHintTextEndToken(EnumDeclarationSyntax node)
        {
            return node.OpenBraceToken.GetPreviousToken();
        }
    }
}
