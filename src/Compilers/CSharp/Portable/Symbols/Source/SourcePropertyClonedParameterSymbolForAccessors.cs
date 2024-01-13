// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourcePropertyClonedParameterSymbolForAccessors : SourceClonedParameterSymbol
    {
        internal SourcePropertyClonedParameterSymbolForAccessors(SourceParameterSymbol originalParam, Symbol newOwner)
            : base(originalParam, newOwner, originalParam.Ordinal, suppressOptional: false)
        {
        }

        internal override bool IsCallerFilePath => _originalParam.IsCallerFilePath;

        internal override bool IsCallerLineNumber => _originalParam.IsCallerLineNumber;

        internal override bool IsCallerMemberName => _originalParam.IsCallerMemberName;

        internal override int CallerArgumentExpressionParameterIndex => _originalParam.CallerArgumentExpressionParameterIndex;

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            return new SourcePropertyClonedParameterSymbolForAccessors(
                _originalParam.WithCustomModifiersAndParamsCore(newType, newCustomModifiers, newRefCustomModifiers, newIsParams),
                this.ContainingSymbol);
        }
    }
}
