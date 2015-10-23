// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource
{
    internal class PropertyDeclarationOutliner : AbstractMetadataAsSourceOutliner<PropertyDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(PropertyDeclarationSyntax node)
        {
            return node.Modifiers.Count > 0
                    ? node.Modifiers.First()
                    : node.Type.GetFirstToken();
        }
    }
}
