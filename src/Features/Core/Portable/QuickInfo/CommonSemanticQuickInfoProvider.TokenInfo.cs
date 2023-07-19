// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract partial class CommonSemanticQuickInfoProvider
    {
        public readonly struct TokenInformation(ImmutableArray<ISymbol> symbols, bool showAwaitReturn = false, NullableFlowState nullableFlowState = NullableFlowState.None)
        {
            public readonly ImmutableArray<ISymbol> Symbols = symbols;

            /// <summary>
            /// True if this quick info came from hovering over an 'await' keyword, which we show the return
            /// type of with special text.
            /// </summary>
            public readonly bool ShowAwaitReturn = showAwaitReturn;

            /// <summary>
            /// The nullable flow state to show in Quick Info; will be <see cref="NullableFlowState.None"/> to show nothing.
            /// </summary>
            public readonly NullableFlowState NullableFlowState = nullableFlowState;
        }
    }
}
