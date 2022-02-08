// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static class YieldAwaitableExtensions
    {
        /// <summary>
        /// Implements <c>ConfigureAwait(bool)</c> for <see cref="Task.Yield"/>. The resulting behavior in asynchronous code
        /// is the same as one would expect for <see cref="Task.ConfigureAwait(bool)"/>.
        /// </summary>
        /// <param name="awaitable">The awaitable provided by <see cref="Task.Yield"/>.</param>
        /// <param name="continueOnCapturedContext"><inheritdoc cref="Task.ConfigureAwait(bool)"/></param>
        /// <returns>An object used to await this yield.</returns>
        public static ConfiguredYieldAwaitable ConfigureAwait(this YieldAwaitable awaitable, bool continueOnCapturedContext)
        {
            return new ConfiguredYieldAwaitable(awaitable, continueOnCapturedContext);
        }
    }
}
