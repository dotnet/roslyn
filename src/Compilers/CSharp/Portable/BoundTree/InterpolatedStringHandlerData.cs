// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct InterpolatedStringHandlerData
    {
        public readonly TypeSymbol BuilderType;
        public readonly BoundExpression Construction;
        public readonly bool UsesBoolReturns;
        /// <summary>
        /// The placeholders that are used for <see cref="Construction"/>.
        /// </summary>
        public readonly ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> ArgumentPlaceholders;

        public readonly ImmutableArray<ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>> PositionInfo;

        public bool HasTrailingHandlerValidityParameter => ArgumentPlaceholders.Length > 0 && ArgumentPlaceholders[^1].ArgumentIndex == BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter;

        public readonly BoundInterpolatedStringHandlerPlaceholder ReceiverPlaceholder;

        public bool IsDefault => Construction is null;

        public InterpolatedStringHandlerData(
            TypeSymbol builderType,
            BoundExpression construction,
            bool usesBoolReturns,
            ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> placeholders,
            ImmutableArray<ImmutableArray<(bool IsLiteral, bool HasAlignment, bool HasFormat)>> positionInfo,
            BoundInterpolatedStringHandlerPlaceholder receiverPlaceholder)
        {
            Debug.Assert(construction is BoundObjectCreationExpression or BoundDynamicObjectCreationExpression or BoundBadExpression);
            Debug.Assert(!placeholders.IsDefault);
            // Only the last placeholder may be the out parameter.
            Debug.Assert(placeholders.IsEmpty || placeholders.AsSpan()[..^1].All(item => item.ArgumentIndex != BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter));
            Debug.Assert(!positionInfo.IsDefault);
            BuilderType = builderType;
            Construction = construction;
            UsesBoolReturns = usesBoolReturns;
            ArgumentPlaceholders = placeholders;
            PositionInfo = positionInfo;
            ReceiverPlaceholder = receiverPlaceholder;
        }

        /// <summary>
        /// Simple helper method to get the object creation expression for this data. This should only be used in
        /// scenarios where the data in <see cref="Construction"/> is known to be valid, or it will throw.
        /// </summary>
        public readonly BoundObjectCreationExpression GetValidConstructor()
            => (BoundObjectCreationExpression)Construction;
    }
}
