// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// <see cref="VSDiagnosticRank"/> represents the rank of a <see cref="VSDiagnostic"/> object.
    /// </summary>
    internal enum VSDiagnosticRank
    {
        /// <summary>
        /// Highest priority.
        /// </summary>
        Highest = 100,

        /// <summary>
        /// High priority.
        /// </summary>
        High = 200,

        /// <summary>
        /// Default priority.
        /// </summary>
        Default = 300,

        /// <summary>
        /// Low priority.
        /// </summary>
        Low = 400,

        /// <summary>
        /// Lowest priority.
        /// </summary>
        Lowest = 500,
    }
}
