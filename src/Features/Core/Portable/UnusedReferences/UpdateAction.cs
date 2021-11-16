// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal enum UpdateAction
    {
        /// <summary>
        /// No action needs to be performed.
        /// </summary>
        None,

        /// <summary>
        /// Indicates the reference should be marked as used.
        /// </summary>
        TreatAsUsed,

        /// <summary>
        /// Indicates the reference should be marked as unused
        /// </summary>
        TreatAsUnused,

        /// <summary>
        /// Indicates the reference should be removed from the project.
        /// </summary>
        Remove,
    }
}
