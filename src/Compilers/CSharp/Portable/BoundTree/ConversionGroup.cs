// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
}
