// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Container for a <see cref="SemaphoreSlim"/> factory.
    /// </summary>
    internal static class SemaphoreSlimFactory
    {
        /// <summary>
        /// Factory object that may be used for lazy initialization. Creates AsyncSemaphore instances with an initial count of 1.
        /// </summary>
        public static readonly Func<SemaphoreSlim> Instance = () => new SemaphoreSlim(initialCount: 1);
    }
}
