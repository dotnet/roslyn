// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    [Flags]
    internal enum FindReferencesCascadeDirection
    {
        /// <summary>
        /// Cascade up the inheritance hierarchy.
        /// </summary>
        Up = 1,

        /// <summary>
        /// Cascade down the inheritance hierarchy.
        /// </summary>
        Down = 2,

        /// <summary>
        /// Cascade in both directions.
        /// </summary>
        UpAndDown = Up | Down,
    }

    internal static class FindReferencesCascadeDirectionExtensions
    {
        public static bool HasFlag(this FindReferencesCascadeDirection value, FindReferencesCascadeDirection flag)
            => (value & flag) == flag;
    }
}
