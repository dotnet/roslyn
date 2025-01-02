// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated synthesized method symbol
    /// representing string switch hash function
    /// </summary>
    internal sealed partial class SynthesizedStringSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedStringSwitchHashMethod(SynthesizedPrivateImplementationDetailsType privateImplType, TypeSymbol returnType, TypeSymbol paramType)
            : base(privateImplType, returnType, PrivateImplementationDetails.SynthesizedStringHashFunctionName)
        {
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(paramType), 0, RefKind.None, "s")));
        }
    }

    internal sealed partial class SynthesizedSpanSwitchHashMethod : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedSpanSwitchHashMethod(SynthesizedPrivateImplementationDetailsType privateImplType, TypeSymbol returnType, TypeSymbol paramType, bool isReadOnlySpan)
            : base(privateImplType, returnType, isReadOnlySpan ? PrivateImplementationDetails.SynthesizedReadOnlySpanHashFunctionName : PrivateImplementationDetails.SynthesizedSpanHashFunctionName)
        {
            _isReadOnlySpan = isReadOnlySpan;
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(paramType), 0, RefKind.None, "s")));
        }

        private readonly bool _isReadOnlySpan;
    }
}
