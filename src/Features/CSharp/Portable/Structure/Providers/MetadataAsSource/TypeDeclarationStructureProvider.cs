// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataTypeDeclarationStructureProvider : AbstractMetadataAsSourceStructureProvider<TypeDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(TypeDeclarationSyntax node)
        {
            return node.Modifiers.Count > 0
                    ? node.Modifiers.First()
                    : node.Keyword;
        }

        protected override SyntaxToken GetHintTextEndToken(TypeDeclarationSyntax node)
        {
            return node.OpenBraceToken.GetPreviousToken();
        }
    }
}
