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
        private const int UninitializedArgumentExpressionParameterIndex = int.MinValue;
        private int _lazyCallerArgumentExpressionParameterIndex = UninitializedArgumentExpressionParameterIndex;


        internal SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(SourceParameterSymbol originalParam, SourceDelegateMethodSymbol newOwner, int newOrdinal)
            : base(originalParam, newOwner, newOrdinal, suppressOptional: true)
        {
        }

        internal override bool IsCallerFilePath => _originalParam.IsCallerFilePath;

        internal override bool IsCallerLineNumber => _originalParam.IsCallerLineNumber;

        internal override bool IsCallerMemberName => _originalParam.IsCallerMemberName;

        internal override int CallerArgumentExpressionParameterIndex
        {
            get
            {
                if (_originalParam.CallerArgumentExpressionParameterIndex == -1)
                {
                    // If original param doesn't have a valid caller argument expression, don't try to recalculate.
                    // NOTE: Recalculation may result in different behavior if the delegate is declared like:
                    // delegate void D(string s1, [CallerArgumentExpression("callback")] [Optional] string s2);
                    // There is no parameter named "callback", however, the BeginInvoke has one.
                    return _lazyCallerArgumentExpressionParameterIndex = -1;
                }

                if (_lazyCallerArgumentExpressionParameterIndex != UninitializedArgumentExpressionParameterIndex)
                {
                    return _lazyCallerArgumentExpressionParameterIndex;
                }

                var parameterName = _originalParam.ContainingSymbol.GetParameters()[_originalParam.CallerArgumentExpressionParameterIndex].Name;
                var parameters = ((SourceDelegateMethodSymbol)ContainingSymbol).Parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name.Equals(parameterName, StringComparison.Ordinal))
                    {
                        return _lazyCallerArgumentExpressionParameterIndex = i;
                    }
                }

                return _lazyCallerArgumentExpressionParameterIndex = -1;
            }
        }

        internal override ParameterSymbol WithCustomModifiersAndParams(TypeSymbol newType, ImmutableArray<CustomModifier> newCustomModifiers, ImmutableArray<CustomModifier> newRefCustomModifiers, bool newIsParams)
        {
            return new SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(
                _originalParam.WithCustomModifiersAndParamsCore(newType, newCustomModifiers, newRefCustomModifiers, newIsParams),
                (SourceDelegateMethodSymbol)ContainingSymbol,
                Ordinal);
        }
    }
}
