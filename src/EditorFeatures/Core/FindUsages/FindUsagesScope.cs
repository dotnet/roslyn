// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal enum FindUsagesScope
    {
        /// <summary>
        /// Use default per operation
        /// </summary>
        Default,
        /// <summary>
        /// Scope results to the active repository
        /// </summary>
        Repository,
        /// <summary>
        /// Scope results to indexes from the same organization
        /// </summary>
        Organization,
        /// <summary>
        /// Scope results to all global public indexes (may not be applicable for all operations)
        /// </summary>
        Global,
    }
}
