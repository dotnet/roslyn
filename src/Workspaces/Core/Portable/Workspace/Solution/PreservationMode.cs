// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The mode in which value is preserved.
    /// </summary>
    public enum PreservationMode
    {
        /// <summary>
        /// The value is guaranteed to have the same contents across multiple accesses.
        /// </summary>
        PreserveValue = 0,

        /// <summary>
        /// The value is guaranteed to the same instance across multiple accesses.
        /// </summary>
        PreserveIdentity = 1
    }

    internal static class PreservationModeExtensions
    {
        public static bool IsValid(this PreservationMode mode)
            => mode is >= PreservationMode.PreserveValue and <= PreservationMode.PreserveIdentity;
    }
}
