﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class BaseMethodDeclarationSyntax
    {
        public abstract override SyntaxList<AttributeListSyntax> AttributeLists { get; }
        public abstract override SyntaxTokenList Modifiers { get; }
    }
}
