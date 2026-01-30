// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A group is a common instance referenced by all BoundConversion instances
    /// generated from a single Conversion. The group is used by NullableWalker to
    /// determine which BoundConversion nodes should be considered as a unit.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class ConversionGroup
    {
        internal ConversionGroup(Conversion conversion, TypeWithAnnotations explicitType = default)
        {
            Conversion = conversion;
            ExplicitType = explicitType;
        }

        /// <summary>
        /// True if the conversion is an explicit conversion.
        /// </summary>
        internal bool IsExplicitConversion => ExplicitType.HasType;

        /// <summary>
        /// The conversion (from Conversions.ClassifyConversionFromExpression for
        /// instance) from which all BoundConversions in the group were created.
        /// </summary>
        internal readonly Conversion Conversion;

        /// <summary>
        /// The target type of the conversion specified explicitly in source,
        /// or null if not an explicit conversion.
        /// </summary>
        internal readonly TypeWithAnnotations ExplicitType;

#if DEBUG
        private static int _nextId;
        private readonly int _id = _nextId++;

        internal string GetDebuggerDisplay()
        {
            var str = $"#{_id} {Conversion}";
            if (ExplicitType.HasType)
            {
                str += $" ({ExplicitType})";
            }
            return str;
        }
#endif
    }

    /// <summary>
    /// Flags assigned to individual conversion nodes associated with
    /// the same <see cref="ConversionGroup"/> instance.
    /// </summary>
    [Flags]
    internal enum InConversionGroupFlags : ushort
    {
        Unspecified = 0,
        LoweredFormOfUserDefinedConversionForExpressionTree = 1 << 0,
        TupleLiteral = 1 << 1,
        TupleLiteralExplicitIdentity = 1 << 2,
        FunctionTypeDelegate = 1 << 3,
        FunctionTypeDelegateToTarget = 1 << 4,
        UserDefinedFromConversion = 1 << 5,
        UserDefinedFromConversionAdjustment = 1 << 6,
        UserDefinedOperator = 1 << 7,
        UserDefinedReturnTypeAdjustment = 1 << 8,
        UserDefinedFinal = 1 << 9,
        UserDefinedErroneous = 1 << 10,
        TupleBinaryOperatorPendingLowering = 1 << 11,
    }
}
