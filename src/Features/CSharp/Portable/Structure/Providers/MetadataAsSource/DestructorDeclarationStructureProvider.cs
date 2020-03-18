// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
