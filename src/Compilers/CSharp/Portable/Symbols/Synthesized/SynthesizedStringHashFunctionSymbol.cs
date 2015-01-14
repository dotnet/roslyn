// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated synthesized method symbol
    /// representing string switch hash function
    /// </summary>
    internal sealed partial class SynthesizedStringSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedStringSwitchHashMethod(SourceModuleSymbol containingModule, PrivateImplementationDetails privateImplType, TypeSymbol returnType, TypeSymbol paramType)
            : base(containingModule, privateImplType, returnType, PrivateImplementationDetails.SynthesizedStringHashFunctionName)
        {
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(new SynthesizedParameterSymbol(this, paramType, 0, RefKind.None, "s")));
        }
    }
}
