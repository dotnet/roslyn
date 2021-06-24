// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceDelegateClonedParameterSymbolForBeginAndEndInvoke : SourceClonedParameterSymbol
    {
        internal SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(SourceParameterSymbol originalParam, Symbol newOwner, int newOrdinal)
            : base(originalParam, newOwner, newOrdinal, suppressOptional: true)
        {
        }

        internal override bool IsCallerFilePath => _originalParam.IsCallerFilePath;

        internal override bool IsCallerLineNumber => _originalParam.IsCallerLineNumber;

        internal override bool IsCallerMemberName => _originalParam.IsCallerMemberName;

        internal override int CallerArgumentExpressionParameterIndex => throw ExceptionUtilities.Unreachable;

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            return new SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(
                _originalParam.WithCustomModifiersAndParamsCore(newType, newCustomModifiers, newRefCustomModifiers, newIsParams),
                ContainingSymbol,
                Ordinal);
        }
    }
}
