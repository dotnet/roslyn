// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public enum XmlNameAttributeElementKind : byte
    {
        Parameter = 0,
        ParameterReference = 1,
        TypeParameter = 2,
        TypeParameterReference = 3,
    }
}
