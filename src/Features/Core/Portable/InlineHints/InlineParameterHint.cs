// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal readonly struct InlineParameterHint
    {
        public readonly IParameterSymbol? Parameter;
        public readonly int Position;
        public readonly InlineParameterHintKind Kind;

        public InlineParameterHint(
            IParameterSymbol? parameter,
            int position,
            InlineParameterHintKind kind)
        {
            Parameter = parameter;
            Position = position;
            Kind = kind;
        }
    }
}
