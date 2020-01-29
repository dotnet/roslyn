// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataEventFieldDeclarationStructureProvider : AbstractMetadataAsSourceStructureProvider<EventFieldDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(EventFieldDeclarationSyntax node)
        {
            return node.Modifiers.Count > 0
                    ? node.Modifiers.First()
                    : node.EventKeyword;
        }
    }
}
