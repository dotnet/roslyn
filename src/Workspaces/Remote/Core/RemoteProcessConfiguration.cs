// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Remote
{
    [Flags]
    internal enum RemoteProcessConfiguration
    {
        /// <summary>
        /// Remote host runs on .NET 6+.
        /// </summary>
        Core = 1,

        /// <summary>
        /// Remote host uses server GC.
        /// </summary>
        ServerGC = 1 << 1,

        /// <summary>
        /// Enable solution crawler in remote workspace.
        /// </summary>
        EnableSolutionCrawler = 1 << 2
    }
}
