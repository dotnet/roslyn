// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The nullable state of an rvalue computed in <see cref="NullableWalker"/>.
    /// When in doubt we conservatively use <see cref="NullableFlowState.NotNull"/>
    /// to minimize diagnostics.
    /// </summary>
    internal enum NullableFlowState : byte
    {
        /// <summary>
        /// Not null.
        /// </summary>
        NotNull = 0b00,

        /// <summary>
        /// Maybe null (type is nullable).
        /// </summary>
        MaybeNull = 0b01,

        /// <summary>
        /// Maybe null (type may be not nullable).
        /// </summary>
        MaybeDefault = 0b11,
    }
}
