﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataDelegateDeclarationStructureProvider : AbstractMetadataAsSourceStructureProvider<DelegateDeclarationSyntax>
    {
        protected override SyntaxToken GetEndToken(DelegateDeclarationSyntax node)
        {
            return node.Modifiers.Count > 0
                    ? node.Modifiers.First()
                    : node.DelegateKeyword;
        }
    }
}
