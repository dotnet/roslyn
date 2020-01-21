// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataConstructorDeclarationStructureProvider : AbstractMetadataAsSourceStructureProvider<ConstructorDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(ConstructorDeclarationSyntax node)
        {
            return node.Modifiers.Count > 0
                    ? node.Modifiers.First()
                    : node.Identifier;
        }
    }
}
