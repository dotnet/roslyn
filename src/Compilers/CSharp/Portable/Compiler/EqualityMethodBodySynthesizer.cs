// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal static class EqualityMethodBodySynthesizer
    {
        public static BoundExpression GenerateEqualsComparisons<TList>(
            NamedTypeSymbol containingType,
            BoundExpression otherAccess,
            TList componentProperties,
            SyntheticBoundNodeFactory F) where TList : IReadOnlyList<FieldSymbol>
        {
            //  Expression:
            //
            //      System.Collections.Generic.EqualityComparer<T_1>.Default.Equals(this.backingFld_1, value.backingFld_1)
            //      ...
            //      && System.Collections.Generic.EqualityComparer<T_N>.Default.Equals(this.backingFld_N, value.backingFld_N);

            //  prepare symbols
            var equalityComparer_get_Default = F.WellKnownMethod(
                WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            var equalityComparer_Equals = F.WellKnownMethod(
                WellKnownMember.System_Collections_Generic_EqualityComparer_T__Equals);

            NamedTypeSymbol equalityComparerType = equalityComparer_Equals.ContainingType;

            BoundExpression? retExpression = null;

            // Compare fields
            foreach (var field in componentProperties)
            {
                // Prepare constructed symbols
                var constructedEqualityComparer = equalityComparerType.Construct(field.Type);

                // System.Collections.Generic.EqualityComparer<T_index>.
                //   Default.Equals(this.backingFld_index, local.backingFld_index)'
                BoundExpression nextEquals = F.Call(
                    F.StaticCall(constructedEqualityComparer,
                                 equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                    equalityComparer_Equals.AsMember(constructedEqualityComparer),
                    F.Field(F.This(), field),
                    F.Field(otherAccess, field));

                // Generate 'retExpression' = 'retExpression && nextEquals'
                retExpression = retExpression is null
                    ? nextEquals
                    : F.LogicalAnd(retExpression, nextEquals);
            }

            RoslynDebug.AssertNotNull(retExpression);

            return retExpression;
        }
    }
}