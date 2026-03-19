// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceDelegateClonedParameterSymbolForBeginAndEndInvoke : SourceClonedParameterSymbol
    {
        internal SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(SourceParameterSymbol originalParam, SourceDelegateMethodSymbol newOwner, int newOrdinal)
            : base(originalParam, newOwner, newOrdinal, suppressOptional: true)
        {
        }

        internal override bool IsCallerFilePath => _originalParam.IsCallerFilePath;

        internal override bool IsCallerLineNumber => _originalParam.IsCallerLineNumber;

        internal override bool IsCallerMemberName => _originalParam.IsCallerMemberName;

        // We don't currently support caller argument expression for cloned begin/end invoke since
        // they throw PlatformNotSupportedException at runtime and we feel it's unnecessary to support them.
        internal override int CallerArgumentExpressionParameterIndex => -1;

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            return new SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(
                _originalParam.WithCustomModifiersAndParamsCore(newType, newCustomModifiers, newRefCustomModifiers, newIsParams),
                (SourceDelegateMethodSymbol)ContainingSymbol,
                Ordinal);
        }
    }
}
