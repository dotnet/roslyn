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
        /// The scope of the expression that contained the interpolated string during initial binding. This is used to determine the SafeToEscape rules
        /// for the builder during lowering.
        /// </summary>
        public readonly uint ScopeOfContainingExpression;
        /// <summary>
        /// The placeholders that are used for <see cref="Construction"/>.
        /// </summary>
        public readonly ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> ArgumentPlaceholders;

        public bool HasTrailingHandlerValidityParameter => ArgumentPlaceholders.Length > 0 && ArgumentPlaceholders[^1].ArgumentIndex == BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter;

        public InterpolatedStringHandlerData(TypeSymbol builderType, BoundExpression construction, bool usesBoolReturns, uint scopeOfContainingExpression, ImmutableArray<BoundInterpolatedStringArgumentPlaceholder> placeholders)
        {
            Debug.Assert(construction is BoundObjectCreationExpression or BoundDynamicObjectCreationExpression or BoundBadExpression);
            Debug.Assert(!placeholders.IsDefault);
            // Only the last placeholder should be the out parameter.
            Debug.Assert(placeholders.IsEmpty || placeholders.AsSpan()[..^1].All(item => item.ArgumentIndex != BoundInterpolatedStringArgumentPlaceholder.TrailingConstructorValidityParameter));
            BuilderType = builderType;
            Construction = construction;
            UsesBoolReturns = usesBoolReturns;
            ScopeOfContainingExpression = scopeOfContainingExpression;
            ArgumentPlaceholders = placeholders;
        }

        /// <summary>
        /// Simple helper method to get the object creation expression for this data. This should only be used in
        /// scenarios where the data in <see cref="Construction"/> is known to be valid, or it will throw.
        /// </summary>
        public readonly BoundObjectCreationExpression GetValidConstructor()
            => (BoundObjectCreationExpression)Construction;
    }
}
