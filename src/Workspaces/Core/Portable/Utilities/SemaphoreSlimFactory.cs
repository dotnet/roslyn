// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
