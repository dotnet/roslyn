﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal ConversionGroup(Conversion conversion, TypeSymbolWithAnnotations explicitType = default)
        {
            Conversion = conversion;
            ExplicitType = explicitType;
        }

        /// <summary>
        /// True if the conversion is an explicit conversion.
        /// </summary>
        internal bool IsExplicitConversion => !ExplicitType.IsNull;

        /// <summary>
        /// The conversion (from Conversions.ClassifyConversionFromExpression for
        /// instance) from which all BoundConversions in the group were created.
        /// </summary>
        internal readonly Conversion Conversion;

        /// <summary>
        /// The target type of the conversion specified explicitly in source,
        /// or null if not an explicit conversion.
        /// </summary>
        internal readonly TypeSymbolWithAnnotations ExplicitType;

#if DEBUG
        private static int _nextId;
        private readonly int _id = _nextId++;

        internal string GetDebuggerDisplay()
        {
            var str = $"#{_id} {Conversion}";
            if (!ExplicitType.IsNull)
            {
                str += $" ({ExplicitType})";
            }
            return str;
        }
#endif
    }
}
