// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static SyntaxList<AttributeListSyntax> GenerateAttributeLists(ImmutableArray<AttributeData> attributes)
        {
            if (attributes.IsDefaultOrEmpty)
                return default;

            throw new NotImplementedException();
        }
    }
}
