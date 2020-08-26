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
        /// Indicates the reference needs to be updated.
        /// </summary>
        Update,

        /// <summary>
        /// Indicates the reference should be removed from the project.
        /// </summary>
        Remove,

        /// <summary>
        /// Indicates the reference should be added to the project.
        /// </summary>
        Add,
    }
}
