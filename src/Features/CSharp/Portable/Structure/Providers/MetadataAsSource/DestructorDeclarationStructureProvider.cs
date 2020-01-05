// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataDestructorDeclarationStructureProvider : AbstractMetadataAsSourceStructureProvider<DestructorDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(DestructorDeclarationSyntax node)
        {
            return node.TildeToken;
        }
    }
}
