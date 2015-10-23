// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource
{
    internal class EnumMemberDeclarationOutliner : AbstractMetadataAsSourceOutliner<EnumMemberDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(EnumMemberDeclarationSyntax node)
        {
            return node.Identifier;
        }
    }
}
