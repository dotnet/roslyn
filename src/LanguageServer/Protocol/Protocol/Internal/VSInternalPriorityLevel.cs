// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Enum which represents the various reference kinds.
    /// </summary>
    internal enum VSInternalPriorityLevel
    {
        /// <summary>
        /// Lowest priority.
        /// </summary>
        Lowest = 0,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium priority.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// High priority.
        /// </summary>
        High = 3,
    }
}
