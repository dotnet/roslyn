// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Emit;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    partial class CSharpCustomModifier : Microsoft.Cci.ICustomModifier
    {
        bool Microsoft.Cci.ICustomModifier.IsOptional
        {
            get { return this.IsOptional; }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.ICustomModifier.GetModifier(Microsoft.CodeAnalysis.Emit.Context context)
        {
            Debug.Assert(this.Modifier.IsDefinition);
            return ((PEModuleBuilder)context.Module).Translate(this.Modifier, (CSharpSyntaxNode)context.SyntaxNodeOpt, context.Diagnostics);
        }
    }
}
