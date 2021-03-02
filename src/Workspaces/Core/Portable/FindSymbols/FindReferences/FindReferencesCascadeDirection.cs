// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal enum FindReferencesCascadeDirection
    {
        /// <summary>
        /// Cascade to all symbols no matter what.  <see cref="All"/> is not the same as <c>Up | Down</c>.  <see
        /// cref="All"/> implies that cascading should go in both directions for every symbol hit.  <c>Up | Down</c>
        /// means going both up and down from the initial symbol, but then any cascaded symbols should stick with <see
        /// cref="Up"/> or <see cref="Down"/> depending on the initial cascading direction.
        /// </summary>
        All = 0,

        /// <summary>
        /// Cascade up the inheritance hierarchy only.
        /// </summary>
        Up = 1,

        /// <summary>
        /// Cascade down the inheritance hierarchy only.
        /// </summary>
        Down = 2,
    }
}
