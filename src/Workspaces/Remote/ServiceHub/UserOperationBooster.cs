// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Boost performance of any servicehub service which is invoked by user explicit actions
    /// </summary>
    internal struct UserOperationBooster : IDisposable
    {
        private static int s_count = 0;
        private static readonly object s_gate = new object();

        /// <summary>
        /// Used to distinguish the default instance from one created by <see cref="Boost()"/>.
        /// </summary>
        private readonly bool _isBoosted;

        private UserOperationBooster(bool isBoosted)
        {
            _isBoosted = isBoosted;
        }

        public static UserOperationBooster Boost()
        {
            lock (s_gate)
            {
                s_count++;

                if (s_count == 1)
                {
                    // boost to normal priority
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                }

                return new UserOperationBooster(isBoosted: true);
            }
        }

        public void Dispose()
        {
            if (!_isBoosted)
            {
                // Avoid decrementing if default(UserOperationBooster).Dispose() is called.
                return;
            }

            lock (s_gate)
            {
                s_count--;

                if (s_count == 0)
                {
                    // when boost is done, set process back to below normal priority
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                }
            }
        }
    }
}
