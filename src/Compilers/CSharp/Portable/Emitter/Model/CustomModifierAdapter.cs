// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Emit;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class CSharpCustomModifier : Cci.ICustomModifier
    {
        bool Cci.ICustomModifier.IsOptional
        {
            get { return this.IsOptional; }
        }

        Cci.ITypeReference Cci.ICustomModifier.GetModifier(EmitContext context)
        {
            return ((PEModuleBuilder)context.Module).Translate(this.ModifierSymbol, (CSharpSyntaxNode)context.SyntaxNode, context.Diagnostics, keepExtension: true);
        }
    }
}
